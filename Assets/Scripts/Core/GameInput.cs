using UnityEngine;

namespace VCS.Core
{
    /// <summary>
    /// Thin wrapper over the legacy Input Manager (see ProjectSettings/InputManager.asset).
    /// Keyboard + mouse + XInput gamepad. Xbox layout: A=0 B=1 X=2 Y=3 LB=4 RB=5 Back=6 Start=7.
    /// </summary>
    public static class GameInput
    {
        /// <summary>When set, replaces the stick/keys for driving (used by the smoke test).</summary>
        public static Vector2? MoveOverride;
        /// <summary>The smoke test holds the boost with this, never with keystrokes.</summary>
        public static bool TurboOverride;

        // ---- the touch layer (2026-09-07, Android): TouchControls writes these, every query below reads them too.
        // TouchMode is on for phones and for "-touch" on the PC (screenshots, testing).
        public static bool TouchMode;
        public static Vector2 TouchMove;
        public static bool TouchTurbo, TouchBlow;
        /// <summary>One-shot taps, consumed by the first read.</summary>
        public static bool TouchHop, TouchEmpty, TouchRewind, TouchPause, TouchConfirm;
        static Vector2 touchLook;
        static int touchLookFrame = -1;

        /// <summary>Called by the look pad while a finger drags: a mouse-like delta for this frame.</summary>
        public static void AddTouchLook(Vector2 delta)
        {
            if (touchLookFrame != Time.frameCount) { touchLook = Vector2.zero; touchLookFrame = Time.frameCount; }
            touchLook += delta;
        }

        static bool Take(ref bool flag) { bool v = flag; flag = false; return v; }

        public static Vector2 Move
        {
            get
            {
                if (MoveOverride.HasValue) return MoveOverride.Value;
                var v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (TouchMode && v.sqrMagnitude < 0.01f) v = TouchMove;
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        /// <summary>Right stick, -1..1 per axis (a rate, multiply by deltaTime).</summary>
        public static Vector2 LookStick => new Vector2(Input.GetAxis("CamX"), Input.GetAxis("CamY"));

        /// <summary>Mouse movement since last frame (already a delta); on a phone, the look pad's drag.</summary>
        public static Vector2 LookMouse
        {
            get
            {
                if (TouchMode) return touchLookFrame == Time.frameCount ? touchLook : Vector2.zero;
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
        }

        public static bool HopDown => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0) || Take(ref TouchHop);

        public static bool Turbo =>
            TurboOverride || TouchTurbo ||
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.JoystickButton5) ||
            Input.GetAxis("TriggerR") > 0.3f;

        public static bool Blow =>
            TouchBlow ||
            Input.GetKey(KeyCode.E) || (!TouchMode && Input.GetMouseButton(1)) ||
            Input.GetKey(KeyCode.JoystickButton1) || Input.GetAxis("TriggerL") > 0.3f;

        public static bool EmptyDown => Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton2) || Take(ref TouchEmpty);

        public static bool RewindDown => Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton3) || Take(ref TouchRewind);

        public static bool PauseDown => Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7) || Take(ref TouchPause);

        public static bool ConfirmDown =>
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0) || Take(ref TouchConfirm);

        static float lastNavAxis;
        static float lastNavAxisH;

        /// <summary>Edge-detected horizontal menu navigation: -1 left, +1 right, 0 nothing. Also LB / RB.</summary>
        public static int MenuNavHorizontal()
        {
            int dir = 0;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.JoystickButton4)) dir = -1;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.JoystickButton5)) dir = 1;
            float axis = Input.GetAxisRaw("Horizontal") + Input.GetAxisRaw("DPadX");
            if (dir == 0)
            {
                if (axis > 0.5f && lastNavAxisH <= 0.5f) dir = 1;
                else if (axis < -0.5f && lastNavAxisH >= -0.5f) dir = -1;
            }
            lastNavAxisH = axis;
            return dir;
        }

        /// <summary>Edge-detected vertical menu navigation: -1 up, +1 down, 0 nothing.</summary>
        public static int MenuNav()
        {
            int dir = 0;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) dir = -1;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) dir = 1;
            float axis = Input.GetAxisRaw("Vertical") + Input.GetAxisRaw("DPadY");
            if (dir == 0)
            {
                if (axis > 0.5f && lastNavAxis <= 0.5f) dir = -1;
                else if (axis < -0.5f && lastNavAxis >= -0.5f) dir = 1;
            }
            lastNavAxis = axis;
            return dir;
        }
    }
}
