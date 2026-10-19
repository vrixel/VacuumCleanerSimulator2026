using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VCS.Player;
using VCS.World;

namespace VCS.Core
{
    /// <summary>
    /// "-hotchrome &lt;dir&gt;": exploration of a hot-pink "car ad" identity rendered in the engine. A crimson burst
    /// backdrop centred on the horizon, a wet cherry floor (semi-transparent over a mirrored copy of the machine),
    /// hot-pink rim lights, speed streaks converging on the vanishing point, bloom. Branch explore/hot-chrome only.
    /// </summary>
    public class HotChromeRunner : MonoBehaviour
    {
        string outDir;
        Transform stage;
        Camera cam;
        ReflectionProbe probe;

        public static void TryStart(GameManager gm)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-hotchrome") continue;
                GameManager.SmokeMode = true;
                gm.gameObject.AddComponent<HotChromeRunner>().outDir = args[i + 1];
                return;
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outDir);
            yield return new WaitForSecondsRealtime(1.5f);
            GameManager.I.Menu.Preview.Hide();
            VacuumVisuals.RealisticLook = true;
            Palette.Realistic = true;
            VacuumModels.UseV2 = true;

            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) l.enabled = false;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.07f, 0.15f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.03f, 0.08f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.0f, 0.02f);

            BuildStage();

            VacuumSpec sled = null, first = null;
            foreach (var s in VacuumCatalog.All) { if (s.Id == "m_redsled") sled = s; if (first == null) first = s; }
            if (sled == null) sled = first;

            yield return Shot("hc-warmup", sled, false, 142f, 256, 144, 1.7f, 0.32f, 0.50f);   // first capture of the probe is stale
            // name, spec, recolour, yaw, width, height, camera distance factor, camera height, look height
            yield return Shot("hc-hero-red", sled, false, 142f, 1920, 1080, 1.7f, 0.32f, 0.50f);
            yield return Shot("hc-hero-cherry", sled, true, 142f, 1920, 1080, 1.7f, 0.32f, 0.50f);
            yield return Shot("hc-square-low", sled, false, 205f, 1080, 1080, 2.2f, 0.16f, 0.55f);
            yield return Shot("hc-portrait", sled, true, 118f, 1080, 1920, 2.1f, 0.25f, 0.62f);
            yield return Shot("hc-side-cherry", sled, true, 90f, 1920, 1080, 2.0f, 0.22f, 0.45f);

            Debug.Log("[VCS] HotChrome done");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        void BuildStage()
        {
            stage = new GameObject("HotChromeStage").transform;
            stage.position = new Vector3(0f, -1000f, 0f);

            // Backdrop: emissive burst centred on the floor line, so the half below the floor reads as its reflection.
            var back = PropFactory.Prim(PrimitiveType.Cube, stage, new Vector3(0f, 0f, -16f), new Vector3(90f, 45f, 0.1f), Color.black, "Burst", false);
            var bm = new Material(Resources.Load<Material>("Materials/LitEmissive") ?? Palette.Led(Color.white));
            bm.color = Color.black;
            bm.SetColor("_EmissionColor", Color.white * 0.4f);
            bm.SetTexture("_EmissionMap", BurstTexture(1024, 512));
            bm.SetFloat("_Glossiness", 0f);
            back.GetComponent<Renderer>().sharedMaterial = bm;
            back.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Wet floor: translucent, glossy, over the mirrored world.
            var floor = PropFactory.Prim(PrimitiveType.Quad, stage, Vector3.zero, new Vector3(80f, 60f, 1f), Color.black, "WetFloor", false, Quaternion.Euler(90f, 0f, 0f));
            var fm = Palette.Fade();
            fm.color = new Color(0.13f, 0.0f, 0.05f, 0.58f);
            fm.SetFloat("_Glossiness", 0.97f);
            fm.SetFloat("_Metallic", 0.25f);
            floor.GetComponent<Renderer>().sharedMaterial = fm;

            // Speed streaks, laid along z so perspective pulls them into the vanishing point.
            var rng = new System.Random(7);
            for (int i = 0; i < 70; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float x = side * (1.4f + (float)rng.NextDouble() * 7f);
                float y = (float)(rng.NextDouble() * rng.NextDouble()) * 3.2f + 0.25f;
                float z = -14f + (float)rng.NextDouble() * 11f;
                float len = 0.8f + (float)rng.NextDouble() * 3.5f;
                float th = 0.008f + (float)rng.NextDouble() * 0.03f;
                bool white = rng.NextDouble() < 0.35;
                Color c = white ? new Color(1f, 0.9f, 0.95f) : new Color(1f, 0.25f, 0.55f);
                var st = PropFactory.Prim(PrimitiveType.Cube, stage, new Vector3(x, y, z), new Vector3(th, th, len), c, "Streak", false);
                var sm = new Material(Palette.Led(c));
                sm.SetColor("_EmissionColor", c * (white ? 4f : 3f));
                var r = st.GetComponent<Renderer>();
                r.sharedMaterial = sm;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                st.layer = 2;   // kept out of the reflection probe: the probe smears them into blobs on the floor
            }

            // Lights: warm-pink key, two hot-pink rims behind, a white kicker from the burst.
            AddLight("Key", LightType.Spot, new Vector3(2.8f, 3.2f, 3.5f), new Color(1f, 0.86f, 0.9f), 3.2f, 14f, true, 40f);
            AddLight("RimL", LightType.Point, new Vector3(-1.8f, 1.3f, -1.6f), new Color(1f, 0.16f, 0.48f), 6f, 5f, false, 0f);
            AddLight("RimR", LightType.Point, new Vector3(1.9f, 0.9f, -1.4f), new Color(1f, 0.22f, 0.55f), 5f, 5f, false, 0f);
            AddLight("Kick", LightType.Point, new Vector3(0f, 0.6f, -3.5f), new Color(1f, 0.85f, 0.92f), 3f, 6f, false, 0f);
            AddLight("Fill", LightType.Point, new Vector3(-3f, 1.2f, 3f), new Color(0.9f, 0.3f, 0.55f), 1.2f, 8f, false, 0f);

            var camGo = new GameObject("HotChromeCamera");
            camGo.transform.SetParent(stage, false);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0f, 0.03f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.cullingMask &= ~(1 << 8);
            cam.enabled = false;
            RenderingSetup.Attach(cam);

            var pg = new GameObject("HotChromeProbe");
            pg.transform.SetParent(stage, false);
            pg.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            probe = pg.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(40f, 20f, 40f);
            probe.importance = 10;
            probe.intensity = 0.9f;
            probe.hdr = true;
            probe.cullingMask &= ~((1 << 8) | (1 << 2));
        }

        void AddLight(string name, LightType type, Vector3 pos, Color c, float intensity, float range, bool shadows, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(stage, false);
            go.transform.localPosition = pos;
            go.transform.LookAt(stage.position + new Vector3(0f, 0.4f, 0f));
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            if (type == LightType.Spot) l.spotAngle = angle;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        IEnumerator Shot(string name, VacuumSpec spec, bool cherry, float yaw, int w, int h, float dist, float camY, float lookY)
        {
            var t = new GameObject("Hero").transform;
            t.SetParent(stage, false);
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            spec.Build(t, spec);
            VacuumDetails.Add(t, spec);
            foreach (var c in t.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var rb in t.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
            if (cherry)
                foreach (var r in t.GetComponentsInChildren<Renderer>())
                    foreach (var m in r.materials)
                    {
                        m.color = m.color * new Color(0.42f, 0.10f, 0.22f, 1f);
                        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.93f);
                        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.35f);
                    }

            var rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            // Machine centred on the stage axis, base on the floor.
            t.position += new Vector3(stage.position.x - b.center.x, stage.position.y - b.min.y, stage.position.z - b.center.z);
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float size = Mathf.Max(b.size.x, b.size.y, b.size.z);

            // Mirror copy under the wet floor.
            var mirrorRoot = new GameObject("Mirror").transform;
            mirrorRoot.SetParent(stage, false);
            mirrorRoot.localScale = new Vector3(1f, -1f, 1f);
            var copy = Instantiate(t.gameObject, mirrorRoot);
            copy.transform.localPosition = t.localPosition;
            copy.transform.localRotation = t.localRotation;
            foreach (var r in copy.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            yield return null;
            probe.RenderProbe();
            yield return null;
            yield return null;

            float aspect = (float)w / h;
            cam.fieldOfView = aspect >= 1f ? 30f : 44f;
            var look = stage.position + new Vector3(0f, lookY * size * 0.7f, 0f);
            cam.transform.position = stage.position + new Vector3(0f, camY * size + 0.08f, size * dist);
            cam.transform.LookAt(look);

            string file = Path.Combine(outDir, name + ".png");
            Shoot(w, h, file);
            Debug.Log("[VCS] HotChrome " + file + " size " + size.ToString("F2"));
            DestroyImmediate(t.gameObject);
            DestroyImmediate(mirrorRoot.gameObject);
            yield return null;
        }

        void Shoot(int w, int h, string path)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 8;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
            Destroy(rt);
        }

        /// <summary>Crimson radial burst with pink rays and a white core on the horizon, HDR so the core blooms.</summary>
        static Texture2D BurstTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBAHalf, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            var hot = new Color(1f, 0.06f, 0.34f);
            var cherry = new Color(0.16f, 0.0f, 0.05f);
            var rng = new System.Random(3);
            var jitter = new float[64];
            for (int i = 0; i < jitter.Length; i++) jitter[i] = (float)rng.NextDouble();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x / (float)(w - 1) - 0.5f) * 2f;          // -1..1, width is 2x height
                    float v = (y / (float)(h - 1) - 0.5f);                // -0.5..0.5
                    float r = Mathf.Sqrt(u * u * 0.25f + v * v) * 2f;     // 0 centre, ~1.4 corners
                    float a = Mathf.Atan2(Mathf.Abs(v), u);               // symmetric about the horizon
                    Color c = Color.Lerp(hot * 1.35f, cherry, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(r / 0.75f)));
                    int ray = Mathf.FloorToInt((a / Mathf.PI) * 64f) & 63;
                    float rays = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(a * 46f + jitter[ray] * 3f)), 8f) * (0.4f + jitter[ray]);
                    c += new Color(1f, 0.35f, 0.62f) * rays * Mathf.Clamp01(1.1f - r) * 0.9f;
                    float core = Mathf.Exp(-r * r / 0.012f) * 7f + Mathf.Exp(-r * r / 0.08f) * 1.2f;
                    float band = Mathf.Exp(-v * v / 0.00008f) * Mathf.Clamp01(1.2f - Mathf.Abs(u)) * 2.2f;
                    c += new Color(1f, 0.88f, 0.93f) * (core + band);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
