using System.Collections.Generic;
using UnityEngine;
using VCS.World;

namespace VCS.CameraRig
{
    /// <summary>
    /// Sees through walls. Every wall between the camera and the vacuum (and between the camera and the bin, once
    /// the bag needs emptying) is swapped to a Fade material and dimmed to a pane of frosted glass; it comes back
    /// as soon as the line of sight is clear. Only the static boxes named "Wall" take part: furniture and mess have
    /// rigidbodies and are low enough to be seen over. Driven by <see cref="FollowCamera"/> after it has moved.
    /// </summary>
    public class WallFader
    {
        const float FadedAlpha = 0.16f;
        const float CastRadius = 0.45f;
        const float Speed = 6f;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        class Entry
        {
            public MeshRenderer Renderer;
            public Material Original;
            public float Alpha = 1f;
            public bool Wanted;
        }

        readonly Dictionary<Collider, Entry> entries = new Dictionary<Collider, Entry>();
        readonly List<Collider> gone = new List<Collider>();
        readonly RaycastHit[] hits = new RaycastHit[24];
        readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        static Material fadeMaterial;

        public int FadedCount { get; private set; }

        static Material FadeMaterial()
        {
            if (fadeMaterial == null)
            {
                fadeMaterial = Palette.Fade();
                fadeMaterial.name = "Wall fade";
                fadeMaterial.SetFloat("_Glossiness", 0.2f);
            }
            return fadeMaterial;
        }

        /// <summary>One step: walls on the camera-to-focus line fade in, the others come back; call once per frame.</summary>
        public void Tick(Vector3 camPos, Vector3 focus, Vector3? extraFocus, float dt)
        {
            foreach (var e in entries.Values) e.Wanted = false;
            Mark(camPos, focus);
            if (extraFocus.HasValue) Mark(camPos, extraFocus.Value);

            gone.Clear();
            int faded = 0;
            foreach (var kv in entries)
            {
                var e = kv.Value;
                if (e.Renderer == null) { gone.Add(kv.Key); continue; }
                float target = e.Wanted ? FadedAlpha : 1f;
                e.Alpha = Mathf.MoveTowards(e.Alpha, target, dt * Speed);
                if (!e.Wanted && e.Alpha >= 1f)
                {
                    Restore(e);
                    gone.Add(kv.Key);
                    continue;
                }
                if (e.Wanted) faded++;
                var c = e.Original.color;
                c.a = e.Alpha;
                block.SetColor(ColorId, c);
                e.Renderer.SetPropertyBlock(block);
            }
            foreach (var k in gone) entries.Remove(k);
            FadedCount = faded;
        }

        void Mark(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.05f) return;
            int n = Physics.SphereCastNonAlloc(from, CastRadius, d / len, hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = hits[i].collider;
                if (col == null || col.attachedRigidbody != null || col.gameObject.name != "Wall") continue;
                if (!entries.TryGetValue(col, out var e))
                {
                    var r = col.GetComponent<MeshRenderer>();
                    if (r == null) continue;
                    e = new Entry { Renderer = r, Original = r.sharedMaterial };
                    r.sharedMaterial = FadeMaterial();
                    entries[col] = e;
                }
                e.Wanted = true;
            }
        }

        static void Restore(Entry e)
        {
            if (e.Renderer == null) return;
            e.Renderer.SetPropertyBlock(null);
            e.Renderer.sharedMaterial = e.Original;
        }

        /// <summary>Puts every wall back (level rebuilt, or the camera left the chase mode).</summary>
        public void Clear()
        {
            foreach (var e in entries.Values) Restore(e);
            entries.Clear();
            FadedCount = 0;
        }
    }
}
