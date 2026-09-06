using System.Collections.Generic;
using UnityEngine;
using VCS.Audio;
using VCS.CameraRig;
using VCS.FX;
using VCS.Objectives;
using VCS.Player;
using VCS.UI;
using VCS.World;

namespace VCS.Core
{
    public enum GameState { Title, Playing, Paused }

    /// <summary>
    /// Owns the run: state machine, score, combo, power level, banners. Everything else hangs off it.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string GameName = "Vacuum Cleaner Simulator 2026";
        public const string Version = "0.4.0";
        public const int MaxPower = 5;
        public static readonly int[] PowerThresholds = { 0, 300, 1000, 2500, 5000 };

        public static GameManager I { get; private set; }

        /// <summary>Set by <see cref="SmokeRunner"/>: never grab the mouse cursor during an automated run.</summary>
        public static bool SmokeMode;

        public GameState State { get; private set; } = GameState.Title;
        public int Score { get; private set; }
        public int PowerLevel { get; private set; } = 1;
        public int ComboCount { get; private set; }
        public float ComboTimeLeft { get; private set; }
        public float ComboMultiplier => Mathf.Min(1f + ComboCount * 0.1f, 3f);
        public float PlayTime { get; private set; }
        public float Cleanliness => Level != null && Level.MessTotal > 0 ? (float)Level.MessCleaned / Level.MessTotal : 0f;

        public int BestScore
        {
            get => PlayerPrefs.GetInt("best_score", 0);
            private set => PlayerPrefs.SetInt("best_score", value);
        }

        public LevelBuilder Level { get; private set; }
        public VacuumController Player { get; private set; }
        public SuctionSystem Suction => Player != null ? Player.Suction : null;
        public FollowCamera Cam { get; private set; }
        public HudController Hud { get; private set; }
        public MenuController Menu { get; private set; }
        public ObjectiveSystem Objectives { get; private set; }
        public GameAudio Audio { get; private set; }
        public EffectsFactory Fx { get; private set; }
        public Telemetry Telemetry { get; } = new Telemetry();

        struct Banner { public string Big; public string Small; public float Duration; public bool Must; }
        // His feedback (2026-09-06): a splash every few seconds in the middle of the screen hid the vacuum. Splashes
        // are now at most one every SplashInterval seconds, in the upper band; anything that cannot splash right
        // away becomes a toast under the power strip, so nothing queues up and nothing is lost.
        const float SplashInterval = 7f;
        float lastSplashAt = -100f;

        readonly List<Banner> banners = new List<Banner>();
        float bannerTimer;
        bool spotlessShown;
        int seed = 2026;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Physics.gravity = new Vector3(0f, -16f, 0f);
            // phones (2026-09-07): the touch layer, and a lighter picture (the post-processing follows in RenderingSetup)
            bool touch = Application.isMobilePlatform;
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-touch") touch = true;
            GameInput.TouchMode = touch;
            bool mobile = Application.isMobilePlatform;
            QualitySettings.vSyncCount = 1;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = mobile ? ShadowResolution.Medium : ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = mobile ? 30f : 45f;
            QualitySettings.shadowCascades = mobile ? 1 : 2;
            QualitySettings.antiAliasing = mobile ? 2 : 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.realtimeReflectionProbes = true;
            if (mobile)
            {
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }

        void Start()
        {
            Audio = GameAudio.Create(transform);
            Fx = EffectsFactory.Create(transform);
            Objectives = new ObjectiveSystem();
            Objectives.Completed += OnObjectiveCompleted;
            Level = new GameObject("Level").AddComponent<LevelBuilder>();
            Cam = FollowCamera.Create();
            RenderingSetup.Attach(Cam.Cam);
            Hud = HudController.Create();
            Menu = MenuController.Create();
            if (GameInput.TouchMode) TouchControls.Create();
            Menu.OnTitleStart = StartGame;
            Menu.OnPauseSelect = OnPauseMenu;
            Level.Build(seed);
            EnterTitle();
            Debug.Log("[VCS] " + GameName + " v" + Version + " ready: " + Level.MessTotal + " pieces of mess, "
                      + Objectives.All.Count + " achievements, best score " + BestScore);
            SmokeRunner.TryStart(this);
            GalleryRunner.TryStart(this);
        }

