using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VCS.Player;
using VCS.UI;
using VCS.World;

namespace VCS.Core
{
    /// <summary>
    /// "-gallery &lt;dir&gt;" on the command line: renders every vacuum of the garage as a PNG still, once in the old
    /// cartoon look (googly eyes, flat materials, no details) and once in the realistic look, then quits.
    /// Used to compare looks side by side; see tools/gallery.ps1.
    /// </summary>
    public class GalleryRunner : MonoBehaviour
    {
        string outDir;
        bool museum;   // "-museum <dir>": orientation diagnostics of the museum pieces instead of the gallery

        public static void TryStart(GameManager gm)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-gallery" && args[i] != "-museum") continue;
                GameManager.SmokeMode = true;
                var r = gm.gameObject.AddComponent<GalleryRunner>();
                r.outDir = args[i + 1];
                r.museum = args[i] == "-museum";
                return;
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outDir);
            yield return new WaitForSecondsRealtime(1.5f);
            var gm = GameManager.I;
            var preview = gm.Menu.Preview;
            preview.Hide();
            if (museum)
            {
                yield return Museum(preview);
                yield break;
            }
            foreach (var look in new[] { false, true })
            {
                VacuumVisuals.RealisticLook = true;
                Palette.Realistic = true;
                VacuumModels.UseV2 = look;
                foreach (var s in VacuumCatalog.All)
                {
                    if (s.Hidden) continue;   // museum pieces are rendered by the import loop below
                    string file = Path.Combine(outDir, (look ? "after-" : "before-") + s.Id + ".png");
                    preview.RenderStill(s, -35f, 768, file);
                    Debug.Log("[VCS] Gallery " + file);
                    yield return null;
                }
            }
            VacuumModels.UseV2 = true;
            // Imported models (Assets/Resources/Models/*.fbx, decimated by tools/lowpoly.py): the feasibility test
            // for real-product meshes. Rendered on the same stage, same framing.
            foreach (var prefab in Resources.LoadAll<GameObject>("Models"))
            {
                var spec = new VacuumSpec { Id = "import-" + prefab.name, Name = prefab.name, Build = (g, s2) =>
                {
                    var inst = UnityEngine.Object.Instantiate(prefab, g);
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    // Normalise whatever the importer produced: largest horizontal extent 0.6 m, base on the floor,
                    // centred on the stage axis (the same convention as the built-in models).
                    var rs = inst.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0)
                    {
                        Bounds nb = rs[0].bounds;
                        foreach (var r in rs) nb.Encapsulate(r.bounds);
                        float horiz = Mathf.Max(nb.size.x, nb.size.z);
                        float k = horiz > 1e-9f ? 0.6f / horiz : 1f;   // importers can hand back centimetre or micro-scale meshes
                        inst.transform.localScale = Vector3.one * k;
                        nb = rs[0].bounds;
                        foreach (var r in rs) nb.Encapsulate(r.bounds);
                        Vector3 shift = g.position - new Vector3(nb.center.x, nb.min.y, nb.center.z);
                        inst.transform.position += shift;
                    }
                    int tris = 0;
                    foreach (var mf in inst.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null) for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++) tris += (int)mf.sharedMesh.GetIndexCount(sm) / 3;
                    // What Unity made of the FBX: bounds in stage space, root rotation and scale, materials.
                    var rends = inst.GetComponentsInChildren<Renderer>();
                    Bounds b = new Bounds(g.position, Vector3.zero);
                    bool first = true;
                    foreach (var r in rends) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                    var t0 = inst.transform.childCount > 0 ? inst.transform.GetChild(0) : inst.transform;
                    string mats = "";
                    foreach (var r in rends) foreach (var m in r.sharedMaterials) if (m != null) mats += m.name + (m.mainTexture != null ? "(tex)" : "(flat)") + " ";
                    Debug.Log("[VCS] Gallery import " + prefab.name + ": " + tris + " tris, renderers " + rends.Length
                              + ", bounds centre " + (b.center - g.position).ToString("F2") + " size " + b.size.ToString("F2")
                              + ", root rot " + inst.transform.localRotation.eulerAngles.ToString("F0") + " scale " + inst.transform.localScale.ToString("F2")
                              + ", child rot " + t0.localRotation.eulerAngles.ToString("F0") + " scale " + t0.localScale.ToString("F2") + " | " + mats);
                } };
                string file = Path.Combine(outDir, "import-" + prefab.name + ".png");
                preview.RenderStill(spec, -35f, 768, file);
                yield return null;
            }
            Debug.Log("[VCS] Gallery done");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        /// <summary>
        /// Each museum piece as the game builds it (yaw and normalisation applied), with a red ball at the nozzle
        /// point and a yellow bar from the pivot along +z (the driving direction), seen from two sides; the bounds
        /// go to the log. Used to set ImportedVacuums.Yaw and Nozzle so the head faces where the vacuum drives.
        /// </summary>
        IEnumerator Museum(VacuumPreview preview)
        {
            VacuumVisuals.RealisticLook = true;
            Palette.Realistic = true;
            foreach (var s in VacuumCatalog.All)
            {
                if (!s.Hidden) continue;
                var spec = s;
                var marked = new VacuumSpec { Id = spec.Id, Name = spec.Name, Height = spec.Height, NozzleLocal = spec.NozzleLocal, Build = (g, s2) =>
                {
                    spec.Build(g, spec);
                    var rs = g.GetComponentsInChildren<Renderer>();
                    Bounds b = new Bounds(g.position, Vector3.zero);
                    bool first = true;
                    foreach (var r in rs) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                    Vector3 lo = g.InverseTransformPoint(b.min), hi = g.InverseTransformPoint(b.max);
                    Debug.Log("[VCS] Museum " + spec.Id + " (" + spec.Name + "): local bounds x " + Mathf.Min(lo.x, hi.x).ToString("F2") + ".." + Mathf.Max(lo.x, hi.x).ToString("F2")
                              + " y " + Mathf.Min(lo.y, hi.y).ToString("F2") + ".." + Mathf.Max(lo.y, hi.y).ToString("F2")
                              + " z " + Mathf.Min(lo.z, hi.z).ToString("F2") + ".." + Mathf.Max(lo.z, hi.z).ToString("F2") + ", nozzle " + spec.NozzleLocal.ToString("F2"));
                    PropFactory.Prim(PrimitiveType.Sphere, g, spec.NozzleLocal, Vector3.one * 0.07f, new Color(1f, 0.1f, 0.1f), "NozzleMarker", false);
                    float len = Mathf.Max(0.3f, Mathf.Max(lo.z, hi.z) + 0.15f);
                    PropFactory.Prim(PrimitiveType.Cube, g, new Vector3(0f, 0.015f, len * 0.5f), new Vector3(0.03f, 0.03f, len), new Color(1f, 0.85f, 0.1f), "ForwardBar", false);
                    PropFactory.Prim(PrimitiveType.Cube, g, new Vector3(0f, 0.015f, len), new Vector3(0.12f, 0.03f, 0.06f), new Color(1f, 0.85f, 0.1f), "ForwardTip", false);
                    // +x in blue, ticks every 0.25 m on both bars so distances can be read off the top view
                    PropFactory.Prim(PrimitiveType.Cube, g, new Vector3(0.2f, 0.015f, 0f), new Vector3(0.4f, 0.03f, 0.03f), new Color(0.2f, 0.4f, 1f), "RightBar", false);
                    for (int t = 1; t * 0.25f < len; t++)
                        PropFactory.Prim(PrimitiveType.Cube, g, new Vector3(0f, 0.02f, t * 0.25f), new Vector3(0.08f, 0.03f, 0.015f), Color.black, "Tick", false);
                } };
                preview.RenderStill(marked, -35f, 512, Path.Combine(outDir, "museum-" + spec.Id + "-a.png"));
                yield return null;
                preview.RenderStill(marked, 145f, 512, Path.Combine(outDir, "museum-" + spec.Id + "-b.png"));
                yield return null;
                preview.RenderTopDown(marked, 512, Path.Combine(outDir, "museum-" + spec.Id + "-top.png"));
                yield return null;
            }
            Debug.Log("[VCS] Museum diagnostics done");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }
    }
}
