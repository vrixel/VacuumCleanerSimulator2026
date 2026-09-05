using System.Collections.Generic;
using UnityEngine;
using VCS.World;

namespace VCS.Player
{
    /// <summary>
    /// Second generation of the garage (art direction settled 2026-09-05 from docs/concepts): angular faceted
    /// shells instead of smooth blobs, grooves and panel seams, bolted panels, desaturated real-product colours,
    /// translucent bins, emissive LEDs and small displays. Same local space and anchors as VacuumModels
    /// (origin on the floor, +z forward, metres) so the nozzles, cords and cockpit stay valid.
    /// </summary>
    public static class VacuumModelsV2
    {
        static readonly Quaternion WheelRot = Quaternion.Euler(0f, 0f, 90f);
        static readonly Color Teal = new Color(0.25f, 0.95f, 0.85f);
        static readonly Color Amber = new Color(1f, 0.62f, 0.15f);
        static readonly Color CoolWhite = new Color(0.85f, 0.95f, 1f);
        static readonly Color GreenLed = new Color(0.35f, 1f, 0.45f);
        static readonly Color RedLed = new Color(1f, 0.25f, 0.2f);

        static Material Graphite => Palette.Plastic(new Color(0.16f, 0.17f, 0.19f));
        static Material Gunmetal => Palette.Plastic(new Color(0.24f, 0.25f, 0.28f));
        static Material Rubber => Palette.Rubber(new Color(0.06f, 0.06f, 0.07f));
        static Material Steel => Palette.Chrome;
        static Material DarkSteel => Palette.Mat(new Color(0.30f, 0.31f, 0.34f), 0.85f, 0.55f);
        static Material Screen => Palette.Glossy(new Color(0.05f, 0.06f, 0.07f));

        static Vector2 P(float r, float y) => new Vector2(r, y);

        // ------------------------------------------------------------------ helpers
        static GameObject RevF(Transform p, Vector2[] profile, Material m, Vector3 pos, string name, int segs = 10, Quaternion? rot = null, bool caps = true)
            => MeshKit.Part(p, MeshKit.Flat(MeshKit.Revolve(profile, segs, name, caps)), m, pos, rot ?? Quaternion.identity, Vector3.one, name);

        static GameObject Rev(Transform p, Vector2[] profile, Material m, Vector3 pos, string name, int segs = 32, Quaternion? rot = null, bool caps = true)
            => MeshKit.Part(p, MeshKit.Revolve(profile, segs, name, caps), m, pos, rot ?? Quaternion.identity, Vector3.one, name);

        static GameObject TubeAlong(Transform p, Vector3[] ctrl, float radius, Material m, string name, bool ribbed = false, int radial = 12)
        {
            var path = MeshKit.Spline(ctrl, ribbed ? 14 : 8);
            var mesh = MeshKit.Tube(path, radius, radial, ribbed ? 0.22f : 0f, ribbed ? 1.9f : 0f, name);
            return MeshKit.Part(p, mesh, m, Vector3.zero, Quaternion.identity, Vector3.one, name);
        }

        static GameObject RBox(Transform p, float w, float h, float d, float r, Material m, Vector3 pos, string name, Quaternion? rot = null, float bevel = 0.008f)
            => MeshKit.Part(p, MeshKit.RoundedBox(w, h, d, r, 3, bevel, name), m, pos, rot ?? Quaternion.identity, Vector3.one, name);

        static GameObject Prim(Transform p, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, string name, Quaternion? rot = null)
        {
            var go = PropFactory.Prim(type, p, pos, scale, Color.white, name, false, rot);
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static GameObject Box(Transform p, Vector3 pos, Vector3 size, Material m, string name, Quaternion? rot = null) => Prim(p, PrimitiveType.Cube, pos, size, m, name, rot);
        static GameObject Sph(Transform p, Vector3 pos, float dia, Material m, string name) => Prim(p, PrimitiveType.Sphere, pos, Vector3.one * dia, m, name);

        static GameObject Torus(Transform p, Vector3 pos, Quaternion rot, float R, float r, Material m, string name, int segs = 24)
        {
            var prof = new List<Vector2>();
            const int n = 10;
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                prof.Add(new Vector2(R + r * Mathf.Cos(a), r * Mathf.Sin(a)));
            }
            return MeshKit.Part(p, MeshKit.Revolve(prof, segs, name, false), m, pos, rot, Vector3.one, name);
        }

        /// <summary>A wheel: rubber tyre with a tread groove, faceted hub, dark centre. Axle along local x.</summary>
        static void Wheel(Transform g, Vector3 pos, float dia, float width, Material tyre, Material hub)
        {
            float R = dia * 0.5f, hw = width * 0.5f;
            Rev(g, new[] { P(R * 0.55f, -hw), P(R, -hw), P(R, -hw * 0.25f), P(R - 0.006f, -hw * 0.2f), P(R - 0.006f, hw * 0.2f), P(R, hw * 0.25f), P(R, hw), P(R * 0.55f, hw) }, tyre, pos, "Tyre", 24, WheelRot, false);
            RevF(g, new[] { P(0f, -hw - 0.002f), P(R * 0.56f, -hw - 0.002f), P(R * 0.56f, hw + 0.002f), P(0f, hw + 0.002f) }, hub, pos, "Hub", 8, WheelRot);
            RevF(g, new[] { P(0f, -hw - 0.005f), P(R * 0.18f, -hw - 0.005f), P(R * 0.18f, hw + 0.005f), P(0f, hw + 0.005f) }, Rubber, pos, "HubCentre", 8, WheelRot);
        }

