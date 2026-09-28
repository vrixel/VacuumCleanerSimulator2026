using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using VCS.Player;

namespace VCS.Core
{
    // Exploration (branch explore/hot-chrome only): "-pink" on the command line repaints the running game in the
    // Hot Chrome car-ad look. Every level build and every new vacuum is recoloured as it appears: walls hot pink,
    // floors wet dark cherry, everything else pulled onto a cherry-to-blush ramp by its luminance, the vacuum
    // black-cherry gloss, pink sun and ambient, and a second post volume that pushes the grade to pink.
    public class HotChromeLook : MonoBehaviour
    {
        static readonly Color Deep = new Color(0.20f, 0.02f, 0.08f);
        static readonly Color Hot = new Color(1.00f, 0.30f, 0.60f);
        static readonly Color Blush = new Color(1.00f, 0.86f, 0.93f);

        readonly Dictionary<Material, Material> remap = new Dictionary<Material, Material>();
        Transform lastRoot;
        VacuumController lastPlayer;

        public static void TryStart(GameManager gm)
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-pink") { gm.gameObject.AddComponent<HotChromeLook>(); Debug.Log("[VCS] Hot Chrome look on"); return; }
        }

        void Start()
        {
            var gm = GameManager.I;
            gm.Cam.Cam.backgroundColor = new Color(0.16f, 0.01f, 0.07f);
            AddVolume();
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Level == null) return;
            if (gm.Level.Root != null && gm.Level.Root != lastRoot)
            {
                lastRoot = gm.Level.Root;
                PaintLevel(lastRoot);
            }
            if (gm.Player != null && gm.Player != lastPlayer)
            {
                lastPlayer = gm.Player;
                PaintVacuum(lastPlayer.transform);
            }
        }

        void PaintLevel(Transform root)
        {
            remap.Clear();
            int n = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                string name = r.gameObject.name;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_Color") || m.renderQueue >= 3000) continue;
                    if (name.StartsWith("Floor")) mats[i] = Floor(m);
                    else if (name == "Wall") mats[i] = Wall(m);
                    else mats[i] = Ramp(m);
                    n++;
                }
                r.sharedMaterials = mats;
            }
            foreach (var l in root.GetComponentsInChildren<Light>(true))
                if (l.type == LightType.Directional) { l.color = new Color(1f, 0.86f, 0.93f); l.intensity = 1.0f; }
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.36f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.20f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.04f, 0.12f);
            foreach (var p in root.GetComponentsInChildren<ReflectionProbe>(true)) p.RenderProbe();
            Debug.Log("[VCS] Hot Chrome: repainted " + n + " material slots");
        }

        Material Copy(Material m, string key, System.Func<Material, Material> make)
        {
            if (remap.TryGetValue(m, out var done)) return done;
            var c = make(new Material(m) { name = m.name + key });
            remap[m] = c;
            return c;
        }

        Material Floor(Material m) => Copy(m, " hcFloor", c =>
        {
            c.color = Color.Lerp(Deep, c.color * new Color(0.45f, 0.10f, 0.22f), 0.35f);
            Gloss(c, 0.93f, 0.2f);
            return c;
        });

        Material Wall(Material m) => Copy(m, " hcWall", c =>
        {
            c.color = Color.Lerp(Deep, Hot, 0.5f);
            Gloss(c, 0.8f, 0.05f);
            return c;
        });

        Material Ramp(Material m) => Copy(m, " hcRamp", c =>
        {
            c.color = PinkOf(c.color);
            Gloss(c, Mathf.Max(Glossiness(c), 0.7f), 0.1f);
            return c;
        });

        static Color PinkOf(Color src)
        {
            float l = src.r * 0.3f + src.g * 0.59f + src.b * 0.11f;
            Color ramp = l < 0.5f ? Color.Lerp(Deep, Hot, l * 2f) : Color.Lerp(Hot, Blush, (l - 0.5f) * 2f);
            var c = Color.Lerp(src, ramp, 0.7f);
            c.a = src.a;
            return c;
        }

        void PaintVacuum(Transform t)
        {
            var own = new Dictionary<Material, Material>();
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_Color") || m.renderQueue >= 3000) continue;
                    if (!own.TryGetValue(m, out var c))
                    {
                        c = new Material(m) { name = m.name + " hcCherry" };
                        c.color = c.color * new Color(0.42f, 0.10f, 0.22f);
                        Gloss(c, 0.93f, 0.35f);
                        own[m] = c;
                    }
                    mats[i] = c;
                }
                r.sharedMaterials = mats;
            }
        }

        static float Glossiness(Material m) => m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.5f;

        static void Gloss(Material m, float gloss, float metal)
        {
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gloss);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        }

        static void AddVolume()
        {
            var grading = ScriptableObject.CreateInstance<ColorGrading>();
            grading.enabled.Override(true);
            grading.colorFilter.Override(new Color(1f, 0.92f, 0.97f));
            grading.saturation.Override(6f);
            grading.contrast.Override(14f);
            grading.temperature.Override(-4f);
            grading.tint.Override(5f);
            var bloom = ScriptableObject.CreateInstance<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(1.6f);
            bloom.threshold.Override(0.95f);
            bloom.color.Override(new Color(1f, 0.55f, 0.78f));
            var vignette = ScriptableObject.CreateInstance<Vignette>();
            vignette.enabled.Override(true);
            vignette.color.Override(new Color(0.18f, 0f, 0.07f));
            vignette.intensity.Override(0.38f);
            var v = PostProcessManager.instance.QuickVolume(RenderingSetup.VolumeLayer, 200f, grading, bloom, vignette);
            v.isGlobal = true;
            DontDestroyOnLoad(v.gameObject);
        }
    }
}
