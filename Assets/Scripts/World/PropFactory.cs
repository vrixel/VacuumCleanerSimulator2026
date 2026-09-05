using System.Collections.Generic;
using UnityEngine;

namespace VCS.World
{
    public struct DebrisSpec
    {
        public int SizeClass;
        public int Points;
        public float Volume;
        public float Mass;
        public bool IsMess;

        public DebrisSpec(int sizeClass, int points, float volume, float mass, bool isMess)
        {
            SizeClass = sizeClass; Points = points; Volume = volume; Mass = mass; IsMess = isMess;
        }
    }

    /// <summary>
    /// Builds every prop out of Unity primitives. No art assets: everything is boxes, spheres, capsules and cylinders.
    /// Local convention: the root sits on the floor (y = 0), parts are placed above it.
    /// </summary>
    public static class PropFactory
    {
        static readonly Dictionary<DebrisKind, DebrisSpec> specs = new Dictionary<DebrisKind, DebrisSpec>
        {
            { DebrisKind.Crumb,     new DebrisSpec(1, 5, 0.4f, 0.05f, true) },
            { DebrisKind.Dust,      new DebrisSpec(1, 5, 0.8f, 0.03f, true) },
            { DebrisKind.Cereal,    new DebrisSpec(1, 5, 0.4f, 0.04f, true) },
            { DebrisKind.Coin,      new DebrisSpec(1, 25, 0.3f, 0.10f, true) },
            { DebrisKind.Leaf,      new DebrisSpec(1, 5, 0.5f, 0.02f, true) },
            { DebrisKind.Sock,      new DebrisSpec(2, 20, 2.5f, 0.20f, true) },
            { DebrisKind.Brick,     new DebrisSpec(2, 15, 1.5f, 0.15f, true) },
            { DebrisKind.Ball,      new DebrisSpec(2, 20, 3.0f, 0.30f, true) },
            { DebrisKind.PaperRoll, new DebrisSpec(2, 15, 3.0f, 0.25f, true) },
            { DebrisKind.Book,      new DebrisSpec(2, 15, 2.5f, 0.60f, true) },
            { DebrisKind.Plant,     new DebrisSpec(3, 100, 10f, 6f, false) },
            { DebrisKind.Lamp,      new DebrisSpec(3, 100, 8f, 4f, false) },
            { DebrisKind.Stool,     new DebrisSpec(3, 120, 10f, 5f, false) },
            { DebrisKind.Chair,     new DebrisSpec(3, 150, 12f, 7f, false) },
            { DebrisKind.Table,     new DebrisSpec(4, 300, 25f, 25f, false) },
            { DebrisKind.Couch,     new DebrisSpec(4, 500, 40f, 50f, false) },
            { DebrisKind.Tv,        new DebrisSpec(4, 400, 15f, 9f, false) },
            { DebrisKind.Fridge,    new DebrisSpec(5, 1000, 60f, 90f, false) },
            { DebrisKind.Bed,       new DebrisSpec(5, 1500, 80f, 120f, false) },
            { DebrisKind.Toilet,    new DebrisSpec(5, 800, 30f, 40f, false) },
            { DebrisKind.Bathtub,   new DebrisSpec(5, 1200, 70f, 80f, false) },
        };

        static readonly Color[] Brights = { Palette.Red, Palette.Blue, Palette.Yellow, Palette.Green, Palette.Orange, Palette.Pink, Palette.Purple, Palette.Teal };

        public static DebrisSpec Spec(DebrisKind kind) => specs[kind];

        public static string EatLabel(int power)
        {
            switch (power)
            {
                case 1: return "crumbs, dust, cereal, coins and leaves";
                case 2: return "socks, bricks, balls, books and paper rolls";
                case 3: return "chairs, stools, lamps and plants";
                case 4: return "tables, couches and TVs";
                default: return "EVERYTHING. Yes, the toilet.";
            }
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Color color,
            string name = null, bool withCollider = true, Quaternion? localRot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name ?? type.ToString();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = Palette.Lit(color);
            if (!withCollider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.Destroy(c);
            }
            return go;
        }