        /// <summary>Thin dark lines on the facet edges of a faceted drum: reads as deep vertical grooves.</summary>
        static void Seams(Transform g, Vector3 center, float radius, float y0, float y1, int n, Material m)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var pos = center + new Vector3(Mathf.Cos(a) * radius, (y0 + y1) * 0.5f, Mathf.Sin(a) * radius);
                var rot = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up);
                Box(g, pos, new Vector3(0.006f, y1 - y0, 0.006f), m, "Seam", rot);
            }
        }

        /// <summary>Row of dark vent slots along the local x axis of rot, facing local +z.</summary>
        static void Slots(Transform g, Vector3 center, Quaternion rot, int n, float pitch, Vector3 slot)
        {
            for (int i = 0; i < n; i++)
                Box(g, center + rot * new Vector3((i - (n - 1) * 0.5f) * pitch, 0f, 0f), slot, Rubber, "Vent", rot);
        }

        /// <summary>Vent slots around a vertical axis between two angles in degrees.</summary>
        static void Ring(Transform g, Vector3 center, float radius, float y, int n, Vector3 slot, float fromDeg, float toDeg)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, n > 1 ? i / (float)(n - 1) : 0f) * Mathf.Deg2Rad;
                var pos = center + new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                var rot = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up);
                Box(g, pos, slot, Rubber, "Vent", rot);
            }
        }

        /// <summary>Heat-sink fins: thin plates stacked along the local y axis of rot.</summary>
        static void Fins(Transform g, Vector3 center, Quaternion rot, int n, float pitch, Vector3 plate, Material m)
        {
            for (int i = 0; i < n; i++)
                Box(g, center + rot * new Vector3(0f, (i - (n - 1) * 0.5f) * pitch, 0f), plate, m, "Fin", rot);
        }

        static void Screw(Transform g, Vector3 pos, Quaternion rot)
        {
            Prim(g, PrimitiveType.Cylinder, pos, new Vector3(0.014f, 0.003f, 0.014f), Steel, "Screw", rot * Quaternion.Euler(90f, 0f, 0f));
            Box(g, pos + rot * new Vector3(0f, 0f, 0.003f), new Vector3(0.008f, 0.002f, 0.002f), Rubber, "ScrewSlot", rot);
        }

        static void ScrewRing(Transform g, Vector3 center, float radius, int n, float offset = 0.3f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f + offset;
                var pos = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Screw(g, pos, Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up));
            }
        }

        static GameObject Led(Transform g, Vector3 pos, Vector3 size, Color c, Quaternion? rot = null, PrimitiveType t = PrimitiveType.Cube)
            => Prim(g, t, pos, size, Palette.Led(c), "Led", rot);

        /// <summary>A tiny monochrome display: dark glossy glass with a glowing line of "text" and a status dot.</summary>
        static void Display(Transform g, Vector3 pos, Quaternion rot, float w, float h, Color glow)
        {
            Box(g, pos, new Vector3(w, h, 0.006f), Screen, "Display", rot);
            Box(g, pos + rot * new Vector3(0f, 0f, 0.004f), new Vector3(w * 0.9f, h * 0.8f, 0.002f), Palette.Glossy(new Color(0.03f, 0.05f, 0.06f)), "DisplayGlass", rot);
            Led(g, pos + rot * new Vector3(-w * 0.12f, h * 0.15f, 0.0055f), new Vector3(w * 0.55f, h * 0.16f, 0.002f), glow * 0.8f, rot);
            Led(g, pos + rot * new Vector3(-w * 0.2f, -h * 0.2f, 0.0055f), new Vector3(w * 0.4f, h * 0.12f, 0.002f), glow * 0.55f, rot);
            Led(g, pos + rot * new Vector3(w * 0.34f, -h * 0.2f, 0.0055f), new Vector3(h * 0.16f, h * 0.16f, 0.002f), GreenLed, rot);
        }

        static void SideBrush(Transform g, Vector3 pos)
        {
            var brush = new GameObject("SideBrush").transform;
            brush.SetParent(g, false);
            brush.localPosition = pos;
            for (int i = 0; i < 3; i++)
            {
                var rot = Quaternion.Euler(0f, i * 120f, 0f);
                Box(brush, rot * new Vector3(0.08f, 0f, 0f), new Vector3(0.16f, 0.006f, 0.02f), Rubber, "Arm", rot);
            }
            brush.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 720f, 0f);
        }

        // ------------------------------------------------------------------ Dusty (boxy graphite prototype)
        public static void Dusty(Transform g, VacuumSpec s)
        {
            var graphite = Graphite;
            var panel = Gunmetal;
            var orange = Palette.Glossy(new Color(0.80f, 0.40f, 0.12f));
            var smoke = Palette.Glass(new Color(0.30f, 0.32f, 0.36f, 0.55f));
            RBox(g, 0.86f, 0.12f, 0.70f, 0.03f, Rubber, new Vector3(0f, 0.06f, -0.02f), "Chassis");
            RBox(g, 0.80f, 0.46f, 0.62f, 0.02f, graphite, new Vector3(0f, 0.18f, 0f), "Body");
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.405f;
                var rot = Quaternion.Euler(0f, side * 90f, 0f);
                Box(g, new Vector3(x, 0.42f, 0f), new Vector3(0.012f, 0.34f, 0.50f), panel, "SidePanel");
                Box(g, new Vector3(x + side * 0.004f, 0.42f, 0.25f), new Vector3(0.006f, 0.34f, 0.008f), Rubber, "PanelSeam");
                Box(g, new Vector3(x + side * 0.004f, 0.42f, -0.25f), new Vector3(0.006f, 0.34f, 0.008f), Rubber, "PanelSeam");
                Slots(g, new Vector3(x + side * 0.007f, 0.30f, -0.10f), rot, 6, 0.045f, new Vector3(0.035f, 0.012f, 0.01f));
                Screw(g, new Vector3(x + side * 0.007f, 0.57f, 0.21f), rot);
                Screw(g, new Vector3(x + side * 0.007f, 0.57f, -0.21f), rot);
                Screw(g, new Vector3(x + side * 0.007f, 0.27f, 0.21f), rot);
                Screw(g, new Vector3(x + side * 0.007f, 0.27f, -0.21f), rot);
            }
            RBox(g, 0.50f, 0.36f, 0.22f, 0.02f, smoke, new Vector3(0f, 0.24f, -0.40f), "Bin");
            Box(g, new Vector3(0f, 0.615f, -0.40f), new Vector3(0.52f, 0.03f, 0.24f), graphite, "BinLid");
            Box(g, new Vector3(0f, 0.40f, -0.40f), new Vector3(0.10f, 0.28f, 0.10f), Gunmetal, "BinCore");
            TubeAlong(g, new[] { new Vector3(-0.14f, 0.63f, 0.05f), new Vector3(-0.14f, 0.74f, 0.05f), new Vector3(0.14f, 0.74f, 0.05f), new Vector3(0.14f, 0.63f, 0.05f) }, 0.016f, Rubber, "Handle");
            Box(g, new Vector3(0f, 0.645f, 0.20f), new Vector3(0.70f, 0.01f, 0.03f), orange, "Stripe");
            Led(g, new Vector3(0.28f, 0.646f, 0.24f), new Vector3(0.03f, 0.008f, 0.025f), Amber);
            Display(g, new Vector3(0f, 0.53f, 0.312f), Quaternion.identity, 0.14f, 0.05f, Teal);
            Box(g, new Vector3(0f, 0.36f, 0.312f), new Vector3(0.62f, 0.006f, 0.006f), Rubber, "FrontSeam");
            TubeAlong(g, new[] { new Vector3(0f, 0.28f, 0.30f), new Vector3(0f, 0.25f, 0.42f), new Vector3(0f, 0.16f, 0.50f) }, 0.055f, Gunmetal, "Neck", true);
            RBox(g, 0.96f, 0.14f, 0.40f, 0.03f, graphite, new Vector3(0f, 0.02f, 0.66f), "NozzleHead");
            Box(g, new Vector3(0f, 0.05f, 0.866f), new Vector3(0.90f, 0.06f, 0.02f), Rubber, "Lip");
            Box(g, new Vector3(0f, 0.135f, 0.86f), new Vector3(0.60f, 0.012f, 0.012f), orange, "HeadStripe");
            Slots(g, new Vector3(0f, 0.161f, 0.60f), Quaternion.Euler(90f, 0f, 0f), 8, 0.09f, new Vector3(0.05f, 0.012f, 0.01f));
            Wheel(g, new Vector3(0.44f, 0.15f, -0.18f), 0.30f, 0.09f, Rubber, Steel);
            Wheel(g, new Vector3(-0.44f, 0.15f, -0.18f), 0.30f, 0.09f, Rubber, Steel);
            Sph(g, new Vector3(0.30f, 0.05f, 0.40f), 0.09f, Rubber, "Caster");
            Sph(g, new Vector3(-0.30f, 0.05f, 0.40f), 0.09f, Rubber, "Caster");
        }

        // ------------------------------------------------------------------ Roomboo (octagonal robot)
        public static void Roomboo(Transform g, VacuumSpec s)
        {
            var body = Graphite;
            RevF(g, new[] { P(0f, 0.012f), P(0.40f, 0.012f), P(0.44f, 0.05f), P(0.44f, 0.085f), P(0.41f, 0.105f), P(0f, 0.105f) }, body, Vector3.zero, "Body", 8);
            RevF(g, new[] { P(0f, 0.105f), P(0.36f, 0.105f), P(0.36f, 0.118f), P(0f, 0.118f) }, Steel, Vector3.zero, "TopPlate", 8);
            Box(g, new Vector3(0f, 0.119f, 0f), new Vector3(0.74f, 0.002f, 0.006f), Rubber, "PlateSeam");
            Torus(g, new Vector3(0f, 0.119f, 0f), Quaternion.identity, 0.15f, 0.006f, Palette.Led(Teal), "LightRing", 32);
            RevF(g, new[] { P(0f, 0.118f), P(0.07f, 0.118f), P(0.07f, 0.15f), P(0.05f, 0.156f), P(0f, 0.156f) }, body, Vector3.zero, "Turret", 8);
            Sph(g, new Vector3(0f, 0.16f, 0f), 0.05f, Palette.Glossy(new Color(0.04f, 0.05f, 0.06f)), "TurretDome");
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) * 45f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a) * 0.45f, 0.065f, Mathf.Sin(a) * 0.45f);
                var rot = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up);
                Box(g, pos, new Vector3(0.30f, 0.045f, 0.022f), Rubber, "BumperSegment", rot);
            }
            Ring(g, Vector3.zero, 0.442f, 0.095f, 7, new Vector3(0.03f, 0.008f, 0.01f), 200f, 340f);
            Led(g, new Vector3(0f, 0.10f, 0.412f), new Vector3(0.18f, 0.006f, 0.01f), Teal);
            Sph(g, new Vector3(0f, 0.115f, 0.30f), 0.05f, Rubber, "Sensor");
            Box(g, new Vector3(0f, 0.02f, 0.40f), new Vector3(0.24f, 0.02f, 0.02f), Rubber, "Bristles");
            SideBrush(g, new Vector3(0.30f, 0.012f, 0.28f));
            Wheel(g, new Vector3(0.30f, 0.05f, -0.05f), 0.08f, 0.03f, Rubber, Steel);
            Wheel(g, new Vector3(-0.30f, 0.05f, -0.05f), 0.08f, 0.03f, Rubber, Steel);
            ScrewRing(g, new Vector3(0f, 0.119f, 0f), 0.33f, 4, 0.8f);
            Display(g, new Vector3(0.17f, 0.119f, 0.16f), Quaternion.Euler(90f, 0f, 0f), 0.09f, 0.035f, Teal);
        }

        // ------------------------------------------------------------------ Cyclonic (faceted bin, ball head)
        public static void Cyclonic(Transform g, VacuumSpec s)
        {
            var purple = Palette.Glossy(new Color(0.40f, 0.22f, 0.55f));
            var glass = Palette.Glass(new Color(0.80f, 0.85f, 0.90f, 0.32f));
            RBox(g, 0.36f, 0.09f, 0.22f, 0.02f, Graphite, new Vector3(0f, 0f, 0.32f), "Head");
            Led(g, new Vector3(0f, 0.05f, 0.436f), new Vector3(0.30f, 0.01f, 0.008f), CoolWhite);
            Slots(g, new Vector3(0f, 0.091f, 0.28f), Quaternion.Euler(90f, 0f, 0f), 6, 0.05f, new Vector3(0.03f, 0.012f, 0.01f));
            var ball = new List<Vector2>();
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / 12f);
                float r = 0.19f * Mathf.Cos(a) * (i == 6 ? 0.96f : 1f);
                ball.Add(new Vector2(r, 0.19f + 0.19f * Mathf.Sin(a)));
            }
            RevF(g, ball.ToArray(), Graphite, new Vector3(0f, 0f, 0.06f), "Ball", 12);
            Torus(g, new Vector3(0f, 0.19f, 0.06f), WheelRot, 0.19f, 0.01f, purple, "BallBand");
            RevF(g, new[] { P(0f, -0.015f), P(0.07f, -0.015f), P(0.07f, 0.015f), P(0f, 0.015f) }, Steel, new Vector3(0.19f, 0.19f, 0.06f), "Hubcap", 8, WheelRot);
            RevF(g, new[] { P(0f, -0.015f), P(0.07f, -0.015f), P(0.07f, 0.015f), P(0f, 0.015f) }, Steel, new Vector3(-0.19f, 0.19f, 0.06f), "Hubcap", 8, WheelRot);
            RevF(g, new[] { P(0f, 0.30f), P(0.09f, 0.30f), P(0.09f, 0.36f), P(0f, 0.36f) }, Rubber, new Vector3(0f, 0f, -0.02f), "Motor", 10);
            Ring(g, new Vector3(0f, 0f, -0.02f), 0.092f, 0.33f, 10, new Vector3(0.022f, 0.02f, 0.008f), 0f, 360f);
            RevF(g, new[] { P(0f, 0.36f), P(0.11f, 0.36f), P(0.11f, 0.70f), P(0f, 0.70f) }, glass, new Vector3(0f, 0f, -0.02f), "Bin", 10);
            RevF(g, new[] { P(0f, 0.37f), P(0.045f, 0.37f), P(0.045f, 0.68f), P(0f, 0.68f) }, purple, new Vector3(0f, 0f, -0.02f), "CycloneCore", 8);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a) * 0.072f, 0.53f, -0.02f + Mathf.Sin(a) * 0.072f);
                var rot = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up);
                Box(g, pos, new Vector3(0.004f, 0.26f, 0.055f), purple, "Fin", rot);
            }
            RevF(g, new[] { P(0.11f, 0.70f), P(0.13f, 0.72f), P(0.13f, 0.80f), P(0.09f, 0.90f), P(0.05f, 0.95f), P(0f, 0.95f) }, purple, new Vector3(0f, 0f, -0.02f), "Cyclone", 10);
            Torus(g, new Vector3(0f, 0.705f, -0.02f), Quaternion.identity, 0.125f, 0.004f, Rubber, "CapSeam", 20);
            RBox(g, 0.18f, 0.40f, 0.12f, 0.015f, Graphite, new Vector3(0f, 0.32f, -0.19f), "Spine");
            Display(g, new Vector3(0f, 0.60f, -0.249f), Quaternion.Euler(0f, 180f, 0f), 0.09f, 0.045f, Teal);
            Fins(g, new Vector3(0f, 0.42f, -0.252f), Quaternion.Euler(0f, 180f, 0f), 5, 0.02f, new Vector3(0.12f, 0.003f, 0.012f), Gunmetal);
            TubeAlong(g, new[] { new Vector3(0f, 0.95f, -0.02f), new Vector3(0f, 1.12f, -0.05f), new Vector3(0f, 1.22f, -0.15f), new Vector3(0f, 1.22f, -0.30f) }, 0.02f, Graphite, "Handle");
            Box(g, new Vector3(0f, 1.22f, -0.26f), new Vector3(0.05f, 0.05f, 0.12f), Rubber, "Grip");
            Led(g, new Vector3(0f, 1.246f, -0.20f), new Vector3(0.02f, 0.004f, 0.02f), RedLed);
            TubeAlong(g, new[] { new Vector3(0f, 0.55f, -0.13f), new Vector3(0f, 0.40f, -0.26f), new Vector3(0f, 0.18f, -0.28f), new Vector3(0f, 0.07f, -0.08f), new Vector3(0f, 0.06f, 0.2f) }, 0.03f, Gunmetal, "Hose", true);
        }

        // ------------------------------------------------------------------ Harold (faceted red drum, riveted lid)
        public static void Harold(Transform g, VacuumSpec s)
        {
            var red = Palette.Plastic(new Color(0.60f, 0.12f, 0.10f));
            var lid = Palette.Mat(new Color(0.13f, 0.13f, 0.15f), 0.6f, 0.5f);
            var hoseM = Palette.Plastic(new Color(0.36f, 0.37f, 0.39f));
            RevF(g, new[] { P(0f, 0f), P(0.25f, 0f), P(0.25f, 0.06f), P(0.24f, 0.06f) }, Rubber, Vector3.zero, "Base", 12);
            RevF(g, new[] { P(0f, 0.06f), P(0.245f, 0.06f), P(0.245f, 0.38f), P(0f, 0.38f) }, red, Vector3.zero, "Drum", 12);
            Seams(g, Vector3.zero, 0.246f, 0.08f, 0.36f, 12, Rubber);
            RevF(g, new[] { P(0.245f, 0.38f), P(0.262f, 0.40f), P(0.262f, 0.47f), P(0.22f, 0.52f), P(0.12f, 0.555f), P(0f, 0.565f) }, lid, Vector3.zero, "Lid", 12);
            Torus(g, new Vector3(0f, 0.385f, 0f), Quaternion.identity, 0.25f, 0.005f, Rubber, "LidSeam", 24);
            ScrewRing(g, new Vector3(0f, 0.44f, 0f), 0.263f, 12, 0.26f);
            Box(g, new Vector3(0.26f, 0.41f, 0f), new Vector3(0.03f, 0.06f, 0.05f), Steel, "Latch");
            Box(g, new Vector3(-0.26f, 0.41f, 0f), new Vector3(0.03f, 0.06f, 0.05f), Steel, "Latch");
            TubeAlong(g, new[] { new Vector3(-0.10f, 0.55f, 0f), new Vector3(-0.10f, 0.66f, 0f), new Vector3(0.10f, 0.66f, 0f), new Vector3(0.10f, 0.55f, 0f) }, 0.014f, Rubber, "Handle");
            Led(g, new Vector3(0f, 0.50f, 0.20f), new Vector3(0.02f, 0.01f, 0.01f), GreenLed, Quaternion.Euler(-50f, 0f, 0f));
            // the face, as a flat decal on the front facet
            var decal = Palette.Glossy(new Color(0.05f, 0.05f, 0.06f));
            Prim(g, PrimitiveType.Cylinder, new Vector3(-0.08f, 0.27f, 0.242f), new Vector3(0.05f, 0.002f, 0.05f), decal, "EyeDecal", Quaternion.Euler(90f, 0f, 0f));
            Prim(g, PrimitiveType.Cylinder, new Vector3(0.08f, 0.27f, 0.242f), new Vector3(0.05f, 0.002f, 0.05f), decal, "EyeDecal", Quaternion.Euler(90f, 0f, 0f));
            Prim(g, PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0.243f), new Vector3(0.025f, 0.002f, 0.025f), decal, "NoseDecal", Quaternion.Euler(90f, 0f, 0f));
            var smile = new List<Vector3>();
            for (int i = 0; i <= 10; i++)
            {
                float k = i / 10f;
                float u = (k - 0.5f) * 2f;
                smile.Add(new Vector3(u * 0.10f, 0.15f + 0.03f * u * u, 0.243f));
            }
            MeshKit.Part(g, MeshKit.Tube(smile, 0.006f, 6, 0f, 0f, "SmileDecal"), decal, Vector3.zero, Quaternion.identity, Vector3.one, "SmileDecal");
            TubeAlong(g, new[] { new Vector3(0.10f, 0.50f, -0.12f), new Vector3(0.30f, 0.46f, 0.05f), new Vector3(0.36f, 0.28f, 0.35f), new Vector3(0.26f, 0.10f, 0.55f) }, 0.035f, hoseM, "Hose", true);
            TubeAlong(g, new[] { new Vector3(0.26f, 0.10f, 0.55f), new Vector3(0.12f, 0.06f, 0.72f), new Vector3(0.05f, 0.05f, 0.78f) }, 0.02f, Steel, "Wand");
            RBox(g, 0.34f, 0.06f, 0.12f, 0.015f, Graphite, new Vector3(0.03f, 0f, 0.80f), "Nozzle");
            Box(g, new Vector3(0.03f, 0.02f, 0.862f), new Vector3(0.30f, 0.03f, 0.01f), Rubber, "NozzleLip");
            Sph(g, new Vector3(0.18f, 0.035f, 0.15f), 0.07f, Rubber, "Caster");
            Sph(g, new Vector3(-0.18f, 0.035f, 0.15f), 0.07f, Rubber, "Caster");
            Sph(g, new Vector3(0.18f, 0.035f, -0.15f), 0.07f, Rubber, "Caster");
            Sph(g, new Vector3(-0.18f, 0.035f, -0.15f), 0.07f, Rubber, "Caster");
        }

        // ------------------------------------------------------------------ Stickmaster (finned motor, glass bin)
        public static void Stickmaster(Transform g, VacuumSpec s)
        {
            var blue = Palette.Plastic(new Color(0.10f, 0.32f, 0.70f));
            var glass = Palette.Glass(new Color(0.80f, 0.85f, 0.90f, 0.32f));
            var cyan = new Color(0.3f, 0.9f, 1f);
            RBox(g, 0.30f, 0.06f, 0.16f, 0.015f, Graphite, new Vector3(0f, 0.005f, 0.42f), "Head");
            Rev(g, new[] { P(0f, -0.13f), P(0.03f, -0.13f), P(0.03f, 0.13f), P(0f, 0.13f) }, blue, new Vector3(0f, 0.035f, 0.47f), "Roller", 16, WheelRot);
            Led(g, new Vector3(0f, 0.045f, 0.501f), new Vector3(0.26f, 0.008f, 0.006f), CoolWhite);
            Slots(g, new Vector3(0f, 0.066f, 0.38f), Quaternion.Euler(90f, 0f, 0f), 5, 0.045f, new Vector3(0.03f, 0.01f, 0.01f));
            RevF(g, new[] { P(0f, -0.015f), P(0.03f, -0.015f), P(0.03f, 0.015f), P(0f, 0.015f) }, Steel, new Vector3(0.155f, 0.035f, 0.47f), "Hubcap", 8, WheelRot);
            RevF(g, new[] { P(0f, -0.015f), P(0.03f, -0.015f), P(0.03f, 0.015f), P(0f, 0.015f) }, Steel, new Vector3(-0.155f, 0.035f, 0.47f), "Hubcap", 8, WheelRot);
            TubeAlong(g, new[] { new Vector3(0f, 0.05f, 0.40f), new Vector3(0f, 0.45f, 0.18f), new Vector3(0f, 0.85f, -0.02f) }, 0.018f, Steel, "Wand");

            var unit = new GameObject("Unit").transform;
            unit.SetParent(g, false);
            unit.localPosition = new Vector3(0f, 0.85f, -0.02f);
            unit.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            RBox(unit, 0.13f, 0.16f, 0.12f, 0.012f, Graphite, new Vector3(0f, -0.14f, 0f), "MotorBlock", null, 0.006f);
            Fins(unit, new Vector3(0.068f, -0.06f, 0f), Quaternion.Euler(0f, 90f, 0f), 6, 0.02f, new Vector3(0.09f, 0.002f, 0.012f), Gunmetal);
            Fins(unit, new Vector3(-0.068f, -0.06f, 0f), Quaternion.Euler(0f, -90f, 0f), 6, 0.02f, new Vector3(0.09f, 0.002f, 0.012f), Gunmetal);
            Box(unit, new Vector3(0f, -0.03f, 0f), new Vector3(0.135f, 0.012f, 0.125f), blue, "Band");
            Display(unit, new Vector3(0f, -0.10f, 0.061f), Quaternion.identity, 0.07f, 0.03f, cyan);
            RBox(unit, 0.10f, 0.05f, 0.16f, 0.01f, Rubber, new Vector3(0f, -0.25f, -0.03f), "Battery");
            for (int i = 0; i < 3; i++) Led(unit, new Vector3(-0.02f + i * 0.02f, -0.224f, 0.03f), new Vector3(0.008f, 0.004f, 0.008f), GreenLed);
            RevF(unit, new[] { P(0f, 0.02f), P(0.065f, 0.02f), P(0.065f, 0.26f), P(0.05f, 0.30f), P(0f, 0.30f) }, glass, Vector3.zero, "Bin", 10);
            RevF(unit, new[] { P(0f, 0.03f), P(0.03f, 0.03f), P(0.03f, 0.27f), P(0f, 0.27f) }, Gunmetal, Vector3.zero, "Core", 8);
            RevF(unit, new[] { P(0.03f, 0.30f), P(0.03f, 0.36f), P(0f, 0.36f) }, Graphite, Vector3.zero, "Cap", 8);
            TubeAlong(g, new[] { new Vector3(0f, 0.88f, -0.10f), new Vector3(0f, 1.02f, -0.22f), new Vector3(0f, 0.92f, -0.34f), new Vector3(0f, 0.78f, -0.28f), new Vector3(0f, 0.80f, -0.12f) }, 0.016f, Rubber, "Handle");
            Led(g, new Vector3(0f, 0.86f, -0.16f), new Vector3(0.028f, 0.045f, 0.02f), cyan);
        }

        // ------------------------------------------------------------------ Grandma (avocado cowl, cloth bag)
        public static void Grandma(Transform g, VacuumSpec s)
        {
            var green = Palette.Plastic(new Color(0.34f, 0.42f, 0.20f));
            var bagMat = Palette.Fabric(new Color(0.68f, 0.58f, 0.40f));
            var bakelite = Palette.Glossy(new Color(0.24f, 0.14f, 0.09f));
            RBox(g, 0.40f, 0.13f, 0.30f, 0.03f, green, new Vector3(0f, 0.01f, 0.28f), "Base");
            Box(g, new Vector3(0f, 0.09f, 0.43f), new Vector3(0.36f, 0.02f, 0.01f), Steel, "ChromeStrip");
            Led(g, new Vector3(0f, 0.10f, 0.43f), new Vector3(0.06f, 0.06f, 0.06f), new Color(1f, 0.9f, 0.7f), null, PrimitiveType.Sphere);
            Torus(g, new Vector3(0f, 0.10f, 0.43f), Quaternion.Euler(90f, 0f, 0f), 0.034f, 0.006f, Steel, "HeadlightRim", 20);
            var lgo = new GameObject("HeadlightLight");
            lgo.transform.SetParent(g, false);
            lgo.transform.localPosition = new Vector3(0f, 0.14f, 0.55f);
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3f;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.92f, 0.7f);
            Screw(g, new Vector3(0.17f, 0.11f, 0.435f), Quaternion.identity);
            Screw(g, new Vector3(-0.17f, 0.11f, 0.435f), Quaternion.identity);
            RevF(g, new[] { P(0f, 0.12f), P(0.16f, 0.12f), P(0.16f, 0.20f), P(0.15f, 0.28f), P(0.11f, 0.34f), P(0f, 0.36f) }, green, new Vector3(0f, 0f, 0.02f), "Motor", 10);
            Ring(g, new Vector3(0f, 0f, 0.02f), 0.162f, 0.19f, 12, new Vector3(0.024f, 0.035f, 0.008f), 20f, 160f);
            Torus(g, new Vector3(0f, 0.125f, 0.02f), Quaternion.identity, 0.163f, 0.006f, Steel, "Trim", 24);
            Torus(g, new Vector3(0f, 0.285f, 0.02f), Quaternion.identity, 0.148f, 0.004f, Rubber, "CowlSeam", 24);
            Box(g, new Vector3(0f, 0.30f, 0.16f), new Vector3(0.05f, 0.02f, 0.006f), Steel, "Badge");
            TubeAlong(g, new[] { new Vector3(0f, 0.30f, -0.02f), new Vector3(0f, 0.75f, -0.18f), new Vector3(0f, 1.15f, -0.36f) }, 0.018f, Steel, "Handle");
            Box(g, new Vector3(0f, 1.17f, -0.37f), new Vector3(0.05f, 0.05f, 0.2f), bakelite, "Grip", Quaternion.Euler(-25f, 0f, 0f));
            var bagT = new GameObject("BagPivot").transform;
            bagT.SetParent(g, false);
            bagT.localPosition = new Vector3(0f, 0.28f, -0.10f);
            bagT.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            Rev(bagT, new[] { P(0.05f, 0f), P(0.12f, 0.05f), P(0.16f, 0.30f), P(0.16f, 0.55f), P(0.12f, 0.72f), P(0.06f, 0.78f), P(0f, 0.78f) }, bagMat, Vector3.zero, "Bag", 24);
            Box(bagT, new Vector3(0f, 0.42f, 0.161f), new Vector3(0.012f, 0.52f, 0.004f), Rubber, "Zipper");
            Box(bagT, new Vector3(0f, 0.20f, 0.166f), new Vector3(0.02f, 0.03f, 0.006f), Steel, "ZipperPull");
            Torus(bagT, new Vector3(0f, 0.05f, 0f), Quaternion.identity, 0.12f, 0.008f, Rubber, "BagCollar", 24);
            Wheel(g, new Vector3(0.21f, 0.06f, -0.02f), 0.11f, 0.03f, Rubber, Steel);
            Wheel(g, new Vector3(-0.21f, 0.06f, -0.02f), 0.11f, 0.03f, Rubber, Steel);
            Sph(g, new Vector3(0.14f, 0.03f, 0.38f), 0.05f, Rubber, "Caster");
            Sph(g, new Vector3(-0.14f, 0.03f, 0.38f), 0.05f, Rubber, "Caster");
        }

        // ------------------------------------------------------------------ Rowinta (ribbed navy canister)
        public static void Rowinta(Transform g, VacuumSpec s)
        {
            var navy = Palette.Plastic(new Color(0.09f, 0.12f, 0.24f));
            var grey = Palette.Plastic(new Color(0.38f, 0.39f, 0.42f));
            var white = Palette.Glossy(new Color(0.85f, 0.85f, 0.86f));
            var red = Palette.Glossy(new Color(0.62f, 0.12f, 0.12f));
            var blue = Palette.Glossy(new Color(0.15f, 0.25f, 0.55f));
            var profile = new List<Vector2> { P(0f, -0.30f), P(0.10f, -0.30f), P(0.16f, -0.26f), P(0.19f, -0.16f) };
            for (int i = 0; i < 8; i++)
            {
                float z = -0.14f + i * 0.035f;
                profile.Add(P(0.19f, z)); profile.Add(P(0.178f, z + 0.008f)); profile.Add(P(0.178f, z + 0.02f)); profile.Add(P(0.19f, z + 0.028f));
            }
            profile.Add(P(0.19f, 0.15f)); profile.Add(P(0.16f, 0.25f)); profile.Add(P(0.10f, 0.30f)); profile.Add(P(0f, 0.30f));
            var body = RevF(g, profile.ToArray(), navy, new Vector3(0f, 0.17f, 0f), "Body", 12, Quaternion.Euler(90f, 0f, 0f));
            body.transform.localScale = new Vector3(1.25f, 1f, 0.8f);
            Torus(g, new Vector3(0f, 0.17f, -0.29f), Quaternion.Euler(90f, 0f, 0f), 0.13f, 0.02f, Rubber, "Bumper");
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.236f;
                Box(g, new Vector3(x, 0.215f, 0.20f), new Vector3(0.012f, 0.03f, 0.10f), blue, "Stripe");
                Box(g, new Vector3(x + side * 0.003f, 0.175f, 0.20f), new Vector3(0.012f, 0.03f, 0.10f), white, "Stripe");
                Box(g, new Vector3(x, 0.135f, 0.20f), new Vector3(0.012f, 0.03f, 0.10f), red, "Stripe");
            }
            TubeAlong(g, new[] { new Vector3(0f, 0.30f, 0.10f), new Vector3(0f, 0.40f, 0.03f), new Vector3(0f, 0.40f, -0.09f), new Vector3(0f, 0.30f, -0.16f) }, 0.014f, Rubber, "Handle");
            RevF(g, new[] { P(0f, 0f), P(0.03f, 0f), P(0.03f, 0.012f), P(0f, 0.012f) }, Steel, new Vector3(-0.09f, 0.315f, -0.14f), "CordButton", 8);
            RevF(g, new[] { P(0f, 0f), P(0.035f, 0f), P(0.035f, 0.02f), P(0.02f, 0.02f), P(0.02f, 0.03f), P(0f, 0.03f) }, grey, new Vector3(0.09f, 0.31f, -0.12f), "SilenceDial", 10);
            Display(g, new Vector3(0f, 0.327f, -0.20f), Quaternion.Euler(70f, 0f, 0f), 0.07f, 0.028f, Teal);
            Ring(g, new Vector3(0f, 0.17f, -0.22f), 0.20f, 0.03f, 5, new Vector3(0.02f, 0.014f, 0.01f), 200f, 250f);
            Ring(g, new Vector3(0f, 0.17f, -0.22f), 0.20f, 0.03f, 5, new Vector3(0.02f, 0.014f, 0.01f), 290f, 340f);
            Wheel(g, new Vector3(0.25f, 0.065f, -0.19f), 0.10f, 0.03f, Rubber, Steel);
            Wheel(g, new Vector3(-0.25f, 0.065f, -0.19f), 0.10f, 0.03f, Rubber, Steel);
            Sph(g, new Vector3(0f, 0.035f, 0.18f), 0.06f, Rubber, "Caster");
            TubeAlong(g, new[] { new Vector3(0f, 0.28f, 0.29f), new Vector3(0f, 0.46f, 0.42f), new Vector3(0.05f, 0.56f, 0.56f), new Vector3(0.08f, 0.46f, 0.68f) }, 0.028f, grey, "Hose", true);
            Box(g, new Vector3(0.08f, 0.45f, 0.69f), new Vector3(0.05f, 0.06f, 0.09f), Graphite, "Grip", Quaternion.Euler(45f, 0f, 0f));
            Led(g, new Vector3(0.08f, 0.49f, 0.67f), new Vector3(0.012f, 0.006f, 0.012f), GreenLed);
            TubeAlong(g, new[] { new Vector3(0.08f, 0.44f, 0.70f), new Vector3(0.06f, 0.26f, 0.86f), new Vector3(0.03f, 0.08f, 0.99f) }, 0.016f, Steel, "Tube");
            RBox(g, 0.32f, 0.05f, 0.12f, 0.015f, Graphite, new Vector3(0.02f, 0f, 1.0f), "FloorHead");
            Box(g, new Vector3(0.02f, 0.02f, 1.061f), new Vector3(0.28f, 0.02f, 0.008f), red, "Felt");
        }

        // ------------------------------------------------------------------ Shop Drum (grooved yellow drum)
        public static void ShopDrum(Transform g, VacuumSpec s)
        {
            var yellow = Palette.Plastic(new Color(0.80f, 0.60f, 0.10f));
            var head = Palette.Plastic(new Color(0.10f, 0.10f, 0.11f));
            var gray = Palette.Plastic(new Color(0.30f, 0.31f, 0.33f));
            RevF(g, new[] { P(0f, 0.03f), P(0.32f, 0.03f), P(0.33f, 0.12f), P(0.30f, 0.12f) }, Rubber, Vector3.zero, "CasterBase", 16);
            var drum = new List<Vector2> { P(0f, 0.12f), P(0.30f, 0.12f) };
            for (int i = 0; i < 4; i++)
            {
                float y = 0.16f + i * 0.09f;
                drum.Add(P(0.30f, y)); drum.Add(P(0.28f, y + 0.012f)); drum.Add(P(0.28f, y + 0.045f)); drum.Add(P(0.30f, y + 0.057f));
            }
            drum.Add(P(0.30f, 0.50f)); drum.Add(P(0f, 0.50f));
            RevF(g, drum.ToArray(), yellow, Vector3.zero, "Drum", 16);
            RevF(g, new[] { P(0.30f, 0.50f), P(0.325f, 0.52f), P(0.325f, 0.60f), P(0.28f, 0.64f), P(0.20f, 0.68f), P(0.17f, 0.74f), P(0.10f, 0.76f), P(0f, 0.76f) }, head, Vector3.zero, "Head", 16);
            Ring(g, Vector3.zero, 0.31f, 0.62f, 16, new Vector3(0.05f, 0.03f, 0.01f), 0f, 360f);
            RevF(g, new[] { P(0.10f, 0.76f), P(0.10f, 0.79f), P(0f, 0.79f) }, Steel, Vector3.zero, "VentCap", 8);
            Torus(g, new Vector3(0f, 0.505f, 0f), Quaternion.identity, 0.312f, 0.005f, Rubber, "HeadSeam", 32);
            Box(g, new Vector3(0.315f, 0.50f, 0f), new Vector3(0.04f, 0.09f, 0.06f), Steel, "Latch");
            Box(g, new Vector3(-0.315f, 0.50f, 0f), new Vector3(0.04f, 0.09f, 0.06f), Steel, "Latch");
            Box(g, new Vector3(0.335f, 0.535f, 0f), new Vector3(0.012f, 0.012f, 0.07f), Rubber, "LatchPin");
            Box(g, new Vector3(-0.335f, 0.535f, 0f), new Vector3(0.012f, 0.012f, 0.07f), Rubber, "LatchPin");
            RBox(g, 0.10f, 0.06f, 0.05f, 0.01f, gray, new Vector3(0.18f, 0.63f, 0.20f), "SwitchBox", Quaternion.Euler(0f, -35f, 0f));
            Led(g, new Vector3(0.20f, 0.675f, 0.215f), new Vector3(0.02f, 0.01f, 0.02f), RedLed);
            Box(g, new Vector3(0.155f, 0.675f, 0.185f), new Vector3(0.03f, 0.014f, 0.02f), Rubber, "Switch", Quaternion.Euler(0f, -35f, 0f));
            ScrewRing(g, new Vector3(0f, 0.125f, 0f), 0.305f, 8);
            Wheel(g, new Vector3(0.22f, 0.04f, 0.20f), 0.08f, 0.035f, Rubber, Steel);
            Wheel(g, new Vector3(-0.22f, 0.04f, 0.20f), 0.08f, 0.035f, Rubber, Steel);
            Wheel(g, new Vector3(0.22f, 0.04f, -0.20f), 0.08f, 0.035f, Rubber, Steel);
            Wheel(g, new Vector3(-0.22f, 0.04f, -0.20f), 0.08f, 0.035f, Rubber, Steel);
            TubeAlong(g, new[] { new Vector3(0.24f, 0.44f, 0.14f), new Vector3(0.42f, 0.30f, 0.38f), new Vector3(0.25f, 0.12f, 0.60f), new Vector3(0.06f, 0.07f, 0.70f) }, 0.045f, gray, "Hose", true);
            RBox(g, 0.42f, 0.08f, 0.14f, 0.02f, head, new Vector3(0f, 0f, 0.78f), "Nozzle");
            Box(g, new Vector3(0f, 0.02f, 0.852f), new Vector3(0.38f, 0.04f, 0.01f), Rubber, "NozzleLip");
        }
    }
}
