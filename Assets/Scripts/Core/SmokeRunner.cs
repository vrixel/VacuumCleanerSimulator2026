using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace VCS.Core
{
    /// <summary>
    /// Automated run used by tools/smoke-test.ps1. Enabled by "-smoke &lt;outputDir&gt;" on the command line.
    /// Screenshots the title screen, starts a run, drives around for a few seconds, screenshots the game,
    /// logs the result and quits. No input injection, no window focus games.
    /// </summary>
    public class SmokeRunner : MonoBehaviour
    {
        string outDir;

        public static void TryStart(GameManager gm)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-smoke") continue;
                GameManager.SmokeMode = true;
                var r = gm.gameObject.AddComponent<SmokeRunner>();
                r.outDir = args[i + 1];
                return;
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outDir);
            Debug.Log("[VCS] Smoke test started, output " + outDir);
            yield return new WaitForSecondsRealtime(3f);
            yield return Capture("smoke-title.png");

            var gm = GameManager.I;
            var garage = VCS.Player.VacuumCatalog.All;
            string savedChoice = VCS.Player.VacuumCatalog.SelectedId;
            for (int i = 0; i < garage.Count; i++)
            {
                gm.Menu.SelectVacuumIndex(i);
                yield return new WaitForSecondsRealtime(0.7f);
                yield return Capture("smoke-model-" + garage[i].Id + ".png");
            }
            gm.Menu.SelectVacuumById("harold");
            yield return new WaitForSecondsRealtime(0.3f);
            gm.StartGame();
            yield return new WaitForSecondsRealtime(1f);
            GameInput.MoveOverride = new Vector2(0f, 1f);
            yield return new WaitForSecondsRealtime(1.5f);
            GameInput.MoveOverride = new Vector2(1f, 0f);
            yield return new WaitForSecondsRealtime(2.5f);
            GameInput.MoveOverride = new Vector2(0f, -1f);
            yield return new WaitForSecondsRealtime(1.5f);
            GameInput.MoveOverride = new Vector2(-1f, 0.3f);
            yield return new WaitForSecondsRealtime(2f);
            GameInput.MoveOverride = Vector2.zero;
            yield return Capture("smoke-game.png");

            var s = gm.Suction;
            string pos = gm.Player != null ? gm.Player.transform.position.ToString("F1") : "none";
            Debug.Log("[VCS] Smoke result: score=" + gm.Score + " power=" + gm.PowerLevel
                      + " bag=" + (s != null ? s.Bag.Count : 0)
                      + " cleaned=" + gm.Level.MessCleaned + "/" + gm.Level.MessTotal
                      + " pos=" + pos + " fps=" + (1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f)).ToString("F0"));
            yield return new WaitForSecondsRealtime(0.5f);
            VCS.Player.VacuumCatalog.SelectedId = savedChoice;
            PlayerPrefs.Save();
            Debug.Log("[VCS] Smoke test finished");
            GameManager.QuitApp();
        }

        IEnumerator Capture(string file)
        {
            string path = Path.Combine(outDir, file);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.8f);
            Debug.Log("[VCS] Screenshot " + path);
        }
    }
}
