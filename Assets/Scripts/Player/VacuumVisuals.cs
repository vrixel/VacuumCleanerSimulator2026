using UnityEngine;
using VCS.World;

namespace VCS.Player
{
    /// <summary>The vacuum body, built from primitives, with googly eyes that look where you drive.</summary>
    public class VacuumVisuals : MonoBehaviour
    {
        Transform group;
        Transform leftEye;
        Transform rightEye;
        VacuumController vac;
        float punch;
        float powerScale = 1f;
        float bob;

        public static VacuumVisuals Build(Transform parent, VacuumController vac)
        {
            var root = new GameObject("Visuals");
            root.transform.SetParent(parent, false);
            var v = root.AddComponent<VacuumVisuals>();
            v.vac = vac;

            var group = new GameObject("BodyGroup").transform;
            group.SetParent(root.transform, false);
            v.group = group;

            var body = new Color(0.95f, 0.38f, 0.28f);
            var dark = new Color(0.15f, 0.15f, 0.18f);
            var bag = new Color(0.92f, 0.82f, 0.55f);

            PropFactory.Prim(PrimitiveType.Cylinder, group, new Vector3(0f, 0.12f, 0f), new Vector3(0.95f, 0.12f, 0.95f), dark, "Base", false);
            PropFactory.Prim(PrimitiveType.Capsule, group, new Vector3(0f, 0.62f, -0.05f), new Vector3(0.7f, 0.42f, 0.7f), body, "Body", false);
            PropFactory.Prim(PrimitiveType.Sphere, group, new Vector3(0f, 0.8f, -0.42f), Vector3.one * 0.45f, bag, "Bag", false);
            PropFactory.Prim(PrimitiveType.Cube, group, new Vector3(0f, 0.16f, 0.66f), new Vector3(0.95f, 0.16f, 0.42f), dark, "NozzleHead", false);
            PropFactory.Prim(PrimitiveType.Cylinder, group, new Vector3(0f, 0.22f, 0.32f), new Vector3(0.14f, 0.22f, 0.14f), Palette.Gray, "Hose", false, Quaternion.Euler(90f, 0f, 0f));
            PropFactory.Prim(PrimitiveType.Cylinder, group, new Vector3(-0.42f, 0.12f, -0.2f), new Vector3(0.22f, 0.05f, 0.22f), dark, "Wheel", false, Quaternion.Euler(0f, 0f, 90f));
            PropFactory.Prim(PrimitiveType.Cylinder, group, new Vector3(0.42f, 0.12f, -0.2f), new Vector3(0.22f, 0.05f, 0.22f), dark, "Wheel", false, Quaternion.Euler(0f, 0f, 90f));
            PropFactory.Prim(PrimitiveType.Cylinder, group, new Vector3(0f, 0.95f, -0.55f), new Vector3(0.1f, 0.15f, 0.1f), Palette.Gray, "Exhaust", false, Quaternion.Euler(60f, 0f, 0f));

            v.leftEye = Eye(group, new Vector3(-0.2f, 0.98f, 0.26f));
            v.rightEye = Eye(group, new Vector3(0.2f, 0.98f, 0.26f));
            return v;
        }

        static Transform Eye(Transform parent, Vector3 pos)
        {
            var eye = PropFactory.Prim(PrimitiveType.Sphere, parent, pos, Vector3.one * 0.24f, Palette.White, "Eye", false);
            PropFactory.Prim(PrimitiveType.Sphere, eye.transform, new Vector3(0f, 0f, 0.42f), Vector3.one * 0.45f, Palette.Black, "Pupil", false);
            return eye.transform;
        }

        public void Punch(float amount) { punch = Mathf.Min(0.6f, punch + amount); }

        public void SetPowerScale(float s) { powerScale = s; }

        void Update()
        {
            float dt = Time.deltaTime;
            float speed = vac != null ? vac.Speed : 0f;
            bob += dt * (6f + speed * 1.5f);
            float bobAmp = 0.01f + 0.02f * Mathf.Clamp01(speed / 8f);

            Vector3 lv = vac != null ? transform.InverseTransformDirection(vac.Rb.linearVelocity) : Vector3.zero;
            float pitch = Mathf.Clamp(lv.z * 1.6f, -12f, 12f);
            float roll = Mathf.Clamp(-lv.x * 1.6f, -12f, 12f);
            group.localPosition = new Vector3(0f, Mathf.Sin(bob) * bobAmp, 0f);
            group.localRotation = Quaternion.Euler(pitch, 0f, roll);

            punch = Mathf.Lerp(punch, 0f, dt * 9f);
            group.localScale = new Vector3(1f + punch * 0.6f, 1f - punch * 0.35f, 1f + punch * 0.6f) * powerScale;

            Vector3 look = vac != null ? transform.InverseTransformDirection(vac.MoveDir) : Vector3.zero;
            if (look.sqrMagnitude < 0.01f) look = Vector3.forward;
            var target = Quaternion.LookRotation((look + Vector3.forward * 0.6f).normalized, Vector3.up);
            leftEye.localRotation = Quaternion.Slerp(leftEye.localRotation, target, dt * 10f);
            rightEye.localRotation = Quaternion.Slerp(rightEye.localRotation, target, dt * 10f);
        }
    }
}
