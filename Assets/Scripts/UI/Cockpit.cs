using UnityEngine;
using UnityEngine.UI;
using VCS.Core;
using VCS.Player;

namespace VCS.UI
{
    /// <summary>
    /// The instrument panel along the bottom of the screen: suction gauge (styled per vacuum), motor readouts,
    /// the real container filling up, warning lamps, odometer and a system-status box. Serious on purpose.
    /// </summary>
    public class Cockpit
    {
        public const float Height = 250f;

        static readonly Color LabelColor = new Color(0.78f, 0.82f, 0.88f);
        static readonly Color DimColor = new Color(0.35f, 0.38f, 0.42f);
        static readonly Color AlarmColor = new Color(1f, 0.28f, 0.2f);
        static readonly Color OkColor = new Color(0.35f, 0.92f, 0.45f);
        static readonly Color WarnColor = new Color(1f, 0.75f, 0.2f);

        public GameObject Root { get; private set; }

        VacuumSpec spec;
        Color accent = Color.white;

        Image gaugeFace, ledRing, needle, needleGlow;
        Text suctionLabel, suctionValue;
        Text[] motorLabels, motorValues;
        Image tempBar, filterBar, batteryBar;
        Image containerEmpty, containerFull, containerFullOverlay;
        Text containerLabel, containerValue, containerFullText;
        Image[] lampLeds, lampGlows;
        Text[] lampTexts;
        Text[] metaValues;
        Text statusText, modeText, modelText;
        Image statusBox;

        float textTimer;
        float blink;

        static readonly string[] LampNames = { "BAG FULL", "OVERHEAT", "TILT", "FILTER", "BATTERY", "TURBO" };
        static readonly string[] MetaNames = { "MODEL", "SERIAL", "ODOMETER", "RUNTIME", "INGESTED", "CLEAN", "POWER" };

        public static Cockpit Build(Transform canvas)
        {
            var c = new Cockpit();
            c.Construct(canvas);
            return c;
        }

        void Construct(Transform canvas)
        {
            Root = new GameObject("Cockpit", typeof(RectTransform));
            Root.transform.SetParent(canvas, false);
            UIFactory.Anchor(Root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, Height));
            var t = Root.transform;

