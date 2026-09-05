using System.Collections.Generic;
using UnityEngine;

namespace VCS.World
{
    /// <summary>
    /// Small tileable textures generated once at startup: normal maps that give the plastics, rubbers, fabrics and
    /// metals a real surface under the lights. No art assets, everything is noise. Normals are stored as RG with
    /// alpha 1 so the Standard shader unpacks them the same way on every platform.
    /// </summary>
    public static class ProceduralTextures
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>Fine plastic grain: soft Perlin lumps with a sprinkle of pinprick pits.</summary>
        public static Texture2D PlasticGrain => Normal("plastic", 256, (x, y) =>
            Fbm(x * 9f, y * 9f, 3) * 0.7f + Fbm(x * 41f + 7f, y * 41f + 3f, 2) * 0.3f, 0.9f, 11);

        /// <summary>Coarser, softer bumps for rubber bumpers and hoses.</summary>
        public static Texture2D RubberGrain => Normal("rubber", 256, (x, y) =>
            Fbm(x * 6f + 2f, y * 6f + 9f, 3), 1.6f, 23);

        /// <summary>Horizontal streaks for brushed steel and aluminium.</summary>
        public static Texture2D Brushed => Normal("brushed", 256, (x, y) =>
            Mathf.PerlinNoise(x * 3f, y * 180f) * 0.8f + Mathf.PerlinNoise(x * 60f + 5f, y * 400f + 1f) * 0.2f, 0.6f, 5);

        /// <summary>Woven cloth: two crossed sine ridges with a little noise, for bags and upholstery.</summary>
        public static Texture2D Weave => Normal("weave", 256, (x, y) =>
        {
            float a = Mathf.Sin(x * Mathf.PI * 2f * 24f) * 0.5f + 0.5f;
            float b = Mathf.Sin(y * Mathf.PI * 2f * 24f) * 0.5f + 0.5f;
            return Mathf.Max(a * (0.6f + 0.4f * b), b * (0.6f + 0.4f * a)) * 0.85f + Fbm(x * 30f, y * 30f, 2) * 0.15f;
        }, 1.8f, 3);

        /// <summary>Wood planks: long grain streaks with plank seams, for the wooden floors.</summary>
        public static Texture2D WoodGrain => Normal("wood", 256, (x, y) =>
        {
            float grain = Mathf.PerlinNoise(x * 4f, y * 90f) * 0.7f + Fbm(x * 20f, y * 20f, 2) * 0.3f;
            float seam = Mathf.Abs(Mathf.Repeat(x * 4f, 1f) - 0.5f) < 0.02f ? -0.6f : 0f;
            return grain + seam;
        }, 1.0f, 17);

        static float Fbm(float x, float y, int octaves)
        {
            float v = 0f, amp = 0.5f, f = 1f, sum = 0f;
            for (int i = 0; i < octaves; i++)
            {
                v += Mathf.PerlinNoise(x * f, y * f) * amp;
                sum += amp;
                amp *= 0.5f;
                f *= 2.1f;
            }
            return v / sum;
        }

        /// <summary>Builds a tileable normal map from a height function over [0,1]²; strength scales the slopes.</summary>
        static Texture2D Normal(string key, int size, System.Func<float, float, float> height, float strength, int seed)
        {
            if (cache.TryGetValue(key, out var t) && t != null) return t;
            var h = new float[size * size];
            float off = seed * 13.37f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    h[y * size + x] = height(x / (float)size + off, y / (float)size + off);
            var px = new Color32[size * size];
            float s = strength * size / 64f;
            for (int y = 0; y < size; y++)
            {
                int ym = (y - 1 + size) % size, yp = (y + 1) % size;
                for (int x = 0; x < size; x++)
                {
                    int xm = (x - 1 + size) % size, xp = (x + 1) % size;
                    float dx = (h[y * size + xp] - h[y * size + xm]) * s;
                    float dy = (h[yp * size + x] - h[ym * size + x]) * s;
                    var n = new Vector3(-dx, -dy, 1f).normalized;
                    px[y * size + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), 255, 255);
                }
            }
            t = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            t.name = "normal_" + key;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            t.SetPixels32(px);
            t.Apply(true, false);
            cache[key] = t;
            return t;
        }
    }
}
