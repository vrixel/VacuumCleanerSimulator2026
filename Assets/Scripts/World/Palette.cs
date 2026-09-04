using System.Collections.Generic;
using UnityEngine;

namespace VCS.World
{
    /// <summary>
    /// Flat-colour material cache. One shared material per colour, all built on the Standard shader
    /// (the material asset in Resources/Materials guarantees the shader ships in builds).
    /// </summary>
    public static class Palette
    {
        public static readonly Color WoodFloor = new Color(0.72f, 0.53f, 0.35f);
        public static readonly Color TileFloor = new Color(0.86f, 0.86f, 0.80f);
        public static readonly Color BathTile = new Color(0.70f, 0.86f, 0.90f);
        public static readonly Color Carpet = new Color(0.52f, 0.60f, 0.80f);
        public static readonly Color Stone = new Color(0.58f, 0.58f, 0.60f);
        public static readonly Color Grass = new Color(0.42f, 0.68f, 0.32f);
        public static readonly Color Wall = new Color(0.95f, 0.92f, 0.84f);
        public static readonly Color WallTrim = new Color(0.80f, 0.74f, 0.62f);
        public static readonly Color DarkWood = new Color(0.45f, 0.30f, 0.18f);
        public static readonly Color LightWood = new Color(0.82f, 0.66f, 0.45f);
        public static readonly Color White = new Color(0.96f, 0.96f, 0.96f);
        public static readonly Color Black = new Color(0.12f, 0.12f, 0.14f);
        public static readonly Color Gray = new Color(0.55f, 0.55f, 0.58f);
        public static readonly Color Red = new Color(0.90f, 0.25f, 0.22f);
        public static readonly Color Blue = new Color(0.25f, 0.45f, 0.90f);
        public static readonly Color Yellow = new Color(0.98f, 0.85f, 0.20f);
        public static readonly Color Green = new Color(0.30f, 0.72f, 0.35f);
        public static readonly Color Orange = new Color(0.98f, 0.55f, 0.15f);
        public static readonly Color Pink = new Color(0.95f, 0.55f, 0.75f);
        public static readonly Color Purple = new Color(0.55f, 0.35f, 0.80f);
        public static readonly Color Teal = new Color(0.20f, 0.70f, 0.70f);
        public static readonly Color Gold = new Color(1.00f, 0.84f, 0.20f);
        public static readonly Color Terracotta = new Color(0.80f, 0.42f, 0.28f);

        static Material litBase;
        static Material particleBase;
        static Texture2D softCircle;
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        static Material LitBase()
        {
            if (litBase != null) return litBase;
            litBase = Resources.Load<Material>("Materials/Lit");
            if (litBase == null)
            {
                var sh = Shader.Find("Standard");
                litBase = new Material(sh);
                litBase.SetFloat("_Glossiness", 0.25f);
                litBase.SetFloat("_Metallic", 0f);
            }
            return litBase;
        }

        public static Material Lit(Color c)
        {
            if (cache.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(LitBase());
            m.color = c;
            m.name = "Lit " + ColorUtility.ToHtmlStringRGB(c);
            cache[c] = m;
            return m;
        }

        public static Material Particle
        {
            get
            {
                if (particleBase != null) return particleBase;
                var loaded = Resources.Load<Material>("Materials/Particle");
                if (loaded != null)
                {
                    particleBase = new Material(loaded);
                }
                else
                {
                    var sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                    if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
                    particleBase = new Material(sh);
                }
                particleBase.mainTexture = SoftCircle();
                return particleBase;
            }
        }

        /// <summary>A 64x64 radial alpha gradient so particles look like puffs rather than squares.</summary>
        public static Texture2D SoftCircle()
        {
            if (softCircle != null) return softCircle;
            const int n = 64;
            softCircle = new Texture2D(n, n, TextureFormat.RGBA32, false);
            softCircle.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
            softCircle.SetPixels(px);
            softCircle.Apply();
            return softCircle;
        }
    }
}
