using UnityEngine;
using VCS.World;

namespace VCS.Player
{
    /// <summary>
    /// The small parts that make a machine read as real: vent slots, brand badges without a name, hubcaps,
    /// screws, seams. Added on top of each VacuumModels builder, in the same local space (origin on the floor,
    /// +z forward, metres). Kept separate so the silhouettes in VacuumModels stay readable.
    /// </summary>
    public static class VacuumDetails
    {
        static Material Dark => Palette.Rubber(new Color(0.05f, 0.05f, 0.06f));
        static Material Steel => Palette.Chrome;
        static Material Plate => Palette.Glossy(new Color(0.16f, 0.16f, 0.18f));

        public static void Add(Transform g, VacuumSpec spec)
        {
            switch (spec.Id)
            {
                case "dusty":
                    Slots(g, new Vector3(0.34f, 0.66f, -0.12f), Quaternion.Euler(0f, 90f, 0f), 5, 0.045f, new Vector3(0.03f, 0.012f, 0.012f));
                    Slots(g, new Vector3(-0.34f, 0.66f, -0.12f), Quaternion.Euler(0f, -90f, 0f), 5, 0.045f, new Vector3(0.03f, 0.012f, 0.012f));
                    Hubcap(g, new Vector3(0.445f, 0.12f, -0.2f), Quaternion.Euler(0f, 90f, 0f), 0.09f);
                    Hubcap(g, new Vector3(-0.445f, 0.12f, -0.2f), Quaternion.Euler(0f, -90f, 0f), 0.09f);
                    Badge(g, new Vector3(0f, 0.8f, 0.29f), Quaternion.Euler(-30f, 0f, 0f), 0.14f, 0.05f);
                    Screws(g, new Vector3(0f, 0.245f, 0f), 0.44f, 6);
                    break;
                case "roomboo":
                    Ring(g, new Vector3(0f, 0f, 0f), 0.395f, 0.075f, 9, new Vector3(0.028f, 0.014f, 0.01f), 200f, 340f);
                    Badge(g, new Vector3(0.19f, 0.116f, 0.12f), Quaternion.identity, 0.09f, 0.03f);
                    Hubcap(g, new Vector3(0f, 0.116f, -0.25f), Quaternion.identity, 0.05f);
                    Screws(g, new Vector3(0f, 0.014f, 0f), 0.3f, 4);
                    break;
                case "cyclonic":
                    Ring(g, new Vector3(0f, 0f, -0.02f), 0.082f, 0.335f, 10, new Vector3(0.022f, 0.02f, 0.008f), 0f, 360f);
                    Badge(g, new Vector3(0f, 0.55f, 0.095f), Quaternion.identity, 0.08f, 0.035f);
                    Hubcap(g, new Vector3(0.19f, 0.19f, 0.06f), Quaternion.Euler(0f, 90f, 0f), 0.07f);
                    Hubcap(g, new Vector3(-0.19f, 0.19f, 0.06f), Quaternion.Euler(0f, -90f, 0f), 0.07f);
                    Slots(g, new Vector3(0f, 0.045f, 0.21f), Quaternion.identity, 7, 0.04f, new Vector3(0.03f, 0.01f, 0.01f));
                    break;
                case "harold":
                    Ring(g, Vector3.zero, 0.242f, 0.33f, 8, new Vector3(0.03f, 0.016f, 0.01f), 200f, 340f);
                    Badge(g, new Vector3(0f, 0.50f, 0.2f), Quaternion.Euler(-50f, 0f, 0f), 0.09f, 0.035f);
                    Screws(g, new Vector3(0f, 0.065f, 0f), 0.235f, 6);
                    Seam(g, 0.248f, 0.385f, Dark);
                    break;
                case "stick":
                    Slots(g, new Vector3(0f, 0.04f, 0.345f), Quaternion.identity, 6, 0.04f, new Vector3(0.03f, 0.012f, 0.01f));
                    Badge(g, new Vector3(0f, 0.037f, 0.42f), Quaternion.Euler(90f, 0f, 0f), 0.07f, 0.028f);
                    Hubcap(g, new Vector3(0.16f, 0.035f, 0.47f), Quaternion.Euler(0f, 90f, 0f), 0.04f);
                    Hubcap(g, new Vector3(-0.16f, 0.035f, 0.47f), Quaternion.Euler(0f, -90f, 0f), 0.04f);
                    break;
                case "grandma":
                    Ring(g, new Vector3(0f, 0f, 0.02f), 0.152f, 0.2f, 12, new Vector3(0.024f, 0.03f, 0.008f), 20f, 160f);
                    Badge(g, new Vector3(0f, 0.145f, 0.30f), Quaternion.Euler(90f, 0f, 0f), 0.12f, 0.04f);
                    Hubcap(g, new Vector3(0.212f, 0.06f, -0.02f), Quaternion.Euler(0f, 90f, 0f), 0.05f);
                    Hubcap(g, new Vector3(-0.212f, 0.06f, -0.02f), Quaternion.Euler(0f, -90f, 0f), 0.05f);
                    Screws(g, new Vector3(0f, 0.075f, 0.28f), 0.17f, 4);
                    break;
                case "rowinta":
                    Slots(g, new Vector3(0.2f, 0.24f, -0.2f), Quaternion.Euler(0f, 90f, 0f), 6, 0.03f, new Vector3(0.02f, 0.014f, 0.01f));
                    Slots(g, new Vector3(-0.2f, 0.24f, -0.2f), Quaternion.Euler(0f, -90f, 0f), 6, 0.03f, new Vector3(0.02f, 0.014f, 0.01f));
                    Badge(g, new Vector3(0f, 0.336f, 0.13f), Quaternion.Euler(90f, 0f, 0f), 0.10f, 0.035f);
                    Hubcap(g, new Vector3(0.265f, 0.065f, -0.19f), Quaternion.Euler(0f, 90f, 0f), 0.055f);
                    Hubcap(g, new Vector3(-0.265f, 0.065f, -0.19f), Quaternion.Euler(0f, -90f, 0f), 0.055f);
                    break;
                case "shopdrum":
                    Ring(g, Vector3.zero, 0.19f, 0.66f, 14, new Vector3(0.03f, 0.03f, 0.01f), 0f, 360f);
                    Badge(g, new Vector3(0f, 0.31f, 0.305f), Quaternion.identity, 0.16f, 0.06f);
                    Screws(g, new Vector3(0f, 0.125f, 0f), 0.30f, 8);
                    Seam(g, 0.305f, 0.31f, Dark);
                    break;
            }
        }

