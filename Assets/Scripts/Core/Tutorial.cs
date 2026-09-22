using UnityEngine;
using VCS.UI;

namespace VCS.Core
{
    /// <summary>
    /// The first-run walkthrough (Testers Community report of 2026-09-15, "no dynamic walkthrough or tutorial for
    /// new users"). Six steps, each one a real action the player performs in the live level, shown one at a time on
    /// the hint line above the cockpit so nothing is introduced before the previous thing has been used
    /// (progressive disclosure). Runs once, then writes the PlayerPref `tutorial_done`; the pause menu can skip it
    /// or replay it. The bin prompt, the bag-full toast and the boost reminder stay context hints outside of it.
    /// </summary>
    public class Tutorial
    {
        public const string DoneKey = "tutorial_done";
        public static bool Done => PlayerPrefs.GetInt(DoneKey, 0) != 0;

        public bool Active { get; private set; }
        public int Step => step;
        public int StepCount => Steps.Length;

        struct StepDef { public string Id, Key, Touch; }

        static readonly StepDef[] Steps =
        {
            new StepDef { Id = "drive", Key = "Drive with WASD or the left stick", Touch = "Push the left stick to drive" },
            new StepDef { Id = "look", Key = "Move the mouse or the right stick to look around", Touch = "Drag the free part of the screen to look around" },
            new StepDef { Id = "absorb", Key = "Drive over the crumbs and dust: small things go straight into the nozzle", Touch = "Drive over the crumbs and dust: small things go straight into the nozzle" },
            new StepDef { Id = "hop", Key = "Press SPACE / (A) to hop onto things", Touch = "Tap HOP to jump onto things" },
            new StepDef { Id = "turbo", Key = "Hold SHIFT / (RB) to boost", Touch = "Hold the big blue TURBO button to boost" },
            new StepDef { Id = "blow", Key = "Hold E / (B) to blow things away", Touch = "Hold BLOW to blast things away" },
        };

        readonly HudController hud;
        int step;
        float travelled, looked, held, gap;
        Vector3 lastPos;
        bool hasPos, pending;

        public Tutorial(HudController hud) { this.hud = hud; }

        /// <summary>Starts the walkthrough for a new run; with force = false it only runs until it has been completed once.</summary>
        public void Begin(bool force)
        {
            if (!force && Done) { Active = false; return; }
            Active = true;
            step = 0;
            travelled = looked = held = 0f;
            hasPos = false;
            pending = false;
            gap = 0f;
            Show();
        }

        public void Skip()
        {
            if (!Active) return;
            Finish(false);
        }

        /// <summary>Events from the game: "absorb" (first thing eaten), "hop" (hop pressed).</summary>
        public void Report(string ev)
        {
            if (!Active || pending || step >= Steps.Length) return;
            if (Steps[step].Id == ev) Complete();
        }

        public void Tick(GameManager gm, float dt)
        {
            if (!Active) return;
            if (pending)
            {
                // a breath between two steps, so the "nice" toast is read before the next instruction appears
                gap -= dt;
                if (gap > 0f) return;
                pending = false;
                if (step >= Steps.Length) { Finish(true); return; }
                Show();
                return;
            }
            var p = gm.Player;
            if (p == null) return;
            switch (Steps[step].Id)
            {
                case "drive":
                    var pos = p.transform.position; pos.y = 0f;
                    if (hasPos) travelled += Vector3.Distance(pos, lastPos);
                    lastPos = pos; hasPos = true;
                    if (travelled >= 4f) Complete();
                    break;
                case "look":
                    looked += GameInput.LookMouse.magnitude + GameInput.LookStick.magnitude * dt * 60f;
                    if (looked >= 25f) Complete();
                    break;
                case "turbo":
                    held = p.Turbo ? held + dt : 0f;
                    if (held >= 0.4f) Complete();
                    break;
                case "blow":
                    held = p.Suction != null && p.Suction.Blowing ? held + dt : 0f;
                    if (held >= 0.3f) Complete();
                    break;
            }
        }

        void Show()
        {
            var s = Steps[step];
            string text = GameInput.TouchMode ? s.Touch : s.Key;
            if (!GameInput.TouchMode)
            {
                // the gamepad button names get their real Xbox colours, like every other hint in the game
                text = text.Replace("(A)", UIStyle.Pad("A")).Replace("(RB)", UIStyle.Pad("RB")).Replace("(B)", UIStyle.Pad("B"));
            }
            hud.ShowHint("TUTORIAL " + (step + 1) + "/" + Steps.Length + "    " + text, 1e6f);
        }

        void Complete()
        {
            held = 0f;
            step++;
            pending = true;
            gap = 1.4f;
            hud.HideHint();
            var gm = GameManager.I;
            if (gm != null) gm.ShowToast("NICE!", step < Steps.Length ? "step " + step + " of " + Steps.Length + " done" : "you know all the controls");
        }

        void Finish(bool completed)
        {
            Active = false;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            hud.HideHint();
            if (completed)
                hud.ShowHint("Tutorial done. Points raise your power level, bigger things fit in the nozzle. Fill the bag, empty it at the bin, find the cat.", 8f);
            Debug.Log("[VCS] Tutorial " + (completed ? "completed" : "skipped") + " at step " + step);
        }
    }
}