        void EnterTitle()
        {
            Debug.Log("[VCS] Title screen");
            State = GameState.Title;
            Time.timeScale = 1f;
            if (Player != null) { Destroy(Player.gameObject); Player = null; }
            Cam.SetOrbit(Level.HouseCenter, 26f, 14f);
            Hud.SetVisible(false);
            Menu.ShowTitle(BestScore, Objectives.DoneCount, Objectives.All.Count);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Audio.SetHum(0f, false);
            Audio.PlayMusic("title");
            Audio.DuckMusic(false);
        }

        public void StartGame()
        {
            Score = 0; PowerLevel = 1; ComboCount = 0; ComboTimeLeft = 0f; PlayTime = 0f;
            spotlessShown = false;
            catHissShown = false;
            powderScore = 0f; powderReported = 0f;
            banners.Clear(); bannerTimer = 0f;
            Objectives.ResetProgress();
            seed++;
            Level.Build(seed);
            if (Player != null) Destroy(Player.gameObject);
            Player = VacuumController.Create(Level.PlayerSpawn, VacuumCatalog.Selected, Level.Sockets.Count > 0 ? Level.Sockets[0] : null);
            Player.Suction.SetPower(PowerLevel);
            Cam.SetFollow(Player.transform);
            Menu.HideAll();
            Hud.SetVisible(true);
            Hud.ResetRun();
            Telemetry.Reset(Player.Spec);
            Hud.BindVacuum(Player.Spec, seed, Player.transform);
            Hud.SetPower(Player.Spec.Name, PowerLevel, PropFactory.EatLabel(PowerLevel + Player.Spec.SizeBonus));
            Audio.PlayMusic("game");
            Audio.DuckMusic(false);
            if (GameInput.TouchMode) Hud.ShowHint("Left stick drives, right buttons act. Drag the free part of the screen to look around.", 8f);
            else Hud.ShowHint(Player.Spec.Cordless
                ? "WASD / left stick: drive     SPACE / A: hop     SHIFT / RB: turbo     E / B: blow     F / X: empty bag at the bin     ESC / Start: pause"
                : "WASD / left stick: drive     SPACE / A: hop     SHIFT / RB: turbo     E / B: blow     F / X: empty bag at the bin     R / Y: rewind the cord     ESC: pause", 14f);
            State = GameState.Playing;
            Time.timeScale = 1f;
            Cursor.lockState = SmokeMode || GameInput.TouchMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = SmokeMode || GameInput.TouchMode;
            Audio.PlayStart();
            Debug.Log("[VCS] Run started, seed " + seed + ", mess " + Level.MessTotal + ", vacuum " + Player.Spec.Id);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            Menu.ShowPause();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Audio.SetHum(0f, false);
            Audio.DuckMusic(true);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            Menu.HideAll();
            Cursor.lockState = SmokeMode || GameInput.TouchMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = SmokeMode || GameInput.TouchMode;
            Audio.DuckMusic(false);
        }

        void OnPauseMenu(int index)
        {
            switch (index)
            {
                case 0: Resume(); break;
                case 1: Time.timeScale = 1f; StartGame(); break;
                case 2: EnterTitle(); break;
                default: QuitApp(); break;
            }
        }

