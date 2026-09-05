using UnityEngine;
using UnityEngine.UI;
using VCS.Core;
using VCS.Player;

namespace VCS.UI
{
    /// <summary>Title screen with the vacuum garage, and the pause menu. Keyboard, gamepad and mouse all work.</summary>
    public class MenuController : MonoBehaviour
    {
        public System.Action OnTitleStart;
        public System.Action<int> OnPauseSelect;

        static readonly string[] PauseLabels = { "RESUME", "RESTART", "TITLE SCREEN", "QUIT GAME" };
        static readonly string[] BarLabels = { "SPEED", "SUCTION", "BAG", "HOP" };

        Canvas canvas;
        GameObject titleRoot, pauseRoot;
        RectTransform titleRect;
        Text titlePrompt, titleStats, vacuumName, vacuumTagline;
        Image[] bars;
        Text[] pauseTexts;
        Image[] pauseBacks;
        VacuumPreview preview;
        public VacuumPreview Preview => preview;
        int sel;
        int vacIndex;
        float t;
        bool titleVisible, pauseVisible;

        public static MenuController Create()
        {
            var canvas = UIFactory.CreateCanvas("Menus", 20);
            var m = canvas.gameObject.AddComponent<MenuController>();
            m.canvas = canvas;
            m.preview = VacuumPreview.Create();
            m.Build();
            m.HideAll();
            return m;
        }

