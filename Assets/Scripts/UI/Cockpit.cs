using UnityEngine;
using UnityEngine.UI;
using VCS.Core;
using VCS.Player;

namespace VCS.UI
{
    /// <summary>
    /// The instrument strip along the bottom: a generated dashboard plate with the per-vacuum suction gauge, motor
    /// readouts, the real container filling up, an annunciator panel of warning tiles, the logbook, MASTER CAUTION
    /// and the mode. Arcade labels, flight-deck instruments.
    /// </summary>
    public class Cockpit
    {
        public const float Height = 250f;

        static readonly Color LabelColor = UIStyle.Steel;
        static readonly Color DimColor = UIStyle.Dim;
        static readonly Color AlarmColor = UIStyle.Red;
        static readonly Color OkColor = UIStyle.Green;
        static readonly Color WarnColor = UIStyle.Amber;

        public GameObject Root { get; private set; }

        VacuumSpec spec;
        Color accent = Color.white;

        Image gaugeFace, ledRing, needle;
        Text suctionLabel, suctionValue;
        Text[] motorLabels, motorValues;
        Image tempBar, filterBar, batteryBar;
        Image containerEmpty, containerFull, containerFullOverlay;
        Text containerLabel, containerValue, containerFullText;
        Image[] tiles;
        Text[] tileTexts;
        Text[] metaValues;
        Image masterTile;
        Text masterText, statusText, modeText, modelText;

        float textTimer;
        float blink;

        static readonly string[] TileNames = { "BAG FULL", "OVERHEAT", "CORD", "FILTER", "REVERSE", "TURBO" };
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

            var dash = UIStyle.PlateSprite("frame_wide", 0.22f);
            if (dash != null)
            {
                var img = UIFactory.Panel(t, "Dash", Color.white, Vector2.zero, Vector2.one, new Vector2(-10f, -40f), new Vector2(10f, 6f));
                img.sprite = dash;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = Mathf.Max(0.2f, dash.border.x / 44f);
            }
            else
            {
                UIFactory.Panel(t, "Back", UIStyle.Panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                UIFactory.Panel(t, "TopLine", UIStyle.Yellow, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f), new Vector2(0f, 0f));
            }
            UIStyle.Scanlines(t, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(0f, -6f), 0.06f);

            BuildSuction(t);
            BuildMotor(t);
            BuildContainer(t);
            BuildTiles(t);
            BuildMeta(t);
            BuildStatus(t);
        }

        static Text Txt(Transform p, string name, string s, int size, Color c, TextAnchor a, float x0, float y0, float x1, float y1, FontStyle style = FontStyle.Bold)
        {
            var z = Vector2.zero;
            var t = UIFactory.Text(p, name, s, size, c, a, z, z, new Vector2(x0, y0), new Vector2(x1, y1), false, FontStyle.Normal);
            bool label = c == LabelColor || c == DimColor;
            t.font = label ? UIStyle.Arcade : UIStyle.Mono;
            if (label) { t.fontSize = Mathf.Max(11, size - 3); t.fontStyle = FontStyle.Italic; }
            UIStyle.Edge(t, 1.5f);
            return t;
        }

