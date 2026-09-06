using UnityEngine;
using VCS.World;

namespace VCS.Player
{
    /// <summary>The vacuum body from the catalogue, with googly eyes that look where you drive, bob, tilt and squash.</summary>
    public class VacuumVisuals : MonoBehaviour
    {
        /// <summary>Realistic look (2026-09-05): machines without googly eyes, grained materials, real lighting.</summary>
        public static bool RealisticLook = true;   // static so the gallery can render both looks

        Transform group;
        Transform leftEye;
        Transform rightEye;
        VacuumController vac;
        float punch;
        float powerScale = 1f;
        float bob;
        // body extents around the pivot (the floor point under the centre), so leaning never pushes the nose or a
        // side under the floor: the group is lifted by exactly what the tilt would sink (his feedback, 2026-09-06)
        float zFront = 0.5f, zBack = 0.5f, xHalf = 0.3f;

        public static VacuumVisuals Build(Transform parent, VacuumController vac, VacuumSpec spec)
        {
            var root = new GameObject("Visuals");
            root.transform.SetParent(parent, false);
            var v = root.AddComponent<VacuumVisuals>();
            v.vac = vac;

            var group = new GameObject("BodyGroup").transform;
            group.SetParent(root.transform, false);
            v.group = group;
            spec.Build(group, spec);
            VacuumDetails.Add(group, spec);
            if (!RealisticLook) AddEyes(group, spec, out v.leftEye, out v.rightEye);
            v.MeasureExtents();
            return v;
        }

        void MeasureExtents()
        {
            var rs = group.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            // world bounds at build time; the group is unrotated, so the offsets from the pivot are local
            Vector3 p = group.position;
            zFront = Mathf.Max(0.05f, b.max.z - p.z);
            zBack = Mathf.Max(0.05f, p.z - b.min.z);
            xHalf = Mathf.Max(0.05f, Mathf.Max(b.max.x - p.x, p.x - b.min.x));
        }

        /// <summary>Two white spheres with pupils, also used by the garage preview.</summary>
        public static void AddEyes(Transform group, VacuumSpec spec, out Transform left, out Transform right)
        {
            float half = spec.EyeSpacing * 0.5f;
            left = Eye(group, spec.EyeCenter + new Vector3(-half, 0f, 0f), spec.EyeSize);
            right = Eye(group, spec.EyeCenter + new Vector3(half, 0f, 0f), spec.EyeSize);
        }

        static Transform Eye(Transform parent, Vector3 pos, float size)
        {
            var eye = PropFactory.Prim(PrimitiveType.Sphere, parent, pos, Vector3.one * size, Palette.White, "Eye", false);
            eye.GetComponent<MeshRenderer>().sharedMaterial = Palette.Glossy(Palette.White);
            var pupil = PropFactory.Prim(PrimitiveType.Sphere, eye.transform, new Vector3(0f, 0f, 0.42f), Vector3.one * 0.45f, Palette.Black, "Pupil", false);
            pupil.GetComponent<MeshRenderer>().sharedMaterial = Palette.Glossy(Palette.Black);
            return eye.transform;
        }

        public void Punch(float amount) { punch = Mathf.Min(0.6f, punch + amount); }

        public void SetPowerScale(float s) { powerScale = s; }

        bool turbo;
        public void SetTurbo(bool on) { turbo = on; }

        void Update()
        {
            float dt = Time.deltaTime;
            float speed = vac != null ? vac.Speed : 0f;
            bob += dt * (6f + speed * 1.5f);
            float bobAmp = 0.01f + 0.02f * Mathf.Clamp01(speed / 8f);

            Vector3 lv = vac != null ? transform.InverseTransformDirection(vac.Rb.linearVelocity) : Vector3.zero;
            float pitch = Mathf.Clamp(lv.z * 0.7f, -5f, 5f) + (turbo ? 2.5f : 0f);
            float roll = Mathf.Clamp(-lv.x * 0.7f, -6f, 6f);
            Vector2 shiver = turbo ? Random.insideUnitCircle * 0.012f : Vector2.zero;
            // nose-down (pitch > 0) sinks the front by sin(pitch) * zFront, nose-up sinks the back; roll sinks a side
            float lift = Mathf.Sin(Mathf.Abs(pitch) * Mathf.Deg2Rad) * (pitch > 0f ? zFront : zBack)
                       + Mathf.Sin(Mathf.Abs(roll) * Mathf.Deg2Rad) * xHalf;
            group.localPosition = new Vector3(shiver.x, Mathf.Sin(bob) * bobAmp + lift + Mathf.Abs(shiver.y), 0f);
            group.localRotation = Quaternion.Euler(pitch, 0f, roll);

            punch = Mathf.Lerp(punch, 0f, dt * 9f);
            group.localScale = new Vector3(1f + punch * 0.6f, 1f - punch * 0.35f, 1f + punch * 0.6f) * powerScale;

            Vector3 look = vac != null ? transform.InverseTransformDirection(vac.MoveDir) : Vector3.zero;
            if (look.sqrMagnitude < 0.01f) look = Vector3.forward;
            var target = Quaternion.LookRotation((look + Vector3.forward * 0.6f).normalized, Vector3.up);
            if (leftEye != null) leftEye.localRotation = Quaternion.Slerp(leftEye.localRotation, target, dt * 10f);
            if (rightEye != null) rightEye.localRotation = Quaternion.Slerp(rightEye.localRotation, target, dt * 10f);
        }
    }
}
