using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using VCS.Player;
using VCS.World;

namespace VCS.Core
{
    // Exploration (branch explore/hot-chrome only): "-pink" on the command line repaints the running game in the
    // Hot Chrome car-ad look. Every level build and every new vacuum is recoloured as it appears: walls hot pink,
    // floors wet dark cherry, everything else pulled onto a cherry-to-blush ramp by its luminance, the vacuum
    // black-cherry gloss, pink sun and ambient, and a second post volume that pushes the grade to pink.
    // Stylisation on top: ink outlines on every edge (HotChromeEdges, screen space) and a thick hot-pink inverted
    // hull (Shaders/HotChromeHull) behind the vacuum and the cat, plus a touch of chromatic aberration.
    // explore/chrome-battery: the look is reversible. SetOn(false) puts every original material, light, ambient
    // colour and the camera background back, destroys the spots and hulls and fades the volume out, so the
    // plutonium battery (ChromeMode) can switch it on for a while. Lite (phones): no edge pass, no spotlights.
    public class HotChromeLook : MonoBehaviour
    {
        static readonly Color Deep = new Color(0.20f, 0.02f, 0.08f);
        static readonly Color Hot = new Color(1.00f, 0.30f, 0.60f);
        static readonly Color Blush = new Color(1.00f, 0.86f, 0.93f);

        public bool On { get; private set; }
        public bool Lite;

        readonly Dictionary<Material, Material> remap = new Dictionary<Material, Material>();
        readonly Dictionary<Renderer, Material[]> original = new Dictionary<Renderer, Material[]>();
        readonly List<Material> made = new List<Material>();
        readonly List<GameObject> added = new List<GameObject>();
        readonly Dictionary<Light, (Color c, float i)> suns = new Dictionary<Light, (Color, float)>();
        readonly Dictionary<Material, Color> powderColor = new Dictionary<Material, Color>();
        Transform lastRoot;
        VacuumController lastPlayer;
        Material hull;
        readonly HashSet<Cat> hulledCats = new HashSet<Cat>();
        PostProcessVolume volume;
        float weight;
        Color ambSky, ambEq, ambGround, camBg;
        int pixelLights;

        public static void TryStart(GameManager gm)
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-pink") { Get(gm).SetOn(true, true); Debug.Log("[VCS] Hot Chrome look on"); return; }
        }

        public static HotChromeLook Get(GameManager gm)
        {
            var h = gm.GetComponent<HotChromeLook>();
            return h != null ? h : gm.gameObject.AddComponent<HotChromeLook>();
        }

        /// <summary>Switches the world look. instant skips the volume fade (the -pink launch flag).</summary>
        public void SetOn(bool on, bool instant = false)
        {
            if (on == On) return;
            On = on;
            var gm = GameManager.I;
            if (on)
            {
                ambSky = RenderSettings.ambientSkyColor;
                ambEq = RenderSettings.ambientEquatorColor;
                ambGround = RenderSettings.ambientGroundColor;
                pixelLights = QualitySettings.pixelLightCount;
                if (gm != null && gm.Cam != null)
                {
                    camBg = gm.Cam.Cam.backgroundColor;
                    gm.Cam.Cam.backgroundColor = new Color(0.16f, 0.01f, 0.07f);
                }
                if (volume == null) volume = AddVolume(Lite);
                lastRoot = null;
                lastPlayer = null;
                if (instant) weight = 1f;
            }
            else
            {
                Restore();
                if (instant) weight = 0f;
            }
            if (volume != null) volume.weight = weight;
        }

        void Restore()
        {
            foreach (var kv in original)
                if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            original.Clear();
            foreach (var go in added) if (go != null) Destroy(go);
            added.Clear();
            hulledCats.Clear();
            foreach (var kv in suns)
                if (kv.Key != null) { kv.Key.color = kv.Value.c; kv.Key.intensity = kv.Value.i; }
            suns.Clear();
            foreach (var kv in powderColor)
                if (kv.Key != null) kv.Key.color = kv.Value;
            powderColor.Clear();
            RenderSettings.ambientSkyColor = ambSky;
            RenderSettings.ambientEquatorColor = ambEq;
            RenderSettings.ambientGroundColor = ambGround;
            QualitySettings.pixelLightCount = pixelLights;
            var gm = GameManager.I;
            if (gm != null && gm.Cam != null) gm.Cam.Cam.backgroundColor = camBg;
            if (lastRoot != null)
                foreach (var p in lastRoot.GetComponentsInChildren<ReflectionProbe>(true)) p.RenderProbe();
            foreach (var m in made) if (m != null) Destroy(m);
            made.Clear();
            remap.Clear();
            glowOf.Clear();
            glowing.Clear();
            litPowder.Clear();
            lastRoot = null;
            lastPlayer = null;
        }

