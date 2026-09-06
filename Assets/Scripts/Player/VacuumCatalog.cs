using System;
using System.Collections.Generic;
using UnityEngine;

namespace VCS.Player
{
    public enum GaugeStyle { Analog, LedRing, Oled, Industrial }

    public enum ContainerKind { Bag, Cyclone, Tray, Drum }

    /// <summary>One selectable vacuum: handling stats, where the eyes and the nozzle go, how to build the body,
    /// and how its cockpit instruments look.</summary>
    public class VacuumSpec
    {
        public string Id;
        public string Name;
        public string Tagline;

        public float Speed = 7f;
        public float Accel = 42f;
        public float Turn = 720f;
        public float Hop = 6.5f;
        public float BagCapacity = 100f;
        public float SuctionRadiusMult = 1f;
        public float PullMult = 1f;
        public float Mass = 10f;
        /// <summary>Extra size classes this vacuum can eat above its power level.</summary>
        public int SizeBonus;
        /// <summary>Seconds a combo stays alive between two bites.</summary>
        public float ComboTime = 1.6f;
        /// <summary>Loudness of the motor hum, 1 = normal.</summary>
        public float HumVolume = 1f;

        public Vector3 EyeCenter = new Vector3(0f, 0.98f, 0.26f);
        public float EyeSpacing = 0.4f;
        public float EyeSize = 0.24f;
        public Vector3 NozzleLocal = new Vector3(0f, 0.25f, 0.8f);
        /// <summary>Visual height in metres, used to frame the garage preview.</summary>
        public float Height = 1.1f;

        public Action<Transform, VacuumSpec> Build;

        // ---- cockpit
        public GaugeStyle Gauge = GaugeStyle.Analog;
        public ContainerKind Container = ContainerKind.Bag;
        public string ContainerLabel = "DUST BAG";
        public string SuctionLabel = "SUCTION";
        public string SuctionUnit = "kPa";
        public float SuctionMax = 20f;
        public float MotorRpmMax = 18000f;
        public Color Accent = new Color(1f, 0.6f, 0.25f);
        public bool Cordless;
        /// <summary>Museum piece (imported real-product mesh): only listed when the museum is unlocked (M on the title screen).</summary>
        public bool Hidden;
        /// <summary>Attribution line for imported meshes (CC-BY), shown under the name.</summary>
        public string Credit;
        public string ModelCode = "DP-01";

        public float SpeedBar => Mathf.InverseLerp(5f, 10f, Speed);
        public float SuctionBar => Mathf.InverseLerp(0.85f, 1.45f, (PullMult + SuctionRadiusMult) * 0.5f + SizeBonus * 0.15f);
        public float BagBar => Mathf.InverseLerp(40f, 230f, BagCapacity);
        public float HopBar => Mathf.InverseLerp(3.5f, 8f, Hop);
    }