            var panelSprite = UISprites.Load("UI/panel");
            var bg = UIFactory.Panel(t, "PanelBack", panelSprite != null ? new Color(0.85f, 0.87f, 0.9f, 0.98f) : new Color(0.08f, 0.09f, 0.11f, 0.97f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (panelSprite != null) { bg.sprite = panelSprite; bg.type = Image.Type.Simple; bg.preserveAspect = false; }
            UIFactory.Panel(t, "TopEdge", new Color(0.55f, 0.6f, 0.68f, 0.9f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 0f));
            UIFactory.Panel(t, "TopEdgeDark", new Color(0f, 0f, 0f, 0.6f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, -3f));

            BuildSuction(t);
            BuildMotor(t);
            BuildContainer(t);
            BuildLamps(t);
            BuildMeta(t);
            BuildStatus(t);
        }

        // ---------------------------------------------------------------- pieces
        // Labels (steel colour) get the squared display face, everything else the monospaced readout face.
        static Text Txt(Transform p, string name, string s, int size, Color c, TextAnchor a, float x0, float y0, float x1, float y1, FontStyle style = FontStyle.Bold)
        {
            var z = Vector2.zero;
            var t = UIFactory.Text(p, name, s, size, c, a, z, z, new Vector2(x0, y0), new Vector2(x1, y1), false, FontStyle.Normal);
            bool label = c == LabelColor || c == DimColor;
            t.font = label ? UIStyle.Display : UIStyle.Mono;
            if (label) t.fontSize = Mathf.Max(11, size - 2);
            UIStyle.Edge(t);
            return t;
        }

        static Image Img(Transform p, string name, Sprite sprite, Color c, float x0, float y0, float x1, float y1)
        {
            var z = Vector2.zero;
            var img = UIFactory.Panel(p, name, c, z, z, new Vector2(x0, y0), new Vector2(x1, y1));
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            return img;
        }

        static Image Divider(Transform p, float x)
        {
            var z = Vector2.zero;
            return UIFactory.Panel(p, "Divider", new Color(0f, 0f, 0f, 0.45f), z, z, new Vector2(x, 12f), new Vector2(x + 2f, 232f));
        }

        void BuildSuction(Transform t)
        {
            suctionLabel = Txt(t, "SuctionLabel", "SUCTION", 18, LabelColor, TextAnchor.MiddleCenter, 40f, 226f, 260f, 248f);
            gaugeFace = Img(t, "GaugeFace", UISprites.GaugeFace, Color.white, 50f, 28f, 250f, 228f);
            ledRing = Img(t, "LedRing", UISprites.SegmentedRing, Color.white, 50f, 28f, 250f, 228f);
            ledRing.type = Image.Type.Filled;
            ledRing.fillMethod = Image.FillMethod.Radial360;
            ledRing.fillOrigin = (int)Image.Origin360.Bottom;
            ledRing.fillClockwise = true;
            ledRing.fillAmount = 0f;
            ledRing.rectTransform.localEulerAngles = new Vector3(0f, 0f, 120f);
            needleGlow = Img(t, "NeedleGlow", UISprites.Glow, new Color(1f, 1f, 1f, 0.25f), 130f, 108f, 170f, 148f);
            needle = Img(t, "Needle", UISprites.Needle, Color.white, 138f, 118f, 162f, 218f);
            needle.rectTransform.pivot = new Vector2(0.5f, 0.12f);
            needle.rectTransform.anchoredPosition = new Vector2(150f, 128f);
            needle.rectTransform.sizeDelta = new Vector2(20f, 100f);
            needle.preserveAspect = false;
            Img(t, "Hub", UISprites.Circle, new Color(0.15f, 0.16f, 0.18f), 141f, 119f, 159f, 137f);
            suctionValue = Txt(t, "SuctionValue", "0.0 kPa", 26, Color.white, TextAnchor.MiddleCenter, 30f, 0f, 270f, 30f);
            UIStyle.Glow(suctionValue, Color.white, 2f, 0.3f);
            Divider(t, 276f);
        }

        void BuildMotor(Transform t)
        {
            Txt(t, "MotorTitle", "MOTOR", 18, LabelColor, TextAnchor.MiddleLeft, 292f, 226f, 560f, 248f);
            string[] labels = { "SPEED", "AIRFLOW", "TEMP", "FILTER", "BATTERY" };
            motorLabels = new Text[labels.Length];
            motorValues = new Text[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 200f - i * 40f;
                motorLabels[i] = Txt(t, "MotorLabel" + i, labels[i], 17, LabelColor, TextAnchor.MiddleLeft, 292f, y - 4f, 400f, y + 18f, FontStyle.Normal);
                motorValues[i] = Txt(t, "MotorValue" + i, "", 22, Color.white, TextAnchor.MiddleRight, 380f, y - 4f, 560f, y + 18f);
            }
            tempBar = MakeBar(t, "TempBar", 292f, 200f - 2 * 40f - 12f);
            filterBar = MakeBar(t, "FilterBar", 292f, 200f - 3 * 40f - 12f);
            batteryBar = MakeBar(t, "BatteryBar", 292f, 200f - 4 * 40f - 12f);
            Divider(t, 576f);
        }

        Image MakeBar(Transform t, string name, float x, float y)
        {
            var z = Vector2.zero;
            var back = UIFactory.Panel(t, name + "Back", new Color(0f, 0f, 0f, 0.6f), z, z, new Vector2(x, y), new Vector2(x + 268f, y + 6f));
            var fill = UIFactory.Panel(back.transform, name, Color.white, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f));
            return fill;
        }

        void BuildContainer(Transform t)
        {
            containerLabel = Txt(t, "ContainerLabel", "DUST BAG", 18, LabelColor, TextAnchor.MiddleCenter, 592f, 226f, 870f, 248f);
            containerEmpty = Img(t, "ContainerEmpty", null, Color.white, 631f, 30f, 831f, 230f);
            containerFull = Img(t, "ContainerFull", null, Color.white, 631f, 30f, 831f, 230f);
            containerFull.type = Image.Type.Filled;
            containerFull.fillMethod = Image.FillMethod.Vertical;
            containerFull.fillOrigin = (int)Image.OriginVertical.Bottom;
            containerFull.fillAmount = 0f;
            // fallback: a simple silhouette that fills up when no illustration is available
            containerFullOverlay = UIFactory.Panel(t, "ContainerFallbackFill", new Color(0.55f, 0.5f, 0.42f, 0.9f), Vector2.zero, Vector2.zero, new Vector2(670f, 50f), new Vector2(792f, 210f));
            containerFullOverlay.type = Image.Type.Filled;
            containerFullOverlay.fillMethod = Image.FillMethod.Vertical;
            containerFullOverlay.fillOrigin = (int)Image.OriginVertical.Bottom;
            containerValue = Txt(t, "ContainerValue", "", 23, Color.white, TextAnchor.MiddleCenter, 580f, 0f, 882f, 30f);
            containerFullText = Txt(t, "ContainerFullText", "FULL", 34, AlarmColor, TextAnchor.MiddleCenter, 592f, 100f, 870f, 160f);
            containerFullText.font = UIStyle.Title;
            containerFullText.fontSize = 56;
            UIStyle.Glow(containerFullText, AlarmColor, 4f, 0.6f);
            containerFullText.gameObject.SetActive(false);
            Divider(t, 886f);
        }