        void LateUpdate()
        {
            // the volume fades both ways over half a second, so the switch reads as a flash, not a cut
            float target = On ? 1f : 0f;
            if (!Mathf.Approximately(weight, target))
            {
                weight = Mathf.MoveTowards(weight, target, Time.unscaledDeltaTime / 0.5f);
                if (volume != null) volume.weight = weight;
            }
            if (!On) return;
            var gm = GameManager.I;
            if (gm == null || gm.Level == null) return;
            if (gm.Level.Root != null && gm.Level.Root != lastRoot)
            {
                lastRoot = gm.Level.Root;
                PaintLevel(lastRoot);
                if (!Lite) AddSpots(lastRoot);
                glowing.Clear();
                litPowder.Clear();
                hulledCats.Clear();
            }
            if (gm.Player != null && gm.Player != lastPlayer)
            {
                lastPlayer = gm.Player;
                PaintVacuum(lastPlayer.transform);
                AddHull(lastPlayer.transform, 0.032f);
            }
            if (lastRoot != null)
                foreach (var cat in lastRoot.GetComponentsInChildren<Cat>())
                    if (hulledCats.Add(cat)) AddHull(cat.transform, 0.02f);
            glowTimer -= Time.unscaledDeltaTime;
            if (glowTimer <= 0f) { glowTimer = 0.4f; UpdateGlow(gm); }
        }

        void Remember(Renderer r)
        {
            if (!original.ContainsKey(r)) original[r] = r.sharedMaterials;
        }

        // Light-emitting mess: every Debris is moved onto the emissive material (LitEmissive.mat ships _EMISSION);
        // what the vacuum can swallow right now glows hot, the rest barely. The cocoa powder is lit from inside.
        float glowTimer;
        Material emissiveBase;
        readonly Dictionary<Material, Material[]> glowOf = new Dictionary<Material, Material[]>();
        readonly Dictionary<Renderer, (Material[] src, int state)> glowing = new Dictionary<Renderer, (Material[], int)>();
        readonly HashSet<PowderLayer> litPowder = new HashSet<PowderLayer>();

        Material[] GlowPair(Material m)
        {
            if (glowOf.TryGetValue(m, out var pair)) return pair;
            Color c = PinkOf(m.color);
            var hot = new Material(emissiveBase) { name = m.name + " hcGlow", color = c };
            hot.SetColor("_EmissionColor", Color.Lerp(c, Hot, 0.5f) * 2.4f);
            var dim = new Material(emissiveBase) { name = m.name + " hcDim", color = c };
            dim.SetColor("_EmissionColor", c * 0.12f);
            foreach (var g in new[] { hot, dim }) { g.SetFloat("_Glossiness", 0.8f); g.SetFloat("_Metallic", 0.1f); made.Add(g); }
            return glowOf[m] = new[] { hot, dim };
        }

        void UpdateGlow(GameManager gm)
        {
            if (emissiveBase == null) emissiveBase = Resources.Load<Material>("Materials/LitEmissive");
            if (emissiveBase == null) return;
            int eats = gm.PowerLevel + (gm.Player != null ? gm.Player.Spec.SizeBonus : 0);
            foreach (var d in FindObjectsByType<Debris>(FindObjectsSortMode.None))
            {
                if (d.Kind == DebrisKind.PlutoniumBattery) continue;   // it keeps its own green glow
                int state = d.SizeClass <= eats ? 0 : 1;
                foreach (var r in d.GetComponentsInChildren<Renderer>())
                {
                    if (r is ParticleSystemRenderer || r is LineRenderer) continue;
                    if (!glowing.TryGetValue(r, out var g))
                    {
                        // the level paint already swapped these once: glow from the original colours
                        g = (original.TryGetValue(r, out var src) ? src : r.sharedMaterials, -1);
                        Remember(r);
                    }
                    if (g.state == state) continue;
                    var mats = new Material[g.src.Length];
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = g.src[i] == null || !g.src[i].HasProperty("_Color") || g.src[i].renderQueue >= 3000 ? g.src[i] : GlowPair(g.src[i])[state];
                    r.sharedMaterials = mats;
                    glowing[r] = (g.src, state);
                }
            }
            foreach (var p in FindObjectsByType<PowderLayer>(FindObjectsSortMode.None))
            {
                if (!litPowder.Add(p)) continue;
                var r = p.GetComponent<MeshRenderer>();
                if (r == null || r.sharedMaterial == null) continue;
                if (!powderColor.ContainsKey(r.sharedMaterial)) powderColor[r.sharedMaterial] = r.sharedMaterial.color;
                r.sharedMaterial.color = new Color(3.2f, 1.1f, 2.3f, 1f);
            }
        }

