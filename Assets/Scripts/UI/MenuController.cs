using UnityEngine;
using UnityEngine.UI;
using VCS.Core;

namespace VCS.UI
{
    /// <summary>Title screen and pause menu. Keyboard, gamepad and mouse all work.</summary>
    public class MenuController : MonoBehaviour
    {
        public System.Action OnTitleStart;
        public System.Action<int> OnPauseSelect;

        static readonly string[] PauseLabels = { "RESUME", "RESTART", "TITLE SCREEN", "QUIT GAME" };

        Canvas canvas;
        GameObject titleRoot, pauseRoot;
        RectTransform titleRect;
        Text titlePrompt, titleStats;
        Text[] pauseTexts;
        Image[] pauseBacks;
        int sel;
        float t;
        bool titleVisible, pauseVisible;

        public static MenuController Create()
        {
            var canvas = UIFactory.CreateCanvas("Menus", 20);
            var m = canvas.gameObject.AddComponent<MenuController>();
            m.canvas = canvas;
            m.Build();
            m.HideAll();
            return m;
        }

        void Build()
        {
            var root = canvas.transform;
            var mid = new Vector2(0.5f, 0.5f);

            titleRoot = new GameObject("Title", typeof(RectTransform));
            titleRoot.transform.SetParent(root, false);
            UIFactory.Anchor(titleRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.Panel(titleRoot.transform, "Dim", new Color(0.05f, 0.05f, 0.1f, 0.35f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var title = UIFactory.Text(titleRoot.transform, "TitleText", "VACUUM CLEANER\nSIMULATOR <color=#FFD84D>2026</color>", 96, Color.white, TextAnchor.MiddleCenter,
                mid, mid, new Vector2(-900f, 120f), new Vector2(900f, 430f));
            titleRect = title.rectTransform;
            UIFactory.Text(titleRoot.transform, "Sub", "Suck it up.", 40, UIFactory.Accent, TextAnchor.MiddleCenter,
                mid, mid, new Vector2(-600f, 50f), new Vector2(600f, 120f));
            titlePrompt = UIFactory.Text(titleRoot.transform, "Prompt", "PRESS ENTER OR (A) TO START CLEANING", 36, Color.white, TextAnchor.MiddleCenter,
                mid, mid, new Vector2(-700f, -70f), new Vector2(700f, -10f));
            UIFactory.Text(titleRoot.transform, "Controls",
                "WASD / left stick  drive        SPACE / A  hop        SHIFT / RB  turbo\nE / B  blow        F / X  empty bag at the bin        ESC / Start  pause",
                24, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, mid, mid, new Vector2(-900f, -210f), new Vector2(900f, -100f));
            titleStats = UIFactory.Text(titleRoot.transform, "Stats", "", 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-900f, 30f), new Vector2(900f, 70f));

            pauseRoot = new GameObject("Pause", typeof(RectTransform));
            pauseRoot.transform.SetParent(root, false);
            UIFactory.Anchor(pauseRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.Panel(pauseRoot.transform, "Dim", new Color(0.02f, 0.02f, 0.06f, 0.65f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.Text(pauseRoot.transform, "PausedText", "PAUSED", 80, UIFactory.Accent, TextAnchor.MiddleCenter,
                mid, mid, new Vector2(-600f, 230f), new Vector2(600f, 380f));
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
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        public void ShowTitle(int best, int achievementsDone, int achievementsTotal)
        {
            titleRoot.SetActive(true);
            pauseRoot.SetActive(false);
            titleVisible = true;
            pauseVisible = false;
            titleStats.text = "Best score " + best.ToString("N0") + "     Achievements " + achievementsDone + "/" + achievementsTotal + "     v" + GameManager.Version;
        }

        public void ShowPause()
        {
            pauseRoot.SetActive(true);
            titleRoot.SetActive(false);
            pauseVisible = true;
            titleVisible = false;
            sel = 0;
            Highlight();
        }

        public void HideAll()
        {
            titleRoot.SetActive(false);
            pauseRoot.SetActive(false);
            titleVisible = false;
            pauseVisible = false;
        }

        void Highlight()
        {
            for (int i = 0; i < pauseBacks.Length; i++)
            {
                bool on = i == sel;
                pauseBacks[i].color = on ? new Color(1f, 0.85f, 0.3f, 0.9f) : new Color(0f, 0f, 0f, 0.5f);
                pauseTexts[i].color = on ? UIFactory.Ink : Color.white;
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            if (titleVisible)
            {
                titleRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 2f);
                titleRect.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(t * 3f));
                titlePrompt.color = new Color(1f, 1f, 1f, 0.6f + 0.4f * Mathf.Sin(t * 4f));
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
