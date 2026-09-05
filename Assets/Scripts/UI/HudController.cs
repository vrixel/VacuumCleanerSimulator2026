using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VCS.Core;
using VCS.Objectives;
using VCS.Player;

namespace VCS.UI
{
    /// <summary>
    /// The in-game overlay, spread around the whole screen like a flight deck: score block top-left, power strip
    /// top-centre, seven-segment timer and dirt radar top-right, vertical meters on the left edge, mission log on
    /// the right edge, the cockpit along the bottom, banners in the middle, brackets in the corners.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        Canvas canvas;
        Text scoreText, comboText, powerText, timeText, objectivesText, achievementsText, bannerBig, bannerSmall, hintText, binPrompt;
        Image[] powerSegments;
        Image meterSuction, meterTemp, meterThird;
        Text meterSuctionValue, meterTempValue, meterThirdValue, meterThirdLabel;
        CanvasGroup bannerGroup, hintGroup, binGroup;
        RectTransform bannerRect, scoreRect, comboRect;
        Cockpit cockpit;
        RadarView radar;
        GameObject playerMarker;

        int lastScore = -1, lastCombo = -1, lastTime = -1, lastPower = -1;
        bool lastBinPrompt;
        float bannerT, bannerDur, hintT, hintDur, objectivesTimer, scorePunch, meterTimer;
        Color accent = UIStyle.Amber;
        readonly List<Objective> objBuffer = new List<Objective>();
        readonly StringBuilder sb = new StringBuilder();

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
            var steel = UIStyle.Steel;

            // screen frame
            UIStyle.Brackets(t, Vector2.zero, Vector2.one, new Vector2(14f, 14f), new Vector2(-14f, -14f), 42f, 3f, new Color(0.8f, 0.86f, 0.95f, 0.55f));