    /// <summary>The garage. Brand-inspired lookalikes with parody names; no logos, no real product names.</summary>
    public static class VacuumCatalog
    {
        public static readonly List<VacuumSpec> All = new List<VacuumSpec>
        {
            new VacuumSpec
            {
                Id = "dusty", Name = "Dusty", Tagline = "The original. Cardboard and dreams.",
                Height = 1.1f, Build = VacuumModels.Dusty,
                Gauge = GaugeStyle.Analog, Container = ContainerKind.Bag, ContainerLabel = "DUST BAG",
                SuctionUnit = "kPa", SuctionMax = 20f, MotorRpmMax = 18000f, Accent = new Color(1f, 0.6f, 0.25f), ModelCode = "DP-01",
            },
            new VacuumSpec
            {
                Id = "roomboo", Name = "Roomboo S9", Tagline = "Round, relentless, slightly lost.",
                Speed = 8f, Accel = 50f, Turn = 900f, Hop = 5f, BagCapacity = 70f, SuctionRadiusMult = 0.9f, PullMult = 0.9f, Mass = 8f,
                EyeCenter = new Vector3(0f, 0.17f, 0.28f), EyeSpacing = 0.16f, EyeSize = 0.14f,
                NozzleLocal = new Vector3(0f, 0.1f, 0.5f), Height = 0.36f, Build = VacuumModels.Roomboo,
                Gauge = GaugeStyle.LedRing, Container = ContainerKind.Tray, ContainerLabel = "DEBRIS TRAY",
                SuctionUnit = "Pa", SuctionMax = 2200f, MotorRpmMax = 12000f, Accent = new Color(0.25f, 0.85f, 0.65f), Cordless = true, ModelCode = "RB-S9",
            },
            new VacuumSpec
            {
                Id = "cyclonic", Name = "Cyclonic V-Storm", Tagline = "Never loses suction. Loses everything else.",
                Speed = 7f, Accel = 42f, Turn = 720f, Hop = 6.5f, BagCapacity = 90f, SuctionRadiusMult = 1.2f, PullMult = 1.25f, Mass = 10f,
                EyeCenter = new Vector3(0f, 0.6f, 0.09f), EyeSpacing = 0.13f, EyeSize = 0.12f,
                NozzleLocal = new Vector3(0f, 0.06f, 0.5f), Height = 1.25f, Build = VacuumModels.Cyclonic,
                Gauge = GaugeStyle.LedRing, Container = ContainerKind.Cyclone, ContainerLabel = "CYCLONE BIN",
                SuctionLabel = "AIR WATTS", SuctionUnit = "AW", SuctionMax = 260f, MotorRpmMax = 104000f, Accent = new Color(0.75f, 0.55f, 1f), ModelCode = "VS-2",
            },
            new VacuumSpec
            {
                Id = "harold", Name = "Harold", Tagline = "Always smiling. Always hungry.",
                Speed = 6.5f, Accel = 40f, Turn = 600f, Hop = 5.5f, BagCapacity = 140f, SuctionRadiusMult = 1.15f, PullMult = 1.2f, Mass = 12f,
                EyeCenter = new Vector3(0f, 0.31f, 0.215f), EyeSpacing = 0.16f, EyeSize = 0.13f,
                NozzleLocal = new Vector3(0.03f, 0.05f, 0.85f), Height = 0.7f, Build = VacuumModels.Harold,
                Gauge = GaugeStyle.Analog, Container = ContainerKind.Bag, ContainerLabel = "PAPER BAG",
                SuctionUnit = "PSI", SuctionMax = 3.5f, MotorRpmMax = 20000f, Accent = new Color(1f, 0.75f, 0.35f), ModelCode = "HRLD-2",
            },
            new VacuumSpec
            {
                Id = "stick", Name = "Stickmaster Cordless", Tagline = "Forty minutes of pure chaos.",
                Speed = 9.5f, Accel = 55f, Turn = 900f, Hop = 7.5f, BagCapacity = 45f, SuctionRadiusMult = 0.85f, PullMult = 0.85f, Mass = 6f,
                EyeCenter = new Vector3(0f, 0.98f, -0.02f), EyeSpacing = 0.09f, EyeSize = 0.09f,
                NozzleLocal = new Vector3(0f, 0.05f, 0.55f), Height = 1.1f, Build = VacuumModels.Stickmaster,
                Gauge = GaugeStyle.Oled, Container = ContainerKind.Cyclone, ContainerLabel = "MINI BIN",
                SuctionLabel = "AIR WATTS", SuctionUnit = "AW", SuctionMax = 150f, MotorRpmMax = 110000f, Accent = new Color(0.4f, 0.7f, 1f), Cordless = true, ModelCode = "SM-40",
            },
            new VacuumSpec
            {
                Id = "grandma", Name = "Grandma's Upright 1978", Tagline = "Older than your parents. Still angry.",
                Speed = 6f, Accel = 35f, Turn = 540f, Hop = 4.5f, BagCapacity = 180f, SuctionRadiusMult = 1.1f, PullMult = 1.35f, Mass = 14f,
                EyeCenter = new Vector3(0f, 0.30f, 0.16f), EyeSpacing = 0.14f, EyeSize = 0.12f,
                NozzleLocal = new Vector3(0f, 0.06f, 0.45f), Height = 1.2f, Build = VacuumModels.Grandma,
                Gauge = GaugeStyle.Analog, Container = ContainerKind.Bag, ContainerLabel = "CLOTH BAG",
                SuctionUnit = "inH2O", SuctionMax = 90f, MotorRpmMax = 9000f, Accent = new Color(0.85f, 0.9f, 0.5f), ModelCode = "GU-1978",
            },
            new VacuumSpec
            {
                Id = "rowinta", Name = "Rowinta Silence Farce", Tagline = "Tres chic, very quiet, still hungry.",
                Speed = 7.5f, Accel = 44f, Turn = 660f, Hop = 5.5f, BagCapacity = 130f, SuctionRadiusMult = 1.1f, PullMult = 1.15f, Mass = 11f,
                ComboTime = 2.4f, HumVolume = 0.45f,
                EyeCenter = new Vector3(0f, 0.24f, 0.27f), EyeSpacing = 0.16f, EyeSize = 0.12f,
                NozzleLocal = new Vector3(0.02f, 0.05f, 1.05f), Height = 0.6f, Build = VacuumModels.Rowinta,
                Gauge = GaugeStyle.Analog, Container = ContainerKind.Bag, ContainerLabel = "MICROFIBRE BAG",
                SuctionLabel = "NOISE", SuctionUnit = "dB(A)", SuctionMax = 75f, MotorRpmMax = 32000f, Accent = new Color(0.85f, 0.9f, 1f), ModelCode = "RS-4",
            },
            new VacuumSpec
            {
                Id = "shopdrum", Name = "Shop Drum 3000", Tagline = "Wet, dry, or furniture.",
                Speed = 5.5f, Accel = 34f, Turn = 480f, Hop = 4f, BagCapacity = 220f, SuctionRadiusMult = 1.3f, PullMult = 1.5f, Mass = 18f, SizeBonus = 1,
                EyeCenter = new Vector3(0f, 0.40f, 0.27f), EyeSpacing = 0.2f, EyeSize = 0.15f,
                NozzleLocal = new Vector3(0f, 0.06f, 0.85f), Height = 0.8f, Build = VacuumModels.ShopDrum,
                Gauge = GaugeStyle.Industrial, Container = ContainerKind.Drum, ContainerLabel = "TANK",
                SuctionUnit = "kPa", SuctionMax = 30f, MotorRpmMax = 24000f, Accent = new Color(1f, 0.35f, 0.25f), ModelCode = "SD-3000",
            },
        };

        static VacuumCatalog()
        {
            ImportedVacuums.AddTo(All);
        }

        /// <summary>The museum: real machines on loan, imported from Objaverse (see ImportedVacuums).</summary>
        public static bool MuseumUnlocked
        {
            get => PlayerPrefs.GetInt("museum", 0) == 1;
            set => PlayerPrefs.SetInt("museum", value ? 1 : 0);
        }

        /// <summary>What the garage shows: the regular eight, plus the museum pieces once unlocked.</summary>
        public static List<VacuumSpec> Visible
        {
            get
            {
                var list = new List<VacuumSpec>();
                bool museum = MuseumUnlocked;
                foreach (var s in All) if (!s.Hidden || museum) list.Add(s);
                return list;
            }
        }

        public static VacuumSpec Get(string id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return All[0];
        }

        public static int IndexOf(string id)
        {
            var v = Visible;
            for (int i = 0; i < v.Count; i++) if (v[i].Id == id) return i;
            return 0;
        }

        public static string SelectedId
        {
            get => PlayerPrefs.GetString("vacuum_id", "dusty");
            set => PlayerPrefs.SetString("vacuum_id", value);
        }

        public static VacuumSpec Selected => Get(SelectedId);
    }
}