        static GameObject Prim(Transform p, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, string name, Quaternion rot)
        {
            var go = PropFactory.Prim(type, p, pos, scale, Color.white, name, false, rot);
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>A row of dark vent slots along the local x axis of rot, facing local +z.</summary>
        static void Slots(Transform g, Vector3 center, Quaternion rot, int n, float pitch, Vector3 slot)
        {
            for (int i = 0; i < n; i++)
            {
                float x = (i - (n - 1) * 0.5f) * pitch;
                Prim(g, PrimitiveType.Cube, center + rot * new Vector3(x, 0f, 0f), slot, Dark, "Vent", rot);
            }
        }

        /// <summary>Vent slots around a vertical axis, each facing outward, between two angles in degrees.</summary>
        static void Ring(Transform g, Vector3 center, float radius, float y, int n, Vector3 slot, float fromDeg, float toDeg)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, n > 1 ? i / (float)(n - 1) : 0f) * Mathf.Deg2Rad;
                var pos = center + new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                var rot = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up);
                Prim(g, PrimitiveType.Cube, pos, slot, Dark, "Vent", rot);
            }
        }

        /// <summary>A brand plate with no brand on it: chrome rim, dark face, a thin accent bar.</summary>
        static void Badge(Transform g, Vector3 pos, Quaternion rot, float w, float h)
        {
            Prim(g, PrimitiveType.Cube, pos, new Vector3(w, h, 0.006f), Steel, "BadgeRim", rot);
            Prim(g, PrimitiveType.Cube, pos + rot * new Vector3(0f, 0f, 0.004f), new Vector3(w * 0.86f, h * 0.7f, 0.004f), Plate, "BadgeFace", rot);
            Prim(g, PrimitiveType.Cube, pos + rot * new Vector3(0f, -h * 0.1f, 0.007f), new Vector3(w * 0.5f, h * 0.12f, 0.002f), Steel, "BadgeBar", rot);
        }

        /// <summary>Chrome disc with a dark centre, on a wheel or an axle end. rot points local +z along the axle.</summary>
        static void Hubcap(Transform g, Vector3 pos, Quaternion rot, float dia)
        {
            Prim(g, PrimitiveType.Cylinder, pos, new Vector3(dia, 0.004f, dia), Steel, "Hubcap", rot * Quaternion.Euler(90f, 0f, 0f));
            Prim(g, PrimitiveType.Cylinder, pos + rot * new Vector3(0f, 0f, 0.003f), new Vector3(dia * 0.35f, 0.004f, dia * 0.35f), Dark, "HubcapCentre", rot * Quaternion.Euler(90f, 0f, 0f));
        }

        /// <summary>Tiny screw heads around a vertical axis at height y.</summary>
        static void Screws(Transform g, Vector3 center, float radius, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f + 0.3f;
                var pos = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                var rot = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                Prim(g, PrimitiveType.Cylinder, pos, new Vector3(0.014f, 0.003f, 0.014f), Steel, "Screw", rot);
            }
        }

        /// <summary>A thin dark ring where two shells meet.</summary>
        static void Seam(Transform g, float radius, float y, Material m)
        {
            var prof = new System.Collections.Generic.List<Vector2>();
            const int n = 8;
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                prof.Add(new Vector2(radius + 0.004f * Mathf.Cos(a), y + 0.004f * Mathf.Sin(a)));
            }
            MeshKit.Part(g, MeshKit.Revolve(prof, 40, "Seam", false), m, Vector3.zero, Quaternion.identity, Vector3.one, "Seam");
        }
    }
}