        public static void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
            if (State == GameState.Playing)
            {
                PlayTime += Time.deltaTime;
                if (GameInput.PauseDown) { Pause(); return; }
                if (ComboTimeLeft > 0f)
                {
                    ComboTimeLeft -= Time.deltaTime;
                    if (ComboTimeLeft <= 0f) ComboCount = 0;
                }
                UpdateBanners(Time.deltaTime);
                Hud.SetScore(Score);
                Hud.SetCombo(ComboCount, ComboMultiplier, ComboTimeLeft);
                Hud.SetTime(PlayTime);
                Hud.SetObjectives(Objectives);
                var s = Suction;
                if (s != null && Level.Bin != null)
                {
                    Telemetry.Tick(this, Time.deltaTime);
                    Hud.SetTelemetry(Telemetry, s, this, Time.deltaTime);
                    bool nearBin = Vector3.Distance(Player.transform.position, Level.Bin.transform.position) < Level.Bin.Radius;
                    bool canEmpty = nearBin && s.BagFill > 0f;
                    Hud.SetBinPrompt(canEmpty);
                    if (canEmpty && GameInput.EmptyDown) EmptyBagIntoBin();
                    Level.Bin.SetHighlight(s.BagFull);
                    if (GameInput.RewindDown && Player.Cord != null) Player.Cord.Rewind();
                }
                if (!spotlessShown && Level.MessTotal > 0 && Level.MessCleaned >= Level.MessTotal)
                {
                    spotlessShown = true;
                    Objectives.Report("clean100");
                    ShowBanner("SPOTLESS!", "House cleaned in " + FormatTime(PlayTime) + ". Keep wrecking it if you like.", 5f, true, true);
                    Audio.PlayFanfare();
                    // the finale of every corded run: the cord reels itself in
                    if (Player.Cord != null) StartCoroutine(FinaleRewind());
                }
                if (Score > BestScore) BestScore = Score;
            }
            else if (State == GameState.Paused)
            {
                if (GameInput.PauseDown) Resume();
            }
        }

        System.Collections.IEnumerator FinaleRewind()
        {
            yield return new WaitForSeconds(2.5f);
            if (State == GameState.Playing && Player != null && Player.Cord != null) Player.Cord.Rewind();
        }

        public void AddScore(int basePoints, bool countsCombo = true)
        {
            if (countsCombo) { ComboCount++; ComboTimeLeft = Player != null ? Player.Spec.ComboTime : 1.6f; }
            int pts = Mathf.RoundToInt(basePoints * (countsCombo ? ComboMultiplier : 1f));
            Score += pts;
            CheckPowerUp();
        }

        void CheckPowerUp()
        {
            while (PowerLevel < MaxPower && Score >= PowerThresholds[PowerLevel])
            {
                PowerLevel++;
                if (Suction != null) Suction.SetPower(PowerLevel);
                if (Player != null) Player.OnPowerUp(PowerLevel);
                int eats = PowerLevel + (Player != null ? Player.Spec.SizeBonus : 0);
                Hud.SetPower(Player != null ? Player.Spec.Name : "", PowerLevel, PropFactory.EatLabel(eats));
                ShowBanner("POWER UP!", "Level " + PowerLevel + ": you can now eat " + PropFactory.EatLabel(eats), 2.6f);
                Audio.PlayLevelUp();
            }
        }

        public void OnDebrisAbsorbed(Debris d)
        {
            AddScore(d.Points);
            if (d.CountsAsMess) Level.OnMessAbsorbed();
            Telemetry.OnItemIngested(d.SizeClass);
            Objectives.Report("absorb:" + d.Kind);
            Objectives.Report("absorb:any");
            Audio.PlayPop(d.SizeClass);
            Fx.Puff(d.transform.position, d.PuffColor, 6 + d.SizeClass * 4);
            if (d.SizeClass >= 3)
            {
                Fx.Sparkle(d.transform.position, 20);
                Cam.Shake(0.15f + d.SizeClass * 0.05f);
            }
        }

        float powderScore, powderReported, powderPuffAt;
        bool catHissShown;

        /// <summary>Square metres of cocoa powder just cleared under the nozzle.</summary>
        public void OnPowderCleaned(float sqm, Vector3 at)
        {
            powderScore += sqm * 40f;
            int pts = Mathf.FloorToInt(powderScore);
            if (pts > 0) { powderScore -= pts; AddScore(pts); }
            powderReported += sqm;
            int whole = Mathf.FloorToInt(powderReported);
            if (whole > 0) { powderReported -= whole; Objectives.Report("powder", whole); Telemetry.OnItemIngested(1); }
            if (Time.time > powderPuffAt)
            {
                powderPuffAt = Time.time + 0.12f;
                Fx.Puff(at + Vector3.up * 0.08f, PowderSystem.Cocoa, 3);
            }
        }

        public void OnCatScared(Cat cat, bool bumped)
        {
            AddScore(bumped ? 50 : 30);
            Objectives.Report("cat");
            if (bumped) { Audio.PlayYowl(); Cam.Shake(0.12f); } else Audio.PlayMeow();
            Fx.Puff(cat.transform.position + Vector3.up * 0.3f, Cat.Fur, bumped ? 12 : 6);
            if (bumped && !catHissShown)
            {
                catHissShown = true;
                ShowBanner("HISSSS", "You ran into the cat. It will remember this", 2f, false);
            }
        }

        public void OnBagFull()
        {
            Objectives.Report("bagfull");
            ShowBanner("BAG FULL!", "Empty it at the bin (F / X) or hold E / B to blow everything back out", 3f, false);
            Audio.PlayBagFull();
        }

        void EmptyBagIntoBin()
        {
            var s = Suction;
            if (s == null || s.BagFill <= 0f) return;
            int items = s.Bag.Count;
            s.EmptyBag();
            Telemetry.OnEmptied();
            int bonus = 50 + items * 2;
            AddScore(bonus, false);
            Objectives.Report("trash");
            ShowBanner("TRASH DAY!", "+" + bonus + " bonus for " + items + " things thrown away", 2f);
            Audio.PlayClunk();
            Fx.Puff(Level.Bin.transform.position + Vector3.up, new Color(0.5f, 0.5f, 0.5f), 30);
        }

        void OnObjectiveCompleted(Objective o)
        {
            AddScore(o.Reward, false);
            ShowBanner("ACHIEVEMENT: " + o.Title, o.Description + "  (+" + o.Reward + ")", 2.4f);
            Audio.PlayDing();
        }

        /// <summary>
        /// bonus: a reward worth a splash (power up, achievement, trash day, spotless). Status messages (bag full,
        /// cord, cat) pass bonus = false and go straight to the toast strip. must: wait for a free slot instead of
        /// degrading to a toast (spotless).
        /// </summary>
        public void ShowBanner(string big, string small, float duration = 2.5f, bool bonus = true, bool must = false)
        {
            if (!bonus) { ShowToast(big, small); return; }
            banners.Add(new Banner { Big = big, Small = small, Duration = duration, Must = must });
        }

        /// <summary>One line on the slim strip under the power strip; queued, never covers the play area.</summary>
        public void ShowToast(string title, string detail)
        {
            if (Hud != null) Hud.ShowToast(string.IsNullOrEmpty(detail) ? title : title + "   " + detail);
        }

        void UpdateBanners(float dt)
        {
            if (bannerTimer > 0f) bannerTimer -= dt;
            if (banners.Count == 0) return;
            bool free = bannerTimer <= 0f && Time.unscaledTime - lastSplashAt >= SplashInterval;
            var b = banners[0];
            if (!free)
            {
                if (b.Must) return;   // spotless waits for its slot
                banners.RemoveAt(0);
                ShowToast(b.Big, b.Small);
                return;
            }
            banners.RemoveAt(0);
            Hud.ShowBanner(b.Big, b.Small, b.Duration);
            lastSplashAt = Time.unscaledTime;
            bannerTimer = b.Duration + 0.3f;
        }

        public static string FormatTime(float t)
        {
            int m = (int)(t / 60f);
            int s = (int)(t % 60f);
            return m.ToString("00") + ":" + s.ToString("00");
        }
    }
}
