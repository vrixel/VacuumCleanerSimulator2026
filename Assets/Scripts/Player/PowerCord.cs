using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VCS.Core;
using VCS.World;

namespace VCS.Player
{
    /// <summary>A wall socket. Corded vacuums plug into the nearest one; the cord anchors just in front of it.</summary>
    public class WallSocket : MonoBehaviour
    {
        public int Index;
        public Vector3 CordAnchor => transform.position + transform.forward * 0.03f;
    }

    /// <summary>
    /// The electric cord of a corded vacuum. It lies where you drove (a breadcrumb trail from the socket to the
    /// vacuum), it has a real length limit that yanks you back, and pressing rewind reels the whole thing into
    /// the body with the plug whipping across the floor. Unplugged means no power, no suction: find a socket.
    /// </summary>
    public class PowerCord : MonoBehaviour
    {
        // Rope length, not distance driven (see Tighten). The hall socket reaches the rooms around it; the far
        // corners and the garden need another socket, or the plug comes out of the wall.
        public static float MaxLength = 22f;   // static, not const: the smoke test shortens it to reach the end
        public const float RewindSpeed = 22f;

        public float Length { get; private set; }
        public bool Plugged { get; private set; }
        public bool Rewinding { get; private set; }
        public bool Taut { get; private set; }
        public WallSocket Socket { get; private set; }
        public float TotalRewound { get; private set; }
        /// <summary>The last point the cord is caught on before the vacuum (the socket when it is straight).</summary>
        public Vector3 LastCorner => trail.Count >= 2 ? trail[trail.Count - 2] : (Socket != null ? Socket.CordAnchor : vac.transform.position);

        VacuumController vac;
        LineRenderer line;
        Transform plug;
        readonly List<Vector3> trail = new List<Vector3>();
        readonly HashSet<int> usedSockets = new HashSet<int>();
        Vector3[] lineBuffer = new Vector3[64];
        float ratchetTimer;
        bool tautReported;
        float strain;

        public static PowerCord Attach(VacuumController vac, WallSocket initial)
        {
            var go = new GameObject("PowerCord " + vac.Spec.Id);
            var c = go.AddComponent<PowerCord>();
            c.vac = vac;
            c.line = go.AddComponent<LineRenderer>();
            c.line.useWorldSpace = true;
            c.line.widthMultiplier = 0.035f;
            c.line.sharedMaterial = Palette.Mat(new Color(0.12f, 0.12f, 0.13f), 0f, 0.15f);   // no tangents on a LineRenderer: flat rubber
            c.line.numCornerVertices = 4;
            c.line.numCapVertices = 3;
            c.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            c.line.receiveShadows = false;
            c.line.positionCount = 0;

            var plugGo = new GameObject("Plug");
            plugGo.transform.SetParent(go.transform, false);
            PropFactory.Prim(PrimitiveType.Cube, plugGo.transform, Vector3.zero, new Vector3(0.1f, 0.07f, 0.06f), Palette.Black, "PlugBody", false);
            PropFactory.Prim(PrimitiveType.Cylinder, plugGo.transform, new Vector3(-0.022f, 0f, 0.045f), new Vector3(0.012f, 0.02f, 0.012f), Palette.Gray, "Pin", false, Quaternion.Euler(90f, 0f, 0f));
            PropFactory.Prim(PrimitiveType.Cylinder, plugGo.transform, new Vector3(0.022f, 0f, 0.045f), new Vector3(0.012f, 0.02f, 0.012f), Palette.Gray, "Pin", false, Quaternion.Euler(90f, 0f, 0f));
            c.plug = plugGo.transform;
            c.plug.gameObject.SetActive(false);

            if (initial != null) c.PlugInto(initial);
            return c;
        }

        void OnDestroy()
        {
            StopAllCoroutines();
        }

        Vector3 Foot()
        {
            Vector3 p = vac.transform.position - vac.transform.forward * 0.35f;
            p.y = 0.04f;
            return p;
        }

        public void PlugInto(WallSocket s)
        {
            Socket = s;
            Plugged = true;
            Taut = false;
            tautReported = false;
            trail.Clear();
            trail.Add(s.CordAnchor);
            trail.Add(Foot());
            plug.gameObject.SetActive(true);
            plug.position = s.CordAnchor;
            plug.rotation = Quaternion.LookRotation(-s.transform.forward, Vector3.up);
            Redraw();
            var gm = GameManager.I;
            if (gm != null)
            {
                gm.Audio.PlayClick();
                if (usedSockets.Add(s.Index)) gm.Objectives.Report("plug");
            }
        }

