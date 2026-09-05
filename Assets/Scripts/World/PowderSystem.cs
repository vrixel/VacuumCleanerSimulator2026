using System.Collections.Generic;
using UnityEngine;
using VCS.Core;

namespace VCS.World
{
    /// <summary>
    /// Cocoa powder spilled over the floors: one erasable layer per room. The nozzle clears a disc wherever it
    /// passes, so the path you vacuumed stays visible as clean floor through the powder. Each layer is a quad
    /// just above the floor with a generated RGBA texture (brown grains, alpha = how much powder is left);
    /// clearing edits the pixels and uploads the texture once per frame.
    /// </summary>
    public class PowderSystem : MonoBehaviour
    {
        public const float PxPerM = 36f;
        public const float SqmPerUnit = 1.5f;      // one "piece of mess" in the cleanliness count per 1.5 m² of powder
        public static readonly Color Cocoa = new Color(0.45f, 0.28f, 0.15f);

        public float TotalSqm { get; private set; }
        public float CleanedSqm { get; private set; }
        public int Units { get; private set; }
        public int UnitsCleaned { get; private set; }

        readonly List<PowderLayer> layers = new List<PowderLayer>();
        LevelBuilder level;

        public static PowderSystem Build(LevelBuilder level, Transform parent, IList<Rect> rooms, System.Random rng)
        {
            var go = new GameObject("Powder");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<PowderSystem>();
            ps.level = level;
            foreach (var r in rooms)
            {
                var layer = PowderLayer.Create(go.transform, r, rng);
                ps.layers.Add(layer);
                ps.TotalSqm += layer.Sqm;
            }
            ps.Units = Mathf.Max(1, Mathf.RoundToInt(ps.TotalSqm / SqmPerUnit));
            return ps;
        }

        /// <summary>Clears a disc of powder under the nozzle. Returns the square metres removed this call.</summary>
        public float Vacuum(Vector3 worldPos, float radius)
        {
            float sqm = 0f;
            foreach (var l in layers) sqm += l.Clear(worldPos, radius);
            if (sqm <= 0f) return 0f;
            CleanedSqm += sqm;
            int units = Mathf.Min(Units, Mathf.FloorToInt(CleanedSqm / SqmPerUnit));
            while (UnitsCleaned < units) { UnitsCleaned++; level.OnMessAbsorbed(); }
            return sqm;
        }

        void LateUpdate()
        {
            foreach (var l in layers) l.Upload();
        }
    }

    public class PowderLayer : MonoBehaviour
    {
        public float Sqm { get; private set; }

        Rect area;
        int w, h;
        Color32[] px;
        Texture2D tex;
        bool dirty;

        public static PowderLayer Create(Transform parent, Rect area, System.Random rng)
        {
            var go = new GameObject("Powder " + area.center);
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<PowderLayer>();
            l.area = area;
            l.w = Mathf.Max(8, Mathf.CeilToInt(area.width * PowderSystem.PxPerM));
            l.h = Mathf.Max(8, Mathf.CeilToInt(area.height * PowderSystem.PxPerM));
            l.Generate(rng);
            l.BuildQuad();
            return l;
        }