        static Text Tab(Transform p, string name, string s, float x0, float y0, float x1, float y1, Color plate)
        {
            var z = Vector2.zero;
            return UIStyle.Tab(p, name, s, z, z, new Vector2(x0, y0), new Vector2(x1, y1), plate);
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

        static void Screen(Transform p, float x0, float y0, float x1, float y1)
        {
            var z = Vector2.zero;
            UIFactory.Panel(p, "ScreenEdge", new Color(0.3f, 0.34f, 0.4f, 0.9f), z, z, new Vector2(x0 - 2f, y0 - 2f), new Vector2(x1 + 2f, y1 + 2f));
            UIFactory.Panel(p, "Screen", UIStyle.Screen, z, z, new Vector2(x0, y0), new Vector2(x1, y1));
        }

        void BuildSuction(Transform t)
        {
            Screen(t, 30f, 10f, 270f, 210f);
            suctionLabel = Tab(t, "SuctionLabel", "SUCTION", 30f, 212f, 180f, 238f, UIStyle.Yellow);
            gaugeFace = Img(t, "GaugeFace", UISprites.GaugeFace, Color.white, 52f, 28f, 248f, 224f);
            ledRing = Img(t, "LedRing", UISprites.SegmentedRing, Color.white, 52f, 28f, 248f, 224f);
            ledRing.type = Image.Type.Filled;
            ledRing.fillMethod = Image.FillMethod.Radial360;
            ledRing.fillOrigin = (int)Image.Origin360.Bottom;
            ledRing.fillClockwise = true;
            ledRing.fillAmount = 0f;
            ledRing.rectTransform.localEulerAngles = new Vector3(0f, 0f, 120f);
            needle = Img(t, "Needle", UISprites.Needle, Color.white, 138f, 116f, 162f, 216f);
            needle.rectTransform.pivot = new Vector2(0.5f, 0.12f);
            needle.rectTransform.anchoredPosition = new Vector2(150f, 126f);
            needle.rectTransform.sizeDelta = new Vector2(20f, 100f);
            needle.preserveAspect = false;
            Img(t, "Hub", UISprites.Circle, new Color(0.1f, 0.1f, 0.12f), 141f, 117f, 159f, 135f);
            suctionValue = Txt(t, "SuctionValue", "0.0 kPa", 24, Color.white, TextAnchor.MiddleCenter, 30f, 12f, 270f, 40f);
        }

        void BuildMotor(Transform t)
        {
            Screen(t, 290f, 10f, 570f, 210f);
            Tab(t, "MotorTitle", "MOTOR", 290f, 212f, 420f, 238f, UIStyle.Blue);
            string[] labels = { "SPEED", "AIRFLOW", "TEMP", "FILTER", "BATTERY" };
            motorLabels = new Text[labels.Length];
            motorValues = new Text[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 184f - i * 38f;
                motorLabels[i] = Txt(t, "MotorLabel" + i, labels[i], 17, LabelColor, TextAnchor.MiddleLeft, 300f, y - 4f, 410f, y + 18f, FontStyle.Normal);
                motorValues[i] = Txt(t, "MotorValue" + i, "", 21, Color.white, TextAnchor.MiddleRight, 380f, y - 4f, 560f, y + 18f);
            }
            tempBar = MakeBar(t, "TempBar", 300f, 184f - 2 * 38f - 12f);
            filterBar = MakeBar(t, "FilterBar", 300f, 184f - 3 * 38f - 12f);
            batteryBar = MakeBar(t, "BatteryBar", 300f, 184f - 4 * 38f - 12f);
        }

        Image MakeBar(Transform t, string name, float x, float y)
        {
            var z = Vector2.zero;
            var back = UIFactory.Panel(t, name + "Back", new Color(0.12f, 0.13f, 0.17f), z, z, new Vector2(x, y), new Vector2(x + 260f, y + 7f));
            var fill = UIFactory.Panel(back.transform, name, Color.white, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f));
            return fill;
        }

        void BuildContainer(Transform t)
        {
            Screen(t, 590f, 10f, 870f, 210f);
            containerLabel = Tab(t, "ContainerLabel", "DUST BAG", 590f, 212f, 760f, 238f, UIStyle.Yellow);
            containerEmpty = Img(t, "ContainerEmpty", null, Color.white, 636f, 34f, 826f, 208f);
            containerFull = Img(t, "ContainerFull", null, Color.white, 636f, 34f, 826f, 208f);
            containerFull.type = Image.Type.Filled;
            containerFull.fillMethod = Image.FillMethod.Vertical;
            containerFull.fillOrigin = (int)Image.OriginVertical.Bottom;
            containerFull.fillAmount = 0f;
            containerFullOverlay = UIFactory.Panel(t, "ContainerFallbackFill", new Color(0.55f, 0.5f, 0.42f, 0.9f), Vector2.zero, Vector2.zero, new Vector2(670f, 50f), new Vector2(792f, 200f));
            containerFullOverlay.type = Image.Type.Filled;
            containerFullOverlay.fillMethod = Image.FillMethod.Vertical;
            containerFullOverlay.fillOrigin = (int)Image.OriginVertical.Bottom;
            containerValue = Txt(t, "ContainerValue", "", 21, Color.white, TextAnchor.MiddleCenter, 590f, 12f, 870f, 38f);
            containerFullText = Txt(t, "ContainerFullText", "FULL", 34, AlarmColor, TextAnchor.MiddleCenter, 592f, 96f, 870f, 160f);
            UIStyle.ArcadeText(containerFullText, AlarmColor, UIStyle.Ink, 4f);
            containerFullText.fontSize = 56;
            containerFullText.gameObject.SetActive(false);
        }

