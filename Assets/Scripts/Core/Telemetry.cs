using UnityEngine;
using VCS.Player;
using PowerCord = VCS.Player.PowerCord;

namespace VCS.Core
{
    /// <summary>
    /// The pretend engineering behind the cockpit: suction, motor speed, airflow, temperature, filter, battery,
    /// odometer. None of it changes the physics; all of it changes the mood.
    /// </summary>
    public class Telemetry
    {
        public float Suction01;
        public float SuctionValue;
        public float Rpm;
        public float AirflowLps;
        public float TempC = 24f;
        public float Filter01 = 1f;
        public float Battery01 = 1f;
        public float OdometerM;
        public float RuntimeS;
        public int ItemsIngested;
        public bool Overheat;
        public bool Tilt;
        public bool LowBattery;
        public bool FilterWarning;
        public bool Turbo;
        public bool Reverse;
        public bool Powered = true;
        public bool CordTaut;
        public bool CordRewinding;
        public float CordLength;
        public float CordMax = PowerCord.MaxLength;

        VacuumSpec spec;
        Vector3 lastPos;
        bool hasPos;

        public void Reset(VacuumSpec s)
        {
            spec = s;
            Suction01 = 0f; SuctionValue = 0f; Rpm = 0f; AirflowLps = 0f;
            TempC = 24f; Filter01 = 1f; Battery01 = 1f;
            OdometerM = 0f; RuntimeS = 0f; ItemsIngested = 0;
            Overheat = false; Tilt = false; LowBattery = false; FilterWarning = false; Turbo = false; Reverse = false;
            Powered = true; CordTaut = false; CordRewinding = false; CordLength = 0f;
            hasPos = false;
        }

        public void OnItemIngested(int sizeClass)
        {
            ItemsIngested++;
            Filter01 = Mathf.Max(0.15f, Filter01 - 0.0015f * sizeClass);
        }

        public void OnEmptied()
        {
            Filter01 = 1f;
        }

        public void Tick(GameManager gm, float dt)
        {
            var p = gm.Player;
            var s = gm.Suction;
            if (p == null || s == null || spec == null) return;
            RuntimeS += dt;

            Turbo = p.Turbo;
            Reverse = s.Blowing;
            Tilt = !p.Grounded;
            Powered = p.Powered;
            if (p.Cord != null)
            {
                CordLength = p.Cord.Length;
                CordTaut = p.Cord.Taut;
                CordRewinding = p.Cord.Rewinding;
            }

            float target = s.Blowing ? 0.12f
                : (0.32f + 0.14f * (gm.PowerLevel - 1)) * (1f + 0.55f * s.Activity) * (p.Turbo ? 1.25f : 1f);
            if (s.BagFull) target *= 0.35f;
            if (!Powered) target = 0f;
            target = Mathf.Clamp01(target);
            float rate = target > Suction01 ? 3.5f : 1.6f;
            Suction01 = Mathf.Lerp(Suction01, target, 1f - Mathf.Exp(-dt * rate));
            Suction01 += (Mathf.PerlinNoise(RuntimeS * 3.1f, 0.3f) - 0.5f) * 0.02f;
            Suction01 = Mathf.Clamp01(Suction01);
            SuctionValue = Suction01 * spec.SuctionMax;

            float rpmTarget = Powered ? (0.28f + 0.72f * Suction01) * spec.MotorRpmMax * (s.Blowing ? 1.12f : 1f) : 0f;
            Rpm = Mathf.Lerp(Rpm, rpmTarget, 1f - Mathf.Exp(-dt * 2.5f));
            AirflowLps = Suction01 * 46f * spec.SuctionRadiusMult;

            float tempTarget = 24f + 58f * Suction01 + (p.Turbo ? 28f : 0f) + (s.Blowing ? 14f : 0f);
            float tRate = tempTarget > TempC ? 0.35f : 0.18f;
            TempC = Mathf.Lerp(TempC, tempTarget, 1f - Mathf.Exp(-dt * tRate));
            Overheat = TempC > 92f;

            Filter01 = Mathf.Max(0.15f, Filter01 - dt * 0.0012f * Suction01);
            FilterWarning = Filter01 < 0.4f;

            if (spec.Cordless)
            {
                float drain = dt * 0.0035f * (0.4f + Suction01 + (p.Turbo ? 0.6f : 0f));
                if (p.Speed < 0.3f && s.Activity < 0.05f) drain = -dt * 0.004f;
                Battery01 = Mathf.Clamp(Battery01 - drain, 0.05f, 1f);
                LowBattery = Battery01 < 0.2f;
            }
            else
            {
                Battery01 = 1f;
                LowBattery = false;
            }

            Vector3 pos = p.transform.position;
            if (hasPos)
            {
                Vector3 d = pos - lastPos;
                d.y = 0f;
                OdometerM += d.magnitude;
            }
            lastPos = pos;
            hasPos = true;
        }
    }
}
