using System.Collections.Generic;
using UnityEngine;
using VCS.World;

namespace VCS.Player
{
    /// <summary>
    /// Real machines in the collection. Meshes downloaded from Objaverse (the Hugging Face mirror of Sketchfab's
    /// Creative Commons models), decimated to about 4k faces by tools/lowpoly.py into Resources/Models, and listed
    /// after the eight built models. Every entry carries its CC-BY credit for docs/CREDITS.md (the garage shows only
    /// the game's own name and tagline). Names are the game's own; no brand is claimed.
    /// </summary>
    public static class ImportedVacuums
    {
        class Entry
        {
            public string Id, Model, Name, Tagline, Credit;
            public float Size = 0.6f;      // largest horizontal extent, metres
            /// <summary>Normalise by height (metres) instead of by horizontal extent: for a mesh whose raw bounding
            /// box is dominated by an outstretched hose/wand rather than the body (the two Henry-inspired models,
            /// measured 2026-09-08: the wand made the body come out at 22 cm, half its neighbours' height). 0 = off,
            /// use Size as before.</summary>
            public float TargetHeight;
            public float Yaw;              // degrees to turn the mesh so its head faces +z, the driving direction (read off tools/museum.ps1 top views;
                                           // Wanda was -90 off, driving handle first, caught on the 2026-09-08 sheet)
            public bool Cordless;
            public Vector3 Nozzle = new Vector3(0f, 0.05f, 0.35f);   // the red ball of the museum sheet: on the floor tool for hose machines,
                                                                    // the front edge of the body otherwise
            public float Height = 0.6f;
            public float Speed = 7f, Hop = 6f, Bag = 100f;
        }

        static readonly Entry[] Entries =
        {
            new Entry { Id = "m_redcanister", Model = "henry", Name = "Hubert the Grin", Tagline = "A smile, a hose, a bag the size of a pillow.",
                        Credit = "Model: Henry Vacuum by rhcreations (CC BY 4.0, Sketchfab)", TargetHeight = 0.86f, Height = 0.86f, Nozzle = new Vector3(0.22f, 0.12f, 1.08f), Bag = 160f, Speed = 6.5f },
            new Entry { Id = "m_cyclone", Model = "dyson_upright", Name = "Baron Vortex", Tagline = "Ball, bin, no bag, no mercy.",
                        Credit = "Model: Upright Dyson Vacuum Cleaner by rhcreations (CC BY 4.0, Sketchfab)", TargetHeight = 1.30f, Height = 1.30f, Nozzle = new Vector3(0f, 0.07f, 0.21f), Speed = 7.5f, Hop = 7f },
            new Entry { Id = "m_aquastick", Model = "philips_aquatrio", Name = "Sir Mops-a-Lot", Tagline = "Vacuums, mops, judges.",
                        Credit = "Model: PHILIPS AquaTrio Pro by artemtem (CC BY 4.0, Sketchfab)", TargetHeight = 1.32f, Height = 1.32f, Nozzle = new Vector3(0f, 0.05f, 0.17f), Cordless = true, Speed = 7.5f, Bag = 60f },
            new Entry { Id = "m_yellowdrum", Model = "vacuum_4k", Name = "Big Bertha", Tagline = "Workshop grade. Eats screws for breakfast.",
                        Credit = "Model: Vacuum Cleaner by rescue3d (CC BY 4.0, Sketchfab)", TargetHeight = 1.45f, Height = 1.45f, Yaw = -29f, Nozzle = new Vector3(0f, 0.14f, 1.01f), Bag = 220f, Speed = 5.5f },
            new Entry { Id = "m_greystick", Model = "sixth_hm", Name = "Twiglet", Tagline = "Student project. Surprisingly hungry.",
                        Credit = "Model: Sixth HM XYZ - A vacuum cleaner by nimzuk (CC BY 4.0, Sketchfab)", TargetHeight = 1.30f, Height = 1.30f, Yaw = -90f, Nozzle = new Vector3(0f, 0.04f, 0.10f), Speed = 7f, Bag = 50f },
            new Entry { Id = "m_wand", Model = "vacuum_20k", Name = "Wanda", Tagline = "Forty minutes of battery, forty years of dust.",
                        Credit = "Model: Vacuum Cleaner by kikumi (CC BY 4.0, Sketchfab)", TargetHeight = 1.20f, Height = 1.20f, Yaw = -90f, Nozzle = new Vector3(0f, 0.18f, 0.63f), Cordless = true, Speed = 8f, Bag = 45f },
            new Entry { Id = "m_redsled", Model = "vacuum_82k", Name = "Monsieur Traineau", Tagline = "Un traineau. Glisse, aspire, ne dit rien.",
                        Credit = "Model: vacuum cleaner by huseyinCG (CC BY 4.0, Sketchfab)", TargetHeight = 1.35f, Height = 1.35f, Yaw = 90f, Nozzle = new Vector3(0f, 0.09f, 0.75f), Bag = 140f, Speed = 7f },
            new Entry { Id = "m_bluedrum", Model = "canister_a", Name = "Bluebarrel", Tagline = "Compact, cheerful, slightly too loud.",
                        Credit = "Model: Vacuum Cleaner by snowykov (CC BY 4.0, Sketchfab)", TargetHeight = 0.95f, Height = 0.95f, Nozzle = new Vector3(0f, 0.10f, 0.60f), Bag = 120f, Speed = 6.5f },
            new Entry { Id = "m_greyrobot", Model = "robvac", Name = "Bumper", Tagline = "Bumps into everything. On purpose.",
                        Credit = "Model: Robot vacuum Cleaner Rob-vac by darkfrei (CC BY 4.0, Sketchfab)", Size = 0.95f, Height = 0.25f, Nozzle = new Vector3(0f, 0.07f, 0.38f), Cordless = true, Speed = 6f, Hop = 4.5f, Bag = 40f },
            new Entry { Id = "m_roundone", Model = "roomba_888", Name = "Puck", Tagline = "Eight hundred and eighty-eight polygons of patience.",
                        Credit = "Model: Low-poly Roomba by Seats (CC BY 4.0, Sketchfab)", Size = 0.95f, Height = 0.13f, Nozzle = new Vector3(0f, 0.06f, 0.38f), Cordless = true, Speed = 6.5f, Hop = 4.5f, Bag = 40f },
            new Entry { Id = "m_littlered", Model = "henry_lowpoly", Name = "Hubert Junior", Tagline = "Five hundred polygons and a grin.",
                        Credit = "Model: Low Poly \"Henry Hoover\" Vacuum Cleaner by TheoClarke (CC BY 4.0, Sketchfab)", TargetHeight = 0.80f, Height = 0.80f, Nozzle = new Vector3(0f, 0.10f, 0.84f), Bag = 150f, Speed = 6.5f },
        };

