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
    /// The electric cord of a corded vacuum, simulated as a real cable: a chain of points (Verlet integration,
    /// distance constraints, gravity, floor friction) pinned to the socket at one end and to the reel outlet on the
    /// back of the vacuum at the other. The reel pays cord out as you pull, slack lies on the floor in loops, and
    /// only walls stop it (furniture and mess are ignored, the cord slides over them). At the end of the cord it
    /// holds you like a leash; keep pulling and the plug pops out. Rewind (R / Y, automatic on Spotless and after a
    /// yank) reels the cord in at the vacuum, and the loose end whips across the floor, knocking light mess.
    /// Unplugged means no power, no suction: drive within reach of a socket to plug back in.
    /// </summary>
    public class PowerCord : MonoBehaviour
    {
        // Cable length on the reel, in metres. Static, not const: the smoke test shortens it to reach the end.
        public static float MaxLength = 22f;
        public const float RewindSpeed = 22f;

        const float SegLen = 0.22f;
        const int Iterations = 6;
        const float Radius = 0.03f;
        const float FloorY = 0.03f;
        const int MaxCount = 240;

        /// <summary>Cord paid out from the reel, in metres.</summary>
        public float Length { get; private set; }
        public bool Plugged { get; private set; }
        public bool Rewinding { get; private set; }
        public bool Taut { get; private set; }
        public WallSocket Socket { get; private set; }
        public float TotalRewound { get; private set; }
        /// <summary>The cord point just before the vacuum (what the leash pulls toward).</summary>
        public Vector3 LastCorner => count >= 2 ? pos[count - 2] : (Socket != null ? Socket.CordAnchor : vac.transform.position);

        VacuumController vac;
        LineRenderer line;
        Transform plug;
        SphereCollider probe;
        readonly Vector3[] pos = new Vector3[MaxCount];
        readonly Vector3[] prev = new Vector3[MaxCount];
        readonly Vector3[] lineBuffer = new Vector3[MaxCount];
        readonly Collider[] hits = new Collider[8];
        readonly HashSet<int> usedSockets = new HashSet<int>();
        int count;
        bool pinPlug;
        float ratchetTimer;
        bool tautReported;
        float strain;
        float reelAcc;
        float tautTimer;

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

            // A probe sphere for wall penetration tests; parked far below the world, never simulated.
            var probeGo = new GameObject("CordProbe");
            probeGo.transform.SetParent(go.transform, false);
            probeGo.transform.position = new Vector3(0f, -900f, 0f);
            c.probe = probeGo.AddComponent<SphereCollider>();
            c.probe.radius = Radius;
            c.probe.isTrigger = true;

            if (initial != null) c.PlugInto(initial);
            return c;
        }

        void OnDestroy()
        {
            StopAllCoroutines();
        }

        /// <summary>The reel outlet on the back of the vacuum.</summary>
        Vector3 Anchor() => vac.transform.TransformPoint(new Vector3(0f, 0.30f, -0.30f));

        public void PlugInto(WallSocket s)
        {
            Socket = s;
            Plugged = true;
            Rewinding = false;
            Taut = false;
            tautReported = false;
            strain = 0f;
            pinPlug = true;
            Vector3 a = s.CordAnchor, b = Anchor();
            float d = Vector3.Distance(a, b);
            count = Mathf.Clamp(Mathf.CeilToInt(d / SegLen) + 1, 2, MaxCount);
            for (int i = 0; i < count; i++) pos[i] = prev[i] = Vector3.Lerp(a, b, i / (float)(count - 1));
            Length = (count - 1) * SegLen;
            plug.gameObject.SetActive(true);
            plug.position = a;
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

            if (!Plugged && !Rewinding)
            {
                if (active && gm.Level != null)
                {
                    var s = gm.Level.NearestSocket(vac.transform.position, 1.4f);
                    if (s != null) PlugInto(s);
                }
                return;
            }

            Simulate(Time.fixedDeltaTime);
            if (Plugged)
            {
                PayOut();
                Leash(gm, active);
            }
            Redraw();
        }

        // Position-based cable: integrate, then satisfy the segment lengths, the floor and the walls a few times.
        void Simulate(float dt)
        {
            if (count < 2) return;
            float g = 9.81f * dt * dt;
            for (int i = 0; i < count; i++)
            {
                if (IsPinned(i)) continue;
                Vector3 p = pos[i];
                Vector3 v = (p - prev[i]) * 0.96f;
                if (p.y <= FloorY + 0.002f) { v.x *= 0.55f; v.z *= 0.55f; }   // lying on the floor: heavy friction
                prev[i] = p;
                pos[i] = p + v + Vector3.down * g;
            }
            if (pinPlug && Socket != null) pos[0] = prev[0] = Socket.CordAnchor;
            pos[count - 1] = prev[count - 1] = Anchor();

            for (int it = 0; it < Iterations; it++)
            {
                // from the vacuum toward the socket: the stretch of a tight chain ends up on the socket segment
                for (int i = count - 2; i >= 0; i--)
                {
                    Vector3 d = pos[i + 1] - pos[i];
                    float len = d.magnitude;
                    if (len < 1e-5f) continue;
                    float diff = (len - SegLen) / len;
                    bool pa = IsPinned(i), pb = IsPinned(i + 1);
                    if (pa && pb) continue;
                    if (pa) pos[i + 1] -= d * diff;
                    else if (pb) pos[i] += d * diff;
                    else { pos[i] += d * diff * 0.5f; pos[i + 1] -= d * diff * 0.5f; }
                }
                for (int i = 0; i < count; i++)
                    if (!IsPinned(i) && pos[i].y < FloorY) pos[i].y = FloorY;
            }

            // Walls only: static colliders taller than a rug. Furniture, mess, the cat and the vacuum all have
            // rigidbodies and are ignored, so the cord slides over them instead of snagging.
            for (int i = 1; i < count - 1; i++)
            {
                int n = Physics.OverlapSphereNonAlloc(pos[i], Radius, hits, ~(1 << 8), QueryTriggerInteraction.Ignore);
                for (int k = 0; k < n; k++)
                {
                    var c = hits[k];
                    if (c == null || c.attachedRigidbody != null || c.bounds.size.y < 0.3f) continue;
                    if (Physics.ComputePenetration(probe, pos[i], Quaternion.identity, c, c.transform.position, c.transform.rotation, out Vector3 dir, out float dist))
                        pos[i] += dir * (dist + 0.002f);
                }
                if (pos[i].y < FloorY) pos[i].y = FloorY;
            }
        }

        bool IsPinned(int i) => (i == 0 && pinPlug) || i == count - 1;

        /// <summary>Tension in the cable: how much longer the whole chain is than its rest length. The solver
        /// spreads the stretch of a tight cable along every segment, so the sum is the honest measure.</summary>
        float Tension()
        {
            float total = 0f;
            for (int i = 0; i < count - 1; i++) total += Vector3.Distance(pos[i], pos[i + 1]);
            return total - (count - 1) * SegLen;
        }

        // The reel lets cord out while the cable is under tension, until the whole cable is out.
        void PayOut()
        {
            float t = Tension();
            // The reel has a brake: it only gives when the cable really pulls, one segment per step.
            if (t > SegLen * 0.7f && Length < MaxLength - 0.001f && count < MaxCount)
            {
                Vector3 a = pos[count - 1];
                Vector3 f = pos[count - 2];
                pos[count] = prev[count] = a;
                pos[count - 1] = prev[count - 1] = Vector3.Lerp(f, a, 0.5f);
                count++;
                Length += SegLen;
                t -= SegLen;
            }
            bool atEnd = Length >= MaxLength - 0.001f;
            Taut = atEnd && t > SegLen * 0.25f;
            if (Taut) tautTimer = 0.4f; else tautTimer -= Time.fixedDeltaTime;
        }

        // A leash, not a spring: no velocity away from the last cord point, the overshoot is taken back, and you
        // can still slide sideways. Keep pulling and the plug comes out of the wall.
        void Leash(GameManager gm, bool active)
        {
            bool atEnd = Length >= MaxLength - 0.001f;
            if (!(atEnd && active && tautTimer > 0f)) { strain = Mathf.Max(0f, strain - Time.fixedDeltaTime * 2f); return; }
            Vector3 corner = pos[count - 2];
            Vector3 outward = vac.transform.position - corner;
            outward.y = 0f;
            if (outward.sqrMagnitude > 0.001f)
            {
                Vector3 n = outward.normalized;
                if (Taut)
                {
                    Vector3 v = vac.Rb.linearVelocity;
                    float along = Vector3.Dot(v, n);
                    if (along > 0f) vac.Rb.linearVelocity = v - n * along;
                    float over = Tension();
                    if (over > 0.02f) vac.Rb.position -= n * Mathf.Min(over, 0.12f);
                }
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

        /// <summary>The player kept pulling on a taut cord: the plug pops out of the wall and the cord reels in.</summary>
        void YankPlug()
        {
            strain = 0f;
            Debug.Log("[VCS] Cord: plug yanked out with " + Length.ToString("0.0") + " m paid out");
            var gm = GameManager.I;
            if (gm != null)
            {
                gm.Audio.PlayThunk();
                gm.Fx.Puff(pos[0] + Vector3.up * 0.3f, Color.white, 10);
                gm.ShowBanner("PLUG YANKED OUT", "You pulled the plug out of the wall. No power: drive to a socket to plug back in", 3f);
                gm.Objectives.Report("yank");
                gm.AddScore(50, false);
                vac.Visuals.Punch(0.5f);
                Vector3 kick = vac.transform.position - pos[count - 2];
                kick.y = 0f;
                if (kick.sqrMagnitude > 0.001f) vac.Rb.AddForce(kick.normalized * 3f + Vector3.up * 2f, ForceMode.VelocityChange);
            }
            StartCoroutine(RewindRoutine(true));
        }

        // The reel pulls the cable in at the vacuum; the free end and the loops on the floor follow, fast.
        IEnumerator RewindRoutine(bool yanked)
        {
            Rewinding = true;
            Plugged = false;
            Socket = null;
            Taut = false;
            pinPlug = false;
            var gm = GameManager.I;
            float rewound = 0f;
            reelAcc = 0f;
            if (gm != null) gm.Audio.SetRewind(true);
            // the plug jumps out of the socket
            if (count >= 2) prev[0] = pos[0] - (pos[1] - pos[0]).normalized * 0.05f - Vector3.up * 0.04f;
            while (count > 2)
            {
                reelAcc += RewindSpeed * Time.deltaTime;
                while (reelAcc >= SegLen && count > 2)
                {
                    // consume the point next to the reel; the anchor stays last
                    pos[count - 2] = pos[count - 1];
                    prev[count - 2] = prev[count - 1];
                    count--;
                    reelAcc -= SegLen;
                    rewound += SegLen;
                    Length = Mathf.Max(0f, Length - SegLen);
                }
                plug.position = pos[0] + Vector3.up * 0.02f;
                if (count > 1)
                {
                    Vector3 dir = pos[1] - pos[0];
                    if (dir.sqrMagnitude > 0.0001f) plug.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
                ratchetTimer -= Time.deltaTime;
                if (ratchetTimer <= 0f)
                {
                    ratchetTimer = 0.06f;
                    if (gm != null && !gm.Audio.HasReelLoop) gm.Audio.PlayRatchet();   // synthesised teeth only without the generated reel loop
                    KnockDebris(pos[0]);
                    KnockDebris(pos[Mathf.Max(0, count / 2)]);
                }
                yield return null;
            }
            TotalRewound += rewound;
            count = 0;
            Redraw();
            plug.gameObject.SetActive(false);
            Length = 0f;
            Taut = false;
            Rewinding = false;
            if (gm != null)
            {
                gm.Audio.SetRewind(false);
                if (!gm.Audio.HasReelLoop) gm.Audio.PlayThunk();
                gm.Fx.Puff(vac.transform.position + Vector3.up * 0.5f, new Color(0.8f, 0.8f, 0.8f), 14);
                gm.Objectives.Report("rewind", Mathf.RoundToInt(rewound));
                gm.AddScore(Mathf.RoundToInt(rewound * 3f), false);
                if (!yanked) gm.ShowBanner("CORD REWOUND", rewound.ToString("0.0") + " m reeled in. No power now: drive to a socket to plug back in", 2.5f);
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
            for (int i = 0; i < count; i++) lineBuffer[i] = pos[i];
            line.positionCount = count;
            if (count > 0) line.SetPositions(new System.ArraySegment<Vector3>(lineBuffer, 0, count).ToArray());
        }
    }
}
