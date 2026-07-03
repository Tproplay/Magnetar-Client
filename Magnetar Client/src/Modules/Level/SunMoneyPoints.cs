using System;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Utils.Magnetar_Logger;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using System.Diagnostics;

namespace Magnetar_Client.Modules
{
    public class SunHack : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Sun Hack";
        public override string Description { get; set; } = "Gives you unlimited sun.";
        public override string SearchHints { get; set; } = "unlimited sun infinite sun snu sum ifnite finite sun" +
            "ulimited sun limitless sun unrestricted sun endless sun sunmultiplier solarmultiplier Money Hack Money Cheat " +
            "sunboost sunbonus sunpowerup sonmultiplier sunmultiplyer sunmultipliar sunmultipier sunmultyplier sunmulltiplier" +
            " sunmultaplier lightmultiplier raymultiplier sunfactor sunamplifier sunintensifier sunenhancer solmultiplier sunx2";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod data
        public static SunHack instance;

        public BoolSetting UnlimitedSun;
        private int originalSunAmount = -1_853_721_342;
        private IntSetting sunSetting;
        private BoolSetting preserveOriginalSetting;

        public BoolSetting SunMultipier;
        private FloatSetting sunMultiplierSetting;

        // State trackers for live toggling
        private bool _lastUnlimitedState = false;
        private bool _lastMultiplierState = false;
        private float _originalSunEfficiency = 1f;

        // extra
        public BoolSetting Active_AutoSunCD;
        public FloatSetting AutoSunCD;
        public BoolSetting AllowSunDropAnywhere;


        public SunHack()
        {
            instance = this;

            CreateCategory("Unlimited Sun");
            UnlimitedSun = new BoolSetting("Unlimited Sun", true);
            Settings.Add(UnlimitedSun);
            sunSetting = new IntSetting("Sun Amount", 0, 50000, 50000);
            Settings.Add(sunSetting);
            preserveOriginalSetting = new BoolSetting("Preserve Original", true);
            Settings.Add(preserveOriginalSetting);
            EndCategory();

            CreateCategory("Sun Multiplier");
            SunMultipier = new BoolSetting("Sun Multiplier", false);
            Settings.Add(SunMultipier);
            sunMultiplierSetting = new FloatSetting("Multiplier", 0, 10, 2);
            Settings.Add(sunMultiplierSetting);
            EndCategory();

            CreateCategory("Extra");
            Active_AutoSunCD = new BoolSetting("Custom Automatic sun drop cd", false);
            AutoSunCD = new FloatSetting("Automatic sun drop cd", 0, 20, 7.5f, 2,0);
            AllowSunDropAnywhere = new BoolSetting("Allow Sun drop anywhere", false)
            {
                OnValueChanged = disableAutoSun,
            };
            AddSettings(Active_AutoSunCD, AutoSunCD, AllowSunDropAnywhere);
            EndCategory();
        }

        // Mod Logic

        private static float last_fallsuncd;
        private static bool wasAutoSun = true;
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            // --- Unlimited Sun ---
            if (UnlimitedSun.Value != _lastUnlimitedState)
            {
                if (UnlimitedSun.Value) // Just turned ON
                {
                    originalSunAmount = board.theSun;
                }
                else // Just turned OFF
                {
                    if (originalSunAmount >= 0 && preserveOriginalSetting.Value)
                        board.theSun = originalSunAmount;

                    originalSunAmount = -1_853_721_342;
                }
                _lastUnlimitedState = UnlimitedSun.Value;
            }

            // Execution
            if (UnlimitedSun.Value)
            {
                board.theSun = sunSetting.Value;
            }

            // --- Sun Multiplier ---
            if (SunMultipier.Value != _lastMultiplierState)
            {
                if (SunMultipier.Value) // Just turned ON
                {
                    _originalSunEfficiency = board.sunEfficiency;
                }
                else // Just turned OFF
                {
                    board.sunEfficiency = _originalSunEfficiency;
                }
                _lastMultiplierState = SunMultipier.Value;
            }

            // Execution
            if (SunMultipier.Value)
            {
                board.sunEfficiency = sunMultiplierSetting.Value;
            }

            // Auto Sun Drop

            if (Active_AutoSunCD.Value)
            {
                if (board.theFallingSunCountDown > Math.Min(AutoSunCD.Value,last_fallsuncd))
                    board.theFallingSunCountDown = AutoSunCD.Value;

                // Update fall sun cd manually (if allow sun drop anywhere)
                if (AllowSunDropAnywhere.Value && last_fallsuncd == board.theFallingSunCountDown && Time.timeScale !=0)
                    board.theFallingSunCountDown -= Time.deltaTime;

                last_fallsuncd = board.theFallingSunCountDown;
            }

