using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VCS.Player;
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

        public static void TryStart(GameManager gm)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-gallery") continue;
                GameManager.SmokeMode = true;
                var r = gm.gameObject.AddComponent<GalleryRunner>();
                r.outDir = args[i + 1];
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
            foreach (var look in new[] { false, true })
            {
                VacuumVisuals.RealisticLook = true;
                Palette.Realistic = true;
                VacuumModels.UseV2 = look;
                foreach (var s in VacuumCatalog.All)
                {
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
    }
}
