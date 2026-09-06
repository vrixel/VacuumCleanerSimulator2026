using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VCS.Core;
using VCS.Objectives;
using VCS.Player;

namespace VCS.UI
{
    /// <summary>
    /// The in-game overlay: arcade-cabinet boldness on serious instrument frames. Score plate top-left, power strip
    /// top-centre, timer and dirt radar top-right, scrolling tapes on the left, mission log on the right, the
    /// cockpit along the bottom, banners in the middle. Frames are generated plates; everything on them is live.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        Canvas canvas;
        Text scoreText, comboText, powerText, timeText, achievementsText, bannerBig, bannerSmall, hintText, binPrompt;
        Image[] powerTiles;
        Text[] powerTileTexts;
        Image[] logTiles;
        Text[] logTileTexts, logTitles, logDescs;
        Tape tapeSuction, tapeTemp, tapeThird;
        CanvasGroup bannerGroup, hintGroup, binGroup;
        RectTransform bannerRect, scoreRect, comboRect;
        Image[] sparkles;
        Image bannerRays;
        Image speedLines;
        float speedLinesAlpha;
        RectTransform toastRect;
        CanvasGroup toastGroup;
        Text toastText;
        readonly Queue<string> toasts = new Queue<string>();
        float toastT = -1f;
        const float ToastDur = 2.6f;
        float[] sparklePhase;
        Cockpit cockpit;
        RadarView radar;
        GameObject playerMarker;

        int targetScore, lastCombo = -1, lastTime = -1, lastPower = -1;
        float displayScore;
        bool lastBinPrompt, thirdIsBattery;
        float bannerT, bannerDur, hintT, hintDur, objectivesTimer, scorePunch, sparkleBurst;
        readonly List<Objective> objBuffer = new List<Objective>();
        static readonly Color[] PowerColors = { UIStyle.Green, UIStyle.Blue, UIStyle.Yellow, UIStyle.Amber, UIStyle.Red };

        public static HudController Create()
        {
            var canvas = UIFactory.CreateCanvas("HUD", 10);
            var h = canvas.gameObject.AddComponent<HudController>();
            h.canvas = canvas;
            h.Build();
            return h;
        }