        void BuildLamps(Transform t)
        {
            Txt(t, "LampsTitle", "WARNINGS", 18, LabelColor, TextAnchor.MiddleLeft, 902f, 226f, 1180f, 248f);
            lampLeds = new Image[LampNames.Length];
            lampGlows = new Image[LampNames.Length];
            lampTexts = new Text[LampNames.Length];
            for (int i = 0; i < LampNames.Length; i++)
            {
                int col = i % 3, row = i / 3;
                float cx = 902f + 48f + col * 92f;
                float cy = 168f - row * 92f;
                lampGlows[i] = Img(t, "LampGlow" + i, UISprites.Glow, new Color(1f, 0f, 0f, 0f), cx - 30f, cy - 30f, cx + 30f, cy + 30f);
                Img(t, "LampBezel" + i, UISprites.Circle, new Color(0.02f, 0.02f, 0.03f), cx - 16f, cy - 16f, cx + 16f, cy + 16f);
                lampLeds[i] = Img(t, "Lamp" + i, UISprites.Circle, DimColor, cx - 12f, cy - 12f, cx + 12f, cy + 12f);
                lampTexts[i] = Txt(t, "LampText" + i, LampNames[i], 14, LabelColor, TextAnchor.MiddleCenter, cx - 46f, cy - 48f, cx + 46f, cy - 22f, FontStyle.Normal);
            }
            Divider(t, 1196f);
        }

        void BuildMeta(Transform t)
        {
            metaValues = new Text[MetaNames.Length];
            for (int i = 0; i < MetaNames.Length; i++)
            {
                float y = 224f - i * 31f;
                Txt(t, "MetaLabel" + i, MetaNames[i], 15, LabelColor, TextAnchor.MiddleLeft, 1212f, y - 12f, 1330f, y + 12f, FontStyle.Normal);
                metaValues[i] = Txt(t, "MetaValue" + i, "", 19, Color.white, TextAnchor.MiddleRight, 1300f, y - 12f, 1520f, y + 12f);
            }
            Divider(t, 1536f);
        }

        void BuildStatus(Transform t)
        {
            var z = Vector2.zero;
            statusBox = UIFactory.Panel(t, "StatusBox", new Color(0f, 0f, 0f, 0.55f), z, z, new Vector2(1552f, 14f), new Vector2(1890f, 232f));
            Txt(t, "StatusTitle", "SYSTEM STATUS", 17, LabelColor, TextAnchor.MiddleCenter, 1552f, 200f, 1890f, 228f, FontStyle.Normal);
            statusText = Txt(t, "StatusText", "ALL SYSTEMS NOMINAL", 26, OkColor, TextAnchor.MiddleCenter, 1556f, 130f, 1886f, 196f);
            statusText.font = UIStyle.Display;
            statusText.fontSize = 22;
            UIStyle.Glow(statusText, OkColor, 2f, 0.35f);
            Txt(t, "ModeTitle", "MODE", 15, LabelColor, TextAnchor.MiddleCenter, 1552f, 96f, 1890f, 118f, FontStyle.Normal);
            modeText = Txt(t, "ModeText", "NORMAL", 32, Color.white, TextAnchor.MiddleCenter, 1556f, 48f, 1886f, 98f);
            modeText.font = UIStyle.Title;
            modeText.fontSize = 44;
            modelText = Txt(t, "ModelText", "", 14, new Color(0.55f, 0.58f, 0.64f), TextAnchor.MiddleCenter, 1552f, 18f, 1890f, 44f, FontStyle.Normal);
        }

