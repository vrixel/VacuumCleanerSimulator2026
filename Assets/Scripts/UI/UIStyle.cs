using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VCS.UI
{
    /// <summary>
    /// The HUD's typography and text effects. Bundled OFL fonts from Resources/Fonts with the built-in font as
    /// a fallback, glow and shadow stacks, seven-segment readouts with ghost digits, scanlines, corner brackets.
    /// </summary>
    public static class UIStyle
    {
        static readonly Dictionary<string, Font> cache = new Dictionary<string, Font>();

        /// <summary>Tall condensed display face for scores and big titles (Bebas Neue).</summary>
        public static Font Title => Load("Fonts/BebasNeue");
        /// <summary>Squared techno face for headings and labels (Orbitron).</summary>
        public static Font Display => Load("Fonts/Orbitron");
        /// <summary>Monospaced readouts with units (Share Tech Mono).</summary>
        public static Font Mono => Load("Fonts/ShareTechMono");
        /// <summary>Seven-segment digits (DSEG7). Digits, colon, dot and minus only.</summary>
        public static Font Seven => Load("Fonts/DSEG7");
        /// <summary>Fourteen-segment alphanumerics (DSEG14).</summary>
        public static Font Fourteen => Load("Fonts/DSEG14");
        /// <summary>Body text (Exo 2).</summary>
        public static Font Body => Load("Fonts/Exo2");

        public static readonly Color Amber = new Color(1f, 0.78f, 0.25f);
        public static readonly Color Cyan = new Color(0.45f, 0.9f, 1f);
        public static readonly Color Green = new Color(0.4f, 1f, 0.55f);
        public static readonly Color Red = new Color(1f, 0.3f, 0.22f);
        public static readonly Color Ink = new Color(0.04f, 0.05f, 0.08f, 0.82f);
        public static readonly Color Steel = new Color(0.62f, 0.68f, 0.78f);

        static Font Load(string path)
        {
            if (cache.TryGetValue(path, out var f) && f != null) return f;
            f = Resources.Load<Font>(path);
            if (f == null) f = UIFactory.Font;
            cache[path] = f;
            return f;
        }

        /// <summary>Restyles a text: font, size, style, colour.</summary>
        public static Text Style(Text t, Font font, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            return t;
        }

        /// <summary>Soft coloured glow behind a text: a wide translucent outline plus a drop shadow.</summary>
        public static Text Glow(Text t, Color c, float distance = 3f, float alpha = 0.45f)
        {
            foreach (var old in t.GetComponents<Outline>()) Object.Destroy(old);
            foreach (var old in t.GetComponents<Shadow>()) Object.Destroy(old);
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(c.r, c.g, c.b, alpha);
            o.effectDistance = new Vector2(distance, -distance);
            o.useGraphicAlpha = true;
            var o2 = t.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(c.r, c.g, c.b, alpha * 0.5f);
            o2.effectDistance = new Vector2(-distance, distance);
            o2.useGraphicAlpha = true;
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.75f);
            s.effectDistance = new Vector2(2f, -2f);
            s.useGraphicAlpha = true;
            return t;
        }

        /// <summary>Crisp dark edge for small labels over busy backgrounds.</summary>
        public static Text Edge(Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.85f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
            s.useGraphicAlpha = true;
            return t;
        }

        /// <summary>
        /// Seven-segment readout: a dim ghost of all segments lit ("888") behind the live digits. Returns the
        /// live text; the ghost is a sibling drawn first. Width is fixed by the ghost pattern.
        /// </summary>
        public static Text Digital(Transform parent, string name, string ghostPattern, int size, Color color, TextAnchor anchor,
            Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool fourteen = false)
        {
            var font = fourteen ? Fourteen : Seven;
            var ghost = UIFactory.Text(parent, name + "Ghost", ghostPattern, size, new Color(color.r, color.g, color.b, 0.13f), anchor, aMin, aMax, oMin, oMax, false, FontStyle.Normal);
            ghost.font = font;
            var live = UIFactory.Text(parent, name, "", size, color, anchor, aMin, aMax, oMin, oMax, false, FontStyle.Normal);
            live.font = font;
            Glow(live, color, 2f, 0.35f);
            return live;
        }

        static Sprite scanlines;

        /// <summary>Faint horizontal scanlines over a readout area (a tiled 1x4 sprite).</summary>
        public static Image Scanlines(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, float alpha = 0.10f)
        {
            if (scanlines == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
                var px = new Color[16];
                for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    px[y * 4 + x] = y == 0 ? new Color(0f, 0f, 0f, 1f) : new Color(0f, 0f, 0f, 0f);
                tex.SetPixels(px);
                tex.Apply();
                scanlines = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
            }
            var img = UIFactory.Panel(parent, "Scanlines", new Color(1f, 1f, 1f, alpha), aMin, aMax, oMin, oMax);
            img.sprite = scanlines;
            img.type = Image.Type.Tiled;
            img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        /// <summary>Four L-shaped brackets marking the corners of a rectangle (HUD frame feel).</summary>
        public static void Brackets(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, float arm = 28f, float thick = 3f, Color? color = null)
        {
            var c = color ?? new Color(0.8f, 0.86f, 0.95f, 0.8f);
            var holder = new GameObject("Brackets", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            UIFactory.Anchor(holder, aMin, aMax, oMin, oMax);
            var h = holder.transform;
            // top-left
            UIFactory.Panel(h, "TL1", c, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -thick), new Vector2(arm, 0f));
            UIFactory.Panel(h, "TL2", c, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -arm), new Vector2(thick, 0f));
            // top-right
            UIFactory.Panel(h, "TR1", c, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-arm, -thick), new Vector2(0f, 0f));
            UIFactory.Panel(h, "TR2", c, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-thick, -arm), new Vector2(0f, 0f));
            // bottom-left
            UIFactory.Panel(h, "BL1", c, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(arm, thick));
            UIFactory.Panel(h, "BL2", c, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(thick, arm));
            // bottom-right
            UIFactory.Panel(h, "BR1", c, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-arm, 0f), new Vector2(0f, thick));
            UIFactory.Panel(h, "BR2", c, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-thick, 0f), new Vector2(0f, arm));
        }

        /// <summary>Dark translucent readout box with a thin steel edge and scanlines.</summary>
        public static Image Box(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, float alpha = 0.72f)
        {
            var edge = UIFactory.Panel(parent, name + "Edge", new Color(0.6f, 0.66f, 0.76f, 0.55f), aMin, aMax, oMin - Vector2.one, oMax + Vector2.one);
            var box = UIFactory.Panel(parent, name, new Color(0.04f, 0.05f, 0.08f, alpha), aMin, aMax, oMin, oMax);
            Scanlines(parent, aMin, aMax, oMin, oMax, 0.08f);
            return box;
        }
    }
}
