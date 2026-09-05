using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VCS.UI
{
    /// <summary>
    /// The HUD's look: bold arcade cabinet (chunky italic type, safety yellow and electric blue, hard shadows)
    /// on generated instrument frames (Resources/UI/Hud, nine-sliced) with serious flight-deck instruments inside
    /// (seven-segment readouts, scrolling tapes, annunciator tiles). Text is always crisp.
    /// </summary>
    public static class UIStyle
    {
        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        static readonly Dictionary<string, Sprite> plates = new Dictionary<string, Sprite>();

        public static Font Arcade => Load("Fonts/RussoOne");
        public static Font Title => Load("Fonts/BebasNeue");
        public static Font Display => Load("Fonts/Orbitron");
        public static Font Mono => Load("Fonts/ShareTechMono");
        public static Font Seven => Load("Fonts/DSEG7");
        public static Font Fourteen => Load("Fonts/DSEG14");
        public static Font Body => Load("Fonts/Exo2");

        public static readonly Color Yellow = new Color(1f, 0.84f, 0f);
        public static readonly Color Blue = new Color(0f, 0.66f, 1f);
        public static readonly Color Red = new Color(1f, 0.22f, 0.16f);
        public static readonly Color Green = new Color(0.3f, 1f, 0.35f);
        public static readonly Color Amber = new Color(1f, 0.62f, 0f);
        public static readonly Color White = Color.white;
        public static readonly Color Ink = new Color(0.04f, 0.04f, 0.06f);
        public static readonly Color Panel = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        public static readonly Color Screen = new Color(0.02f, 0.03f, 0.04f, 0.98f);
        public static readonly Color Steel = new Color(0.78f, 0.83f, 0.9f);
        public static readonly Color Dim = new Color(0.42f, 0.45f, 0.52f);

        static Font Load(string path)
        {
            if (fonts.TryGetValue(path, out var f) && f != null) return f;
            f = Resources.Load<Font>(path);
            if (f == null) f = UIFactory.Font;
            fonts[path] = f;
            return f;
        }

        public static Text Style(Text t, Font font, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            return t;
        }

        static void ClearEffects(Text t)
        {
            foreach (var old in t.GetComponents<Shadow>()) Object.Destroy(old);
        }

        /// <summary>Crisp: a thin black edge and a hard black drop shadow, for small and medium text.</summary>
        public static Text Edge(Text t, float shadow = 2f)
        {
            ClearEffects(t);
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 1f);
            o.effectDistance = new Vector2(1.2f, -1.2f);
            o.useGraphicAlpha = true;
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.9f);
            s.effectDistance = new Vector2(shadow, -shadow);
            s.useGraphicAlpha = true;
            return t;
        }

        /// <summary>
        /// Arcade headline: bold italic, bright fill, thick black edge, hard coloured block shadow, black under it.
        /// </summary>
        public static Text ArcadeText(Text t, Color fill, Color shadowColor, float shadow = 4f, bool italic = true)
        {
            ClearEffects(t);
            t.font = Arcade;
            t.fontStyle = italic ? FontStyle.Italic : FontStyle.Normal;
            t.color = fill;
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 1f);
            o.effectDistance = new Vector2(2f, -2f);
            o.useGraphicAlpha = true;
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = shadowColor;
            s.effectDistance = new Vector2(shadow, -shadow);
            s.useGraphicAlpha = true;
            var s2 = t.gameObject.AddComponent<Shadow>();
            s2.effectColor = new Color(0f, 0f, 0f, 1f);
            s2.effectDistance = new Vector2(shadow + 2f, -shadow - 2f);
            s2.useGraphicAlpha = true;
            return t;
        }

        /// <summary>Kept for old callers.</summary>
        public static Text Jackpot(Text t, Color fill, Color shadowColor, float shadow = 5f) => ArcadeText(t, fill, shadowColor, shadow);
        public static Text Neon(Text t, Color c) => ArcadeText(t, Color.Lerp(c, Color.white, 0.6f), c, 3f, false);
        public static Text Glow(Text t, Color c, float distance = 3f, float alpha = 0.45f) => Edge(t);

        /// <summary>Seven-segment readout with a dim ghost of every segment behind the live digits.</summary>
        public static Text Digital(Transform parent, string name, string ghostPattern, int size, Color color, TextAnchor anchor,
            Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool fourteen = false)
        {
            var font = fourteen ? Fourteen : Seven;
            var ghost = UIFactory.Text(parent, name + "Ghost", ghostPattern, size, new Color(color.r, color.g, color.b, 0.14f), anchor, aMin, aMax, oMin, oMax, false, FontStyle.Normal);
            ghost.font = font;
            var live = UIFactory.Text(parent, name, "", size, color, anchor, aMin, aMax, oMin, oMax, false, FontStyle.Normal);
            live.font = font;
            var s = live.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.9f);
            s.effectDistance = new Vector2(2f, -2f);
            s.useGraphicAlpha = true;
            return live;
        }

        static Sprite scanlines;

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

        /// <summary>A generated frame plate from Resources/UI/Hud, nine-sliced with a border of the given fraction. Null if missing.</summary>
        public static Sprite PlateSprite(string name, float borderFrac = 0.24f)
        {
            string key = name + "|" + borderFrac.ToString("F2");
            if (plates.TryGetValue(key, out var s) && s != null) return s;
            var src = Resources.Load<Sprite>("UI/Hud/" + name);
            if (src == null) return null;
            float b = Mathf.Floor(Mathf.Min(src.rect.width, src.rect.height) * borderFrac);
            s = Sprite.Create(src.texture, src.rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.name = name + "-sliced";
            plates[key] = s;
            return s;
        }

        /// <summary>
        /// An instrument frame: the generated plate stretched nine-sliced over the rectangle (border rendered at
        /// about a quarter of the panel's short side), or a drawn dark panel with yellow and blue accent lines when
        /// the plate is missing. Returns the inner screen area for content.
        /// </summary>
        public static RectTransform Frame(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
            string plate, float width, float height, float screenInset = -1f)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            UIFactory.Anchor(holder, aMin, aMax, oMin, oMax);
            var h = holder.transform;
            float shortSide = Mathf.Min(width, height);
            float border = Mathf.Clamp(shortSide * 0.22f, 22f, 90f);
            var sprite = PlateSprite(plate);
            if (sprite != null)
            {
                var img = UIFactory.Panel(h, "Plate", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = Mathf.Max(0.2f, sprite.border.x / border);
            }
            else
            {
                UIFactory.Panel(h, "Back", Panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                UIFactory.Panel(h, "Screen", Screen, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
                UIFactory.Panel(h, "TopLine", Yellow, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -4f), new Vector2(0f, 0f));
                UIFactory.Panel(h, "BottomLine", Blue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 4f));
                border = 14f;
            }
            float inset = screenInset >= 0f ? screenInset : border * 0.8f;
            var content = new GameObject("Screen", typeof(RectTransform));
            content.transform.SetParent(h, false);
            UIFactory.Anchor(content, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
            return content.GetComponent<RectTransform>();
        }

        /// <summary>Bold section label: italic arcade type on a yellow tab with a black edge.</summary>
        public static Text Tab(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color? color = null)
        {
            var c = color ?? Yellow;
            var back = UIFactory.Panel(parent, name + "Tab", c, aMin, aMax, oMin, oMax);
            var t = UIFactory.Text(back.transform, name, label, 16, Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f), false);
            Style(t, Arcade, 15, Ink, FontStyle.Italic);
            return t;
        }

        /// <summary>A lit tile: bright fill with black text when on, dark with dim text when off (aviation annunciator).</summary>
        public static Image Tile(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, out Text text)
        {
            var back = UIFactory.Panel(parent, name, new Color(0.1f, 0.11f, 0.14f), aMin, aMax, oMin, oMax);
            text = UIFactory.Text(back.transform, "Label", label, 14, Dim, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
            Style(text, Arcade, 13, Dim);
            return back;
        }

        public static void SetTile(Image tile, Text text, bool on, Color color)
        {
            tile.color = on ? color : new Color(0.1f, 0.11f, 0.14f);
            text.color = on ? Ink : Dim;
        }
    }

    /// <summary>
    /// A flight-deck tape: a scrolling scale with numbers and ticks moving behind a fixed readout box, the way
    /// an airspeed or altitude tape does.
    /// </summary>
    public class Tape
    {
        RectTransform content;
        Text readout;
        Image readoutBox;
        float min, max, pxPerUnit, height;
        Color color;

        /// <summary>height is the tape's pixel height in reference resolution (needed for stretched anchors).</summary>
        public static Tape Build(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
            float min, float max, float major, string unit, Color color, float height, string format = "0")
        {
            var t = new Tape { min = min, max = max, color = color, height = height };
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            UIFactory.Anchor(holder, aMin, aMax, oMin, oMax);
            var h = holder.transform;
            UIFactory.Panel(h, "Back", UIStyle.Screen, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.Panel(h, "EdgeL", color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(2f, 0f));
            UIFactory.Panel(h, "EdgeR", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(0f, 0f));
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D));
            maskGo.transform.SetParent(h, false);
            UIFactory.Anchor(maskGo, Vector2.zero, Vector2.one, new Vector2(3f, 26f), new Vector2(-3f, -26f));
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(maskGo.transform, false);
            t.content = contentGo.GetComponent<RectTransform>();
            t.content.anchorMin = new Vector2(0f, 0.5f);
            t.content.anchorMax = new Vector2(1f, 0.5f);
            t.content.pivot = new Vector2(0.5f, 0f);
            t.pxPerUnit = (height * 0.9f) / (major * 4f);
            float total = (max - min) * t.pxPerUnit;
            t.content.sizeDelta = new Vector2(0f, total);
            t.content.anchoredPosition = Vector2.zero;
            float minor = major / 2f;
            int count = Mathf.RoundToInt((max - min) / minor);
            for (int i = 0; i <= count; i++)
            {
                float v = min + i * minor;
                bool isMajor = i % 2 == 0;
                float y = (v - min) * t.pxPerUnit;
                float len = isMajor ? 12f : 6f;
                UIFactory.Panel(t.content, "Tick", isMajor ? Color.white : new Color(1f, 1f, 1f, 0.5f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-4f - len, y - 1f), new Vector2(-4f, y + 1f));
                if (isMajor)
                {
                    var num = UIFactory.Text(t.content, "Num", v.ToString(format), 15, Color.white, TextAnchor.MiddleRight, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, y - 12f), new Vector2(-20f, y + 12f), false);
                    UIStyle.Style(num, UIStyle.Mono, 15, Color.white);
                }
            }
            t.readoutBox = UIFactory.Panel(h, "Readout", color, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(2f, -15f), new Vector2(-2f, 15f));
            var chev = UIFactory.Panel(h, "Chevron", color, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-2f, -10f), new Vector2(10f, 10f));
            chev.sprite = UISprites.Chevron;
            chev.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            t.readout = UIFactory.Text(t.readoutBox.transform, "Value", "", 17, UIStyle.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
            UIStyle.Style(t.readout, UIStyle.Mono, 17, UIStyle.Ink, FontStyle.Bold);
            var plate = UIFactory.Panel(h, "UnitPlate", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(2f, -24f), new Vector2(-2f, -2f));
            var unitText = UIFactory.Text(plate.transform, "Unit", unit, 12, UIStyle.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
            UIStyle.Style(unitText, UIStyle.Arcade, 11, UIStyle.Ink);
            return t;
        }

        public void Set(float value, string text = null)
        {
            value = Mathf.Clamp(value, min, max);
            content.anchoredPosition = new Vector2(0f, -(value - min) * pxPerUnit);
            readout.text = text ?? value.ToString("0");
        }

        public void SetColor(Color c)
        {
            color = c;
            readoutBox.color = c;
        }
    }
}
