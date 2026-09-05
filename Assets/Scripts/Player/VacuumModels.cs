using System.Collections.Generic;
using UnityEngine;
using VCS.World;

namespace VCS.Player
{
    /// <summary>Rotates a part forever (side brushes, fans).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 DegreesPerSecond;
        void Update() { transform.Rotate(DegreesPerSecond * Time.deltaTime, Space.Self); }
    }

    /// <summary>
    /// Builders for every vacuum in the garage. Local space: origin on the floor, +z is the front, metres.
    /// Built from MeshKit solids so the silhouettes read as real machines; eyes are added by VacuumVisuals.
    /// </summary>
    public static class VacuumModels
    {
        static readonly Quaternion WheelRot = Quaternion.Euler(0f, 0f, 90f);

        static Vector2 P(float r, float y) => new Vector2(r, y);

        static GameObject Rev(Transform p, Vector2[] profile, Material m, Vector3 pos, string name, Quaternion? rot = null, int segs = 32, bool caps = true)
            => MeshKit.Part(p, MeshKit.Revolve(profile, segs, name, caps), m, pos, rot ?? Quaternion.identity, Vector3.one, name);

        static GameObject TubeAlong(Transform p, Vector3[] ctrl, float radius, Material m, string name, bool ribbed = false, int radial = 12)
        {
            var path = MeshKit.Spline(ctrl, ribbed ? 14 : 8);
            var mesh = MeshKit.Tube(path, radius, radial, ribbed ? 0.22f : 0f, ribbed ? 1.9f : 0f, name);
            return MeshKit.Part(p, mesh, m, Vector3.zero, Quaternion.identity, Vector3.one, name);
        }

        static GameObject RBox(Transform p, float w, float h, float d, float r, Material m, Vector3 pos, string name, Quaternion? rot = null)
            => MeshKit.Part(p, MeshKit.RoundedBox(w, h, d, r, 4, 0.012f, name), m, pos, rot ?? Quaternion.identity, Vector3.one, name);

        static GameObject Prim(Transform p, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, string name, Quaternion? rot = null)
        {
            var go = PropFactory.Prim(type, p, pos, scale, Color.white, name, false, rot);
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static GameObject Sph(Transform p, Vector3 pos, float dia, Material m, string name) => Prim(p, PrimitiveType.Sphere, pos, Vector3.one * dia, m, name);
        static GameObject Box(Transform p, Vector3 pos, Vector3 size, Material m, string name, Quaternion? rot = null) => Prim(p, PrimitiveType.Cube, pos, size, m, name, rot);

        static GameObject Torus(Transform p, Vector3 pos, Quaternion rot, float R, float r, Material m, string name)
        {
            var prof = new List<Vector2>();
            const int n = 12;
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                prof.Add(new Vector2(R + r * Mathf.Cos(a), r * Mathf.Sin(a)));
            }
            return MeshKit.Part(p, MeshKit.Revolve(prof, 24, name, false), m, pos, rot, Vector3.one, name);
        }

        static void Casters(Transform g, float x, float z, float y, float dia, Material m)
        {
            Sph(g, new Vector3(x, y, z), dia, m, "Caster");
            Sph(g, new Vector3(-x, y, z), dia, m, "Caster");
            Sph(g, new Vector3(x, y, -z), dia, m, "Caster");
            Sph(g, new Vector3(-x, y, -z), dia, m, "Caster");
        }

        // ------------------------------------------------------------------ Dusty (the prototype, primitives)
        public static void Dusty(Transform g, VacuumSpec s)
        {
            var body = Palette.Plastic(new Color(0.95f, 0.38f, 0.28f));
            var dark = Palette.Rubber(new Color(0.15f, 0.15f, 0.18f));
            var bag = Palette.Fabric(new Color(0.92f, 0.82f, 0.55f));
            var gray = Palette.Plastic(Palette.Gray);
            Prim(g, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.95f, 0.12f, 0.95f), dark, "Base");
            Prim(g, PrimitiveType.Capsule, new Vector3(0f, 0.62f, -0.05f), new Vector3(0.7f, 0.42f, 0.7f), body, "Body");
            Sph(g, new Vector3(0f, 0.8f, -0.42f), 0.45f, bag, "Bag");
            Box(g, new Vector3(0f, 0.16f, 0.66f), new Vector3(0.95f, 0.16f, 0.42f), dark, "NozzleHead");
            Prim(g, PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0.32f), new Vector3(0.14f, 0.22f, 0.14f), gray, "Hose", Quaternion.Euler(90f, 0f, 0f));
            Prim(g, PrimitiveType.Cylinder, new Vector3(-0.42f, 0.12f, -0.2f), new Vector3(0.22f, 0.05f, 0.22f), dark, "Wheel", WheelRot);
            Prim(g, PrimitiveType.Cylinder, new Vector3(0.42f, 0.12f, -0.2f), new Vector3(0.22f, 0.05f, 0.22f), dark, "Wheel", WheelRot);
            Prim(g, PrimitiveType.Cylinder, new Vector3(0f, 0.95f, -0.55f), new Vector3(0.1f, 0.15f, 0.1f), gray, "Exhaust", Quaternion.Euler(60f, 0f, 0f));
        }

        // ------------------------------------------------------------------ Roomboo (robot disc)
        public static void Roomboo(Transform g, VacuumSpec s)
        {
            var top = Palette.Glossy(new Color(0.22f, 0.22f, 0.25f));
            var bumper = Palette.Rubber(new Color(0.08f, 0.08f, 0.09f));
            var accent = Palette.Glossy(new Color(0.2f, 0.75f, 0.55f));
            Rev(g, new[] { P(0f, 0.015f), P(0.40f, 0.015f), P(0.40f, 0.015f), P(0.42f, 0.03f), P(0.42f, 0.09f), P(0.40f, 0.11f), P(0.36f, 0.115f), P(0f, 0.115f) }, top, Vector3.zero, "Body", null, 48);
            Rev(g, new[] { P(0.42f, 0.02f), P(0.445f, 0.035f), P(0.445f, 0.085f), P(0.42f, 0.10f) }, bumper, Vector3.zero, "Bumper", null, 48, false);
            Rev(g, new[] { P(0f, 0.115f), P(0.09f, 0.115f), P(0.09f, 0.115f), P(0.10f, 0.125f), P(0.10f, 0.135f), P(0f, 0.135f) }, Palette.Chrome, Vector3.zero, "Button");
            Rev(g, new[] { P(0f, 0.135f), P(0.03f, 0.135f), P(0.03f, 0.15f), P(0f, 0.15f) }, accent, Vector3.zero, "ButtonTop");
            Sph(g, new Vector3(0f, 0.12f, 0.30f), 0.07f, bumper, "Sensor");
            Box(g, new Vector3(0f, 0.02f, 0.40f), new Vector3(0.24f, 0.02f, 0.02f), accent, "Bristles");

            var brush = new GameObject("SideBrush").transform;
            brush.SetParent(g, false);
            brush.localPosition = new Vector3(0.30f, 0.012f, 0.28f);
            for (int i = 0; i < 3; i++)
            {
                var rot = Quaternion.Euler(0f, i * 120f, 0f);
                Box(brush, rot * new Vector3(0.08f, 0f, 0f), new Vector3(0.16f, 0.008f, 0.02f), bumper, "Arm", rot);
            }
            brush.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 720f, 0f);

            Torus(g, new Vector3(0.30f, 0.05f, -0.05f), WheelRot, 0.035f, 0.02f, bumper, "Wheel");
            Torus(g, new Vector3(-0.30f, 0.05f, -0.05f), WheelRot, 0.035f, 0.02f, bumper, "Wheel");
        }

        // ------------------------------------------------------------------ Cyclonic (bagless upright on a ball)
        public static void Cyclonic(Transform g, VacuumSpec s)
        {
            var purple = Palette.Glossy(new Color(0.45f, 0.25f, 0.65f));
            var gray = Palette.Plastic(new Color(0.35f, 0.35f, 0.38f));
            var lightGray = Palette.Glossy(new Color(0.78f, 0.78f, 0.82f));
            var dark = Palette.Rubber(new Color(0.12f, 0.12f, 0.14f));
            RBox(g, 0.36f, 0.09f, 0.22f, 0.04f, gray, new Vector3(0f, 0f, 0.32f), "Head");
            Box(g, new Vector3(0f, 0.05f, 0.435f), new Vector3(0.30f, 0.02f, 0.012f), purple, "HeadStripe");
            Sph(g, new Vector3(0f, 0.19f, 0.06f), 0.38f, purple, "Ball");
            Torus(g, new Vector3(0f, 0.19f, 0.06f), WheelRot, 0.19f, 0.012f, Palette.Chrome, "BallBand");
            Rev(g, new[] { P(0f, 0.30f), P(0.08f, 0.30f), P(0.08f, 0.36f), P(0f, 0.36f) }, dark, new Vector3(0f, 0f, -0.02f), "Motor");
            Rev(g, new[] { P(0f, 0.34f), P(0.11f, 0.34f), P(0.11f, 0.34f), P(0.11f, 0.70f), P(0.11f, 0.70f), P(0f, 0.70f) }, lightGray, new Vector3(0f, 0f, -0.02f), "Bin");
            Rev(g, new[] { P(0f, 0.70f), P(0.11f, 0.70f), P(0.11f, 0.70f), P(0.13f, 0.72f), P(0.13f, 0.80f), P(0.09f, 0.90f), P(0.05f, 0.95f), P(0f, 0.95f) }, purple, new Vector3(0f, 0f, -0.02f), "Cyclone");
            TubeAlong(g, new[] { new Vector3(0f, 0.95f, -0.02f), new Vector3(0f, 1.12f, -0.05f), new Vector3(0f, 1.22f, -0.15f), new Vector3(0f, 1.22f, -0.30f) }, 0.02f, gray, "Handle");
            Box(g, new Vector3(0f, 1.22f, -0.26f), new Vector3(0.05f, 0.05f, 0.12f), dark, "Grip");
            TubeAlong(g, new[] { new Vector3(0f, 0.55f, -0.13f), new Vector3(0f, 0.40f, -0.26f), new Vector3(0f, 0.18f, -0.28f), new Vector3(0f, 0.07f, -0.08f), new Vector3(0f, 0.06f, 0.2f) }, 0.03f, gray, "Hose", true);
        }

        // ------------------------------------------------------------------ Harold (canister with a face)
        public static void Harold(Transform g, VacuumSpec s)
        {
            var red = Palette.Glossy(new Color(0.85f, 0.15f, 0.12f));
            var black = Palette.Rubber(new Color(0.08f, 0.08f, 0.09f));
            var gray = Palette.Plastic(new Color(0.4f, 0.4f, 0.42f));
            Rev(g, new[] { P(0f, 0f), P(0.25f, 0f), P(0.25f, 0f), P(0.25f, 0.06f), P(0.25f, 0.06f), P(0.24f, 0.06f) }, black, Vector3.zero, "Base");
            Rev(g, new[] { P(0f, 0.06f), P(0.24f, 0.06f), P(0.24f, 0.06f), P(0.24f, 0.38f), P(0.24f, 0.38f), P(0f, 0.38f) }, red, Vector3.zero, "Drum", null, 40);
            Rev(g, new[] { P(0f, 0.38f), P(0.24f, 0.38f), P(0.24f, 0.38f), P(0.255f, 0.40f), P(0.255f, 0.48f), P(0.21f, 0.52f), P(0.11f, 0.55f), P(0f, 0.56f) }, black, Vector3.zero, "Lid", null, 40);
            Box(g, new Vector3(-0.09f, 0.60f, 0f), new Vector3(0.03f, 0.10f, 0.03f), black, "Post");
            Box(g, new Vector3(0.09f, 0.60f, 0f), new Vector3(0.03f, 0.10f, 0.03f), black, "Post");
            Box(g, new Vector3(0f, 0.655f, 0f), new Vector3(0.22f, 0.03f, 0.03f), black, "HatBar");
            Sph(g, new Vector3(0f, 0.24f, 0.235f), 0.045f, black, "Nose");

            var smile = new List<Vector3>();
            for (int i = 0; i <= 12; i++)
            {
                float k = i / 12f;
                float phi = Mathf.Lerp(-38f, 38f, k) * Mathf.Deg2Rad;
                float u = (k - 0.5f) * 2f;
                smile.Add(new Vector3(Mathf.Sin(phi) * 0.247f, 0.145f + 0.05f * u * u, Mathf.Cos(phi) * 0.247f));
            }
            MeshKit.Part(g, MeshKit.Tube(smile, 0.012f, 8, 0f, 0f, "Smile"), black, Vector3.zero, Quaternion.identity, Vector3.one, "Smile");

            TubeAlong(g, new[] { new Vector3(0.10f, 0.50f, -0.12f), new Vector3(0.30f, 0.46f, 0.05f), new Vector3(0.36f, 0.28f, 0.35f), new Vector3(0.26f, 0.10f, 0.55f) }, 0.035f, gray, "Hose", true);
            TubeAlong(g, new[] { new Vector3(0.26f, 0.10f, 0.55f), new Vector3(0.12f, 0.06f, 0.72f), new Vector3(0.05f, 0.05f, 0.78f) }, 0.02f, Palette.Chrome, "Wand");
            RBox(g, 0.34f, 0.06f, 0.12f, 0.02f, black, new Vector3(0.03f, 0f, 0.80f), "Nozzle");
            Casters(g, 0.18f, 0.15f, 0.035f, 0.07f, gray);
        }

        // ------------------------------------------------------------------ Stickmaster (cordless stick)
        public static void Stickmaster(Transform g, VacuumSpec s)
        {
            var dark = Palette.Rubber(new Color(0.15f, 0.15f, 0.17f));
            var blue = Palette.Glossy(new Color(0.2f, 0.5f, 0.9f));
            var lightGray = Palette.Glossy(new Color(0.8f, 0.8f, 0.84f));
            var cyan = Palette.Glossy(new Color(0.3f, 0.9f, 1f));
            RBox(g, 0.30f, 0.06f, 0.16f, 0.02f, dark, new Vector3(0f, 0.005f, 0.42f), "Head");
            Rev(g, new[] { P(0f, -0.13f), P(0.03f, -0.13f), P(0.03f, 0.13f), P(0f, 0.13f) }, blue, new Vector3(0f, 0.035f, 0.47f), "Roller", WheelRot, 16);
            Box(g, new Vector3(0f, 0.05f, 0.50f), new Vector3(0.26f, 0.008f, 0.01f), cyan, "LED");
            TubeAlong(g, new[] { new Vector3(0f, 0.05f, 0.40f), new Vector3(0f, 0.45f, 0.18f), new Vector3(0f, 0.85f, -0.02f) }, 0.018f, Palette.Chrome, "Wand");

            var unit = new GameObject("Unit").transform;
            unit.SetParent(g, false);
            unit.localPosition = new Vector3(0f, 0.85f, -0.02f);
            unit.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            Rev(unit, new[] { P(0f, -0.06f), P(0.07f, -0.06f), P(0.07f, -0.06f), P(0.075f, 0f), P(0.075f, 0.02f), P(0.065f, 0.02f) }, dark, Vector3.zero, "Motor");
            Rev(unit, new[] { P(0f, 0.02f), P(0.065f, 0.02f), P(0.065f, 0.02f), P(0.065f, 0.24f), P(0.05f, 0.28f), P(0.05f, 0.32f), P(0f, 0.32f) }, lightGray, Vector3.zero, "Bin");
            Rev(unit, new[] { P(0.03f, 0.32f), P(0.03f, 0.36f), P(0f, 0.36f) }, dark, Vector3.zero, "Cap");

            TubeAlong(g, new[] { new Vector3(0f, 0.88f, -0.10f), new Vector3(0f, 1.02f, -0.22f), new Vector3(0f, 0.92f, -0.34f), new Vector3(0f, 0.78f, -0.28f), new Vector3(0f, 0.80f, -0.12f) }, 0.016f, dark, "Handle");
            Box(g, new Vector3(0f, 0.86f, -0.16f), new Vector3(0.03f, 0.05f, 0.02f), cyan, "Trigger");
        }

        // ------------------------------------------------------------------ Grandma's upright (bagged, headlight)
        public static void Grandma(Transform g, VacuumSpec s)
        {
            var green = Palette.Plastic(new Color(0.50f, 0.58f, 0.30f));
            var black = Palette.Rubber(new Color(0.1f, 0.1f, 0.1f));
            var bagMat = Palette.Fabric(new Color(0.78f, 0.66f, 0.45f));
            var yellow = Palette.Glossy(new Color(1f, 0.9f, 0.5f));
            RBox(g, 0.38f, 0.13f, 0.28f, 0.05f, green, new Vector3(0f, 0.01f, 0.28f), "Head");
            Box(g, new Vector3(0f, 0.09f, 0.42f), new Vector3(0.34f, 0.02f, 0.01f), Palette.Chrome, "ChromeStrip");
            Sph(g, new Vector3(0f, 0.10f, 0.42f), 0.06f, yellow, "Headlight");
            var lgo = new GameObject("HeadlightLight");
            lgo.transform.SetParent(g, false);
            lgo.transform.localPosition = new Vector3(0f, 0.14f, 0.55f);
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3f;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.92f, 0.7f);
            Rev(g, new[] { P(0f, 0.12f), P(0.15f, 0.12f), P(0.15f, 0.12f), P(0.15f, 0.27f), P(0.12f, 0.34f), P(0f, 0.36f) }, green, new Vector3(0f, 0f, 0.02f), "Motor");
            TubeAlong(g, new[] { new Vector3(0f, 0.30f, -0.02f), new Vector3(0f, 0.75f, -0.18f), new Vector3(0f, 1.15f, -0.36f) }, 0.018f, Palette.Chrome, "Handle");
            Box(g, new Vector3(0f, 1.17f, -0.37f), new Vector3(0.05f, 0.05f, 0.2f), black, "Grip", Quaternion.Euler(-25f, 0f, 0f));

            var bagT = new GameObject("BagPivot").transform;
            bagT.SetParent(g, false);
            bagT.localPosition = new Vector3(0f, 0.28f, -0.10f);
            bagT.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            Rev(bagT, new[] { P(0.05f, 0f), P(0.12f, 0.05f), P(0.16f, 0.30f), P(0.16f, 0.55f), P(0.12f, 0.72f), P(0.06f, 0.78f), P(0f, 0.78f) }, bagMat, Vector3.zero, "Bag", null, 24);

            Torus(g, new Vector3(0.19f, 0.06f, -0.02f), WheelRot, 0.045f, 0.02f, black, "Wheel");
            Torus(g, new Vector3(-0.19f, 0.06f, -0.02f), WheelRot, 0.045f, 0.02f, black, "Wheel");
            Sph(g, new Vector3(0.14f, 0.03f, 0.38f), 0.05f, black, "Caster");
            Sph(g, new Vector3(-0.14f, 0.03f, 0.38f), 0.05f, black, "Caster");
        }

        // ------------------------------------------------------------------ Shop Drum (wet and dry)
        public static void ShopDrum(Transform g, VacuumSpec s)
        {
            var yellow = Palette.Plastic(new Color(0.95f, 0.75f, 0.10f));
            var black = Palette.Rubber(new Color(0.09f, 0.09f, 0.1f));
            var gray = Palette.Plastic(new Color(0.35f, 0.35f, 0.37f));
            Rev(g, new[] { P(0f, 0.03f), P(0.31f, 0.03f), P(0.31f, 0.03f), P(0.32f, 0.12f), P(0.30f, 0.12f) }, black, Vector3.zero, "Skirt", null, 40);
            Rev(g, new[] { P(0f, 0.12f), P(0.30f, 0.12f), P(0.30f, 0.12f), P(0.30f, 0.22f), P(0.315f, 0.24f), P(0.30f, 0.26f), P(0.30f, 0.36f), P(0.315f, 0.38f), P(0.30f, 0.40f), P(0.30f, 0.50f), P(0.30f, 0.50f), P(0f, 0.50f) }, yellow, Vector3.zero, "Drum", null, 40);
            Rev(g, new[] { P(0f, 0.50f), P(0.30f, 0.50f), P(0.30f, 0.50f), P(0.325f, 0.52f), P(0.325f, 0.58f), P(0.22f, 0.62f), P(0.16f, 0.72f), P(0.16f, 0.72f), P(0.10f, 0.74f), P(0f, 0.74f) }, black, Vector3.zero, "Lid", null, 40);
            Rev(g, new[] { P(0.10f, 0.74f), P(0.10f, 0.78f), P(0f, 0.78f) }, Palette.Chrome, Vector3.zero, "VentCap");
            Box(g, new Vector3(0.31f, 0.50f, 0f), new Vector3(0.04f, 0.08f, 0.06f), gray, "Latch");
            Box(g, new Vector3(-0.31f, 0.50f, 0f), new Vector3(0.04f, 0.08f, 0.06f), gray, "Latch");
            Casters(g, 0.22f, 0.20f, 0.035f, 0.07f, black);
            TubeAlong(g, new[] { new Vector3(0.24f, 0.44f, 0.14f), new Vector3(0.42f, 0.30f, 0.38f), new Vector3(0.25f, 0.12f, 0.60f), new Vector3(0.06f, 0.07f, 0.70f) }, 0.045f, gray, "Hose", true);
            RBox(g, 0.42f, 0.08f, 0.14f, 0.03f, black, new Vector3(0f, 0f, 0.78f), "Nozzle");
        }
    }
}
