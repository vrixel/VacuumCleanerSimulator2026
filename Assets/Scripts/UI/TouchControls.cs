using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VCS.Core;

namespace VCS.UI
{
    /// <summary>
    /// The phone layer (2026-09-07, Android): a virtual stick bottom-left, hold and tap buttons bottom-right, camera
    /// orbit by dragging the free part of the screen, a big START on the title screen and a small PAUSE in play.
    /// Everything it reads goes through GameInput.Touch*, so the rest of the game does not know about touch. Built
    /// only in GameInput.TouchMode (phones, or "-touch" on the PC for screenshots and testing).
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        Canvas canvas;
        GameObject playRoot, titleRoot;
        VirtualStick stick;

        public static TouchControls Create()
        {
            var canvas = UIFactory.CreateCanvas("Touch", 30);
            var t = canvas.gameObject.AddComponent<TouchControls>();
            t.canvas = canvas;
            t.Build();
            return t;
        }

        void Build()
        {
            var root = canvas.transform;
            var bl = new Vector2(0f, 0f);
            var br = new Vector2(1f, 0f);
            var bc = new Vector2(0.5f, 0f);

            // ---- play: the look pad first (under everything else on this canvas), the right half of the screen
            playRoot = new GameObject("Play", typeof(RectTransform));
            playRoot.transform.SetParent(root, false);
            UIFactory.Anchor(playRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var pad = UIFactory.Panel(playRoot.transform, "LookPad", new Color(0f, 0f, 0f, 0f), new Vector2(0.45f, 0f), new Vector2(1f, 1f), new Vector2(0f, 330f), new Vector2(0f, -140f));
            pad.raycastTarget = true;
            pad.gameObject.AddComponent<LookPad>();

            // the stick: a translucent base ring and a knob, bottom-left
            var baseImg = UIFactory.Panel(playRoot.transform, "StickBase", new Color(1f, 1f, 1f, 0.28f), bl, bl, new Vector2(70f, 70f), new Vector2(370f, 370f));
            baseImg.sprite = UISprites.Ring;
            baseImg.raycastTarget = true;
            var knob = UIFactory.Panel(baseImg.transform, "Knob", new Color(1f, 0.84f, 0f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-60f, -60f), new Vector2(60f, 60f));
            knob.sprite = UISprites.Circle;
            knob.raycastTarget = false;
            stick = baseImg.gameObject.AddComponent<VirtualStick>();
            stick.Init(baseImg.rectTransform, knob.rectTransform, 100f);

            // the buttons: big round enamel pads, bottom-right cluster
            Button(playRoot.transform, "HOP", UIStyle.Green, br, new Vector2(-250f, 60f), 170f, () => GameInput.TouchHop = true, null);
            Button(playRoot.transform, "TURBO", UIStyle.Blue, br, new Vector2(-70f, 180f), 170f, () => GameInput.TouchTurbo = true, () => GameInput.TouchTurbo = false);
            Button(playRoot.transform, "BLOW", UIStyle.Amber, br, new Vector2(-430f, 180f), 140f, () => GameInput.TouchBlow = true, () => GameInput.TouchBlow = false);
            Button(playRoot.transform, "EMPTY", UIStyle.Yellow, br, new Vector2(-250f, 300f), 120f, () => GameInput.TouchEmpty = true, null);
            Button(playRoot.transform, "REWIND", UIStyle.Red, br, new Vector2(-70f, 20f), 120f, () => GameInput.TouchRewind = true, null);
            Button(playRoot.transform, "II", UIStyle.Steel, bc, new Vector2(0f, 24f), 90f, () => GameInput.TouchPause = true, null);

            // ---- title: tap anywhere on the big START plate
            titleRoot = new GameObject("Title", typeof(RectTransform));
            titleRoot.transform.SetParent(root, false);
            UIFactory.Anchor(titleRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var start = UIStyle.Plate(titleRoot.transform, "Start", "tab_plate", UIStyle.Yellow, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(74f, 90f), new Vector2(760f, 200f), 10f, UIStyle.Yellow, 0.34f);
            start.raycastTarget = true;
            var startBtn = start.gameObject.AddComponent<UnityEngine.UI.Button>();
            startBtn.onClick.AddListener(() => GameInput.TouchConfirm = true);
            var label = UIFactory.Text(start.transform, "Label", "TAP TO START CLEANING", 40, UIStyle.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
            UIStyle.Style(label, UIStyle.Arcade, 40, UIStyle.Ink, FontStyle.Italic);
        }

        /// <summary>A round pad with a label; down and up callbacks (up may be null for tap buttons).</summary>
        static void Button(Transform parent, string label, Color color, Vector2 anchor, Vector2 centre, float size, System.Action down, System.Action up)
        {
            var img = UIFactory.Panel(parent, "Btn" + label, color, anchor, anchor, centre + new Vector2(-size * 0.5f, 0f), centre + new Vector2(size * 0.5f, size));
            var sp = Resources.Load<Sprite>("UI/Hud/button_square");
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Simple; img.preserveAspect = true; }
            else img.sprite = UISprites.Circle;
            img.raycastTarget = true;
            var hb = img.gameObject.AddComponent<HoldButton>();
            hb.Down = down;
            hb.Up = up;
            var t = UIFactory.Text(img.transform, "Label", label, 26, UIStyle.Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
            UIStyle.Style(t, UIStyle.Arcade, label.Length > 5 ? 20 : 26, UIStyle.Ink, FontStyle.Italic);
        }

        void Update()
        {
            var gm = GameManager.I;
            bool playing = gm != null && gm.State == GameState.Playing;
            bool title = gm != null && gm.State == GameState.Title;
            if (playRoot.activeSelf != playing) playRoot.SetActive(playing);
            if (titleRoot.activeSelf != title) titleRoot.SetActive(title);
            GameInput.TouchMove = playing ? stick.Value : Vector2.zero;
            if (!playing) { GameInput.TouchTurbo = false; GameInput.TouchBlow = false; }
        }
    }

    /// <summary>A stick: the knob follows the finger inside the base radius; Value is -1..1 per axis.</summary>
    public class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform baseRect, knob;
        float radius;
        public Vector2 Value { get; private set; }

        public void Init(RectTransform b, RectTransform k, float r) { baseRect = b; knob = k; radius = r; }

        void Track(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, e.position, e.pressEventCamera, out var local)) return;
            var v = Vector2.ClampMagnitude(local / radius, 1f);
            Value = v;
            knob.anchoredPosition = v * radius;
        }

        public void OnPointerDown(PointerEventData e) => Track(e);
        public void OnDrag(PointerEventData e) => Track(e);
        public void OnPointerUp(PointerEventData e) { Value = Vector2.zero; knob.anchoredPosition = Vector2.zero; }
    }

    /// <summary>Down and up callbacks; the button lights while held.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action Down, Up;
        Image img;
        Color rest;

        void Awake() { img = GetComponent<Image>(); rest = img.color; }
        public void OnPointerDown(PointerEventData e) { img.color = Color.Lerp(rest, Color.white, 0.5f); Down?.Invoke(); }
        public void OnPointerUp(PointerEventData e) { img.color = rest; Up?.Invoke(); }
    }

    /// <summary>Dragging the free part of the screen orbits the camera, the way the mouse does.</summary>
    public class LookPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData e) { }
        public void OnDrag(PointerEventData e) { GameInput.AddTouchLook(e.delta * (0.12f * 1920f / Screen.width)); }
        public void OnPointerUp(PointerEventData e) { }
    }
}