        void BuildTiles(Transform t)
        {
            Screen(t, 890f, 10f, 1180f, 210f);
            Tab(t, "TilesTitle", "ANNUNCIATOR", 890f, 212f, 1060f, 238f, UIStyle.Red);
            tiles = new Image[TileNames.Length];
            tileTexts = new Text[TileNames.Length];
            for (int i = 0; i < TileNames.Length; i++)
            {
                int col = i % 3, row = i / 3;
                float x0 = 900f + col * 92f, y0 = 114f - row * 92f;
                tiles[i] = UIStyle.Tile(t, "Tile" + i, TileNames[i], Vector2.zero, Vector2.zero, new Vector2(x0, y0), new Vector2(x0 + 86f, y0 + 82f), out tileTexts[i]);
                tileTexts[i].fontSize = 13;
            }
        }

        void BuildMeta(Transform t)
        {
            Screen(t, 1200f, 10f, 1520f, 210f);
            Tab(t, "MetaTitle", "LOGBOOK", 1200f, 212f, 1340f, 238f, UIStyle.Blue);
            metaValues = new Text[MetaNames.Length];
            for (int i = 0; i < MetaNames.Length; i++)
            {
                float y = 190f - i * 27f;
                Txt(t, "MetaLabel" + i, MetaNames[i], 15, LabelColor, TextAnchor.MiddleLeft, 1210f, y - 12f, 1330f, y + 12f, FontStyle.Normal);
                metaValues[i] = Txt(t, "MetaValue" + i, "", 18, Color.white, TextAnchor.MiddleRight, 1300f, y - 12f, 1510f, y + 12f);
            }
        }

        void BuildStatus(Transform t)
        {
            var z = Vector2.zero;
            Screen(t, 1540f, 10f, 1890f, 210f);
            Tab(t, "ModeTitle", "STATUS", 1540f, 212f, 1660f, 238f, UIStyle.Yellow);
            masterTile = UIStyle.Tile(t, "MasterTile", "MASTER CAUTION", z, z, new Vector2(1552f, 156f), new Vector2(1878f, 202f), out masterText);
            masterText.fontSize = 19;
            statusText = Txt(t, "StatusText", "ALL SYSTEMS NOMINAL", 18, OkColor, TextAnchor.MiddleCenter, 1544f, 118f, 1886f, 150f);
            statusText.font = UIStyle.Arcade;
            statusText.fontStyle = FontStyle.Italic;
            statusText.fontSize = 17;
            modeText = Txt(t, "ModeText", "NORMAL", 32, Color.white, TextAnchor.MiddleCenter, 1544f, 46f, 1886f, 114f);
            UIStyle.ArcadeText(modeText, Color.white, UIStyle.Blue, 4f);
            modeText.fontSize = 46;
            modelText = Txt(t, "ModelText", "", 13, DimColor, TextAnchor.MiddleCenter, 1544f, 14f, 1886f, 40f, FontStyle.Normal);
        }

