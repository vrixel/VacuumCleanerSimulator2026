using System.Collections.Generic;
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
            p.Texture = new RenderTexture(1024, 1024, 24);
            p.Texture.name = "VacuumPreview";

            // Studio: a light grey cyclorama (floor curving up into a back wall, seen from inside), a pale podium.
            var cyc = new List<Vector2>();
            cyc.Add(new Vector2(0f, -0.001f)); cyc.Add(new Vector2(3.0f, -0.001f));
            for (int i = 1; i <= 8; i++)
            {
                float a = i / 8f * Mathf.PI * 0.5f;
                cyc.Add(new Vector2(3.0f + 2.5f * Mathf.Sin(a), 2.5f - 2.5f * Mathf.Cos(a)));
            }
            cyc.Add(new Vector2(5.5f, 6f));
            var cycMesh = MeshKit.Revolve(cyc, 48, "Cyclorama", false);
            var tris = cycMesh.triangles; System.Array.Reverse(tris); cycMesh.triangles = tris;
            var norms = cycMesh.normals; for (int i = 0; i < norms.Length; i++) norms[i] = -norms[i]; cycMesh.normals = norms;
            MeshKit.Part(p.stage, cycMesh, Palette.Mat(new Color(0.80f, 0.80f, 0.82f), 0f, 0.15f), Vector3.zero, Quaternion.identity, Vector3.one, "Cyclorama");
            var podium = MeshKit.Part(p.stage, MeshKit.Revolve(new[] { new Vector2(0f, 0f), new Vector2(0.72f, 0f), new Vector2(0.72f, 0f), new Vector2(0.74f, 0.02f), new Vector2(0.74f, 0.04f), new Vector2(0f, 0.04f) }, 48, "Podium"),
                Palette.Mat(new Color(0.70f, 0.70f, 0.73f), 0.1f, 0.5f), Vector3.zero, Quaternion.identity, Vector3.one, "Podium");
            podium.transform.localPosition = new Vector3(0f, -0.04f, 0f);

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(p.stage, false);
            p.cam = camGo.AddComponent<Camera>();
            p.cam.targetTexture = p.Texture;
            p.cam.clearFlags = CameraClearFlags.SolidColor;
            p.cam.backgroundColor = new Color(0.80f, 0.80f, 0.82f);
            p.cam.nearClipPlane = 0.05f;
            p.cam.farClipPlane = 12f;
            p.cam.fieldOfView = 40f;
            p.cam.cullingMask &= ~(1 << 8);
            p.cam.enabled = false;
            VCS.Core.RenderingSetup.Attach(p.cam);

            // Three-point studio lighting: a soft key spot with shadows, a cool fill, a rim from behind.
            var keyGo = new GameObject("PreviewKey");
            keyGo.transform.SetParent(p.stage, false);
            keyGo.transform.localPosition = new Vector3(1.6f, 2.6f, 2.0f);
            keyGo.transform.LookAt(p.stage.position + Vector3.up * 0.3f);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Spot;
            key.spotAngle = 70f;
            key.innerSpotAngle = 40f;
            key.range = 9f;
            key.intensity = 3.2f;
            key.color = new Color(1f, 0.96f, 0.9f);
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.8f;

            var fillGo = new GameObject("PreviewFill");
            fillGo.transform.SetParent(p.stage, false);
            fillGo.transform.localPosition = new Vector3(-2.2f, 1.4f, 1.6f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 8f;
            fill.intensity = 0.9f;
            fill.color = new Color(0.85f, 0.9f, 1f);

            var rimGo = new GameObject("PreviewRim");
            rimGo.transform.SetParent(p.stage, false);
            rimGo.transform.localPosition = new Vector3(-1.0f, 1.8f, -2.2f);
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Point;
            rim.range = 7f;
            rim.intensity = 1.4f;
            rim.color = new Color(0.9f, 0.95f, 1f);
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
            VacuumDetails.Add(model, s);
            if (!VacuumVisuals.RealisticLook) VacuumVisuals.AddEyes(model, s, out _, out _);
            Frame();
            cam.enabled = true;
        }

        public void Hide() { cam.enabled = false; }

        /// <summary>Renders one model at a fixed yaw into a PNG (the gallery); leaves the stage empty afterwards.</summary>
        public void RenderStill(VacuumSpec s, float yawDeg, int size, string path)
        {
            if (model != null) Destroy(model.gameObject);
            model = new GameObject("StillModel").transform;
            model.SetParent(stage, false);
            model.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            s.Build(model, s);
            VacuumDetails.Add(model, s);
            if (!VacuumVisuals.RealisticLook) VacuumVisuals.AddEyes(model, s, out _, out _);
            Frame();
            var rt = new RenderTexture(size, size, 24);
            rt.antiAliasing = 8;
            var saved = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = saved;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
            Destroy(rt);
            DestroyImmediate(model.gameObject);
            model = null;
        }

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
            // the model spins around the stage axis: take the farthest horizontal point from that axis as the radius
            float cx = stage.position.x, cz = stage.position.z;
            float horiz = Mathf.Max(Mathf.Abs(b.max.x - cx), Mathf.Abs(b.min.x - cx), Mathf.Abs(b.max.z - cz), Mathf.Abs(b.min.z - cz));
            float radius = Mathf.Sqrt(horiz * horiz + b.extents.y * b.extents.y);
            Vector3 look = new Vector3(cx, b.center.y, cz);
            float dist = radius / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.9f + 0.15f;
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
