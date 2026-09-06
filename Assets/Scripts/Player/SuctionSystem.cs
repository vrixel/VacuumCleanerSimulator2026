using System.Collections.Generic;
using UnityEngine;
using VCS.Core;
using VCS.World;

namespace VCS.Player
{
    public struct BagItem
    {
        public DebrisKind Kind;
        public int ColorSeed;
        public float Volume;
        public bool Mess;
    }

    /// <summary>
    /// The nozzle: pulls debris inside a cone, eats what is small enough for the current power level,
    /// stores it in the bag, and blows it all back out when asked.
    /// </summary>
    public class SuctionSystem : MonoBehaviour
    {
        static readonly float[] RadiusByPower = { 0f, 2.4f, 3.0f, 3.8f, 4.6f, 5.5f };
        // 2026-09-06: shortened by about a third, things vanished too far from the nozzle
        static readonly float[] AbsorbByPower = { 0f, 0.55f, 0.66f, 0.8f, 1.0f, 1.25f };
        static readonly float[] PullByPower = { 0f, 16f, 20f, 26f, 34f, 44f };
        const float HalfAngle = 65f;
        const float BlowForce = 40f;
        const float SpitInterval = 0.07f;

        public int PowerLevel { get; private set; } = 1;
        public float BagCapacity { get; private set; } = 100f;
        public float BagFill { get; private set; }
        public bool BagFull { get; private set; }
        public bool Blowing { get; private set; }
        public float Activity { get; private set; }
        public float Radius => RadiusByPower[PowerLevel] * (spec != null ? spec.SuctionRadiusMult : 1f);
        public float AbsorbRadius => AbsorbByPower[PowerLevel];
        public int SizeBonus => spec != null ? spec.SizeBonus : 0;
        public List<BagItem> Bag { get; } = new List<BagItem>();

        VacuumController vac;
        VacuumSpec spec;
        Transform nozzle;
        ParticleSystem swirl;
        readonly Collider[] buffer = new Collider[512];
        readonly HashSet<int> seen = new HashSet<int>();
        float spitTimer;
        bool wasBlowing;

        public void Init(VacuumController v)
        {
            vac = v;
            spec = v.Spec;
            nozzle = v.Nozzle;
            SetPower(PowerLevel);
            var gm = GameManager.I;
            if (gm != null && gm.Fx != null) swirl = gm.Fx.CreateSuctionSwirl(nozzle);
        }

        public void SetPower(int p)
        {
            PowerLevel = Mathf.Clamp(p, 1, GameManager.MaxPower);
            float baseCapacity = spec != null ? spec.BagCapacity : 100f;
            BagCapacity = baseCapacity * (1f + (PowerLevel - 1) * 0.25f);
            if (swirl != null)
            {
                var sh = swirl.shape;
                sh.radius = Radius * 0.45f;
                sh.position = new Vector3(0f, 0f, Radius * 0.55f);
            }
        }

        public void EmptyBag()
        {
            Bag.Clear();
            BagFill = 0f;
            BagFull = false;
        }

        void FixedUpdate()
        {
            var gm = GameManager.I;
            bool active = gm != null && gm.State == GameState.Playing;
            bool powered = vac.Powered;
            Blowing = active && powered && GameInput.Blow;
            if (Blowing && !wasBlowing) gm.Audio.PlayWhoosh();
            wasBlowing = Blowing;
            if (!active) { Activity = 0f; gm?.Audio.SetSuction(0f, false); return; }
            if (!powered)
            {
                Activity = 0f;
                if (swirl != null) { var em0 = swirl.emission; em0.rateOverTimeMultiplier = 0f; }
                gm.Audio.SetHum(0f, false);
                gm.Audio.SetSuction(0f, false);
                return;
            }

            if (Blowing) Blow(gm); else Suck(gm);

            if (swirl != null)
            {
                var em = swirl.emission;
                em.rateOverTimeMultiplier = (Blowing || BagFull) ? 0f : 25f + 40f * Activity;
            }
            float intensity = Mathf.Clamp01(vac.Speed / 12f) * 0.6f + Activity * 0.4f + (vac.Turbo ? 0.2f : 0f);
            float humVolume = spec != null ? spec.HumVolume : 1f;
            gm.Audio.SetHum((0.35f + intensity * 0.65f) * humVolume, Blowing);
            gm.Audio.SetSuction(Activity, !Blowing && !BagFull && vac.Grounded);
        }