        // ---------------------------------------------------------------- binding
        public void Bind(VacuumSpec s, int serial)
        {
            spec = s;
            accent = s.Accent;
            suctionValue.color = accent;
            UIStyle.Glow(suctionValue, accent, 2f, 0.35f);
            for (int i = 0; i < motorValues.Length; i++) motorValues[i].color = accent;
            containerValue.color = accent;
            for (int i = 0; i < metaValues.Length; i++) metaValues[i].color = accent;
            ledRing.color = accent;
            needle.color = s.Gauge == GaugeStyle.Analog || s.Gauge == GaugeStyle.Industrial ? new Color(1f, 0.3f, 0.25f) : accent;
            needleGlow.color = new Color(accent.r, accent.g, accent.b, 0.3f);
            tempBar.color = new Color(1f, 0.55f, 0.25f);
            filterBar.color = accent;
            batteryBar.color = OkColor;

            var face = UISprites.Load("UI/Gauges/gauge_" + s.Id);
            gaugeFace.sprite = face != null ? face : UISprites.GaugeFace;
            bool showNeedle = s.Gauge == GaugeStyle.Analog || s.Gauge == GaugeStyle.Industrial;
            bool showRing = s.Gauge != GaugeStyle.Analog;
            needle.gameObject.SetActive(showNeedle);
            needleGlow.gameObject.SetActive(showNeedle);
            ledRing.gameObject.SetActive(showRing);
            suctionLabel.text = s.SuctionLabel;

            string kind = s.Container.ToString().ToLowerInvariant();
            var empty = UISprites.Load("UI/Containers/" + kind + "_empty");
            var full = UISprites.Load("UI/Containers/" + kind + "_full");
            containerEmpty.sprite = empty;
            containerFull.sprite = full;
            bool hasArt = empty != null && full != null;
            containerEmpty.gameObject.SetActive(empty != null);
            containerFull.gameObject.SetActive(hasArt);
            containerFullOverlay.gameObject.SetActive(!hasArt);
            containerLabel.text = s.ContainerLabel;

            motorLabels[4].text = s.Cordless ? "BATTERY" : "MAINS";
            batteryBar.gameObject.SetActive(s.Cordless);
            batteryBar.transform.parent.gameObject.SetActive(s.Cordless);
            lampTexts[2].text = s.Cordless ? "TILT" : "CORD";
            lampTexts[4].text = s.Cordless ? "BATTERY" : "REVERSE";

            metaValues[0].text = s.Name.ToUpperInvariant();
            metaValues[1].text = s.ModelCode + "-" + (1000 + serial % 9000).ToString();
            modelText.text = s.ModelCode + "  " + s.Tagline.ToUpperInvariant();
            textTimer = 0f;
        }

        public void SetVisible(bool v) { Root.SetActive(v); }

        // ---------------------------------------------------------------- refresh
        public void Refresh(Telemetry tm, SuctionSystem s, GameManager gm, float dt)
        {
            if (spec == null || tm == null || s == null) return;
            blink += dt;
            bool blinkOn = Mathf.Repeat(blink, 0.5f) < 0.3f;

            // gauge: 240 degree scale from -120 (empty) to +120 (full)
            float v = Mathf.Clamp01(tm.Suction01);
            needle.rectTransform.localEulerAngles = new Vector3(0f, 0f, 120f - 240f * v);
            ledRing.fillAmount = v * (240f / 360f);

            // container: real fill
            float fill = Mathf.Clamp01(s.BagFill / s.BagCapacity);
            containerFull.fillAmount = fill;
            containerFullOverlay.fillAmount = fill;
            containerFullText.gameObject.SetActive(s.BagFull && blinkOn);

            tempBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((tm.TempC - 20f) / 100f), 1f);
            tempBar.color = tm.Overheat ? AlarmColor : (tm.TempC > 75f ? WarnColor : new Color(1f, 0.55f, 0.25f));
            filterBar.rectTransform.anchorMax = new Vector2(tm.Filter01, 1f);
            filterBar.color = tm.FilterWarning ? WarnColor : accent;
            batteryBar.rectTransform.anchorMax = new Vector2(tm.Battery01, 1f);
            batteryBar.color = tm.LowBattery ? AlarmColor : OkColor;

