using UnityEngine;
using VCS.Core;

namespace VCS.CameraRig
{
    /// <summary>Third-person chase camera with mouse / right-stick orbit, plus a slow orbit mode for the title screen.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public Camera Cam { get; private set; }

        Transform target;
        bool orbit;
        Vector3 orbitCenter;
        float orbitRadius, orbitHeight, orbitAngle;
        float yaw, pitch = 42f, distance = 9f, shake;
        Vector3 vel;

        public static FollowCamera Create()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            go.AddComponent<AudioListener>();
            var fc = go.AddComponent<FollowCamera>();
            fc.Cam = cam;
            return fc;
        }

        public void SetFollow(Transform t)
        {
            target = t;
            orbit = false;
            yaw = 0f;
            pitch = 42f;
            vel = Vector3.zero;
            transform.position = Desired();
            transform.LookAt(t.position + Vector3.up * 0.6f);
        }

        public void SetOrbit(Vector3 center, float radius, float height)
        {
            orbit = true;
            target = null;
            orbitCenter = center;
            orbitRadius = radius;
            orbitHeight = height;
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        Vector3 Desired()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            return target.position + Vector3.up * 0.6f + rot * new Vector3(0f, 0f, -distance);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (orbit)
            {
                orbitAngle += dt * 5f;
                float a = orbitAngle * Mathf.Deg2Rad;
                var p = orbitCenter + new Vector3(Mathf.Cos(a) * orbitRadius, orbitHeight, Mathf.Sin(a) * orbitRadius);
                transform.position = Vector3.Lerp(transform.position, p, 1f - Mathf.Exp(-dt * 2.5f));
                transform.LookAt(orbitCenter);
                Cam.fieldOfView = 55f;
                return;
            }
            if (target == null) return;

            var gm = GameManager.I;
            bool playing = gm != null && gm.State == GameState.Playing;
            if (playing)
            {
                var stick = GameInput.LookStick;
                var mouse = GameInput.LookMouse;
                yaw += stick.x * 160f * Time.deltaTime + mouse.x * 2.5f;
                pitch = Mathf.Clamp(pitch - stick.y * 100f * Time.deltaTime - mouse.y * 2f, 22f, 70f);
            }

            float wantedFov = playing && gm.Player != null && gm.Player.Turbo ? 72f : 60f;
            Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, wantedFov, 1f - Mathf.Exp(-dt * 4f));

            Vector3 desired = Desired();
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, 0.10f, Mathf.Infinity, dt);
            if (shake > 0f)
            {
                transform.position += Random.insideUnitSphere * shake * 0.35f;
                shake = Mathf.Max(0f, shake - dt * 1.5f);
            }
            transform.LookAt(target.position + Vector3.up * 0.6f);
        }
    }
}
