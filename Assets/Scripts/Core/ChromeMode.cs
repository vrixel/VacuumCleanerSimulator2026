using UnityEngine;
using UnityEngine.UI;
using VCS.UI;
using VCS.World;

namespace VCS.Core
{
    // explore/chrome-battery (2026-09-29, his "bascule sur l'aspiration d'un objet particulier genre une batterie
    // au plutonium, comme un objet bonus"): one glowing green plutonium battery lies somewhere in the house. Swallow
    // it and the whole game flips to the Hot Chrome look for Duration seconds, HUD included (UIStyle.PinkLook plus a
    // rebuilt HUD), with double points; then everything flips back and a new battery turns up RespawnDelay seconds
    // later. A pink flash covers both swaps. Launched with -pink, the look stays on for good and only the bonus runs.
    public class ChromeMode : MonoBehaviour
    {
        public const float Duration = 20f;
        public const float RespawnDelay = 45f;
        const float FirstDelay = 6f;

        GameManager gm;
        HotChromeLook look;
        bool launchPink;
        Debris battery;
        Light batteryLight;
        Transform lastRoot;
        float spawnAt = -1f;
        float endsAt = -1f;
        bool warned;
        Image flash;
        float flashA;

        public bool Active => endsAt > 0f;
        public float Remaining => Active ? Mathf.Max(0f, endsAt - Time.time) : 0f;
        public Debris Battery => battery;

        public static ChromeMode Create(GameManager gm)
        {
            var c = gm.gameObject.AddComponent<ChromeMode>();
            c.gm = gm;
            c.launchPink = UIStyle.PinkLook;
            c.look = HotChromeLook.Get(gm);
            c.look.Lite = Application.isMobilePlatform;
            c.BuildFlash();
            return c;
        }

        void BuildFlash()
        {
            var go = new GameObject("ChromeFlash");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var img = new GameObject("Flash").AddComponent<Image>();
            img.transform.SetParent(go.transform, false);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.color = new Color(1f, 0.45f, 0.75f, 0f);
            flash = img;
        }

        void Update()
        {
            if (flashA > 0f)
            {
                flashA = Mathf.Max(0f, flashA - Time.unscaledDeltaTime / 0.6f);
                flash.color = new Color(1f, 0.45f, 0.75f, flashA * flashA);
            }
            if (gm == null || gm.State != GameState.Playing || gm.Level == null || gm.Level.Root == null) return;

            if (gm.Level.Root != lastRoot)
            {
                lastRoot = gm.Level.Root;
                battery = null;
                spawnAt = Time.time + FirstDelay;
            }
            if (Active)
            {
                if (!warned && Remaining < 4f) { warned = true; gm.ShowToast("PLUTONIUM LOW", "Hot Chrome ends in 4 s"); }
                if (Remaining <= 0f) Revert(true);
            }
            else if (battery == null && spawnAt > 0f && Time.time >= spawnAt)
            {
                spawnAt = -1f;
                SpawnBattery();
            }
            if (batteryLight != null)
                batteryLight.intensity = 2.2f + 1.2f * Mathf.Sin(Time.time * 5f);
        }

        void SpawnBattery()
        {
            var level = gm.Level;
            for (int tries = 0; tries < 30; tries++)
            {
                Vector3 p = level.RandomFloorPoint(1.2f);
                if (!Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (!hit.collider.name.StartsWith("Floor")) continue;   // not on a sofa, a bed or a rug of mess
                if (Physics.CheckSphere(hit.point + Vector3.up * 0.45f, 0.35f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (gm.Player != null && Vector3.Distance(gm.Player.transform.position, hit.point) < 4f) continue;
                Spawn(hit.point + Vector3.up * 0.05f);
                return;
            }
            spawnAt = Time.time + 3f;   // the house was busy: try again shortly
        }

        void Spawn(Vector3 pos)
        {
            battery = PropFactory.Spawn(DebrisKind.PlutoniumBattery, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), gm.Level.Root, 7);
            var lg = new GameObject("PlutoniumGlow");
            lg.transform.SetParent(battery.transform, false);
            lg.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            batteryLight = lg.AddComponent<Light>();
            batteryLight.type = LightType.Point;
            batteryLight.color = new Color(0.35f, 1f, 0.35f);
            batteryLight.range = 3.2f;
            batteryLight.shadows = LightShadows.None;
            RadarView.Marker(battery.transform, new Color(0.35f, 1f, 0.35f), 1.1f);
            Debug.Log("[VCS] Plutonium battery at " + pos.ToString("F1"));
        }

        /// <summary>The vacuum swallowed the battery: Hot Chrome for Duration seconds, double points.</summary>
        public void OnBatteryEaten(Vector3 pos)
        {
            battery = null;
            batteryLight = null;
            bool again = Active;
            endsAt = Time.time + Duration;
            warned = false;
            gm.ScoreBoost = 2f;
            if (!again)
            {
                Flash();
                if (!launchPink)
                {
                    UIStyle.PinkLook = true;
                    gm.RebuildHud();
                }
                look.SetOn(true);
            }
            gm.Audio.PlayFanfare();
            gm.Audio.PlayWhoosh();
            if (gm.Fx != null) gm.Fx.Sparkle(pos, 40);
            if (gm.Cam != null) gm.Cam.Shake(0.5f);
            gm.SplashNow("PLUTONIUM POWER!", "Hot Chrome: double points for " + Mathf.RoundToInt(Duration) + " s", 2.6f);
            Debug.Log("[VCS] Chrome mode on (" + Duration + " s, lite " + look.Lite + ")");
        }

        void Revert(bool announce)
        {
            if (!Active) return;
            endsAt = -1f;
            gm.ScoreBoost = 1f;
            if (announce) Flash();
            if (!launchPink)
            {
                look.SetOn(false, !announce);
                UIStyle.PinkLook = false;
                gm.RebuildHud();
            }
            if (announce)
            {
                gm.Audio.PlayWhoosh();
                gm.ShowToast("BACK TO NORMAL", "find another plutonium battery");
                spawnAt = Time.time + RespawnDelay;
            }
            Debug.Log("[VCS] Chrome mode off");
        }

        void Flash()
        {
            flashA = 1f;
            flash.color = new Color(1f, 0.45f, 0.75f, 1f);
        }

        /// <summary>Run over (title, new run): back to the normal look at once, no banner, no pending battery.</summary>
        public void EndRun()
        {
            Revert(false);
            battery = null;
            batteryLight = null;
            lastRoot = null;
            spawnAt = -1f;
        }

        /// <summary>Smoke test: ends the mode now, as the timer would.</summary>
        public void DebugEnd() => Revert(true);

        /// <summary>Smoke test: a battery right here, now.</summary>
        public Debris DebugSpawn(Vector3 pos)
        {
            if (battery != null) Destroy(battery.gameObject);
            spawnAt = -1f;
            Spawn(pos);
            return battery;
        }
    }
}