        void Build()
        {
            var root = canvas.transform;
            var mid = new Vector2(0.5f, 0.5f);
            var left = new Vector2(0f, 0.5f);
            var right = new Vector2(1f, 0.5f);
            var accent = UIFactory.Accent;

            // ---- title screen
            titleRoot = new GameObject("Title", typeof(RectTransform));
            titleRoot.transform.SetParent(root, false);
            UIFactory.Anchor(titleRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.Panel(titleRoot.transform, "Dim", new Color(0.05f, 0.05f, 0.1f, 0.35f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var title = UIFactory.Text(titleRoot.transform, "TitleText", "VACUUM CLEANER\nSIMULATOR <color=#FFD84D>2026</color>", 84, Color.white, TextAnchor.MiddleLeft,
                left, left, new Vector2(70f, 130f), new Vector2(1250f, 430f), false);
            UIStyle.Style(title, UIStyle.Arcade, 96, Color.white, FontStyle.Italic);
            UIStyle.ArcadeText(title, Color.white, UIStyle.Yellow, 7f);
            titleRect = title.rectTransform;
            var sub = UIFactory.Text(titleRoot.transform, "Sub", "Suck it up.", 36, accent, TextAnchor.MiddleLeft,
                left, left, new Vector2(74f, 70f), new Vector2(1250f, 130f), false, FontStyle.Italic);
            UIStyle.Style(sub, UIStyle.Body, 40, accent, FontStyle.Italic);
            UIStyle.Edge(sub);
            titlePrompt = UIFactory.Text(titleRoot.transform, "Prompt", "PRESS ENTER OR (A) TO START CLEANING", 34, Color.white, TextAnchor.MiddleLeft,
                left, left, new Vector2(74f, -20f), new Vector2(1250f, 50f), false);
            UIStyle.Style(titlePrompt, UIStyle.Arcade, 28, UIStyle.Yellow, FontStyle.Italic);
            UIStyle.ArcadeText(titlePrompt, UIStyle.Yellow, UIStyle.Blue, 3f);
            var controls = UIFactory.Text(titleRoot.transform, "Controls",
                "WASD / left stick  drive        SPACE / A  hop        SHIFT / RB  turbo\nE / B  blow        F / X  empty bag at the bin        R / Y  rewind the cord        ESC / Start  pause",
                22, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft, left, left, new Vector2(74f, -170f), new Vector2(1250f, -50f), false);
            UIStyle.Style(controls, UIStyle.Body, 22, new Color(1f, 1f, 1f, 0.85f));
            UIStyle.Edge(controls);
            titleStats = UIFactory.Text(titleRoot.transform, "Stats", "", 22, new Color(1f, 1f, 1f, 0.7f), TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(74f, 30f), new Vector2(1250f, 70f), false);
            UIStyle.Style(titleStats, UIStyle.Mono, 20, new Color(1f, 1f, 1f, 0.7f));
            UIStyle.Edge(titleStats);

            // ---- garage (right column): an instrument frame
            UIStyle.Frame(titleRoot.transform, "GarageBack", right, right, new Vector2(-600f, -500f), new Vector2(-14f, 340f), "frame_square", 586f, 840f, 30f);
            UIStyle.Tab(titleRoot.transform, "GarageTitle", "CHOOSE YOUR VACUUM", right, right, new Vector2(-500f, 302f), new Vector2(-114f, 332f));
            var rawGo = new GameObject("Preview", typeof(RectTransform));
            rawGo.transform.SetParent(titleRoot.transform, false);
            UIFactory.Anchor(rawGo, right, right, new Vector2(-506f, -108f), new Vector2(-108f, 246f));
            var raw = rawGo.AddComponent<RawImage>();
            raw.texture = preview.Texture;
            raw.raycastTarget = false;

            vacuumName = UIFactory.Text(titleRoot.transform, "VacuumName", "", 38, Color.white, TextAnchor.MiddleCenter,
                right, right, new Vector2(-500f, -172f), new Vector2(-114f, -117f), false);
            UIStyle.Style(vacuumName, UIStyle.Arcade, 40, Color.white, FontStyle.Italic);
            UIStyle.ArcadeText(vacuumName, Color.white, UIStyle.Yellow, 3f);
            // Wrap + best fit: sizes that would need two lines do not fit the 55 px rect, so long names shrink to one line.
            vacuumName.horizontalOverflow = HorizontalWrapMode.Wrap;
            vacuumName.verticalOverflow = VerticalWrapMode.Truncate;
            vacuumName.resizeTextForBestFit = true;
            vacuumName.resizeTextMinSize = 18;
            vacuumName.resizeTextMaxSize = 40;
            vacuumTagline = UIFactory.Text(titleRoot.transform, "VacuumTagline", "", 21, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter,
                right, right, new Vector2(-530f, -212f), new Vector2(-84f, -174f), false, FontStyle.Italic);
            UIStyle.Style(vacuumTagline, UIStyle.Body, 21, new Color(1f, 1f, 1f, 0.85f), FontStyle.Italic);
            UIStyle.Edge(vacuumTagline);
            vacuumTagline.horizontalOverflow = HorizontalWrapMode.Wrap;
            vacuumTagline.verticalOverflow = VerticalWrapMode.Truncate;
            vacuumTagline.resizeTextForBestFit = true;
            vacuumTagline.resizeTextMinSize = 12;
            vacuumTagline.resizeTextMaxSize = 21;
            MakeArrow(titleRoot.transform, "<", new Vector2(-556f, -172f), new Vector2(-506f, -117f), -1);
            MakeArrow(titleRoot.transform, ">", new Vector2(-108f, -172f), new Vector2(-58f, -117f), 1);

            bars = new Image[BarLabels.Length];
            for (int i = 0; i < BarLabels.Length; i++)
            {
                float y = -250f - i * 38f;
                var barLabel = UIFactory.Text(titleRoot.transform, "BarLabel" + i, BarLabels[i], 20, Color.white, TextAnchor.MiddleLeft,
                    right, right, new Vector2(-516f, y - 14f), new Vector2(-400f, y + 14f), false);
                UIStyle.Style(barLabel, UIStyle.Arcade, 15, UIStyle.Steel, FontStyle.Italic);
                UIStyle.Edge(barLabel);
                var back = UIFactory.Panel(titleRoot.transform, "BarBack" + i, new Color(0f, 0f, 0f, 0.8f), right, right, new Vector2(-390f, y - 11f), new Vector2(-100f, y + 11f));
                Color[] barColors = { UIStyle.Green, UIStyle.Red, UIStyle.Yellow, UIStyle.Blue };
                bars[i] = UIFactory.Panel(back.transform, "BarFill" + i, barColors[i], new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(3f, 3f), new Vector2(-3f, -3f));
            }
            var garageHint = UIFactory.Text(titleRoot.transform, "GarageHint", "A / D   or   LB / RB   to choose", 19, new Color(1f, 1f, 1f, 0.65f), TextAnchor.MiddleCenter,
                right, right, new Vector2(-530f, -456f), new Vector2(-84f, -420f), false);
            UIStyle.Style(garageHint, UIStyle.Body, 19, new Color(1f, 1f, 1f, 0.65f));

            // ---- pause menu
            pauseRoot = new GameObject("Pause", typeof(RectTransform));
            pauseRoot.transform.SetParent(root, false);
            UIFactory.Anchor(pauseRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.Panel(pauseRoot.transform, "Dim", new Color(0.02f, 0.02f, 0.06f, 0.65f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var paused = UIFactory.Text(pauseRoot.transform, "PausedText", "PAUSED", 80, accent, TextAnchor.MiddleCenter,
                mid, mid, new Vector2(-600f, 230f), new Vector2(600f, 380f), false);
            UIStyle.Style(paused, UIStyle.Arcade, 100, Color.white, FontStyle.Italic);
            UIStyle.ArcadeText(paused, Color.white, UIStyle.Yellow, 7f);
            pauseTexts = new Text[PauseLabels.Length];
            pauseBacks = new Image[PauseLabels.Length];
            for (int i = 0; i < PauseLabels.Length; i++)
            {
                float y = 110f - i * 90f;
                var back = UIFactory.Panel(pauseRoot.transform, "Item" + i, new Color(0f, 0f, 0f, 0.5f), mid, mid, new Vector2(-260f, y - 35f), new Vector2(260f, y + 35f));
                back.raycastTarget = true;
                var btn = back.gameObject.AddComponent<Button>();
                int idx = i;
                btn.onClick.AddListener(() => { sel = idx; Highlight(); OnPauseSelect?.Invoke(idx); });
                pauseBacks[i] = back;
                pauseTexts[i] = UIFactory.Text(back.transform, "Label", PauseLabels[i], 36, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
                UIStyle.Style(pauseTexts[i], UIStyle.Arcade, 28, Color.white, FontStyle.Italic);
            }
        }

        void MakeArrow(Transform parent, string label, Vector2 oMin, Vector2 oMax, int dir)
        {
            var right = new Vector2(1f, 0.5f);
            var back = UIFactory.Panel(parent, "Arrow" + label, new Color(0f, 0f, 0f, 0.45f), right, right, oMin, oMax);
            back.raycastTarget = true;
            var btn = back.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => { SelectVacuumIndex(vacIndex + dir); var gm = GameManager.I; if (gm != null) gm.Audio.PlayClick(); });
            UIFactory.Text(back.transform, "Label", label, 40, UIFactory.Accent, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public void ShowTitle(int best, int achievementsDone, int achievementsTotal)
        {
            titleRoot.SetActive(true);
            pauseRoot.SetActive(false);
            titleVisible = true;
            pauseVisible = false;
            titleStats.text = "Best score " + best.ToString("N0") + "     Achievements " + achievementsDone + "/" + achievementsTotal + "     v" + GameManager.Version;
            SelectVacuumIndex(VacuumCatalog.IndexOf(VacuumCatalog.SelectedId));
        }

        public void ShowPause()
        {
            pauseRoot.SetActive(true);
            titleRoot.SetActive(false);
            pauseVisible = true;
            titleVisible = false;
            preview.Hide();
            sel = 0;
            Highlight();
        }

        public void HideAll()
        {
            titleRoot.SetActive(false);
            pauseRoot.SetActive(false);
            titleVisible = false;
            pauseVisible = false;
            preview.Hide();
        }

        public void SelectVacuumIndex(int i)
        {
            var all = VacuumCatalog.All;
            vacIndex = ((i % all.Count) + all.Count) % all.Count;
            var s = all[vacIndex];
            VacuumCatalog.SelectedId = s.Id;
            PlayerPrefs.Save();
            vacuumName.text = s.Name.ToUpperInvariant();
            vacuumTagline.text = s.Tagline;
            float[] values = { s.SpeedBar, s.SuctionBar, s.BagBar, s.HopBar };
            for (int k = 0; k < bars.Length; k++)
                bars[k].rectTransform.anchorMax = new Vector2(Mathf.Clamp(values[k], 0.04f, 1f), 1f);
            preview.Show(s);
        }

        public void SelectVacuumById(string id) { SelectVacuumIndex(VacuumCatalog.IndexOf(id)); }

        void Highlight()
        {
            for (int i = 0; i < pauseBacks.Length; i++)
            {
                bool on = i == sel;
                pauseBacks[i].color = on ? UIStyle.Yellow : new Color(0f, 0f, 0f, 0.6f);
                pauseTexts[i].color = on ? UIStyle.Ink : Color.white;
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            if (titleVisible)
            {
                titleRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 1.5f);
                titleRect.localScale = Vector3.one * (1f + 0.02f * Mathf.Sin(t * 3f));
                titlePrompt.color = new Color(1f, 1f, 1f, 0.85f + 0.15f * Mathf.Sin(t * 4f));
                int h = GameInput.MenuNavHorizontal();
                if (h != 0)
                {
                    SelectVacuumIndex(vacIndex + h);
                    var gm = GameManager.I;
                    if (gm != null) gm.Audio.PlayClick();
                }
                if (GameInput.ConfirmDown) OnTitleStart?.Invoke();
            }
            else if (pauseVisible)
            {
                int nav = GameInput.MenuNav();
                if (nav != 0)
                {
                    sel = (sel + nav + PauseLabels.Length) % PauseLabels.Length;
                    Highlight();
                }
                if (GameInput.ConfirmDown) OnPauseSelect?.Invoke(sel);
            }
        }
    }
}