        // ---------------------------------------------------------------- binding
        public void Bind(VacuumSpec s, int serial)
        {
            spec = s;
            accent = s.Accent;
            suctionValue.color = accent;
            for (int i = 0; i < motorValues.Length; i++) motorValues[i].color = accent;
            containerValue.color = accent;
            for (int i = 0; i < metaValues.Length; i++) metaValues[i].color = accent;
            ledRing.color = accent;
            needle.color = s.Gauge == GaugeStyle.Analog || s.Gauge == GaugeStyle.Industrial ? new Color(1f, 0.3f, 0.25f) : accent;
            tempBar.color = UIStyle.Amber;
            filterBar.color = accent;
            batteryBar.color = OkColor;

            var face = UISprites.Load("UI/Gauges/gauge_" + s.Id);
            gaugeFace.sprite = face != null ? face : UISprites.GaugeFace;
            bool showNeedle = s.Gauge == GaugeStyle.Analog || s.Gauge == GaugeStyle.Industrial;
            bool showRing = s.Gauge != GaugeStyle.Analog;
            needle.gameObject.SetActive(showNeedle);
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
            tileTexts[2].text = s.Cordless ? "TILT" : "CORD";
            tileTexts[4].text = s.Cordless ? "BATTERY" : "REVERSE";

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

            float v = Mathf.Clamp01(tm.Suction01);
            needle.rectTransform.localEulerAngles = new Vector3(0f, 0f, 120f - 240f * v);
            ledRing.fillAmount = v * (240f / 360f);

            float fill = Mathf.Clamp01(s.BagFill / s.BagCapacity);
            containerFull.fillAmount = fill;
            containerFullOverlay.fillAmount = fill;
            containerFullText.gameObject.SetActive(s.BagFull && blinkOn);

            tempBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((tm.TempC - 20f) / 100f), 1f);
            tempBar.color = tm.Overheat ? AlarmColor : (tm.TempC > 75f ? WarnColor : UIStyle.Amber);
            filterBar.rectTransform.anchorMax = new Vector2(tm.Filter01, 1f);
            filterBar.color = tm.FilterWarning ? WarnColor : accent;
            batteryBar.rectTransform.anchorMax = new Vector2(tm.Battery01, 1f);
            batteryBar.color = tm.LowBattery ? AlarmColor : OkColor;

            UIStyle.SetTile(tiles[0], tileTexts[0], s.BagFull && blinkOn, AlarmColor);
            UIStyle.SetTile(tiles[1], tileTexts[1], tm.Overheat && blinkOn, AlarmColor);
            if (spec.Cordless) UIStyle.SetTile(tiles[2], tileTexts[2], tm.Tilt, WarnColor);
            else UIStyle.SetTile(tiles[2], tileTexts[2], !tm.Powered || (tm.CordTaut && blinkOn), !tm.Powered ? AlarmColor : WarnColor);
            UIStyle.SetTile(tiles[3], tileTexts[3], tm.FilterWarning, WarnColor);
            if (spec.Cordless) UIStyle.SetTile(tiles[4], tileTexts[4], tm.LowBattery && blinkOn, AlarmColor);
            else UIStyle.SetTile(tiles[4], tileTexts[4], tm.Reverse, UIStyle.Blue);
            UIStyle.SetTile(tiles[5], tileTexts[5], tm.Turbo, UIStyle.Blue);

            bool caution = s.BagFull || tm.Overheat || !tm.Powered || tm.LowBattery || tm.CordTaut;
            UIStyle.SetTile(masterTile, masterText, caution && blinkOn, (tm.Overheat || !tm.Powered || s.BagFull) ? AlarmColor : WarnColor);

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
            modeText.text = !tm.Powered ? "OFF" : (tm.Reverse ? "REVERSE" : (tm.Turbo ? "TURBO" : "NORMAL"));
            modeText.color = !tm.Powered ? DimColor : (tm.Reverse ? UIStyle.Blue : (tm.Turbo ? UIStyle.Yellow : Color.white));
        }

        void SetStatus(string text, Color c)
        {
            statusText.text = text;
            statusText.color = c;
        }

        string FormatSuction(float v)
        {
            if (spec.SuctionMax >= 500f) return v.ToString("N0");
            if (spec.SuctionMax >= 50f) return v.ToString("0.0");
            return v.ToString("0.00");
        }
    }
}
