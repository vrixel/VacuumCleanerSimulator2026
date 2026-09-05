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
        public const float MaxLength = 18f;
        public const float RewindSpeed = 15f;

        public float Length { get; private set; }
        public bool Plugged { get; private set; }
        public bool Rewinding { get; private set; }
        public bool Taut { get; private set; }
        public WallSocket Socket { get; private set; }
        public float TotalRewound { get; private set; }

        VacuumController vac;
        LineRenderer line;
        Transform plug;
        readonly List<Vector3> trail = new List<Vector3>();
        readonly HashSet<int> usedSockets = new HashSet<int>();
        Vector3[] lineBuffer = new Vector3[64];
        float ratchetTimer;
        bool tautReported;

        public static PowerCord Attach(VacuumController vac, WallSocket initial)
        {
            var go = new GameObject("PowerCord " + vac.Spec.Id);
            var c = go.AddComponent<PowerCord>();
            c.vac = vac;
            c.line = go.AddComponent<LineRenderer>();
            c.line.useWorldSpace = true;
            c.line.widthMultiplier = 0.035f;
            c.line.sharedMaterial = Palette.Rubber(new Color(0.12f, 0.12f, 0.13f));
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
            StartCoroutine(RewindRoutine());
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

            float len = 0f;
            for (int i = 1; i < trail.Count; i++) len += (trail[i] - trail[i - 1]).magnitude;
            Length = len;
            Taut = Length > MaxLength;

            if (Taut && active)
            {
                float over = Length - MaxLength;
                Vector3 back = trail[trail.Count - 2] - vac.transform.position;
                back.y = 0f;
                if (back.sqrMagnitude > 0.001f)
                    vac.Rb.AddForce(back.normalized * (14f + over * 30f), ForceMode.Acceleration);
                if (over > 2.5f) vac.Rb.linearVelocity = Vector3.ClampMagnitude(vac.Rb.linearVelocity, 2.5f);
                if (!tautReported)
                {
                    tautReported = true;
                    gm.Objectives.Report("taut");
                    gm.ShowBanner("END OF THE CORD", "That is all " + MaxLength.ToString("0") + " metres. Press R / Y to rewind, then find another socket", 3f);
                    gm.Audio.PlayBoing();
                    vac.Visuals.Punch(0.3f);
                }
            }
            Redraw();
        }

        IEnumerator RewindRoutine()
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
                gm.ShowBanner("CORD REWOUND", rewound.ToString("0.0") + " m reeled in. No power now: drive to a socket to plug back in", 2.5f);
                vac.Visuals.Punch(0.45f);
            }
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