            if (AllowSunDropAnywhere.Value)
            {
                if (!board.fallSun)
                {
                    wasAutoSun = false;
                    board.fallSun = true;
                }

                if (board.theFallingSunCountDown <= 0 && GameAPP.theGameStatus == GameStatus.InGame)
                {
                    int col = UnityEngine.Random.RandomRangeInt(3, 10);
                    createManualFallSun(col);
                    board.theFallingSunCountDown = 7.5f;
                }
            }
        }

        public override void OnDisable()
        {
            if (!BoardInstanceIsNull)
            {
                // Clean up Unlimited Sun
                if (_lastUnlimitedState)
                {
                    if (originalSunAmount >= 0 && preserveOriginalSetting.Value)
                        board.theSun = originalSunAmount;

                    originalSunAmount = -1_853_721_342;
                    _lastUnlimitedState = false;
                }

                // Clean up Multiplier
                if (_lastMultiplierState)
                {
                    board.sunEfficiency = _originalSunEfficiency;
                    _lastMultiplierState = false;
                }

                // Auto Sun
            }

            disableAutoSun();
        }

        public void disableAutoSun(bool enable = false)
        {
            if (enable) return;

            if (!BoardInstanceIsNull) board.fallSun = wasAutoSun;
            wasAutoSun = true;
        }

        public void createManualFallSun(int col)
        {
            CreateItem.Instance.SetCoin(col, -1, 0, 1);
        }

        [HarmonyPatch(typeof(Board))]
        public static class BoardPatch
        {
            [HarmonyPatch(nameof(Board.Start))]
            [HarmonyPostfix]
            public static void StartPostfix()
            {
                wasAutoSun = true;
            }
        }
    }

    public class MoneyHack : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Money Hack";
        public override string Description { get; set; } = "Dave will become rich with this one.";
        public override string SearchHints { get; set; } = "unlimited money infinite money mny mney ifnite finite" +
            "ulimited limitless unrestricted endless moneymultiplier cashmultiplier moneyboost moneybonus moneyincrease" +
            " moneymultiplyer monymultiplier moenymultiplier moneymultipier moneymultaplier moneymulltiplier moneymultipyler" +
            " wealthmultiplier richesmultiplier coinmultiplier dollarmultiplier currencymultiplier moneyfactor moneyx2 cashboost";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod data
        public static MoneyHack instance;

        public BoolSetting UnlimitedMoney;
        private int originalMoneyAmount = -1_853_721_342;
        private IntSetting moneySetting;
        private BoolSetting preserveOriginalSetting;

        public BoolSetting MoneyMultiplier;
        public FloatSetting moneyMultiplierSetting;

        // State trackers for live toggling
        private bool _lastUnlimitedState = false;
        private bool _lastMultiplierState = false;
        private float _originalMoneyEfficiency = 1f;

        public override bool Active { get; set; } = false;

        public MoneyHack()
        {
            instance = this;

            CreateCategory("Unlimited Money");
            UnlimitedMoney = new BoolSetting("Unlimited Money", true);
            Settings.Add(UnlimitedMoney);
            moneySetting = new IntSetting("Money Amount", 0, 99999, 99999);
            Settings.Add(moneySetting);
            preserveOriginalSetting = new BoolSetting("Preserve Original", true);
            Settings.Add(preserveOriginalSetting);
            EndCategory();

            CreateCategory("Money Multiplier");
            MoneyMultiplier = new BoolSetting("Money Multiplier", false);
            Settings.Add(MoneyMultiplier);
            moneyMultiplierSetting = new FloatSetting("Multiplier", 0, 10, 2);
            Settings.Add(moneyMultiplierSetting);
            EndCategory();
        }

        // Mod Logic
        public override void OnDisable()
        {
            if (BoardInstanceIsNull) return;

            // Clean up Unlimited Money
            if (_lastUnlimitedState)
            {
                if (originalMoneyAmount >= 0 && preserveOriginalSetting.Value)
                    board.theMoney = originalMoneyAmount;

                originalMoneyAmount = -1_853_721_342;
                _lastUnlimitedState = false;
            }

            // Clean up Multiplier
            if (_lastMultiplierState)
            {
                board.moneyEfficiency = _originalMoneyEfficiency;
                _lastMultiplierState = false;
            }
        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            // --- Unlimited Money ---
            if (UnlimitedMoney.Value != _lastUnlimitedState)
            {
                if (UnlimitedMoney.Value) // Just turned ON
                {
                    originalMoneyAmount = board.theMoney;
                }
                else // Just turned OFF
                {
                    if (originalMoneyAmount >= 0 && preserveOriginalSetting.Value)
                        board.theMoney = originalMoneyAmount;

                    originalMoneyAmount = -1_853_721_342;
                }
                _lastUnlimitedState = UnlimitedMoney.Value;
            }

            // Execution
            if (UnlimitedMoney.Value)
            {
                board.theMoney = moneySetting.Value;
            }

            // --- Money Multiplier ---
            if (MoneyMultiplier.Value != _lastMultiplierState)
            {
                if (MoneyMultiplier.Value) // Just turned ON
                {
                    _originalMoneyEfficiency = board.moneyEfficiency;
                }
                else // Just turned OFF
                {
                    board.moneyEfficiency = _originalMoneyEfficiency;
                }
                _lastMultiplierState = MoneyMultiplier.Value;
            }

            // Execution
            if (MoneyMultiplier.Value)
            {
                board.moneyEfficiency = moneyMultiplierSetting.Value;
            }
        }
    }

    public class PointsHack : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Points Hack";
        public override string Description { get; set; } = "Now you can purchase any modifier.";
        public override string SearchHints { get; set; } = "unlimited points infinite points pts currency gold ifnite finite" +
            "ulimited limitless unrestricted endless pointsmultiplier pointmultiplier ptsmultiplier scoremultiplier scoreboost" +
            " pointsboost pointmultiplyer pointsmultiplyer pointmultipier pointsmultipier pointsmultyplier pointmultyplier" +
            " scoreincrease pointsbonus pointbonus pointx2 ptsbonus pointsfactor scorefactor";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod data
        public static PointsHack instance;

        public BoolSetting UnlimitedPoints;
        private float originalPointsAmount = -1_853_721_342;
        private FloatSetting pointsSetting;
        private BoolSetting preserveOriginalSetting;

        public BoolSetting PointsMultiplier;
        private FloatSetting pointsMultiplierSetting;
        private float _pointsAmount = -9476224.35443f;

        // State trackers for live toggling
        private bool _lastUnlimitedState = false;
        private bool _lastMultiplierState = false;

        public override bool Active { get; set; } = false;

        public PointsHack()
        {
            instance = this;

            CreateCategory("Unlimited Points");
            UnlimitedPoints = new BoolSetting("Unlimited Points", true);
            Settings.Add(UnlimitedPoints);
            pointsSetting = new FloatSetting("Points Amount", 0, 99999, 99999);
            Settings.Add(pointsSetting);
            preserveOriginalSetting = new BoolSetting("Preserve Original", true);
            Settings.Add(preserveOriginalSetting);
            EndCategory();

            CreateCategory("Points Multiplier");
            PointsMultiplier = new BoolSetting("Points Multiplier", false);
            Settings.Add(PointsMultiplier);
            pointsMultiplierSetting = new FloatSetting("Multiplier", 0, 10, 2);
            Settings.Add(pointsMultiplierSetting);
            EndCategory();
        }

        // Mod Logic
        public override void OnDisable()
        {
            if (BoardInstanceIsNull) return;

            // Clean up Unlimited Points
            if (_lastUnlimitedState)
            {
                if (originalPointsAmount >= 0 && preserveOriginalSetting.Value)
                    board.thePoints = originalPointsAmount;

                originalPointsAmount = -1_853_721_342;
                _lastUnlimitedState = false;
            }

            // Clean up Multiplier Tracker
            _pointsAmount = -9476224.35443f;
            _lastMultiplierState = false;
        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            // --- Unlimited Points ---
            if (UnlimitedPoints.Value != _lastUnlimitedState)
            {
                if (UnlimitedPoints.Value) // Just turned ON
                {
                    originalPointsAmount = board.thePoints;
                }
                else // Just turned OFF
                {
                    if (originalPointsAmount >= 0 && preserveOriginalSetting.Value)
                        board.thePoints = originalPointsAmount;

                    originalPointsAmount = -1_853_721_342;
                }
                _lastUnlimitedState = UnlimitedPoints.Value;
            }

            // Execution
            if (UnlimitedPoints.Value)
            {
                board.thePoints = pointsSetting.Value;
            }

            // --- Points Multiplier ---
            if (PointsMultiplier.Value != _lastMultiplierState)
            {
                if (PointsMultiplier.Value) // Just turned ON
                {
                    _pointsAmount = board.thePoints;
                }
                else // Just turned OFF
                {
                    _pointsAmount = -9476224.35443f;
                }
                _lastMultiplierState = PointsMultiplier.Value;
            }

            // Execution (Since Points don't have an efficiency field)
            if (PointsMultiplier.Value)
            {
                float points = board.thePoints;

                if (_pointsAmount == -9476224.35443f || points == pointsSetting.Value)
                    _pointsAmount = points;

                if (points != _pointsAmount)
                {
                    if ((points - _pointsAmount) > 0)
                        board.thePoints += (points - _pointsAmount) * (pointsMultiplierSetting.Value - 1);

                    _pointsAmount = board.thePoints;
                }
            }
        }
    }
}