        public void Rewind()
        {
            if (!Plugged || Rewinding) return;
            StartCoroutine(RewindRoutine(false));
        }

        void FixedUpdate()
        {
            if (vac == null) { Destroy(gameObject); return; }
            var gm = GameManager.I;
            bool active = gm != null && gm.State == GameState.Playing;
            if (Rewinding) return;

            if (!Plugged)
            {
                if (active && gm.Level != null)
                {
                    var s = gm.Level.NearestSocket(vac.transform.position, 1.4f);
                    if (s != null) PlugInto(s);
                }
                return;
            }

            Vector3 foot = Foot();
            if (trail.Count < 2) trail.Add(foot);
            else
            {
                Vector3 lastFixed = trail[trail.Count - 2];
                if ((foot - lastFixed).magnitude > 0.35f) trail.Add(foot);
                else trail[trail.Count - 1] = foot;
            }

            Tighten();
            Length = PathLength();
            Taut = Length > MaxLength;

            if (Taut && active)
            {
                // A leash, not a spring: no velocity away from the last corner, the overshoot is taken back, and
                // you can still slide sideways along the arc. Keep pulling and the plug comes out of the wall.
                Vector3 corner = trail[trail.Count - 2];
                Vector3 outward = vac.transform.position - corner;
                outward.y = 0f;
                if (outward.sqrMagnitude > 0.001f)
                {
                    Vector3 n = outward.normalized;
                    Vector3 v = vac.Rb.linearVelocity;
                    float along = Vector3.Dot(v, n);
                    if (along > 0f) vac.Rb.linearVelocity = v - n * along;
                    float over = Length - MaxLength;
                    if (over > 0.02f) vac.Rb.position -= n * Mathf.Min(over, 0.12f);
                    float pull = Vector3.Dot(vac.MoveDir, n);
                    strain = pull > 0.4f ? strain + Time.fixedDeltaTime : Mathf.Max(0f, strain - Time.fixedDeltaTime * 2f);
                    if (strain > 0.9f)
                    {
                        YankPlug();
                        return;
                    }
                }
                if (!tautReported)
                {
                    tautReported = true;
                    gm.Objectives.Report("taut");
                    gm.ShowBanner("END OF THE CORD", "All " + MaxLength.ToString("0") + " metres are out. Keep pulling and the plug comes out; R / Y rewinds", 3f);
                    gm.Audio.PlayBoing();
                    vac.Visuals.Punch(0.3f);
                }
            }
            else strain = Mathf.Max(0f, strain - Time.fixedDeltaTime * 2f);
            Redraw();
        }

        /// <summary>The player kept pulling on a taut cord: the plug pops out of the wall and the cord reels in.</summary>
        void YankPlug()
        {
            strain = 0f;
            var gm = GameManager.I;
            if (gm != null)
            {
                gm.Audio.PlayThunk();
                gm.Fx.Puff(trail[0] + Vector3.up * 0.3f, Color.white, 10);
                gm.ShowBanner("PLUG YANKED OUT", "You pulled the plug out of the wall. No power: drive to a socket to plug back in", 3f);
                gm.Objectives.Report("yank");
                gm.AddScore(50, false);
                vac.Visuals.Punch(0.5f);
                Vector3 kick = vac.transform.position - trail[trail.Count - 2];
                kick.y = 0f;
                if (kick.sqrMagnitude > 0.001f) vac.Rb.AddForce(kick.normalized * 3f + Vector3.up * 2f, ForceMode.VelocityChange);
            }
            StartCoroutine(RewindRoutine(true));
        }

