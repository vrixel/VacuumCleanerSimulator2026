using UnityEngine;
using VCS.Core;
using VCS.Player;

namespace VCS.World
{
    /// <summary>
    /// The house cat. Wanders and naps until the vacuum comes close, then bolts: it steers around walls and
    /// furniture with three feelers, hops when cornered, and knocks light mess about on the way. Bumping into it
    /// gets a yowl. It cannot be eaten, whatever the power level.
    /// </summary>
    public class Cat : MonoBehaviour
    {
        enum Mode { Idle, Wander, Flee }

        const float FleeRadius = 3.6f;
        const float TurboFleeRadius = 5.2f;
        const float SafeRadius = 7f;
        const float WanderSpeed = 1.3f;
        const float FleeSpeed = 8.5f;

        public static readonly Color Fur = new Color(0.82f, 0.55f, 0.28f);

        /// <summary>For the smoke log: what the cat is doing and how fast it goes.</summary>
        public string State => mode.ToString();
        public float Speed => rb != null ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude : 0f;
        static readonly Color FurDark = new Color(0.55f, 0.34f, 0.16f);
        static readonly Color FurLight = new Color(0.93f, 0.85f, 0.72f);

        Rigidbody rb;
        Transform body, head, tail1, tail2, tail3, earL, earR;
        Transform[] legs = new Transform[4];
        Mode mode = Mode.Idle;
        Vector3 target;
        float modeTimer;
        float fleeTimer;
        float gait;
        float scaredCooldown;
        float hopCooldown;
        readonly RaycastHit[] hits = new RaycastHit[16];
        LevelBuilder level;

        public static Cat Spawn(LevelBuilder level, Transform parent, Vector3 pos)
        {
            var go = new GameObject("Cat");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var cat = go.AddComponent<Cat>();
            cat.level = level;
            cat.BuildBody();
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 4f;
            rb.linearDamping = 1.5f;
            rb.angularDamping = 8f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var col = go.AddComponent<CapsuleCollider>();
            col.direction = 2;
            col.center = new Vector3(0f, 0.2f, 0f);
            col.radius = 0.15f;
            col.height = 0.62f;
            cat.rb = rb;
            VCS.UI.RadarView.Marker(go.transform, new Color(1f, 0.62f, 0.2f), 1.3f, 24f);
            cat.PickWanderTarget();
            cat.mode = Mode.Idle;
            cat.modeTimer = 2f;
            return cat;
        }