        public static GameObject StaticBox(Transform parent, Vector3 center, Vector3 size, Color color, string name, float yRot = 0f)
        {
            return Prim(PrimitiveType.Cube, parent, center, size, color, name, true, Quaternion.Euler(0f, yRot, 0f));
        }

        public static Debris Spawn(DebrisKind kind, Vector3 pos, Quaternion rot, Transform parent, int colorSeed)
        {
            var spec = specs[kind];
            var root = new GameObject(kind.ToString());
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, rot);
            var rng = new System.Random(colorSeed);
            Color puff = BuildParts(kind, root.transform, rng);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = spec.Mass;
            rb.linearDamping = spec.SizeClass <= 2 ? 0.4f : 0.15f;
            rb.angularDamping = spec.SizeClass <= 2 ? 0.5f : 1.0f;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            var d = root.AddComponent<Debris>();
            d.Kind = kind;
            d.SizeClass = spec.SizeClass;
            d.Points = spec.Points;
            d.Volume = spec.Volume;
            d.Mass = spec.Mass;
            d.CountsAsMess = spec.IsMess;
            d.ColorSeed = colorSeed;
            d.PuffColor = puff;
            d.Rb = rb;
            if (spec.SizeClass >= 3) root.AddComponent<TipOverTracker>();
            if (spec.IsMess) VCS.UI.RadarView.Marker(root.transform, spec.SizeClass == 1 ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.2f), 0.55f, 20f);
            return d;
        }

        static Color Pick(System.Random rng, params Color[] options) => options[rng.Next(options.Length)];

        static Color BuildParts(DebrisKind kind, Transform t, System.Random rng)
        {
            switch (kind)
            {
                case DebrisKind.Crumb:
                {
                    var c = Pick(rng, new Color(0.55f, 0.35f, 0.15f), new Color(0.72f, 0.52f, 0.25f), new Color(0.85f, 0.70f, 0.40f));
                    Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.06f, 0f), Vector3.one * 0.12f, c, "Crumb");
                    return c;
                }
                case DebrisKind.Dust:
                {
                    var c = Pick(rng, new Color(0.62f, 0.62f, 0.64f), new Color(0.70f, 0.68f, 0.66f));
                    Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.12f, 0f), Vector3.one * 0.24f, c, "Dust");
                    Prim(PrimitiveType.Sphere, t, new Vector3(0.09f, 0.08f, 0.05f), Vector3.one * 0.14f, c, "DustBit", false);
                    Prim(PrimitiveType.Sphere, t, new Vector3(-0.08f, 0.07f, -0.06f), Vector3.one * 0.12f, c, "DustBit", false);
                    return c;
                }
                case DebrisKind.Cereal:
                {
                    var c = Pick(rng, Palette.Orange, new Color(0.9f, 0.7f, 0.3f), new Color(0.6f, 0.35f, 0.2f));
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.025f, 0f), new Vector3(0.14f, 0.025f, 0.14f), c, "Cereal");
                    return c;
                }
                case DebrisKind.Coin:
                {
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.012f, 0f), new Vector3(0.18f, 0.012f, 0.18f), Palette.Gold, "Coin");
                    return Palette.Gold;
                }
                case DebrisKind.Leaf:
                {
                    var c = Pick(rng, Palette.Green, Palette.Orange, new Color(0.75f, 0.55f, 0.15f), new Color(0.55f, 0.3f, 0.1f));
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.012f, 0f), new Vector3(0.28f, 0.02f, 0.18f), c, "Leaf");
                    return c;
                }
                case DebrisKind.Sock:
                {
                    var c = Pick(rng, Brights);
                    Prim(PrimitiveType.Capsule, t, new Vector3(0f, 0.08f, 0f), new Vector3(0.16f, 0.16f, 0.16f), c, "Sock", true, Quaternion.Euler(90f, 0f, 0f));
                    Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.08f, 0.12f), Vector3.one * 0.15f, Palette.White, "SockToe", false);
                    return c;
                }
                case DebrisKind.Brick:
                {
                    var c = Pick(rng, Palette.Red, Palette.Blue, Palette.Yellow, Palette.Green);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.06f, 0f), new Vector3(0.24f, 0.12f, 0.12f), c, "Brick");
                    Prim(PrimitiveType.Cylinder, t, new Vector3(-0.06f, 0.13f, 0f), new Vector3(0.06f, 0.012f, 0.06f), c, "Stud", false);
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0.06f, 0.13f, 0f), new Vector3(0.06f, 0.012f, 0.06f), c, "Stud", false);
                    return c;
                }
                case DebrisKind.Ball:
                {
                    var c = Pick(rng, Brights);
                    Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.16f, 0f), Vector3.one * 0.32f, c, "Ball");
                    return c;
                }
                case DebrisKind.PaperRoll:
                {
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.11f, 0f), new Vector3(0.22f, 0.11f, 0.22f), Palette.White, "Roll", true, Quaternion.Euler(0f, 0f, 90f));
                    return Palette.White;
                }
                case DebrisKind.Book:
                {
                    var c = Pick(rng, Brights);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.025f, 0f), new Vector3(0.32f, 0.05f, 0.24f), c, "Book");
                    Prim(PrimitiveType.Cube, t, new Vector3(0.02f, 0.025f, 0f), new Vector3(0.30f, 0.04f, 0.23f), Palette.White, "Pages", false);
                    return c;
                }
                case DebrisKind.Plant:
                {
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.2f, 0f), new Vector3(0.42f, 0.2f, 0.42f), Palette.Terracotta, "Pot");
                    Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.85f, 0f), new Vector3(0.8f, 0.75f, 0.8f), Palette.Green, "Leaves");
                    Prim(PrimitiveType.Sphere, t, new Vector3(0.25f, 0.65f, 0.1f), Vector3.one * 0.4f, new Color(0.25f, 0.6f, 0.3f), "Leaf", false);
                    return Palette.Green;
                }
                case DebrisKind.Lamp:
                {
                    var shade = new Color(1f, 0.9f, 0.6f);
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.02f, 0.36f), Palette.Black, "Base");
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.72f, 0f), new Vector3(0.06f, 0.7f, 0.06f), Palette.Black, "Pole");
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.55f, 0f), new Vector3(0.5f, 0.18f, 0.5f), shade, "Shade");
                    return shade;
                }
                case DebrisKind.Stool:
                {
                    var c = Pick(rng, Palette.Red, Palette.Teal, Palette.Yellow);
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.5f, 0f), new Vector3(0.42f, 0.03f, 0.42f), c, "Seat");
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 120f * Mathf.Deg2Rad;
                        Prim(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * 0.15f, 0.25f, Mathf.Sin(a) * 0.15f), new Vector3(0.05f, 0.5f, 0.05f), Palette.DarkWood, "Leg");
                    }
                    return c;
                }
                case DebrisKind.Chair:
                {
                    var cushion = Pick(rng, Palette.Red, Palette.Blue, Palette.Green, Palette.Orange);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.06f, 0.45f), Palette.DarkWood, "Seat");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.49f, 0f), new Vector3(0.40f, 0.04f, 0.40f), cushion, "Cushion", false);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.72f, -0.2f), new Vector3(0.45f, 0.5f, 0.06f), Palette.DarkWood, "Back");
                    for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        Prim(PrimitiveType.Cube, t, new Vector3(sx * 0.19f, 0.225f, sz * 0.19f), new Vector3(0.05f, 0.45f, 0.05f), Palette.DarkWood, "Leg");
                    return cushion;
                }
                case DebrisKind.Table:
                {
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.75f, 0f), new Vector3(1.6f, 0.06f, 0.9f), Palette.LightWood, "Top");
                    for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        Prim(PrimitiveType.Cube, t, new Vector3(sx * 0.72f, 0.375f, sz * 0.38f), new Vector3(0.06f, 0.75f, 0.06f), Palette.DarkWood, "Leg");
                    return Palette.LightWood;
                }
                case DebrisKind.Couch:
                {
                    var fabric = Pick(rng, new Color(0.25f, 0.45f, 0.65f), new Color(0.65f, 0.3f, 0.3f), new Color(0.45f, 0.55f, 0.35f));
                    var light = Color.Lerp(fabric, Color.white, 0.2f);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.225f, 0f), new Vector3(2.0f, 0.45f, 0.9f), fabric, "Base");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.7f, -0.33f), new Vector3(2.0f, 0.5f, 0.25f), fabric, "Back");
                    Prim(PrimitiveType.Cube, t, new Vector3(-0.9f, 0.325f, 0f), new Vector3(0.25f, 0.65f, 0.9f), fabric, "Arm");
                    Prim(PrimitiveType.Cube, t, new Vector3(0.9f, 0.325f, 0f), new Vector3(0.25f, 0.65f, 0.9f), fabric, "Arm");
                    Prim(PrimitiveType.Cube, t, new Vector3(-0.42f, 0.52f, 0.1f), new Vector3(0.8f, 0.15f, 0.6f), light, "Cushion", false);
                    Prim(PrimitiveType.Cube, t, new Vector3(0.42f, 0.52f, 0.1f), new Vector3(0.8f, 0.15f, 0.6f), light, "Cushion", false);
                    return fabric;
                }
                case DebrisKind.Tv:
                {
                    var screen = new Color(0.2f, 0.35f, 0.6f);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.025f, 0f), new Vector3(0.5f, 0.05f, 0.25f), Palette.Black, "Foot");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.15f, 0f), new Vector3(0.1f, 0.2f, 0.06f), Palette.Black, "Neck");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.6f, 0f), new Vector3(1.3f, 0.75f, 0.06f), Palette.Black, "Frame");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.6f, 0.035f), new Vector3(1.2f, 0.65f, 0.01f), screen, "Screen", false);
                    return screen;
                }
                case DebrisKind.Fridge:
                {
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 1.8f, 0.8f), Palette.White, "Body");
                    Prim(PrimitiveType.Cube, t, new Vector3(-0.35f, 1.1f, 0.42f), new Vector3(0.04f, 0.5f, 0.04f), Palette.Gray, "Handle", false);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.2f, 0.405f), new Vector3(0.9f, 0.02f, 0.01f), Palette.Gray, "Seam", false);
                    return Palette.White;
                }
                case DebrisKind.Bed:
                {
                    var blanket = Pick(rng, Palette.Teal, Palette.Purple, Palette.Red);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.2f, 0f), new Vector3(1.6f, 0.4f, 2.1f), Palette.DarkWood, "Frame");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.52f, 0f), new Vector3(1.5f, 0.25f, 2.0f), Palette.White, "Mattress");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.72f, -0.75f), new Vector3(0.6f, 0.15f, 0.4f), Palette.White, "Pillow", false);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.68f, 0.35f), new Vector3(1.52f, 0.08f, 1.2f), blanket, "Blanket", false);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.75f, -1.02f), new Vector3(1.6f, 1.1f, 0.08f), Palette.DarkWood, "Headboard");
                    return blanket;
                }
                case DebrisKind.Toilet:
                {
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.2f, 0.05f), new Vector3(0.4f, 0.4f, 0.55f), Palette.White, "Base");
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.42f, 0.08f), new Vector3(0.48f, 0.04f, 0.55f), Palette.White, "Bowl");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.68f, -0.3f), new Vector3(0.42f, 0.5f, 0.2f), Palette.White, "Tank");
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.465f, 0.08f), new Vector3(0.36f, 0.005f, 0.42f), new Color(0.6f, 0.8f, 0.95f), "Water", false);
                    return Palette.White;
                }
                case DebrisKind.Bathtub:
                {
                    var water = new Color(0.55f, 0.8f, 0.95f);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.28f, 0f), new Vector3(0.85f, 0.56f, 1.8f), Palette.White, "Tub");
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.5f, 0f), new Vector3(0.65f, 0.14f, 1.6f), water, "Water", false);
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.75f, -0.85f), new Vector3(0.05f, 0.2f, 0.05f), Palette.Gray, "Tap", false);
                    return water;
                }
            }
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.15f, 0f), Vector3.one * 0.3f, Palette.Gray, "Unknown");
            return Palette.Gray;
        }
    }
}