        // Splats and streaks of cocoa with noisy edges, darker grains and a few pale sugar specks.
        void Generate(System.Random rng)
        {
            var cov = new float[w * h];
            float ppm = PowderSystem.PxPerM;
            int splats = Mathf.RoundToInt(area.width * area.height * 0.035f) + 2;
            for (int s = 0; s < splats; s++)
            {
                float cx = (float)rng.NextDouble() * w, cy = (float)rng.NextDouble() * h;
                float r = (0.5f + (float)rng.NextDouble() * 1.5f) * ppm;
                bool streak = rng.NextDouble() < 0.35;
                float ang = (float)rng.NextDouble() * Mathf.PI;
                float stretch = streak ? 2.2f + (float)rng.NextDouble() * 1.5f : 1f + (float)rng.NextDouble() * 0.3f;
                float weight = 0.55f + (float)rng.NextDouble() * 0.5f;
                float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                float rx = r * stretch, ry = r;
                int x0 = Mathf.Max(0, (int)(cx - rx)), x1 = Mathf.Min(w - 1, (int)(cx + rx));
                int y0 = Mathf.Max(0, (int)(cy - rx)), y1 = Mathf.Min(h - 1, (int)(cy + rx));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = x - cx, dy = y - cy;
                        float u = (dx * cs + dy * sn) / rx, v = (-dx * sn + dy * cs) / ry;
                        float d = u * u + v * v;
                        if (d >= 1f) continue;
                        float f = 1f - d;
                        cov[y * w + x] += f * f * weight;
                    }
            }
            px = new Color32[w * h];
            float ox = (float)rng.NextDouble() * 50f, oy = (float)rng.NextDouble() * 50f;
            float sum = 0f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x / ppm, fy = y / ppm;
                    float edge = Mathf.PerlinNoise(ox + fx * 1.7f, oy + fy * 1.7f);
                    float grain = Mathf.PerlinNoise(ox + fx * 14f, oy + fy * 14f);
                    float fine = Mathf.PerlinNoise(ox + fx * 60f, oy + fy * 60f);
                    float c = cov[y * w + x] + (edge - 0.5f) * 0.45f;
                    // A dense core where the powder piled up, and a sprinkled dusting around it: individual
                    // grains (fine noise above a threshold) get thinner the further out they lie.
                    float core = Mathf.Clamp01((c - 0.30f) * 3f) * 0.88f;
                    float dusting = Mathf.Clamp01((c - 0.08f) * 4f) * (fine > 0.52f ? 0.55f : 0.12f);
                    float a = Mathf.Max(core, dusting) * (0.85f + 0.15f * fine);
                    if (a < 0.04f) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                    float shade = 0.8f + grain * 0.35f + (fine - 0.5f) * 0.2f;
                    Color col = PowderSystem.Cocoa * shade;
                    if (fine > 0.93f) col = Color.Lerp(col, new Color(0.7f, 0.58f, 0.44f), 0.7f);
                    px[y * w + x] = new Color32((byte)(Mathf.Clamp01(col.r) * 255f), (byte)(Mathf.Clamp01(col.g) * 255f), (byte)(Mathf.Clamp01(col.b) * 255f), (byte)(a * 255f));
                    sum += a;
                }
            Sqm = sum / (ppm * ppm);
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.name = "powder";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(px);
            tex.Apply(false, false);
        }

        void BuildQuad()
        {
            var mesh = new Mesh { name = "powder quad" };
            float hw = area.width * 0.5f, hd = area.height * 0.5f;
            mesh.vertices = new[] { new Vector3(-hw, 0f, -hd), new Vector3(hw, 0f, -hd), new Vector3(hw, 0f, hd), new Vector3(-hw, 0f, hd) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            transform.position = new Vector3(area.center.x, 0.012f, area.center.y);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            var m = Palette.Fade();
            m.mainTexture = tex;
            m.color = Color.white;
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
        }

        /// <summary>Erases a disc; returns the square metres of powder that were actually there.</summary>
        public float Clear(Vector3 worldPos, float radius)
        {
            float ppm = PowderSystem.PxPerM;
            float cx = (worldPos.x - area.xMin) * ppm, cy = (worldPos.z - area.yMin) * ppm;
            float r = radius * ppm;
            if (cx < -r || cy < -r || cx > w + r || cy > h + r) return 0f;
            int x0 = Mathf.Max(0, (int)(cx - r)), x1 = Mathf.Min(w - 1, (int)(cx + r));
            int y0 = Mathf.Max(0, (int)(cy - r)), y1 = Mathf.Min(h - 1, (int)(cy + r));
            float r2 = r * r, removed = 0f;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy > r2) continue;
                    int i = y * w + x;
                    byte a = px[i].a;
                    if (a == 0) continue;
                    removed += a / 255f;
                    px[i].a = 0;
                }
            if (removed > 0f) dirty = true;
            return removed / (ppm * ppm);
        }

        public void Upload()
        {
            if (!dirty) return;
            dirty = false;
            tex.SetPixels32(px);
            tex.Apply(false, false);
        }
    }
}