        void BuildBody()
        {
            var fur = Palette.Fabric(Fur);
            var dark = Palette.Fabric(FurDark);
            var light = Palette.Fabric(FurLight);
            body = Part(PrimitiveType.Capsule, new Vector3(0f, 0.2f, 0f), new Vector3(0.24f, 0.26f, 0.24f), fur, "Body", Quaternion.Euler(90f, 0f, 0f)).transform;
            Part(PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.05f), new Vector3(0.18f, 0.12f, 0.36f), light, "Belly");
            for (int i = 0; i < 3; i++)
                Part(PrimitiveType.Cube, new Vector3(0f, 0.31f, -0.12f + i * 0.12f), new Vector3(0.22f, 0.02f, 0.05f), dark, "Stripe");
            head = Part(PrimitiveType.Sphere, new Vector3(0f, 0.3f, 0.3f), Vector3.one * 0.2f, fur, "Head").transform;
            Part(PrimitiveType.Sphere, new Vector3(0f, -0.15f, 0.55f), new Vector3(0.6f, 0.45f, 0.6f), light, "Muzzle", null, head);
            Part(PrimitiveType.Sphere, new Vector3(0f, -0.08f, 0.75f), new Vector3(0.16f, 0.12f, 0.12f), Palette.Mat(new Color(0.9f, 0.5f, 0.55f), 0f, 0.6f), "Nose", null, head);
            Part(PrimitiveType.Sphere, new Vector3(-0.18f, 0.12f, 0.42f), Vector3.one * 0.14f, Palette.Mat(new Color(0.3f, 0.75f, 0.35f), 0.1f, 0.9f), "EyeL", null, head);
            Part(PrimitiveType.Sphere, new Vector3(0.18f, 0.12f, 0.42f), Vector3.one * 0.14f, Palette.Mat(new Color(0.3f, 0.75f, 0.35f), 0.1f, 0.9f), "EyeR", null, head);
            earL = Part(PrimitiveType.Cube, new Vector3(-0.3f, 0.5f, 0f), new Vector3(0.22f, 0.32f, 0.08f), fur, "EarL", Quaternion.Euler(0f, 0f, 20f), head).transform;
            earR = Part(PrimitiveType.Cube, new Vector3(0.3f, 0.5f, 0f), new Vector3(0.22f, 0.32f, 0.08f), fur, "EarR", Quaternion.Euler(0f, 0f, -20f), head).transform;
            Vector3[] legPos = { new Vector3(-0.08f, 0.1f, 0.16f), new Vector3(0.08f, 0.1f, 0.16f), new Vector3(-0.08f, 0.1f, -0.16f), new Vector3(0.08f, 0.1f, -0.16f) };
            for (int i = 0; i < 4; i++)
            {
                var pivot = new GameObject("Leg" + i).transform;
                pivot.SetParent(transform, false);
                pivot.localPosition = legPos[i] + Vector3.up * 0.08f;
                Part(PrimitiveType.Capsule, new Vector3(0f, -0.09f, 0f), new Vector3(0.07f, 0.1f, 0.07f), i < 2 ? fur : dark, "Shin", null, pivot);
                Part(PrimitiveType.Sphere, new Vector3(0f, -0.17f, 0.01f), new Vector3(0.08f, 0.05f, 0.09f), light, "Paw", null, pivot);
                legs[i] = pivot;
            }
            tail1 = Part(PrimitiveType.Capsule, new Vector3(0f, 0.24f, -0.3f), new Vector3(0.06f, 0.12f, 0.06f), fur, "Tail1", Quaternion.Euler(60f, 0f, 0f)).transform;
            tail2 = Part(PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 0.9f, 0.9f), dark, "Tail2", Quaternion.Euler(-20f, 0f, 0f), tail1).transform;
            tail3 = Part(PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.85f, 0.85f, 0.85f), fur, "Tail3", Quaternion.Euler(-25f, 0f, 0f), tail2).transform;
        }

        GameObject Part(PrimitiveType type, Vector3 pos, Vector3 scale, Material m, string name, Quaternion? rot = null, Transform parent = null)
        {
            var go = PropFactory.Prim(type, parent ?? transform, pos, scale, Color.white, name, false, rot);
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        void PickWanderTarget()
        {
            target = level != null ? level.RandomFloorPoint(1.2f) : transform.position;
        }

        VacuumController Player => GameManager.I != null ? GameManager.I.Player : null;

        void FixedUpdate()
        {
            var gm = GameManager.I;
            bool active = gm != null && gm.State == GameState.Playing;
            float dt = Time.fixedDeltaTime;
            modeTimer -= dt;
            scaredCooldown -= dt;
            hopCooldown -= dt;
            var player = Player;
            Vector3 pos = transform.position;
            Vector3 away = Vector3.zero;
            float dist = 99f;
            if (player != null)
            {
                away = pos - player.transform.position;
                away.y = 0f;
                dist = away.magnitude;
            }
            float scareRadius = player != null && player.Turbo ? TurboFleeRadius : FleeRadius;

            if (active && player != null && dist < scareRadius && mode != Mode.Flee) StartFlee(false);

            Vector3 wish = Vector3.zero;
            float speed = 0f;
            switch (mode)
            {
                case Mode.Idle:
                    if (modeTimer <= 0f) { mode = Mode.Wander; PickWanderTarget(); modeTimer = 8f; }
                    break;
                case Mode.Wander:
                    {
                        Vector3 to = target - pos; to.y = 0f;
                        if (to.magnitude < 0.5f || modeTimer <= 0f) { mode = Mode.Idle; modeTimer = 2f + Random.value * 4f; break; }
                        wish = Steer(to.normalized, 1.2f);
                        speed = WanderSpeed;
                        break;
                    }
                case Mode.Flee:
                    {
                        fleeTimer -= dt;
                        if (dist > SafeRadius && fleeTimer <= 0f) { mode = Mode.Idle; modeTimer = 1.5f; break; }
                        Vector3 dir = dist > 0.01f ? away / dist : transform.forward;
                        wish = Steer(dir, 2.2f);
                        speed = FleeSpeed;
                        // Nearly caught: a panic hop sideways gets it out from under the nozzle.
                        if (dist < 1.3f && hopCooldown <= 0f)
                        {
                            hopCooldown = 0.9f;
                            Vector3 side = Vector3.Cross(Vector3.up, dir) * (Random.value < 0.5f ? 1f : -1f);
                            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5.5f, rb.linearVelocity.z) + side * 3f;
                        }
                        if (wish == Vector3.zero && hopCooldown <= 0f)
                        {
                            hopCooldown = 1.2f;
                            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 6.5f, rb.linearVelocity.z);
                            wish = dir;
                        }
                        KnockMess();
                        break;
                    }
            }

            Vector3 v = rb.linearVelocity;
            Vector3 horiz = wish * speed;
            Vector3 cur = new Vector3(v.x, 0f, v.z);
            Vector3 next = Vector3.MoveTowards(cur, horiz, (mode == Mode.Flee ? 70f : 12f) * dt);
            rb.linearVelocity = new Vector3(next.x, v.y, next.z);
            if (next.sqrMagnitude > 0.05f)
            {
                var look = Quaternion.LookRotation(next.normalized, Vector3.up);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, look, (mode == Mode.Flee ? 900f : 360f) * dt));
            }
        }

        /// <summary>Wanted direction bent around obstacles with three feelers; zero when everything is blocked.</summary>
        Vector3 Steer(Vector3 dir, float reach)
        {
            if (Clear(dir, reach)) return dir;
            for (int i = 1; i <= 4; i++)
            {
                float a = 35f * i;
                Vector3 l = Quaternion.Euler(0f, -a, 0f) * dir, r = Quaternion.Euler(0f, a, 0f) * dir;
                bool lc = Clear(l, reach), rc = Clear(r, reach);
                if (lc && rc) return (i % 2 == 0) ? l : r;
                if (lc) return l;
                if (rc) return r;
            }
            return Vector3.zero;
        }

        bool Clear(Vector3 dir, float reach)
        {
            Vector3 from = transform.position + Vector3.up * 0.22f + dir * 0.2f;
            int n = Physics.RaycastNonAlloc(from, dir, hits, reach, ~(1 << 8), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (c == null || c.transform == transform || c.transform.IsChildOf(transform)) continue;
                var d = c.GetComponentInParent<Debris>();
                if (d != null && d.SizeClass <= 2) continue;   // small mess is run through, not around
                var p = Player;
                if (p != null && c.attachedRigidbody == p.Rb) continue;
                return false;
            }
            return true;
        }

        void KnockMess()
        {
            var cols = Physics.OverlapSphere(transform.position + Vector3.up * 0.15f, 0.45f, ~(1 << 8), QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                var r = c.attachedRigidbody;
                if (r == null || r == rb || r.mass > 0.5f) continue;
                Vector3 push = r.worldCenterOfMass - transform.position; push.y = 0.3f;
                r.AddForce(push.normalized * 1.8f, ForceMode.VelocityChange);
            }
        }

        void StartFlee(bool bumped)
        {
            bool wasCalm = mode != Mode.Flee;
            mode = Mode.Flee;
            fleeTimer = bumped ? 2.5f : 1.5f;
            if ((wasCalm || bumped) && scaredCooldown <= 0f)
            {
                scaredCooldown = bumped ? 0.8f : 3f;
                var gm = GameManager.I;
                if (gm != null) gm.OnCatScared(this, bumped);
            }
        }

        void OnCollisionEnter(Collision c)
        {
            var p = Player;
            if (p != null && c.rigidbody == p.Rb && GameManager.I != null && GameManager.I.State == GameState.Playing)
            {
                StartFlee(true);
                if (hopCooldown <= 0f)
                {
                    hopCooldown = 0.8f;
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5.5f, rb.linearVelocity.z);
                }
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
            float sp = new Vector3(v.x, 0f, v.z).magnitude;
            gait += dt * (mode == Mode.Flee ? 22f : 9f) * Mathf.Clamp01(sp / 1.5f + 0.1f);
            float amp = Mathf.Clamp01(sp / 2f) * (mode == Mode.Flee ? 45f : 25f);
            for (int i = 0; i < 4; i++)
            {
                float phase = (i == 0 || i == 3) ? 0f : Mathf.PI;
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(gait + phase) * amp, 0f, 0f);
            }
            float wag = Mathf.Sin(Time.time * (mode == Mode.Flee ? 14f : 3f));
            tail1.localRotation = Quaternion.Euler(60f + wag * 10f, wag * 25f, 0f);
            tail2.localRotation = Quaternion.Euler(-20f, wag * 30f, 0f);
            tail3.localRotation = Quaternion.Euler(-25f, wag * 35f, 0f);
            float bob = Mathf.Abs(Mathf.Sin(gait)) * Mathf.Clamp01(sp / 3f) * 0.025f;
            body.localPosition = new Vector3(0f, 0.2f + bob, 0f);

            // Head: watch the vacuum when it is near, ears back when running.
            var p = Player;
            Quaternion headTarget = Quaternion.identity;
            if (p != null)
            {
                Vector3 to = transform.InverseTransformPoint(p.transform.position + Vector3.up * 0.4f) - head.localPosition;
                if (to.magnitude < 6f && to.z > -0.5f) headTarget = Quaternion.LookRotation(to.normalized, Vector3.up);
            }
            head.localRotation = Quaternion.Slerp(head.localRotation, headTarget, dt * 6f);
            float earBack = mode == Mode.Flee ? -60f : 0f;
            earL.localRotation = Quaternion.Slerp(earL.localRotation, Quaternion.Euler(earBack, 0f, 20f), dt * 8f);
            earR.localRotation = Quaternion.Slerp(earR.localRotation, Quaternion.Euler(earBack, 0f, -20f), dt * 8f);
        }
    }
}
