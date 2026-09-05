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
                VacuumVisuals.RealisticLook = look;
                Palette.Realistic = look;
                foreach (var s in VacuumCatalog.All)
                {
                    string file = Path.Combine(outDir, (look ? "after-" : "before-") + s.Id + ".png");
                    preview.RenderStill(s, -35f, 768, file);
                    Debug.Log("[VCS] Gallery " + file);
                    yield return null;
                }
            }
            VacuumVisuals.RealisticLook = true;
            Palette.Realistic = true;
            Debug.Log("[VCS] Gallery done");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }
    }
}
