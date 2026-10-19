using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VCS.UI
{
    /// <summary>
    /// Exploration (explore/hot-chrome): the title lettering of the first Hot Chrome boards on live uGUI text.
    /// Each glyph quad is cut at a horizon line; above it the face runs pale blush to white, below it hot pink back
    /// to blush, so every line reads as polished chrome reflecting a pink sky. A sheen sweeps across the line every
    /// few seconds. The cherry outline, the stepped cherry extrusion, the dark base and the pink halo are drawn here
    /// too, each as ONE copy of the plain glyphs: stacked Shadow components double the mesh each time and blew
    /// Unity's 65000-vertex limit. Text smaller than MinSize keeps its flat colour.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChromeText : BaseMeshEffect
    {
        public const int MinSize = 18;
        public float horizon = 0.46f;
        public float period = 4.5f;
        public float sweep = 0.7f;
        public float depth = 4f;

        static readonly Color Cherry = new Color(0.30f, 0f, 0.11f, 1f);
        static readonly Color CherryDeep = new Color(0.12f, 0f, 0.04f, 1f);
        static readonly Color Halo = new Color(1f, 0.18f, 0.54f, 0.22f);

        static readonly Color Sky = Color.white;
        static readonly Color SkyLow = new Color(1f, 0.80f, 0.90f);
        static readonly Color Ground = new Color(1f, 0.30f, 0.62f);
        static readonly Color GroundLow = new Color(1f, 0.86f, 0.94f);

        readonly List<UIVertex> stream = new List<UIVertex>();
        readonly List<UIVertex> output = new List<UIVertex>();
        readonly List<Vector2> bands = new List<Vector2>();
        Text text;
        float phase;
        float sheenX = float.NaN;

        protected override void Awake()
        {
            base.Awake();
            text = GetComponent<Text>();
            phase = Random.value * period;
        }

        void Update()
        {
            if (text == null || text.fontSize < MinSize) return;
            float t = (Time.unscaledTime + phase) % period;
            bool active = t < sweep;
            if (active || !float.IsNaN(sheenX))
            {
                sheenX = active ? t / sweep : float.NaN;
                graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || text == null || text.fontSize < MinSize) return;
            stream.Clear();
            vh.GetUIVertexStream(stream);
            if (stream.Count < 6) return;

            // Line bands: glyph quads whose vertical ranges overlap share one horizon.
            bands.Clear();
            float xMin = float.MaxValue, xMax = float.MinValue;
            for (int i = 0; i + 5 < stream.Count; i += 6)
            {
                Range(i, out float lo, out float hi);
                xMin = Mathf.Min(xMin, stream[i].position.x);
                xMax = Mathf.Max(xMax, stream[i + 1].position.x);
                int b = FindBand(lo, hi);
                if (b < 0) bands.Add(new Vector2(lo, hi));
                else bands[b] = new Vector2(Mathf.Min(bands[b].x, lo), Mathf.Max(bands[b].y, hi));
            }

            output.Clear();
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dy = -1; dy <= 1; dy += 2) Copy(Halo, 5f * dx, 5f * dy);
            Copy(CherryDeep, depth + 2f, -depth - 2f);
            for (float k = depth; k > 0.1f; k -= 1.5f) Copy(Cherry, k, -k);
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dy = -1; dy <= 1; dy += 2) Copy(Cherry, 2f * dx, 2f * dy);

            for (int i = 0; i + 5 < stream.Count; i += 6)
            {
                // Text writes each quad as TL TR BR, BR BL TL.
                UIVertex tl = stream[i], tr = stream[i + 1], br = stream[i + 2], bl = stream[i + 4];
                Range(i, out float lo, out float hi);
                var band = bands[Mathf.Max(0, FindBand(lo, hi))];
                float h = Mathf.Lerp(band.x, band.y, horizon);
                float yt = tl.position.y, yb = bl.position.y;
                if (h > yb + 0.5f && h < yt - 0.5f)
                {
                    float k = (h - yb) / (yt - yb);
                    UIVertex ml = Lerp(bl, tl, k), mr = Lerp(br, tr, k);
                    UIVertex mlUp = ml, mrUp = mr;
                    Paint(ref ml, band, h, xMin, xMax, false);
                    Paint(ref mr, band, h, xMin, xMax, false);
                    Paint(ref mlUp, band, h, xMin, xMax, true);
                    Paint(ref mrUp, band, h, xMin, xMax, true);
                    Paint(ref tl, band, h, xMin, xMax, true);
                    Paint(ref tr, band, h, xMin, xMax, true);
                    Paint(ref bl, band, h, xMin, xMax, false);
                    Paint(ref br, band, h, xMin, xMax, false);
                    Quad(tl, tr, mrUp, mlUp);
                    Quad(ml, mr, br, bl);
                }
                else
                {
                    bool up = yb >= h;
                    Paint(ref tl, band, h, xMin, xMax, up);
                    Paint(ref tr, band, h, xMin, xMax, up);
                    Paint(ref br, band, h, xMin, xMax, up);
                    Paint(ref bl, band, h, xMin, xMax, up);
                    Quad(tl, tr, br, bl);
                }
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
        }

        void Copy(Color c, float x, float y)
        {
            Color32 col = c;
            for (int i = 0; i < stream.Count; i++)
            {
                var v = stream[i];
                v.position += new Vector3(x, y, 0f);
                v.color = new Color32(col.r, col.g, col.b, (byte)(col.a * v.color.a / 255));
                output.Add(v);
            }
        }

        void Range(int i, out float lo, out float hi)
        {
            lo = Mathf.Min(stream[i].position.y, stream[i + 4].position.y);
            hi = Mathf.Max(stream[i].position.y, stream[i + 4].position.y);
        }

        int FindBand(float lo, float hi)
        {
            for (int b = 0; b < bands.Count; b++)
            {
                float overlap = Mathf.Min(hi, bands[b].y) - Mathf.Max(lo, bands[b].x);
                if (overlap > 0.4f * Mathf.Min(hi - lo, bands[b].y - bands[b].x)) return b;
            }
            return -1;
        }

        void Paint(ref UIVertex v, Vector2 band, float h, float xMin, float xMax, bool sky)
        {
            float y = v.position.y;
            Color c = sky
                ? Color.Lerp(SkyLow, Sky, Mathf.InverseLerp(h, band.y, y))
                : Color.Lerp(Ground, GroundLow, Mathf.InverseLerp(h, band.x, y));
            if (!float.IsNaN(sheenX) && xMax > xMin)
            {
                // A slanted bright bar, leaning like the italic.
                float u = (v.position.x - xMin - (y - band.x) * 0.35f) / (xMax - xMin);
                float d = (u - Mathf.Lerp(-0.2f, 1.2f, sheenX)) / 0.09f;
                c = Color.Lerp(c, Color.white, Mathf.Exp(-d * d) * 0.95f);
            }
            byte a = v.color.a;
            v.color = c;
            v.color.a = a;
        }

        void Quad(UIVertex a, UIVertex b, UIVertex c, UIVertex d)
        {
            output.Add(a); output.Add(b); output.Add(c);
            output.Add(c); output.Add(d); output.Add(a);
        }

        static UIVertex Lerp(UIVertex a, UIVertex b, float t)
        {
            var v = a;
            v.position = Vector3.Lerp(a.position, b.position, t);
            v.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
            v.color = Color32.Lerp(a.color, b.color, t);
            return v;
        }
    }
}
