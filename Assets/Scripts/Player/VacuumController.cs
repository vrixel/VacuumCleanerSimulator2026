using UnityEngine;
using VCS.Core;

namespace VCS.Player
{
    /// <summary>
    /// Physics-driven vacuum: camera-relative driving on a sphere collider, hop, turbo, spin and speed tracking.
    /// </summary>
    public class VacuumController : MonoBehaviour
    {
        public const float TurboMult = 1.7f;

        public VacuumSpec Spec { get; private set; }
        public PowerCord Cord { get; private set; }
        /// <summary>Cordless vacuums are always powered; corded ones only while plugged in.</summary>
        public bool Powered => Cord == null || Cord.Plugged;
        public Rigidbody Rb { get; private set; }
        public SuctionSystem Suction { get; private set; }
        public VacuumVisuals Visuals { get; private set; }
        public Transform Nozzle { get; private set; }
        public float Speed { get; private set; }
        public bool Turbo { get; private set; }
        ParticleSystem[] boostTrail;

        /// <summary>For the smoke log: what the boost trail is doing.</summary>
        public string BoostDebug()
        {
            if (boostTrail == null) return "trail missing";
            var d = boostTrail[0];
            return "turbo " + Turbo + ", grounded " + Grounded + ", dust rate " + d.emission.rateOverTime.constant.ToString("0") + " x" + d.emission.rateOverTimeMultiplier.ToString("0.0")
                   + ", alive " + d.particleCount + "/" + boostTrail[1].particleCount + ", playing " + d.isPlaying + ", emitting " + d.isEmitting
                   + ", pos " + d.transform.position.ToString("F1") + ", mat " + (d.GetComponent<ParticleSystemRenderer>().sharedMaterial != null ? d.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name : "none");
        }
        public bool Grounded { get; private set; }
        public Vector3 MoveDir { get; private set; }

        bool hopQueued;
        float yawPrev;
        float spinAccum;
        float spinWindow;
        bool speedReported;

        public static VacuumController Create(Vector3 pos, VacuumSpec spec, WallSocket socket)
        {
            var go = new GameObject("Vacuum " + spec.Id);
            go.transform.position = pos;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = spec.Mass;
            rb.linearDamping = 1.2f;
            rb.angularDamping = 5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.5f;
            col.center = new Vector3(0f, 0.5f, 0f);
            var mat = new PhysicsMaterial("Vacuum");
            mat.dynamicFriction = 0.15f;
            mat.staticFriction = 0.15f;
            mat.bounciness = 0.05f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            col.material = mat;

            var vc = go.AddComponent<VacuumController>();
            vc.Spec = spec;
            vc.Rb = rb;
            vc.yawPrev = 0f;
            var nozzle = new GameObject("Nozzle").transform;
            nozzle.SetParent(go.transform, false);
            nozzle.localPosition = spec.NozzleLocal;
            vc.Nozzle = nozzle;
            vc.Visuals = VacuumVisuals.Build(go.transform, vc, spec);
            vc.Suction = go.AddComponent<SuctionSystem>();
            vc.Suction.Init(vc);
            var gmFx = GameManager.I;
            if (gmFx != null && gmFx.Fx != null) vc.boostTrail = gmFx.Fx.CreateBoostTrail(go.transform);
            if (!spec.Cordless) vc.Cord = PowerCord.Attach(vc, socket);
            return vc;
        }

        void OnDestroy()
        {
            if (Cord != null) Destroy(Cord.gameObject);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm != null && gm.State == GameState.Playing && GameInput.HopDown) hopQueued = true;
        }

        void FixedUpdate()
        {
            var gm = GameManager.I;
            bool active = gm != null && gm.State == GameState.Playing;
            Vector2 input = active ? GameInput.Move : Vector2.zero;
            bool wasTurbo = Turbo;
            Turbo = active && GameInput.Turbo && input.sqrMagnitude > 0.01f && Powered;
            if (Turbo != wasTurbo)
            {
                // the boost is felt: spool-up sound, a camera kick and a squash on ignition; spool-down on release
                if (gm != null)
                {
                    gm.Audio.SetTurbo(Turbo);
                    if (Turbo) { gm.Cam.Shake(0.22f); Visuals.Punch(0.3f); }
                }
            }
            if (boostTrail != null)
            {
                // on a constant curve the multiplier is the rate itself: dust at 70 per second, sparks at 45
                bool on = Turbo && Grounded;
                for (int i = 0; i < boostTrail.Length; i++) { var e = boostTrail[i].emission; e.rateOverTimeMultiplier = on ? (i == 0 ? 110f : 60f) : 0f; }
            }
            Visuals.SetTurbo(Turbo);

            var cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 dir = fwd * input.y + right * input.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            MoveDir = dir;

            Grounded = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 0.62f, ~0, QueryTriggerInteraction.Ignore);

            if (dir.sqrMagnitude > 0.001f)
            {
                Rb.AddForce(dir * Spec.Accel * (Grounded ? 1f : 0.4f), ForceMode.Acceleration);
                var targetRot = Quaternion.LookRotation(dir, Vector3.up);
                Rb.MoveRotation(Quaternion.RotateTowards(Rb.rotation, targetRot, Spec.Turn * Time.fixedDeltaTime));
            }

            float maxSpeed = Spec.Speed * (Turbo ? TurboMult : 1f);
            Vector3 v = Rb.linearVelocity;
            Vector3 hvel = new Vector3(v.x, 0f, v.z);
            if (hvel.magnitude > maxSpeed)
            {
                Vector3 clamped = hvel.normalized * maxSpeed;
                Rb.linearVelocity = new Vector3(clamped.x, v.y, clamped.z);
                hvel = clamped;
            }

            if (hopQueued)
            {
                hopQueued = false;
                if (Grounded)
                {
                    Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, Spec.Hop, Rb.linearVelocity.z);
                    if (gm != null)
                    {
                        gm.Audio.PlayBoing();
                        gm.Fx.Puff(transform.position, new Color(0.85f, 0.85f, 0.85f), 8);
                    }
                    Visuals.Punch(0.2f);
                }
            }

            Speed = hvel.magnitude;
            if (active && !speedReported && Speed > Spec.Speed * TurboMult * 0.93f)
            {
                speedReported = true;
                gm.Objectives.Report("speed");
            }

            float yaw = Rb.rotation.eulerAngles.y;
            spinAccum += Mathf.Abs(Mathf.DeltaAngle(yawPrev, yaw));
            yawPrev = yaw;
            spinWindow += Time.fixedDeltaTime;
            if (spinWindow >= 2f)
            {
                if (active && spinAccum >= 1080f) gm.Objectives.Report("spin");
                spinAccum = 0f;
                spinWindow = 0f;
            }
        }

        public void OnPowerUp(int level)
        {
            Visuals.SetPowerScale(1f + (level - 1) * 0.08f);
            Visuals.Punch(0.5f);
        }
    }
}
