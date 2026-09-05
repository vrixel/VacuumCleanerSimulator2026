using UnityEngine;
using VCS.Player;
using VCS.World;

namespace VCS.UI
{
    /// <summary>
    /// Turntable for the garage: a hidden stage far below the world, its own camera rendering into a RenderTexture
    /// that the title screen shows in a RawImage.
    /// </summary>
    public class VacuumPreview : MonoBehaviour
    {
        public RenderTexture Texture { get; private set; }

        Camera cam;
        Transform stage;
        Transform model;
        VacuumSpec spec;
        float angle;

        public static VacuumPreview Create()
        {
            var go = new GameObject("PreviewStage");
            go.transform.position = new Vector3(0f, -500f, 0f);
            var p = go.AddComponent<VacuumPreview>();
            p.stage = go.transform;
            p.Texture = new RenderTexture(640, 640, 16);
            p.Texture.name = "VacuumPreview";

            var podium = MeshKit.Part(p.stage, MeshKit.Revolve(new[] { new Vector2(0f, -0.04f), new Vector2(0.7f, -0.04f), new Vector2(0.7f, -0.04f), new Vector2(0.72f, -0.02f), new Vector2(0.72f, 0f), new Vector2(0f, 0f) }, 48, "Podium"),
                Palette.Glossy(new Color(0.18f, 0.18f, 0.24f)), Vector3.zero, Quaternion.identity, Vector3.one, "Podium");
            podium.transform.localPosition = Vector3.zero;

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(p.stage, false);
            p.cam = camGo.AddComponent<Camera>();
            p.cam.targetTexture = p.Texture;
            p.cam.clearFlags = CameraClearFlags.SolidColor;
            p.cam.backgroundColor = new Color(0.10f, 0.10f, 0.16f);
            p.cam.nearClipPlane = 0.05f;
            p.cam.farClipPlane = 12f;
            p.cam.fieldOfView = 36f;
            p.cam.enabled = false;

            var fillGo = new GameObject("PreviewFill");
            fillGo.transform.SetParent(p.stage, false);
            fillGo.transform.localPosition = new Vector3(1.5f, 2f, 1.8f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 7f;
            fill.intensity = 1.6f;
            fill.color = new Color(1f, 0.97f, 0.9f);

            var rimGo = new GameObject("PreviewRim");
            rimGo.transform.SetParent(p.stage, false);
            rimGo.transform.localPosition = new Vector3(-1.5f, 1.2f, -1.5f);
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Point;
            rim.range = 6f;
            rim.intensity = 0.9f;
            rim.color = new Color(0.7f, 0.8f, 1f);
            return p;
        }

        public void Show(VacuumSpec s)
        {
            spec = s;
            if (model != null) Destroy(model.gameObject);
            model = new GameObject("PreviewModel").transform;
            model.SetParent(stage, false);
            model.localRotation = Quaternion.Euler(0f, angle, 0f);
            s.Build(model, s);
            VacuumVisuals.AddEyes(model, s, out _, out _);
            Frame();
            cam.enabled = true;
        }

        public void Hide() { cam.enabled = false; }

        // Frames the union of the model's renderer bounds so every silhouette fills the picture the same way.
        void Frame()
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(stage.position + Vector3.up * 0.4f, Vector3.one * 0.8f);
            bool first = true;
            foreach (var r in renderers)
            {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            // the model spins around its own Y axis: use the horizontal footprint as a radius so nothing pops out
            float horiz = Mathf.Max(b.extents.x, b.extents.z);
            float radius = Mathf.Sqrt(horiz * horiz * 2f + b.extents.y * b.extents.y);
            Vector3 look = new Vector3(stage.position.x, b.center.y, stage.position.z);
            float dist = radius / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.72f + 0.15f;
            cam.transform.position = look + new Vector3(0f, dist * 0.34f, dist);
            cam.transform.LookAt(look);
        }

        void LateUpdate()
        {
            if (!cam.enabled || model == null) return;
            angle += Time.unscaledDeltaTime * 40f;
            model.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }
}