        public static void AddTo(List<VacuumSpec> all)
        {
            int n = 0;
            foreach (var e in Entries)
            {
                var entry = e;
                all.Add(new VacuumSpec
                {
                    Id = entry.Id, Name = entry.Name, Tagline = entry.Tagline, Credit = entry.Credit, Imported = true,
                    Speed = entry.Speed, Hop = entry.Hop, BagCapacity = entry.Bag, Cordless = entry.Cordless,
                    NozzleLocal = entry.Nozzle, Height = entry.Height, ModelCode = "GUEST-" + (++n).ToString("00"),
                    Accent = new Color(0.85f, 0.85f, 0.9f), Build = (g, s) => Build(entry, g),
                });
            }
        }

        /// <summary>Instantiates the imported mesh and normalises it: size, base on the floor, centred, facing +z.</summary>
        static void Build(Entry e, Transform g)
        {
            var prefab = Resources.Load<GameObject>("Models/" + e.Model);
            if (prefab == null)
            {
                Debug.LogWarning("[VCS] Imported model missing: " + e.Model);
                PropFactory.Prim(PrimitiveType.Cube, g, new Vector3(0f, 0.25f, 0f), new Vector3(0.4f, 0.5f, 0.4f), Color.gray, "Missing", false);
                return;
            }
            var inst = Object.Instantiate(prefab, g);
            inst.name = e.Model;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.Euler(0f, e.Yaw, 0f);
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float k;
            if (e.TargetHeight > 0f) k = b.size.y > 1e-9f ? e.TargetHeight / b.size.y : 1f;
            else { float horiz = Mathf.Max(b.size.x, b.size.z); k = horiz > 1e-9f ? e.Size / horiz : 1f; }
            inst.transform.localScale = Vector3.one * k;
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            inst.transform.position += g.position - new Vector3(b.center.x, b.min.y, b.center.z);
            foreach (var r in rs)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }
    }
}
