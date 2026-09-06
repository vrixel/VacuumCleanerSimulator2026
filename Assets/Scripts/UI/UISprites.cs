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

        static Sprite bulb, bevel, vgrad, sparkle, chevron;

        /// <summary>A marquee light bulb: bright centre, soft rim, thin dark edge. Tint it for on and off states.</summary>
        public static Sprite Bulb => bulb != null ? bulb : bulb = Make("bulb", 48, (x, y) =>
        {
            float d = Dist(x, y, 48);
            if (d > 1f) return new Color(1f, 1f, 1f, 0f);
            float core = Mathf.Clamp01(1f - d * 1.35f);
            float v = 0.55f + 0.45f * core * core;
            float a = Mathf.Clamp01((1f - d) * 12f);
            return new Color(v, v, v, a);
        });

        /// <summary>Nine-sliced bevel frame: dark outer lip, bright ridge, dark inner lip, transparent middle. Tint gold.</summary>
        public static Sprite Bevel
        {
            get
            {
                if (bevel != null) return bevel;
                const int n = 48;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y));
                    Color c;
                    if (edge >= 12) c = new Color(0f, 0f, 0f, 0f);
                    else if (edge <= 1) c = new Color(0.25f, 0.18f, 0.02f, 1f);
                    else if (edge <= 4) c = new Color(1f, 0.95f, 0.65f, 1f);
                    else if (edge <= 7) c = new Color(0.85f, 0.65f, 0.12f, 1f);
                    else if (edge <= 9) c = new Color(0.5f, 0.36f, 0.05f, 1f);
                    else c = new Color(0.12f, 0.08f, 0.02f, 1f);
                    px[y * n + x] = c;
                }
                tex.SetPixels(px);
                tex.Apply();
                bevel = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14f, 14f, 14f, 14f));
                bevel.name = "bevel";
                return bevel;
            }
        }

        /// <summary>Vertical gradient, opaque at the top fading to 35 percent at the bottom. Tint it for panel glass.</summary>
        public static Sprite VGradient => vgrad != null ? vgrad : vgrad = Make("vgrad", 4, 64, (x, y) =>
        {
            float v = y / 63f;
            return new Color(1f, 1f, 1f, 0.35f + 0.65f * v);
        }, new Vector2(0.5f, 0.5f));

        /// <summary>Four-point star for sparkles.</summary>
        public static Sprite Sparkle => sparkle != null ? sparkle : sparkle = Make("sparkle", 64, (x, y) =>
        {
            float dx = Mathf.Abs(x + 0.5f - 32f) / 32f, dy = Mathf.Abs(y + 0.5f - 32f) / 32f;
            float a = Mathf.Clamp01(1f - (dx + dy) * 1.05f);
            a = a * a * a;
            float d = Dist(x, y, 64);
            a += Mathf.Clamp01(1f - d * 4f) * 0.8f;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        });

        /// <summary>Small chevron pointing right, for tape readouts.</summary>
        public static Sprite Chevron => chevron != null ? chevron : chevron = Make("chevron", 16, 24, (x, y) =>
        {
            float cy = Mathf.Abs(y + 0.5f - 12f) / 12f;
            bool inside = x < 16f * (1f - cy);
            return new Color(1f, 1f, 1f, inside ? 1f : 0f);
        }, new Vector2(0.5f, 0.5f));

        static Sprite vignette;

        /// <summary>Soft radial veil: 55 percent in the middle, opaque in the corners. Tint dark for backdrops.</summary>
        public static Sprite Vignette => vignette != null ? vignette : vignette = Make("vignette", 128, (x, y) =>
        {
            float d = Dist(x, y, 128);
            float a = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((d - 0.35f) / 0.75f));
            return new Color(1f, 1f, 1f, a);
        });

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
