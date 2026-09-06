using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VCS.Player;

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
            var garage = VCS.Player.VacuumCatalog.Visible;
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
            yield return new WaitForSecondsRealtime(0.6f);
            // the boost: trail, camera kick, speed lines and the turbo tile, photographed mid-run
            GameInput.TurboOverride = true;
            yield return new WaitForSecondsRealtime(1.3f);
            Debug.Log("[VCS] Boost: " + gm.Player.BoostDebug() + ", speed " + gm.Player.Speed.ToString("0.0"));
            yield return Capture("smoke-turbo.png");
            GameInput.TurboOverride = false;
            yield return new WaitForSecondsRealtime(0.6f);
            GameInput.MoveOverride = new Vector2(0f, -1f);
            yield return new WaitForSecondsRealtime(1.5f);
            GameInput.MoveOverride = new Vector2(-1f, 0.3f);
            yield return new WaitForSecondsRealtime(2f);
            GameInput.MoveOverride = Vector2.zero;
            yield return Capture("smoke-game.png");

            // The cat: drive at it for a few seconds, it should bolt; shoot it while it runs and log its state.
            if (gm.Level != null && gm.Level.Cat != null && gm.Player != null)
            {
                var cat = gm.Level.Cat;
                float tc = 0f;
                bool shot = false;
                while (tc < 6f)
                {
                    Vector3 to = cat.transform.position - gm.Player.transform.position;
                    to.y = 0f;
                    var camT = Camera.main != null ? Camera.main.transform : null;
                    Vector3 fwd = camT != null ? Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized : Vector3.forward;
                    Vector3 right = Vector3.Cross(Vector3.up, fwd);
                    Vector3 dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward;
                    GameInput.MoveOverride = new Vector2(Vector3.Dot(dir, right), Vector3.Dot(dir, fwd));
                    yield return new WaitForSecondsRealtime(0.1f);
                    tc += 0.1f;
                    if (!shot && to.magnitude < 4.5f) { shot = true; yield return Capture("smoke-cat.png"); }
                }
                GameInput.MoveOverride = Vector2.zero;
                Debug.Log("[VCS] Cat: distance " + Vector3.Distance(cat.transform.position, gm.Player.transform.position).ToString("0.0")
                          + " m, state " + cat.State + ", speed " + cat.Speed.ToString("0.0") + " m/s, at " + cat.transform.position
                          + ", powder cleaned " + (gm.Level.Powder != null ? gm.Level.Powder.CleanedSqm.ToString("0.00") : "-") + " m2 of "
                          + (gm.Level.Powder != null ? gm.Level.Powder.TotalSqm.ToString("0.0") : "-"));
                // Straight down over the vacuum: the cleared path through the powder should read as a trail.
                gm.Cam.SetView(80f, 11f);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Capture("smoke-powder.png");
                gm.Cam.SetView(42f, 9f);
            }
            if (gm.Player != null && gm.Player.Cord != null && !gm.Player.Cord.Plugged && gm.Level.Sockets.Count > 0)
            {
                // The drive above can use the whole cable and pop the plug; park next to the first socket to replug.
                var sk = gm.Level.Sockets[0];
                gm.Player.Rb.position = sk.transform.position + sk.transform.forward * 0.9f + Vector3.up * 0.3f;
                gm.Player.Rb.linearVelocity = Vector3.zero;
                yield return new WaitForSecondsRealtime(0.6f);
                Debug.Log("[VCS] Cord: replugged for the end-of-cord phase: " + gm.Player.Cord.Plugged);
            }
            if (gm.Player != null && gm.Player.Cord != null && gm.Player.Cord.Plugged)
            {
                // End of the cord: shorten it, drive away from the socket until it is taut, keep pulling until the
                // plug pops out of the wall, then watch the cord reel itself in.
                var cord = gm.Player.Cord;
                float saved = PowerCord.MaxLength;
                PowerCord.MaxLength = 4f;
                float t = 0f, tautAt = -1f, yankAt = -1f;
                while (t < 12f)
                {
                    // Pull straight away from whatever the cord is caught on, in camera-relative input terms.
                    Vector3 away = gm.Player.transform.position - cord.LastCorner;
                    away.y = 0f;
                    var camT = Camera.main != null ? Camera.main.transform : null;
                    Vector3 fwd = camT != null ? Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized : Vector3.forward;
                    Vector3 right = Vector3.Cross(Vector3.up, fwd);
                    if (away.sqrMagnitude < 0.01f) away = Vector3.right;
                    away.Normalize();
                    GameInput.MoveOverride = new Vector2(Vector3.Dot(away, right), Vector3.Dot(away, fwd));
                    yield return new WaitForSecondsRealtime(0.1f);
                    t += 0.1f;
                    if (tautAt < 0f && cord.Taut) { tautAt = t; yield return Capture("smoke-taut.png"); }
                    if (!cord.Plugged) { yankAt = t; break; }
                }
                Debug.Log("[VCS] Cord: taut at " + tautAt.ToString("0.0") + " s, plug yanked at " + yankAt.ToString("0.0")
                          + " s, length " + cord.Length.ToString("0.0") + " m");
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Capture("smoke-rewind.png");
                float w = 0f;
                while (cord.Rewinding && w < 5f) { yield return new WaitForSecondsRealtime(0.1f); w += 0.1f; }
                GameInput.MoveOverride = Vector2.zero;
                PowerCord.MaxLength = saved;
                Debug.Log("[VCS] Cord: rewound " + cord.TotalRewound.ToString("0.0") + " m, plugged=" + cord.Plugged);
            }

            var s = gm.Suction;
            string pos = gm.Player != null ? gm.Player.transform.position.ToString("F1") : "none";
            var tm = gm.Telemetry;
            Debug.Log("[VCS] Telemetry: suction=" + tm.SuctionValue.ToString("0.0") + " rpm=" + tm.Rpm.ToString("0")
                      + " temp=" + tm.TempC.ToString("0") + " filter=" + tm.Filter01.ToString("0.00") + " odo=" + tm.OdometerM.ToString("0.0")
                      + " ingested=" + tm.ItemsIngested);
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