        IEnumerator RewindRoutine(bool yanked)
        {
            Rewinding = true;
            Plugged = false;
            Socket = null;
            var gm = GameManager.I;
            float rewound = 0f;
            while (trail.Count > 1)
            {
                float step = RewindSpeed * Time.deltaTime;
                while (step > 0f && trail.Count > 1)
                {
                    Vector3 a = trail[0], b = trail[1];
                    float seg = (b - a).magnitude;
                    if (seg <= step) { step -= seg; rewound += seg; trail.RemoveAt(0); }
                    else { trail[0] = a + (b - a) / seg * step; rewound += step; step = 0f; }
                }
                if (trail.Count > 1) trail[trail.Count - 1] = Foot();
                if (trail.Count > 0)
                {
                    plug.position = trail[0] + Vector3.up * 0.05f;
                    if (trail.Count > 1)
                    {
                        Vector3 dir = trail[1] - trail[0];
                        if (dir.sqrMagnitude > 0.0001f) plug.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    }
                }
                ratchetTimer -= Time.deltaTime;
                if (ratchetTimer <= 0f)
                {
                    ratchetTimer = 0.06f;
                    if (gm != null) gm.Audio.PlayRatchet();
                    KnockDebris(plug.position);
                }
                Redraw();
                yield return null;
            }
            TotalRewound += rewound;
            trail.Clear();
            Redraw();
            plug.gameObject.SetActive(false);
            Length = 0f;
            Taut = false;
            Rewinding = false;
            if (gm != null)
            {
                gm.Audio.PlayThunk();
                gm.Fx.Puff(vac.transform.position + Vector3.up * 0.5f, new Color(0.8f, 0.8f, 0.8f), 14);
                gm.Objectives.Report("rewind", Mathf.RoundToInt(rewound));
                gm.AddScore(Mathf.RoundToInt(rewound * 3f), false);
                if (!yanked) gm.ShowBanner("CORD REWOUND", rewound.ToString("0.0") + " m reeled in. No power now: drive to a socket to plug back in", 2.5f);
                vac.Visuals.Punch(0.45f);
            }
        }

        // The cord is a rope, not a footprint. The part being dragged straightens behind the vacuum, and once
        // most of it is paid out the whole thing pulls tight around whatever corners it is caught on. Before this
        // the trail only ever grew, so the "length" was the distance driven: 45 m of driving in circles left the
        // cord at full length anywhere in the house and every step got yanked back (the zigzag a tester filmed).
        const float CordHeight = 0.18f;
        const int DragWindow = 14;              // points behind the vacuum that drag straight while there is slack
        readonly RaycastHit[] hits = new RaycastHit[24];
        int sweepIndex = 1;

        float PathLength()
        {
            float len = 0f;
            for (int i = 1; i < trail.Count; i++) len += (trail[i] - trail[i - 1]).magnitude;
            return len;
        }

        void Tighten()
        {
            if (trail.Count < 3) return;
            bool pulled = PathLength() > MaxLength * 0.8f;
            int budget = pulled ? 40 : 12;
            int floor = pulled ? 1 : Mathf.Max(1, trail.Count - DragWindow);
            // From the vacuum end backwards: a point goes when the straight line around it is clear.
            for (int i = trail.Count - 2; i >= floor && budget > 0; i--, budget--)
            {
                if (i + 1 >= trail.Count || i < 1) continue;
                if (Clear(trail[i - 1], trail[i + 1])) trail.RemoveAt(i);
            }
            // One round-robin check per step further back, so a cord caught on furniture settles after the
            // furniture is knocked away.
            if (trail.Count >= 3)
            {
                if (sweepIndex >= trail.Count - 1) sweepIndex = 1;
                if (Clear(trail[sweepIndex - 1], trail[sweepIndex + 1])) trail.RemoveAt(sweepIndex);
                else sweepIndex++;
            }
        }

        /// <summary>True when nothing that can hold a cord (walls, furniture, anything heavy) lies between a and b.</summary>
        bool Clear(Vector3 a, Vector3 b)
        {
            a.y = CordHeight;
            b.y = CordHeight;
            Vector3 d = b - a;
            float dist = d.magnitude;
            if (dist < 0.01f) return true;
            int n = Physics.RaycastNonAlloc(a, d / dist, hits, dist, ~(1 << 8), QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++)
            {
                var c = hits[k].collider;
                if (c == null) continue;
                var rb = c.attachedRigidbody;
                if (rb != null && (rb == vac.Rb || rb.mass < 5f)) continue;   // the vacuum and light debris slide under it
                return false;
            }
            return n < hits.Length;   // a buffer that overflowed may have dropped a wall: keep the corner
        }

        void KnockDebris(Vector3 p)
        {
            var cols = Physics.OverlapSphere(p, 0.55f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                var rb = c.attachedRigidbody;
                if (rb == null || rb == vac.Rb || rb.mass > 5f) continue;
                Vector3 away = rb.worldCenterOfMass - p;
                away.y = 0f;
                rb.AddForce(away.normalized * 2.5f + Vector3.up * 2f, ForceMode.VelocityChange);
            }
        }

        void Redraw()
        {
            if (lineBuffer.Length < trail.Count) lineBuffer = new Vector3[Mathf.Max(trail.Count, lineBuffer.Length * 2)];
            for (int i = 0; i < trail.Count; i++) lineBuffer[i] = trail[i];
            line.positionCount = trail.Count;
            if (trail.Count > 0) line.SetPositions(new System.ArraySegment<Vector3>(lineBuffer, 0, trail.Count).ToArray());
        }
    }
}