        // One spotlight per room hanging over the floor, alternating hot pink and violet, so pools of light cut the
        // dark cherry: the showroom lighting of the car ads.
        void AddSpots(Transform root)
        {
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 12);
            int i = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.gameObject.name.StartsWith("Floor")) continue;
                var b = r.bounds;
                var go = new GameObject("hcSpot");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(b.center.x, 3.4f, b.center.z);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.color = i++ % 2 == 0 ? new Color(1f, 0.28f, 0.62f) : new Color(0.72f, 0.42f, 1f);
                l.spotAngle = 95f;
                l.innerSpotAngle = 30f;
                l.range = 8f;
                l.intensity = 4.5f;
                l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.ForcePixel;
                added.Add(go);
            }
            Debug.Log("[VCS] Hot Chrome: " + i + " spotlights");
        }

        // One child per mesh renderer carrying the same mesh with the hull material; skinned or line renderers skipped.
        void AddHull(Transform t, float width)
        {
            if (hull == null)
            {
                var sh = Resources.Load<Shader>("Shaders/HotChromeHull");
                if (sh == null) { Debug.Log("[VCS] Hot Chrome: hull shader missing"); return; }
                hull = new Material(sh) { name = "hcHull" };
                hull.SetColor("_Color", new Color(2.6f, 0.36f, 1.3f));   // HDR: bloom turns the rim into neon
            }
            int n = 0;
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null || mf.name == "hcHull") continue;
                var mats = mr.sharedMaterials;
                if (mats.Length > 0 && mats[0] != null && mats[0].renderQueue >= 3000) continue;
                var go = new GameObject("hcHull");
                go.layer = mf.gameObject.layer;
                go.transform.SetParent(mf.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = hull;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                var block = new MaterialPropertyBlock();
                block.SetFloat("_Width", width);
                r.SetPropertyBlock(block);
                added.Add(go);
                n++;
            }
            Debug.Log("[VCS] Hot Chrome: hull on " + t.name + " (" + n + " meshes)");
        }

        void PaintLevel(Transform root)
        {
            remap.Clear();
            int n = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<Debris>() is Debris d && d.Kind == DebrisKind.PlutoniumBattery) continue;
                string name = r.gameObject.name;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_Color") || m.renderQueue >= 3000) continue;
                    if (name.StartsWith("Floor")) mats[i] = Floor(m);
                    else if (name == "Wall") mats[i] = Wall(m);
                    else mats[i] = Ramp(m);
                    changed = true;
                    n++;
                }
                if (!changed) continue;
                Remember(r);
                r.sharedMaterials = mats;
            }
            foreach (var l in root.GetComponentsInChildren<Light>(true))
                if (l.type == LightType.Directional)
                {
                    if (!suns.ContainsKey(l)) suns[l] = (l.color, l.intensity);
                    l.color = new Color(1f, 0.86f, 0.93f);
                    l.intensity = 0.7f;
                }
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
            made.Add(c);
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
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_Color") || m.renderQueue >= 3000) continue;
                    if (!own.TryGetValue(m, out var c))
                    {
                        c = new Material(m) { name = m.name + " hcCherry" };
                        c.color = Color.Lerp(c.color * new Color(0.42f, 0.10f, 0.22f), Hot, 0.25f);
                        Gloss(c, 0.93f, 0.35f);
                        own[m] = c;
                        made.Add(c);
                    }
                    mats[i] = c;
                    changed = true;
                }
                if (!changed) continue;
                Remember(r);
                r.sharedMaterials = mats;
            }
        }

        static float Glossiness(Material m) => m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.5f;

        static void Gloss(Material m, float gloss, float metal)
        {
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gloss);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        }

        static PostProcessVolume AddVolume(bool lite)
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
            var aberration = ScriptableObject.CreateInstance<ChromaticAberration>();
            aberration.enabled.Override(true);
            aberration.intensity.Override(0.18f);
            PostProcessVolume v;
            if (lite)
            {
                // phones: the edge pass needs a DepthNormals texture, a second render of the scene
                v = PostProcessManager.instance.QuickVolume(RenderingSetup.VolumeLayer, 200f, grading, bloom, vignette, aberration);
            }
            else
            {
                var edges = ScriptableObject.CreateInstance<HotChromeEdges>();
                edges.enabled.Override(true);
                edges.color.Override(new Color(2.2f, 0.3f, 1.1f, 1f));   // HDR pink: the outlines glow through the bloom
                v = PostProcessManager.instance.QuickVolume(RenderingSetup.VolumeLayer, 200f, grading, bloom, vignette, edges, aberration);
            }
            v.isGlobal = true;
            v.weight = 0f;
            DontDestroyOnLoad(v.gameObject);
            return v;
        }
    }
}
