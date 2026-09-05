using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VCS.Core;
using VCS.Objectives;
using VCS.Player;

namespace VCS.UI
{
    /// <summary>In-game overlay: score, combo, power, timer, objectives, banners, hints, and the cockpit panel.</summary>
    public class HudController : MonoBehaviour
    {
        Canvas canvas;
        Text scoreText, comboText, powerText, timeText, objectivesText, bannerBig, bannerSmall, hintText, binPrompt;
        CanvasGroup bannerGroup, hintGroup, binGroup;
        RectTransform bannerRect;
        Cockpit cockpit;

        int lastScore = -1, lastCombo = -1, lastTime = -1;
        bool lastBinPrompt;
        float bannerT, bannerDur, hintT, hintDur, objectivesTimer;
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
            var accent = UIFactory.Accent;

            scoreText = UIFactory.Text(t, "Score", "SCORE\n<size=72>0</size>", 30, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -150f), new Vector2(600f, -30f));
            comboText = UIFactory.Text(t, "Combo", "", 34, accent, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -215f), new Vector2(600f, -155f));
            powerText = UIFactory.Text(t, "Power", "", 28, Color.white, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-520f, -110f), new Vector2(520f, -30f));
            timeText = UIFactory.Text(t, "Time", "00:00", 34, Color.white, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-340f, -90f), new Vector2(-36f, -30f));

            objectivesText = UIFactory.Text(t, "Objectives", "", 24, Color.white, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, Cockpit.Height + 20f), new Vector2(800f, Cockpit.Height + 150f));

            var bannerPanel = UIFactory.Panel(t, "Banner", new Color(0.05f, 0.05f, 0.1f, 0.72f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-760f, 60f), new Vector2(760f, 250f));
            bannerRect = bannerPanel.rectTransform;
            bannerGroup = bannerPanel.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            bannerBig = UIFactory.Text(bannerPanel.transform, "Big", "", 64, accent, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(20f, 0f), new Vector2(-20f, -10f));
            bannerSmall = UIFactory.Text(bannerPanel.transform, "Small", "", 30, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector2(20f, 10f), new Vector2(-20f, 0f));

            hintText = UIFactory.Text(t, "Hint", "", 24, new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-900f, Cockpit.Height + 165f), new Vector2(900f, Cockpit.Height + 215f));
            hintGroup = hintText.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;

            binPrompt = UIFactory.Text(t, "BinPrompt", "Press F / X to empty the bag into the bin", 32, accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-600f, Cockpit.Height + 230f), new Vector2(600f, Cockpit.Height + 280f));
            binGroup = binPrompt.gameObject.AddComponent<CanvasGroup>();
            binGroup.alpha = 0f;

            cockpit = Cockpit.Build(t);
        }

        public void SetVisible(bool v) { canvas.gameObject.SetActive(v); }

        public void ResetRun()
        {
            lastScore = -1; lastCombo = -1; lastTime = -1;
            lastBinPrompt = false;
            bannerDur = 0f; bannerGroup.alpha = 0f;
            hintDur = 0f; hintGroup.alpha = 0f;
            binGroup.alpha = 0f;
            objectivesTimer = 0f;
        }

        public void BindVacuum(VacuumSpec spec, int serial) { cockpit.Bind(spec, serial); }

        public void SetTelemetry(Telemetry tm, SuctionSystem suction, GameManager gm, float dt) { cockpit.Refresh(tm, suction, gm, dt); }

        public void SetScore(int score)
        {
            if (score == lastScore) return;
            lastScore = score;
            scoreText.text = "SCORE\n<size=72>" + score.ToString("N0") + "</size>";
        }

        public void SetCombo(int count, float mult, float timeLeft)
        {
            int key = count > 1 ? count : 0;
            if (key == lastCombo) return;
            lastCombo = key;
            comboText.text = key > 1 ? "x" + mult.ToString("0.0") + " COMBO  " + count : "";
        }

        public void SetPower(string vacuumName, int level, string canEat)
        {
            powerText.text = vacuumName.ToUpperInvariant() + "   -   POWER " + level + " / 5   -   eats " + canEat;
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
                sb.Append("<b>").Append(o.Title).Append("</b>  ").Append(o.Description);
                sb.Append("   <color=#FFD84D>").Append(o.Progress).Append('/').Append(o.Target).Append("</color>");
                if (i < objBuffer.Count - 1) sb.Append('\n');
            }
            objectivesText.text = sb.ToString();
        }

        public void SetBinPrompt(bool on)
        {
            if (on == lastBinPrompt) return;
            lastBinPrompt = on;
            binGroup.alpha = on ? 1f : 0f;
        }

        public void ShowBanner(string big, string small, float duration)
        {
            bannerBig.text = big;
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