            // ---- top-left: score block
            UIStyle.Box(t, "ScoreBox", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -215f), new Vector2(520f, -36f), 0.6f);
            UIStyle.Brackets(t, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -215f), new Vector2(520f, -36f), 22f, 2f);
            var scoreLabel = UIFactory.Text(t, "ScoreLabel", "SCORE", 20, steel, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f, -72f), new Vector2(500f, -42f), false);
            UIStyle.Style(scoreLabel, UIStyle.Display, 20, steel);
            scoreText = UIFactory.Text(t, "Score", "0", 104, UIStyle.Amber, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(52f, -176f), new Vector2(500f, -66f), false);
            UIStyle.Style(scoreText, UIStyle.Title, 104, UIStyle.Amber);
            UIStyle.Glow(scoreText, UIStyle.Amber, 4f, 0.4f);
            scoreRect = scoreText.rectTransform;
            comboText = UIFactory.Text(t, "Combo", "", 26, UIStyle.Cyan, TextAnchor.LowerLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f, -212f), new Vector2(500f, -176f), false);
            UIStyle.Style(comboText, UIStyle.Display, 26, UIStyle.Cyan);
            UIStyle.Glow(comboText, UIStyle.Cyan, 3f, 0.5f);
            comboRect = comboText.rectTransform;

            // ---- top-centre: power strip
            UIStyle.Box(t, "PowerBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-560f, -104f), new Vector2(560f, -36f), 0.6f);
            powerText = UIFactory.Text(t, "Power", "", 24, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-540f, -84f), new Vector2(540f, -40f), false);
            UIStyle.Style(powerText, UIStyle.Display, 22, Color.white);
            UIStyle.Edge(powerText);
            powerSegments = new Image[GameManager.MaxPower];
            for (int i = 0; i < powerSegments.Length; i++)
            {
                float x = -200f + i * 82f;
                UIFactory.Panel(t, "PowerSegBack" + i, new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -100f), new Vector2(x + 76f, -88f));
                powerSegments[i] = UIFactory.Panel(t, "PowerSeg" + i, UIStyle.Amber, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x + 2f, -98f), new Vector2(x + 74f, -90f));
            }

            // ---- top-right: timer and radar
            UIStyle.Box(t, "TimeBox", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-330f, -112f), new Vector2(-36f, -36f), 0.6f);
            var timeLabel = UIFactory.Text(t, "TimeLabel", "RUNTIME", 16, steel, TextAnchor.UpperLeft,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-322f, -60f), new Vector2(-40f, -40f), false);
            UIStyle.Style(timeLabel, UIStyle.Display, 15, steel);
            timeText = UIStyle.Digital(t, "Time", "88:88", 50, UIStyle.Amber, TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-322f, -110f), new Vector2(-46f, -56f));
            radar = RadarView.Build(t, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -395f), new Vector2(-66f, -161f), new Vector3(14f, 0f, 10f), 15f);
            var radarLabel = UIFactory.Text(t, "RadarLabel", "DIRT RADAR", 16, steel, TextAnchor.MiddleCenter,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-330f, -430f), new Vector2(-36f, -398f), false);
            UIStyle.Style(radarLabel, UIStyle.Display, 16, steel);
            UIStyle.Edge(radarLabel);
            UIStyle.Brackets(t, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-320f, -410f), new Vector2(-46f, -146f), 22f, 2f);

            // ---- left edge: vertical meters
            var mid = new Vector2(0f, 0.5f);
            UIStyle.Box(t, "MetersBox", mid, mid, new Vector2(36f, -170f), new Vector2(230f, 200f), 0.6f);
            meterSuction = Meter(t, 60f, "SUCTION", out meterSuctionValue);
            meterTemp = Meter(t, 118f, "TEMP", out meterTempValue);
            meterThird = Meter(t, 176f, "CORD", out meterThirdValue);
            meterThirdLabel = meterThird.transform.parent.parent.Find("MeterLabel176")?.GetComponent<Text>();

            // ---- right edge: mission log
            var rmid = new Vector2(1f, 0.5f);
            UIStyle.Box(t, "LogBox", rmid, rmid, new Vector2(-520f, -120f), new Vector2(-36f, 200f), 0.6f);
            UIFactory.Panel(t, "LogHeader", new Color(UIStyle.Amber.r, UIStyle.Amber.g, UIStyle.Amber.b, 0.85f), rmid, rmid, new Vector2(-520f, 166f), new Vector2(-36f, 200f));
            var logTitle = UIFactory.Text(t, "LogTitle", "MISSION LOG", 18, UIStyle.Ink, TextAnchor.MiddleLeft,
                rmid, rmid, new Vector2(-508f, 166f), new Vector2(-40f, 200f), false);
            UIStyle.Style(logTitle, UIStyle.Display, 18, new Color(0.08f, 0.07f, 0.1f));
            objectivesText = UIFactory.Text(t, "Objectives", "", 22, Color.white, TextAnchor.UpperLeft,
                rmid, rmid, new Vector2(-506f, -80f), new Vector2(-46f, 158f), false);
            UIStyle.Style(objectivesText, UIStyle.Body, 22, Color.white);
            UIStyle.Edge(objectivesText);
            achievementsText = UIFactory.Text(t, "Achievements", "", 16, steel, TextAnchor.LowerLeft,
                rmid, rmid, new Vector2(-506f, -114f), new Vector2(-46f, -84f), false);
            UIStyle.Style(achievementsText, UIStyle.Mono, 16, steel);

            // ---- banner
            var bannerPanel = UIFactory.Panel(t, "Banner", new Color(0.04f, 0.05f, 0.09f, 0.78f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-760f, 80f), new Vector2(760f, 270f));
            bannerRect = bannerPanel.rectTransform;
            bannerGroup = bannerPanel.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            UIStyle.Brackets(bannerPanel.transform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f), 30f, 3f, UIStyle.Amber);
            bannerBig = UIFactory.Text(bannerPanel.transform, "Big", "", 84, UIStyle.Amber, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(20f, 0f), new Vector2(-20f, -8f), false);
            UIStyle.Style(bannerBig, UIStyle.Title, 84, UIStyle.Amber);
            UIStyle.Glow(bannerBig, UIStyle.Amber, 4f, 0.45f);
            bannerSmall = UIFactory.Text(bannerPanel.transform, "Small", "", 28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector2(20f, 10f), new Vector2(-20f, 0f), false);
            UIStyle.Style(bannerSmall, UIStyle.Body, 28, Color.white);
            UIStyle.Edge(bannerSmall);

            // ---- hints above the cockpit
            hintText = UIFactory.Text(t, "Hint", "", 22, new Color(0.9f, 0.93f, 1f, 0.95f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-900f, Cockpit.Height + 12f), new Vector2(900f, Cockpit.Height + 52f), false);
            UIStyle.Style(hintText, UIStyle.Body, 22, new Color(0.9f, 0.93f, 1f, 0.95f));
            UIStyle.Edge(hintText);
            hintGroup = hintText.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;
            binPrompt = UIFactory.Text(t, "BinPrompt", "PRESS F / X TO EMPTY THE BAG INTO THE BIN", 28, UIStyle.Amber, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-600f, Cockpit.Height + 70f), new Vector2(600f, Cockpit.Height + 120f), false);
            UIStyle.Style(binPrompt, UIStyle.Display, 26, UIStyle.Amber);
            UIStyle.Glow(binPrompt, UIStyle.Amber, 3f, 0.5f);
            binGroup = binPrompt.gameObject.AddComponent<CanvasGroup>();
            binGroup.alpha = 0f;

            cockpit = Cockpit.Build(t);
        }

        Image Meter(Transform t, float x, string label, out Text value)
        {
            var mid = new Vector2(0f, 0.5f);
            var holder = new GameObject("Meter" + (int)x, typeof(RectTransform));
            holder.transform.SetParent(t, false);
            UIFactory.Anchor(holder, mid, mid, new Vector2(x - 20f, -150f), new Vector2(x + 20f, 180f));
            var back = UIFactory.Panel(holder.transform, "Back", new Color(0f, 0f, 0f, 0.7f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 34f), new Vector2(-8f, -40f));
            var fill = UIFactory.Panel(back.transform, "Fill", UIStyle.Amber, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(2f, 2f), new Vector2(-2f, -2f));
            for (int i = 1; i < 10; i++)
                UIFactory.Panel(back.transform, "Tick" + i, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, i / 10f), new Vector2(1f, i / 10f), new Vector2(0f, -1f), new Vector2(0f, 1f));
            var lbl = UIFactory.Text(holder.transform, "MeterLabel" + (int)x, label, 12, UIStyle.Steel, TextAnchor.UpperCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 4f), new Vector2(34f, 32f), false);
            UIStyle.Style(lbl, UIStyle.Display, 13, UIStyle.Steel);
            UIStyle.Edge(lbl);
            value = UIFactory.Text(holder.transform, "MeterValue", "", 15, Color.white, TextAnchor.LowerCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -38f), new Vector2(34f, -4f), false);
            UIStyle.Style(value, UIStyle.Mono, 15, Color.white);
            return fill;
        }

        public void SetVisible(bool v)
        {
            canvas.gameObject.SetActive(v);
            if (radar != null) radar.SetActive(v);
        }

        public void ResetRun()
        {
            lastScore = -1; lastCombo = -1; lastTime = -1; lastPower = -1;
            lastBinPrompt = false;
            bannerDur = 0f; bannerGroup.alpha = 0f;
            hintDur = 0f; hintGroup.alpha = 0f;
            binGroup.alpha = 0f;
            objectivesTimer = 0f;
        }

        public void BindVacuum(VacuumSpec spec, int serial, Transform player)
        {
            accent = spec.Accent;
            cockpit.Bind(spec, serial);
            meterSuction.color = accent;
            meterSuctionValue.color = accent;
            if (meterThirdLabel != null) meterThirdLabel.text = spec.Cordless ? "BATT" : "CORD";
            meterThird.color = spec.Cordless ? UIStyle.Green : UIStyle.Steel;
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
            meterSuction.rectTransform.anchorMax = new Vector2(1f, Mathf.Clamp01(tm.Suction01));
            meterTemp.rectTransform.anchorMax = new Vector2(1f, Mathf.Clamp01((tm.TempC - 20f) / 100f));
            meterTemp.color = tm.Overheat ? UIStyle.Red : new Color(1f, 0.55f, 0.25f);
            float third = gm.Player != null && gm.Player.Spec.Cordless ? tm.Battery01 : Mathf.Clamp01(tm.CordLength / tm.CordMax);
            meterThird.rectTransform.anchorMax = new Vector2(1f, third);
            if (gm.Player != null && !gm.Player.Spec.Cordless) meterThird.color = tm.CordTaut ? UIStyle.Red : (!tm.Powered ? new Color(0.35f, 0.38f, 0.42f) : UIStyle.Steel);
            meterTimer -= dt;
            if (meterTimer <= 0f)
            {
                meterTimer = 0.15f;
                meterSuctionValue.text = Mathf.RoundToInt(tm.Suction01 * 100f) + "%";
                meterTempValue.text = tm.TempC.ToString("0") + "C";
                meterThirdValue.text = gm.Player != null && gm.Player.Spec.Cordless
                    ? Mathf.RoundToInt(tm.Battery01 * 100f) + "%"
                    : tm.CordLength.ToString("0.0") + "m";
            }
        }

        public void SetScore(int score)
        {
            if (score == lastScore) return;
            if (lastScore >= 0) scorePunch = 1f;
            lastScore = score;
            scoreText.text = score.ToString("N0");
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
                comboText.color = Color.Lerp(new Color(UIStyle.Cyan.r, UIStyle.Cyan.g, UIStyle.Cyan.b, 0.45f), UIStyle.Cyan, k);
                comboRect.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 12f) * k);
            }
        }

        public void SetPower(string vacuumName, int level, string canEat)
        {
            powerText.text = vacuumName.ToUpperInvariant() + "   |   POWER " + level + " / " + GameManager.MaxPower + "   |   EATS " + canEat.ToUpperInvariant();
            if (level != lastPower)
            {
                lastPower = level;
                for (int i = 0; i < powerSegments.Length; i++)
                    powerSegments[i].color = i < level ? accent : new Color(accent.r, accent.g, accent.b, 0.12f);
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
            sb.Length = 0;
            for (int i = 0; i < objBuffer.Count; i++)
            {
                var o = objBuffer[i];
                sb.Append("<color=#FFC740>[ ").Append(o.Progress).Append(" / ").Append(o.Target).Append(" ]</color>  ");
                sb.Append("<b>").Append(o.Title.ToUpperInvariant()).Append("</b>\n      <size=17><color=#B8C0CC>").Append(o.Description).Append("</color></size>");
                if (i < objBuffer.Count - 1) sb.Append('\n');
            }
            objectivesText.text = sb.ToString();
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
            if (scorePunch > 0f)
            {
                scorePunch = Mathf.Max(0f, scorePunch - dt * 4f);
                scoreRect.localScale = Vector3.one * (1f + 0.12f * scorePunch);
            }
            if (bannerDur > 0f)
            {
                bannerT += dt;
                float a = bannerT < 0.25f ? bannerT / 0.25f : (bannerT > bannerDur - 0.4f ? Mathf.Clamp01((bannerDur - bannerT) / 0.4f) : 1f);
                bannerGroup.alpha = a;
                float s = 1f + 0.35f * Mathf.Max(0f, 1f - bannerT / 0.3f);
                bannerRect.localScale = Vector3.one * s;
                if (bannerT >= bannerDur) { bannerDur = 0f; bannerGroup.alpha = 0f; }
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