        void Suck(GameManager gm)
        {
            Vector3 np = nozzle.position;
            Vector3 nf = nozzle.forward;
            float radius = Radius;
            float absorb = AbsorbRadius;
            float pull = PullByPower[PowerLevel] * (spec != null ? spec.PullMult : 1f);
            int maxClass = PowerLevel + SizeBonus;

            // Cocoa powder on the floor: the nozzle clears a disc wherever it passes.
            if (!BagFull && vac.Grounded && gm.Level != null && gm.Level.Powder != null)
            {
                float sqm = gm.Level.Powder.Vacuum(np + nf * 0.05f, 0.28f + 0.03f * PowerLevel);
                if (sqm > 0f)
                {
                    BagFill += sqm * 1.5f;
                    gm.OnPowderCleaned(sqm, np);
                    if (BagFill >= BagCapacity && !BagFull) { BagFull = true; gm.OnBagFull(); }
                }
            }
            int n = Physics.OverlapSphereNonAlloc(np, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            seen.Clear();
            int pulled = 0;
            for (int i = 0; i < n; i++)
            {
                var rb = buffer[i].attachedRigidbody;
                if (rb == null || rb == vac.Rb) continue;
                if (!seen.Add(rb.GetInstanceID())) continue;
                var d = rb.GetComponent<Debris>();
                if (d == null) continue;

                Vector3 to = np - rb.worldCenterOfMass;
                float dist = to.magnitude;
                if (dist < 0.001f) dist = 0.001f;
                bool inCone = dist < 1.0f || Vector3.Angle(nf, -to) <= HalfAngle;
                if (!inCone) continue;

                bool edible = d.SizeClass <= maxClass && !BagFull;
                if (edible && dist < absorb * (1f + d.SizeClass * 0.12f))
                {
                    Absorb(gm, d);
                    continue;
                }

                float k = 1f - dist / radius;
                float strength = pull * (0.35f + k) * (edible ? 1f : 0.12f);
                Vector3 f = to / dist * strength + Vector3.up * (edible ? 3f : 0f);
                rb.AddForce(f, ForceMode.Acceleration);
                if (edible) rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Acceleration);
                pulled++;
            }
            Activity = Mathf.Clamp01(pulled / 8f);
        }

        void Absorb(GameManager gm, Debris d)
        {
            Bag.Add(new BagItem { Kind = d.Kind, ColorSeed = d.ColorSeed, Volume = d.Volume, Mess = d.CountsAsMess });
            BagFill += d.Volume;
            gm.OnDebrisAbsorbed(d);
            vac.Visuals.Punch(0.12f + d.SizeClass * 0.08f);
            Destroy(d.gameObject);
            if (BagFill >= BagCapacity && !BagFull)
            {
                BagFull = true;
                gm.OnBagFull();
            }
        }

        void Blow(GameManager gm)
        {
            Vector3 np = nozzle.position;
            Vector3 nf = nozzle.forward;
            float reach = Radius * 1.3f;
            int n = Physics.OverlapSphereNonAlloc(np, reach, buffer, ~0, QueryTriggerInteraction.Ignore);
            seen.Clear();
            int pushed = 0;
            for (int i = 0; i < n; i++)
            {
                var rb = buffer[i].attachedRigidbody;
                if (rb == null || rb == vac.Rb) continue;
                if (!seen.Add(rb.GetInstanceID())) continue;
                var d = rb.GetComponent<Debris>();
                Vector3 away = rb.worldCenterOfMass - np;
                float dist = away.magnitude;
                if (dist < 0.05f) continue;
                if (Vector3.Angle(nf, away) > 40f) continue;
                float k = 1f - dist / reach;
                float scale = d == null ? 0.3f : (d.SizeClass <= PowerLevel + SizeBonus ? 1f : 0.35f);
                rb.AddForce((away / dist + Vector3.up * 0.3f) * BlowForce * k * scale, ForceMode.Acceleration);
                pushed++;
            }
            Activity = Mathf.Clamp01(pushed / 6f);

            spitTimer -= Time.fixedDeltaTime;
            if (spitTimer <= 0f && Bag.Count > 0)
            {
                spitTimer = SpitInterval;
                var item = Bag[Bag.Count - 1];
                Bag.RemoveAt(Bag.Count - 1);
                BagFill = Mathf.Max(0f, BagFill - item.Volume);
                if (BagFill < BagCapacity) BagFull = false;

                var spec = PropFactory.Spec(item.Kind);
                Vector3 spawnPos = np + nf * (0.7f + spec.SizeClass * 0.35f) + Vector3.up * 0.25f;
                var d = PropFactory.Spawn(item.Kind, spawnPos, Random.rotation, gm.Level.Root, item.ColorSeed);
                d.Rb.linearVelocity = nf * 15f + Vector3.up * 4f + Random.insideUnitSphere * 1.5f;
                d.Rb.angularVelocity = Random.insideUnitSphere * 8f;
                d.gameObject.AddComponent<LaunchTracker>();
                if (item.Mess) gm.Level.OnMessReleased();
                gm.Fx.Puff(np + nf * 0.5f, d.PuffColor, 4);
                vac.Visuals.Punch(0.1f);
            }
        }
    }
}
