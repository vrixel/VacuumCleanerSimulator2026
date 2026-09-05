using System;
using UnityEngine;

namespace VCS.UI
{
    /// <summary>Procedural sprites for the cockpit: LED rings, lamps, glows, needles, a fallback gauge face.</summary>
    public static class UISprites
    {
        static Sprite circle, glow, needle, ring, segRing, face, hbar;

        public static Sprite Circle => circle != null ? circle : circle = Make("circle", 64, (x, y) =>
        {
            float d = Dist(x, y, 64);
            return new Color(1f, 1f, 1f, Mathf.Clamp01((0.98f - d) * 32f));
        });

        public static Sprite Glow => glow != null ? glow : glow = Make("glow", 96, (x, y) =>
        {
            float d = Dist(x, y, 96);
            float a = Mathf.Clamp01(1f - d);
            return new Color(1f, 1f, 1f, a * a * 0.9f);
        });

        /// <summary>Tapered needle, pivot meant at (0.5, 0.12): the tip points up.</summary>
        public static Sprite Needle => needle != null ? needle : needle = Make("needle", 32, 160, (x, y) =>
        {
            float v = y / 160f;
            float half = Mathf.Lerp(3.5f, 0.8f, v);
            float dx = Mathf.Abs(x + 0.5f - 16f);
            float a = Mathf.Clamp01(half - dx + 0.5f);
            if (v < 0.06f) a *= Mathf.Clamp01(1f - Dist(x, y - 0, 32) * 0f);
            return new Color(1f, 1f, 1f, a);
        }, new Vector2(0.5f, 0.12f));

        public static Sprite Ring => ring != null ? ring : ring = Make("ring", 256, (x, y) =>
        {
            float d = Dist(x, y, 256);
            float a = Mathf.Clamp01((1f - d) * 128f) * Mathf.Clamp01((d - 0.80f) * 128f);
            return new Color(1f, 1f, 1f, a);
        });

        /// <summary>Ring cut into 48 LED segments with small gaps. Use with Image.fillMethod Radial360.</summary>
        public static Sprite SegmentedRing => segRing != null ? segRing : segRing = Make("segring", 256, (x, y) =>
        {
            float d = Dist(x, y, 256);
            float a = Mathf.Clamp01((1f - d) * 128f) * Mathf.Clamp01((d - 0.74f) * 128f);
            float ang = Mathf.Atan2(y - 127.5f, x - 127.5f) / (Mathf.PI * 2f) + 0.5f;
            float seg = ang * 48f;
            float frac = seg - Mathf.Floor(seg);
            if (frac > 0.72f) a = 0f;
            return new Color(1f, 1f, 1f, a);
        });

        /// <summary>Fallback dial when no generated gauge face is available: dark disc, bezel, ticks.</summary>
        public static Sprite GaugeFace => face != null ? face : face = Make("gaugeface", 256, (x, y) =>
        {
            float d = Dist(x, y, 256);
            if (d > 1f) return new Color(0f, 0f, 0f, 0f);
            Color c = new Color(0.08f, 0.09f, 0.11f, 1f);
            if (d > 0.93f) c = new Color(0.55f, 0.57f, 0.6f, 1f);
            else if (d > 0.9f) c = new Color(0.2f, 0.21f, 0.24f, 1f);
            float ang = Mathf.Atan2(y - 127.5f, x - 127.5f) * Mathf.Rad2Deg;
            // ticks every 10 degrees along the 240-degree scale that starts at -120 (bottom left)
            float scale = ang + 90f;
            if (scale > 180f) scale -= 360f;
            if (scale >= -122f && scale <= 122f && d > 0.66f && d < 0.86f)
            {
                float m = Mathf.Repeat(scale + 120f, 24f);
                bool major = m < 1.6f || m > 22.4f;
                float m2 = Mathf.Repeat(scale + 120f, 8f);
                bool minor = m2 < 1.2f || m2 > 6.8f;
                if (major && d > 0.68f) c = new Color(0.85f, 0.87f, 0.9f, 1f);
                else if (minor && d > 0.78f) c = new Color(0.55f, 0.57f, 0.6f, 1f);
                if (scale > 90f && d > 0.76f && !major) c = Color.Lerp(c, new Color(0.8f, 0.15f, 0.12f, 1f), 0.6f);
            }
            return c;
        });

        public static Sprite Bar => hbar != null ? hbar : hbar = Make("bar", 8, 8, (x, y) => Color.white, new Vector2(0.5f, 0.5f));

        static Sprite radarSweep;

        /// <summary>A 70-degree wedge fading behind its leading edge, for the radar sweep.</summary>
        public static Sprite RadarSweep => radarSweep != null ? radarSweep : radarSweep = Make("sweep", 256, (x, y) =>
        {
            float d = Dist(x, y, 256);
            if (d > 0.98f) return new Color(1f, 1f, 1f, 0f);
            float ang = Mathf.Atan2(y - 127.5f, x - 127.5f) * Mathf.Rad2Deg;
            float rel = Mathf.Repeat(90f - ang, 360f);
            if (rel > 70f) return new Color(1f, 1f, 1f, 0f);
            float a = (1f - rel / 70f);
            a = a * a * (rel < 2f ? 1f : 0.6f);
            return new Color(1f, 1f, 1f, a);
        });

        static float Dist(int x, int y, int n)
        {
            float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy) * 2f;
        }

        static Sprite Make(string name, int n, Func<int, int, Color> f) => Make(name, n, n, f, new Vector2(0.5f, 0.5f));

        static Sprite Make(string name, int w, int h, Func<int, int, Color> f, Vector2 pivot)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = f(x, y);
            tex.SetPixels(px);
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0f, 0f, w, h), pivot, 100f);
            s.name = name;
            return s;
        }

        /// <summary>Loads a generated sprite from Resources, or null.</summary>
        public static Sprite Load(string path) => Resources.Load<Sprite>(path);
    }
}