            Lamp(0, s.BagFull, AlarmColor, blinkOn);
            Lamp(1, tm.Overheat, AlarmColor, blinkOn);
            if (spec.Cordless) Lamp(2, tm.Tilt, WarnColor, true);
            else Lamp(2, !tm.Powered || tm.CordTaut, !tm.Powered ? AlarmColor : WarnColor, tm.CordTaut ? blinkOn : true);
            Lamp(3, tm.FilterWarning, WarnColor, true);
            if (spec.Cordless) Lamp(4, tm.LowBattery, AlarmColor, blinkOn);
            else Lamp(4, tm.Reverse, new Color(0.4f, 0.7f, 1f), true);
            Lamp(5, tm.Turbo, new Color(0.4f, 0.9f, 1f), true);

            textTimer -= dt;
            if (textTimer > 0f) return;
            textTimer = 0.1f;

            suctionValue.text = FormatSuction(tm.SuctionValue) + " " + spec.SuctionUnit;
            motorValues[0].text = tm.Rpm.ToString("N0") + " rpm";
            motorValues[1].text = tm.AirflowLps.ToString("0.0") + " L/s";
            motorValues[2].text = tm.TempC.ToString("0") + " C";
            motorValues[3].text = (tm.Filter01 * 100f).ToString("0") + " %";
            motorValues[4].text = spec.Cordless ? (tm.Battery01 * 100f).ToString("0") + " %" : (tm.Powered ? "230 V  50 Hz" : "0 V  UNPLUGGED");

            float litres = s.BagFill / 10f;
            containerValue.text = litres.ToString("0.0") + " / " + (s.BagCapacity / 10f).ToString("0.0") + " L   " + (fill * 100f).ToString("0") + " %";

            metaValues[2].text = tm.OdometerM.ToString("0000.0") + " m";
            metaValues[3].text = GameManager.FormatTime(tm.RuntimeS);
            metaValues[4].text = tm.ItemsIngested.ToString("000000");
            metaValues[5].text = (gm.Cleanliness * 100f).ToString("0.0") + " %";
            metaValues[6].text = "LEVEL " + gm.PowerLevel + " / " + GameManager.MaxPower;

            if (tm.CordRewinding) SetStatus("REWINDING CORD", WarnColor);
            else if (!tm.Powered) SetStatus("NO POWER - FIND A SOCKET", AlarmColor);
            else if (tm.Overheat) SetStatus("MOTOR OVERHEAT", AlarmColor);
            else if (tm.CordTaut) SetStatus("CORD AT FULL LENGTH", WarnColor);
            else if (s.BagFull) SetStatus("CONTAINER FULL", AlarmColor);
            else if (tm.LowBattery) SetStatus("BATTERY LOW", AlarmColor);
            else if (tm.FilterWarning) SetStatus("CLEAN FILTER SOON", WarnColor);
            else if (tm.Tilt) SetStatus("AIRBORNE", WarnColor);
            else SetStatus("ALL SYSTEMS NOMINAL", OkColor);
            modeText.text = !tm.Powered ? "OFF" : (tm.Reverse ? "REVERSE FLOW" : (tm.Turbo ? "TURBO" : "NORMAL"));
            modeText.color = !tm.Powered ? DimColor : (tm.Reverse ? new Color(0.5f, 0.75f, 1f) : (tm.Turbo ? new Color(0.5f, 0.95f, 1f) : Color.white));
        }

        void SetStatus(string text, Color c)
        {
            statusText.text = text;
            statusText.color = c;
            statusBox.color = c == OkColor ? new Color(0f, 0f, 0f, 0.55f) : new Color(c.r * 0.25f, c.g * 0.25f, c.b * 0.25f, 0.7f);
        }

        void Lamp(int i, bool on, Color c, bool blinkOn)
        {
            bool lit = on && blinkOn;
            lampLeds[i].color = lit ? c : (on ? Color.Lerp(DimColor, c, 0.35f) : DimColor);
            lampGlows[i].color = lit ? new Color(c.r, c.g, c.b, 0.55f) : new Color(c.r, c.g, c.b, 0f);
            lampTexts[i].color = on ? c : LabelColor;
        }

        string FormatSuction(float v)
        {
            if (spec.SuctionMax >= 500f) return v.ToString("N0");
            if (spec.SuctionMax >= 50f) return v.ToString("0.0");
            return v.ToString("0.00");
        }
    }
}