        void Build()
        {
            var t = canvas.transform;
            var tl = new Vector2(0f, 1f);
            var tc = new Vector2(0.5f, 1f);
            var tr = new Vector2(1f, 1f);

            // ---- boost: radial speed lines over the whole picture, under every plate (first child draws first)
            if (UIStyle.Has("speed_lines"))
            {
                speedLines = UIStyle.Simple(t, "SpeedLines", "speed_lines", new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.one, new Vector2(-80f, -80f), new Vector2(80f, 80f), Color.clear, false);
                speedLines.raycastTarget = false;
            }

            // ---- top-left: score plate
            var scoreBox = UIStyle.Frame(t, "ScoreBox", tl, tl, new Vector2(30f, -230f), new Vector2(560f, -30f), "plate_score", 530f, 200f, 34f);
            UIStyle.Tab(scoreBox, "ScoreTab", "SCORE", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -26f), new Vector2(120f, 0f));
            scoreText = UIFactory.Text(scoreBox, "Score", "0", 96, Color.white, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(4f, 30f), new Vector2(-4f, -24f), false);
            UIStyle.Style(scoreText, UIStyle.Arcade, 92, Color.white, FontStyle.Italic);
            UIStyle.ArcadeText(scoreText, Color.white, UIStyle.Yellow, 5f);
            scoreRect = scoreText.rectTransform;
            comboText = UIFactory.Text(scoreBox, "Combo", "", 22, UIStyle.Blue, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 0f), new Vector2(-6f, 30f), false);
            UIStyle.ArcadeText(comboText, UIStyle.Blue, UIStyle.Ink, 2f);
            comboText.fontSize = 22;
            comboRect = comboText.rectTransform;
            sparkles = new Image[6];
            sparklePhase = new float[6];
            for (int i = 0; i < sparkles.Length; i++)
            {
                float x = 40f + i * 90f, y = -60f - (i % 2) * 120f;
                sparkles[i] = UIFactory.Panel(t, "Sparkle" + i, new Color(1f, 1f, 0.8f, 0f), tl, tl, new Vector2(x, y - 40f), new Vector2(x + 80f, y + 40f));
                sparkles[i].sprite = UISprites.Sparkle;
                sparklePhase[i] = i * 0.37f;
            }

            // ---- top-centre: power strip
            var powerBox = UIStyle.Frame(t, "PowerBox", tc, tc, new Vector2(-600f, -126f), new Vector2(600f, -30f), "frame_wide", 1200f, 96f, 22f);
            powerText = UIFactory.Text(powerBox, "Power", "", 22, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, -4f), new Vector2(0f, 0f), false);
            UIStyle.ArcadeText(powerText, Color.white, UIStyle.Blue, 3f);
            powerText.fontSize = 21;
            powerTiles = new Image[GameManager.MaxPower];
            powerTileTexts = new Text[GameManager.MaxPower];
            for (int i = 0; i < powerTiles.Length; i++)
            {
                float x = -460f + i * 186f;
                powerTiles[i] = UIStyle.Tile(powerBox, "PowerTile" + i, "POWER " + (i + 1), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 0f), new Vector2(x + 176f, 24f), out powerTileTexts[i]);
            }

            // ---- top-right: timer
            var timeBox = UIStyle.Frame(t, "TimeBox", tr, tr, new Vector2(-340f, -126f), new Vector2(-30f, -30f), "frame_wide", 310f, 96f, 20f);
            var timeLabel = UIFactory.Text(timeBox, "TimeLabel", "TIME", 14, UIStyle.Yellow, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(2f, 0f), new Vector2(0f, -1f), false);
            UIStyle.Style(timeLabel, UIStyle.Arcade, 13, UIStyle.Yellow, FontStyle.Italic);
            timeText = UIStyle.Digital(timeBox, "Time", "88:88", 44, UIStyle.Green, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(0f, -2f), new Vector2(-2f, -6f));

            // ---- top-right: radar
            var radarBox = UIStyle.Frame(t, "RadarBox", tr, tr, new Vector2(-320f, -420f), new Vector2(-30f, -140f), "frame_square", 290f, 280f, 30f);
            radar = RadarView.Build(radarBox, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f), new Vector3(14f, 0f, 10f), 15f);
            UIStyle.Tab(radarBox, "RadarTab", "DIRT RADAR", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(-8f, -8f), new Vector2(150f, 18f));

            // ---- left: instrument tapes
            var mid = new Vector2(0f, 0.5f);
            var tapesBox = UIStyle.Frame(t, "TapesBox", mid, mid, new Vector2(30f, -230f), new Vector2(280f, 230f), "frame_tall", 250f, 460f, 26f);
            const float tapeH = 460f - 52f;
            tapeSuction = Tape.Build(tapesBox, "TapeSuction", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(62f, 0f), 0f, 100f, 25f, "SUCT %", UIStyle.Yellow, tapeH);
            tapeTemp = Tape.Build(tapesBox, "TapeTemp", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(68f, 0f), new Vector2(130f, 0f), 20f, 120f, 25f, "TEMP C", UIStyle.Amber, tapeH);
            tapeThird = Tape.Build(tapesBox, "TapeThird", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(136f, 0f), new Vector2(198f, 0f), 0f, 100f, 25f, "CORD %", UIStyle.Blue, tapeH);
            UIStyle.Tab(tapesBox, "TapesTab", "INSTRUMENTS", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-10f, 4f), new Vector2(190f, 30f));

            // ---- right: mission log
            var rmid = new Vector2(1f, 0.5f);
            var logBox = UIStyle.Frame(t, "LogBox", rmid, rmid, new Vector2(-520f, -300f), new Vector2(-30f, 30f), "frame_square", 490f, 330f, 30f);
            UIStyle.Tab(logBox, "LogTab", "MISSION LOG", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-10f, 4f), new Vector2(190f, 30f));
            logTiles = new Image[3];
            logTileTexts = new Text[3];
            logTitles = new Text[3];
            logDescs = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = -14f - i * 80f;
                logTiles[i] = UIStyle.Tile(logBox, "LogTile" + i, "0 / 0", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y - 56f), new Vector2(92f, y - 6f), out logTileTexts[i]);
                logTileTexts[i].font = UIStyle.Mono;
                logTileTexts[i].fontSize = 18;
                logTitles[i] = UIFactory.Text(logBox, "LogTitle" + i, "", 22, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(104f, y - 34f), new Vector2(0f, y - 4f), false);
                UIStyle.ArcadeText(logTitles[i], Color.white, UIStyle.Ink, 2f);
                logTitles[i].fontSize = 19;
                logDescs[i] = UIFactory.Text(logBox, "LogDesc" + i, "", 17, UIStyle.Steel, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(104f, y - 58f), new Vector2(0f, y - 34f), false);
                UIStyle.Style(logDescs[i], UIStyle.Body, 17, UIStyle.Steel);
                UIStyle.Edge(logDescs[i], 1.5f);
            }
            achievementsText = UIFactory.Text(logBox, "Achievements", "", 15, UIStyle.Yellow, TextAnchor.LowerRight,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 22f), false);
            UIStyle.Style(achievementsText, UIStyle.Mono, 15, UIStyle.Yellow);
            UIStyle.Edge(achievementsText);

            // ---- banner
            var bannerHolder = new GameObject("BannerHolder", typeof(RectTransform));
            bannerHolder.transform.SetParent(t, false);
            // upper band, between the vacuum and the power strip: the splash never sits on the player
            UIFactory.Anchor(bannerHolder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-500f, 110f), new Vector2(500f, 270f));
            bannerRect = bannerHolder.GetComponent<RectTransform>();
            bannerGroup = bannerHolder.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            // Bonus text deserves a splash, not a black plate (his feedback, 2026-09-06): a slowly turning ray wheel
            // under a comic bang burst, both generated; the framed plate only when the art is missing.
            RectTransform bannerBox;
            if (UIStyle.Has("banner_burst"))
            {
                bannerRays = UIStyle.Simple(bannerHolder.transform, "Rays", "banner_rays", new Color(1f, 1f, 1f, 0.65f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190f, -190f), new Vector2(190f, 190f), Color.clear, true);
                bannerRays.raycastTarget = false;
                // the burst is stretched wide so the headline sits inside its core
                var burst = UIStyle.Simple(bannerHolder.transform, "Burst", "banner_burst", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-430f, -150f), new Vector2(430f, 150f), Color.clear, false);
                burst.raycastTarget = false;
                var inner = new GameObject("Screen", typeof(RectTransform));
                inner.transform.SetParent(bannerHolder.transform, false);
                UIFactory.Anchor(inner, Vector2.zero, Vector2.one, new Vector2(70f, 4f), new Vector2(-70f, -4f));
                bannerBox = inner.GetComponent<RectTransform>();
            }
            else bannerBox = UIStyle.Frame(bannerHolder.transform, "Banner", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "plate_banner", 1000f, 160f, 30f);
            bannerBig = UIFactory.Text(bannerBox, "Big", "", 60, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -2f), false);
            UIStyle.Style(bannerBig, UIStyle.Arcade, 48, Color.white, FontStyle.Italic);
            // blue edge on the yellow burst, yellow edge on the dark plate
            UIStyle.ArcadeText(bannerBig, Color.white, UIStyle.Has("banner_burst") ? UIStyle.Blue : UIStyle.Yellow, 4f);
            bannerSmall = UIFactory.Text(bannerBox, "Small", "", 22, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0.42f), new Vector2(0f, 4f), new Vector2(0f, 0f), false);
            UIStyle.Style(bannerSmall, UIStyle.Body, 20, Color.white, FontStyle.Bold);
            UIStyle.Edge(bannerSmall, 2.5f);

            // ---- toast strip: one slim enamel line right under the power strip, queued
            var toastGo = new GameObject("Toast", typeof(RectTransform));
            toastGo.transform.SetParent(t, false);
            UIFactory.Anchor(toastGo, tc, tc, new Vector2(-470f, -178f), new Vector2(470f, -134f));
            toastRect = toastGo.GetComponent<RectTransform>();
            toastGroup = toastGo.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            UIStyle.Plate(toastGo.transform, "Plate", "tab_plate", UIStyle.Yellow, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 7f, UIStyle.Yellow, 0.34f);
            toastText = UIFactory.Text(toastGo.transform, "Text", "", 20, UIStyle.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f), false);
            UIStyle.Style(toastText, UIStyle.Arcade, 19, UIStyle.Ink, FontStyle.Italic);
            toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastText.verticalOverflow = VerticalWrapMode.Truncate;
            toastText.resizeTextForBestFit = true;
            toastText.resizeTextMinSize = 12;
            toastText.resizeTextMaxSize = 19;

            // ---- prompts above the cockpit
            hintText = UIFactory.Text(t, "Hint", "", 22, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-900f, Cockpit.Height + 14f), new Vector2(900f, Cockpit.Height + 54f), false);
            UIStyle.Style(hintText, UIStyle.Body, 22, Color.white, FontStyle.Bold);
            UIStyle.Edge(hintText);
            hintGroup = hintText.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;
            binPrompt = UIFactory.Text(t, "BinPrompt", "PRESS F / X TO EMPTY THE BAG INTO THE BIN", 28, UIStyle.Yellow, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-600f, Cockpit.Height + 66f), new Vector2(600f, Cockpit.Height + 120f), false);
            UIStyle.Style(binPrompt, UIStyle.Arcade, 30, UIStyle.Yellow, FontStyle.Italic);
            UIStyle.ArcadeText(binPrompt, UIStyle.Yellow, UIStyle.Blue, 3f);
            binGroup = binPrompt.gameObject.AddComponent<CanvasGroup>();
            binGroup.alpha = 0f;

            cockpit = Cockpit.Build(t);

            if (GameInput.TouchMode)
            {
                // a phone has no room for the cockpit strip and the tapes: the stick and the buttons live there
                cockpit.Root.SetActive(false);
                tapesBox.parent.gameObject.SetActive(false);
                hintText.rectTransform.offsetMin = new Vector2(-900f, 130f);
                hintText.rectTransform.offsetMax = new Vector2(900f, 170f);
                binPrompt.rectTransform.offsetMin = new Vector2(-600f, 180f);
                binPrompt.rectTransform.offsetMax = new Vector2(600f, 234f);
                var log = logBox.parent.GetComponent<RectTransform>();
                log.offsetMin = new Vector2(-520f, -120f);
                log.offsetMax = new Vector2(-30f, 210f);
            }
        }

        public void SetVisible(bool v)
        {
            canvas.gameObject.SetActive(v);
            if (radar != null) radar.SetActive(v);
        }

        public void ResetRun()
        {
            targetScore = 0; displayScore = 0f; lastCombo = -1; lastTime = -1; lastPower = -1;
            lastBinPrompt = false;
            bannerDur = 0f; bannerGroup.alpha = 0f;
            hintDur = 0f; hintGroup.alpha = 0f;
            toasts.Clear(); toastT = -1f; toastGroup.alpha = 0f;
            binGroup.alpha = 0f;
            objectivesTimer = 0f;
            scoreText.text = "0";
        }

        public void BindVacuum(VacuumSpec spec, int serial, Transform player)
        {
            cockpit.Bind(spec, serial);
            thirdIsBattery = spec.Cordless;
            tapeThird.SetColor(thirdIsBattery ? UIStyle.Green : UIStyle.Blue);
            if (playerMarker != null) Destroy(playerMarker);
            if (player != null)
            {
                playerMarker = RadarView.Marker(player, UIStyle.Green, 1.6f);
                var nose = RadarView.Marker(player, UIStyle.Green, 0.7f);
                nose.transform.localPosition = new Vector3(0f, 25f, 1.1f);
            }
            radar.SetActive(true);
        }

        public void SetTelemetry(Telemetry tm, SuctionSystem suction, GameManager gm, float dt)
        {
            cockpit.Refresh(tm, suction, gm, dt);
            if (speedLines != null)
            {
                speedLinesAlpha = Mathf.Lerp(speedLinesAlpha, tm.Turbo ? 0.42f : 0f, 1f - Mathf.Exp(-dt * (tm.Turbo ? 8f : 5f)));
                speedLines.color = new Color(1f, 1f, 1f, speedLinesAlpha);
                if (speedLinesAlpha > 0.01f)
                {
                    float tt = Time.unscaledTime;
                    speedLines.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(tt * 21f));
                    speedLines.rectTransform.localEulerAngles = new Vector3(0f, 0f, 1.5f * Mathf.Sin(tt * 13f));
                }
            }
            tapeSuction.Set(tm.Suction01 * 100f, Mathf.RoundToInt(tm.Suction01 * 100f).ToString());
            tapeTemp.Set(tm.TempC, tm.TempC.ToString("0"));
            tapeTemp.SetColor(tm.Overheat ? UIStyle.Red : UIStyle.Amber);
            if (thirdIsBattery) tapeThird.Set(tm.Battery01 * 100f, Mathf.RoundToInt(tm.Battery01 * 100f).ToString());
            else
            {
                tapeThird.Set(tm.CordLength / tm.CordMax * 100f, tm.CordLength.ToString("0.0"));
                tapeThird.SetColor(tm.CordTaut ? UIStyle.Red : (!tm.Powered ? UIStyle.Dim : UIStyle.Blue));
            }
        }

        public void SetScore(int score)
        {
            if (score == targetScore) return;
            int gain = score - targetScore;
            targetScore = score;
            scorePunch = 1f;
            if (gain >= 100) sparkleBurst = 1f;
        }

        public void SetCombo(int count, float mult, float timeLeft)
        {
            int key = count > 1 ? count : 0;
            if (key != lastCombo)
            {
                lastCombo = key;
                comboText.text = key > 1 ? "COMBO x" + mult.ToString("0.0") + "   " + count + " HITS" : "";
            }
            if (key > 1)
            {
                float k = Mathf.Clamp01(timeLeft / 1.6f);
                comboRect.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 14f) * k);
            }
        }

        public void SetPower(string vacuumName, int level, string canEat)
        {
            powerText.text = vacuumName.ToUpperInvariant() + "     EATS " + canEat.ToUpperInvariant();
            if (level != lastPower)
            {
                lastPower = level;
                for (int i = 0; i < powerTiles.Length; i++)
                    UIStyle.SetTile(powerTiles[i], powerTileTexts[i], i < level, PowerColors[i]);
            }
        }

        public void SetTime(float seconds)
        {
            int s = (int)seconds;
            if (s == lastTime) return;
            lastTime = s;
            timeText.text = GameManager.FormatTime(seconds);
        }

        public void SetObjectives(ObjectiveSystem os)
        {
            objectivesTimer -= Time.unscaledDeltaTime;
            if (objectivesTimer > 0f) return;
            objectivesTimer = 0.2f;
            os.FillActive(objBuffer, 3);
            for (int i = 0; i < 3; i++)
            {
                bool has = i < objBuffer.Count;
                logTiles[i].gameObject.SetActive(has);
                logTitles[i].gameObject.SetActive(has);
                logDescs[i].gameObject.SetActive(has);
                if (!has) continue;
                var o = objBuffer[i];
                bool started = o.Progress > 0;
                UIStyle.SetTile(logTiles[i], logTileTexts[i], true, started ? UIStyle.Yellow : UIStyle.Blue);
                logTileTexts[i].text = o.Progress + " / " + o.Target;
                logTitles[i].text = o.Title.ToUpperInvariant();
                logDescs[i].text = o.Description;
            }
            achievementsText.text = "ACHIEVEMENTS " + os.DoneCount + " / " + os.All.Count;
        }

        public void SetBinPrompt(bool on)
        {
            if (on == lastBinPrompt) return;
            lastBinPrompt = on;
            binGroup.alpha = on ? 1f : 0f;
        }

        public void ShowBanner(string big, string small, float duration)
        {
            bannerBig.text = big.ToUpperInvariant();
            bannerSmall.text = small;
            bannerT = 0f;
            bannerDur = duration;
            bannerGroup.alpha = 0f;
            sparkleBurst = 1f;
        }

        /// <summary>Queues one line on the toast strip; shown for a couple of seconds each, in order.</summary>
        public void ShowToast(string text)
        {
            if (toasts.Count > 6) toasts.Dequeue();   // a flood keeps only the freshest lines
            toasts.Enqueue(text);
        }

        public void ShowHint(string text, float duration)
        {
            hintText.text = text;
            hintT = 0f;
            hintDur = duration;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (displayScore < targetScore)
            {
                float speed = Mathf.Max(60f, (targetScore - displayScore) * 6f);
                displayScore = Mathf.Min(targetScore, displayScore + speed * dt);
                scoreText.text = Mathf.RoundToInt(displayScore).ToString("N0");
            }
            else if (displayScore > targetScore)
            {
                displayScore = targetScore;
                scoreText.text = targetScore.ToString("N0");
            }
            if (scorePunch > 0f)
            {
                scorePunch = Mathf.Max(0f, scorePunch - dt * 4f);
                scoreRect.localScale = Vector3.one * (1f + 0.1f * scorePunch);
            }
            if (sparkleBurst > 0f)
            {
                sparkleBurst = Mathf.Max(0f, sparkleBurst - dt * 1.2f);
                for (int i = 0; i < sparkles.Length; i++)
                {
                    float p = Mathf.Repeat(sparklePhase[i] + Time.unscaledTime * 3f, 1f);
                    float a = sparkleBurst * Mathf.Sin(p * Mathf.PI);
                    sparkles[i].color = new Color(1f, 0.95f, 0.6f, a);
                    sparkles[i].rectTransform.localScale = Vector3.one * (0.5f + 0.8f * a);
                    sparkles[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, Time.unscaledTime * 120f + i * 45f);
                }
            }
            else
            {
                for (int i = 0; i < sparkles.Length; i++) sparkles[i].color = new Color(1f, 1f, 0.8f, 0f);
            }
            if (bannerDur > 0f)
            {
                bannerT += dt;
                float a = bannerT < 0.2f ? bannerT / 0.2f : (bannerT > bannerDur - 0.4f ? Mathf.Clamp01((bannerDur - bannerT) / 0.4f) : 1f);
                bannerGroup.alpha = a;
                float s = 1f + 0.3f * Mathf.Max(0f, 1f - bannerT / 0.3f);
                bannerRect.localScale = Vector3.one * s;
                if (bannerRays != null)
                {
                    bannerRays.rectTransform.localEulerAngles = new Vector3(0f, 0f, -Time.unscaledTime * 25f);
                    bannerRays.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 6f));
                }
                if (bannerT >= bannerDur) { bannerDur = 0f; bannerGroup.alpha = 0f; }
            }
            if (toastT < 0f && toasts.Count > 0)
            {
                toastText.text = toasts.Dequeue();
                toastT = 0f;
            }
            if (toastT >= 0f)
            {
                toastT += dt;
                float a = toastT < 0.15f ? toastT / 0.15f : (toastT > ToastDur - 0.3f ? Mathf.Clamp01((ToastDur - toastT) / 0.3f) : 1f);
                toastGroup.alpha = a;
                toastRect.anchoredPosition = new Vector2(0f, -156f - 10f * (1f - Mathf.Min(1f, toastT / 0.15f)));
                if (toastT >= ToastDur) { toastT = -1f; toastGroup.alpha = 0f; }
            }
            if (hintDur > 0f)
            {
                hintT += dt;
                float a = hintT < 0.3f ? hintT / 0.3f : (hintT > hintDur - 0.5f ? Mathf.Clamp01((hintDur - hintT) / 0.5f) : 1f);
                hintGroup.alpha = a;
                if (hintT >= hintDur) { hintDur = 0f; hintGroup.alpha = 0f; }
            }
        }
    }
}
