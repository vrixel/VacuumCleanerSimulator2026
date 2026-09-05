using System.Collections.Generic;
using UnityEngine;

namespace VCS.World
{
    /// <summary>
    /// Runtime mesh builders: solids of revolution, tubes along splines (optionally corrugated), rounded boxes.
    /// Conventions: Y up, metres, outward normals, Unity clockwise winding.
    /// </summary>
    public static class MeshKit
    {
        /// <summary>
        /// Revolves a (radius, height) profile around Y. Consecutive duplicate points create a hard edge.
        /// With caps, discs close the first and last ring when their radius is not zero.
        /// </summary>
        public static Mesh Revolve(IList<Vector2> profile, int segments = 32, string name = "revolve", bool caps = true)
        {
            int rings = profile.Count;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int i = 0; i < rings; i++)
            {
                Vector2 p = profile[i];
                Vector2 prev = i > 0 ? profile[i - 1] : p;
                Vector2 next = i < rings - 1 ? profile[i + 1] : p;
                Vector2 tPrev = p - prev, tNext = next - p;
                Vector2 t;
                if (tPrev.sqrMagnitude < 1e-10f) t = tNext;
                else if (tNext.sqrMagnitude < 1e-10f) t = tPrev;
                else t = tPrev.normalized + tNext.normalized;
                if (t.sqrMagnitude < 1e-10f) t = Vector2.up;
                t.Normalize();
                Vector2 n2 = new Vector2(t.y, -t.x);
                for (int s = 0; s <= segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    verts.Add(new Vector3(ca * p.x, p.y, sa * p.x));
                    norms.Add(new Vector3(n2.x * ca, n2.y, n2.x * sa).normalized);
                    uvs.Add(new Vector2(s / (float)segments, i / (float)Mathf.Max(1, rings - 1)));
                }
            }
            for (int i = 0; i < rings - 1; i++)
            {
                if ((profile[i] - profile[i + 1]).sqrMagnitude < 1e-10f) continue;
                for (int s = 0; s < segments; s++)
                {
                    int a = i * (segments + 1) + s, b = a + 1, c = a + segments + 1, d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
            if (caps)
            {
                CapRing(verts, norms, uvs, tris, profile[0], segments, false);
                CapRing(verts, norms, uvs, tris, profile[rings - 1], segments, true);
            }
            return Build(name, verts, norms, uvs, tris);
        }

        static void CapRing(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Vector2 ring, int segments, bool top)
        {
            if (ring.x <= 0.0005f) return;
            Vector3 normal = top ? Vector3.up : Vector3.down;
            int center = v.Count;
            v.Add(new Vector3(0f, ring.y, 0f)); n.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
            int start = v.Count;
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a) * ring.x, ring.y, Mathf.Sin(a) * ring.x));
                n.Add(normal);
                uv.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(a), 0.5f + 0.5f * Mathf.Sin(a)));
            }
            for (int s = 0; s < segments; s++)
            {
                if (top) { t.Add(center); t.Add(start + s + 1); t.Add(start + s); }
                else { t.Add(center); t.Add(start + s); t.Add(start + s + 1); }
            }
        }

        /// <summary>Catmull-Rom spline through the control points, clamped at both ends.</summary>
        public static List<Vector3> Spline(IList<Vector3> c, int perSegment = 8)
        {
            var pts = new List<Vector3>();
            if (c.Count < 2) { pts.AddRange(c); return pts; }
            for (int i = 0; i < c.Count - 1; i++)
            {
                Vector3 p0 = c[Mathf.Max(i - 1, 0)], p1 = c[i], p2 = c[i + 1], p3 = c[Mathf.Min(i + 2, c.Count - 1)];
                int steps = i == c.Count - 2 ? perSegment + 1 : perSegment;
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)perSegment, t2 = t * t, t3 = t2 * t;
                    pts.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            return pts;
        }

        /// <summary>Tube of the given radius along an already-sampled path. ribAmp/ribFreq make a corrugated hose.</summary>
        public static Mesh Tube(IList<Vector3> path, float radius, int radial = 12, float ribAmp = 0f, float ribFreq = 0f, string name = "tube")
        {
            int n = path.Count;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            Vector3 normal = Vector3.zero;
            Vector3 firstT = Vector3.forward, lastT = Vector3.forward;
            for (int i = 0; i < n; i++)
            {
                Vector3 t;
                if (n == 1) t = Vector3.up;
                else if (i == 0) t = path[1] - path[0];
                else if (i == n - 1) t = path[n - 1] - path[n - 2];
                else t = path[i + 1] - path[i - 1];
                if (t.sqrMagnitude < 1e-10f) t = Vector3.up;
                t.Normalize();
                if (i == 0)
                {
                    normal = Vector3.Cross(t, Vector3.up);
                    if (normal.sqrMagnitude < 1e-4f) normal = Vector3.Cross(t, Vector3.right);
                    normal.Normalize();
                    firstT = t;
                }
                else
                {
                    normal -= t * Vector3.Dot(normal, t);
                    if (normal.sqrMagnitude < 1e-6f) normal = Vector3.Cross(t, Vector3.up);
                    normal.Normalize();
                }
                lastT = t;
                Vector3 bi = Vector3.Cross(normal, t);
                float r = radius * (1f + ribAmp * Mathf.Sin(i * ribFreq));
                for (int s = 0; s <= radial; s++)
                {
                    float a = s / (float)radial * Mathf.PI * 2f;
                    Vector3 dir = normal * Mathf.Cos(a) + bi * Mathf.Sin(a);
                    verts.Add(path[i] + dir * r);
                    norms.Add(dir);
                    uvs.Add(new Vector2(s / (float)radial, i / (float)Mathf.Max(1, n - 1)));
                }
            }
            for (int i = 0; i < n - 1; i++)
            for (int s = 0; s < radial; s++)
            {
                int a = i * (radial + 1) + s, b = a + 1, c = a + radial + 1, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
            if (n >= 2)
            {
                CapTube(verts, norms, uvs, tris, 0, radial, path[0], -firstT, false);
                CapTube(verts, norms, uvs, tris, (n - 1) * (radial + 1), radial, path[n - 1], lastT, true);
            }
            return Build(name, verts, norms, uvs, tris);
        }

        static void CapTube(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, int ringStart, int radial, Vector3 center, Vector3 normal, bool end)
        {
            int c = v.Count;
            v.Add(center); n.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
            int start = v.Count;
            for (int s = 0; s <= radial; s++)
            {
                v.Add(v[ringStart + s]); n.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
            }
            for (int s = 0; s < radial; s++)
            {
                if (end) { t.Add(c); t.Add(start + s + 1); t.Add(start + s); }
                else { t.Add(c); t.Add(start + s); t.Add(start + s + 1); }
            }
        }

        /// <summary>Box with rounded vertical edges and a small top bevel. Origin at the bottom centre, extends up to h.</summary>
        public static Mesh RoundedBox(float w, float h, float d, float r, int cornerSegs = 4, float bevel = 0.012f, string name = "rbox")
        {
            r = Mathf.Min(r, Mathf.Min(w, d) * 0.5f - 0.001f);
            bevel = Mathf.Min(bevel, h * 0.4f, r * 0.9f);
            var o0 = RoundedRect(w, d, r, cornerSegs);
            var o1 = RoundedRect(w - 2f * bevel, d - 2f * bevel, Mathf.Max(0.001f, r - bevel), cornerSegs);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float[] ys = { 0f, h - bevel, h };
            List<Vector2>[] outlines = { o0, o0, o1 };
            int m = o0.Count;
            for (int ring = 0; ring < 3; ring++)
            for (int i = 0; i <= m; i++)
            {
                var p = outlines[ring][i % m];
                verts.Add(new Vector3(p.x, ys[ring], p.y));
                uvs.Add(new Vector2(i / (float)m, ring / 2f));
            }
            for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i < m; i++)
            {
                int a = ring * (m + 1) + i, b = a + 1, c = a + m + 1, dd = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(dd);
            }
            CapPoly(verts, uvs, tris, o1, h, true);
            CapPoly(verts, uvs, tris, o0, 0f, false);
            return Build(name, verts, null, uvs, tris);
        }

        static void CapPoly(List<Vector3> v, List<Vector2> uv, List<int> t, List<Vector2> outline, float y, bool top)
        {
            int center = v.Count;
            v.Add(new Vector3(0f, y, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
            int start = v.Count;
            int m = outline.Count;
            for (int i = 0; i <= m; i++)
            {
                var p = outline[i % m];
                v.Add(new Vector3(p.x, y, p.y)); uv.Add(new Vector2(0.5f + p.x, 0.5f + p.y));
            }
            for (int i = 0; i < m; i++)
            {
                if (top) { t.Add(center); t.Add(start + i + 1); t.Add(start + i); }
                else { t.Add(center); t.Add(start + i); t.Add(start + i + 1); }
            }
        }

        // Outline of a rounded rectangle in the XZ plane. On the +x side the points advance toward +z, which is
        // what the side-face winding above expects for outward normals.
        static List<Vector2> RoundedRect(float w, float d, float r, int segs)
        {
            var pts = new List<Vector2>();
            float hw = w * 0.5f - r, hd = d * 0.5f - r;
            Vector2[] centers = { new Vector2(hw, hd), new Vector2(-hw, hd), new Vector2(-hw, -hd), new Vector2(hw, -hd) };
            for (int k = 0; k < 4; k++)
            {
                for (int i = 0; i <= segs; i++)
                {
                    float a = (k * 90f + i * 90f / segs) * Mathf.Deg2Rad;
                    pts.Add(centers[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            }
            return pts;
        }

        static Mesh Build(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            if (n != null && n.Count == v.Count) m.SetNormals(n);
            else m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static GameObject Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }
    }
}
