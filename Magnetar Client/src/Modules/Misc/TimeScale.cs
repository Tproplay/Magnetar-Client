using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class TimeScale : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Time Scale";
        public override string Description { get; set; } = "Changes the Time scale.";
        public override string SearchHints { get; set; } = "timescale slowmo slowmotion slow-mo speedmultiplier " +
            "gamespeed timespeed slowmoion slowmoshun slowmow slomo slomotion slomow timesclae timescael timescal" +
            " timesclae speedfactor timescaleup timescaleslow timescalebot speedcontrol fastforward speedchange" +
            " bullettime speedup speeddown velocitycontrol timingmultiplier timefactor";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod data
        public static TimeScale instance;

        public FloatSetting TimeScaleSetting;

        public Dictionary<FloatSetting, BindSetting> Buttons = new Dictionary<FloatSetting, BindSetting>();
        public BoolSetting ResetOnDoubleActive;
        public BoolSetting DisableOnGamePaused;
        public BoolSetting ReEnableAfterPause;

        public float TargetSpeed { get; private set; } = 1f;
        private bool wasPausedLastFrame = false;

        public TimeScale()
        {
            instance = this;

            CreateCategory("General");

            TimeScaleSetting = new FloatSetting("Time Scale (Engine)", 0f, 10f, 1f, 3, 0)
            {
                OnValueChanged = x =>
                {
                    if (instance.Active && !IsGamePaused())
                    {
                        TargetSpeed = x;
                        Time.timeScale = x;
                    }
                }
            };
            AddSettings(TimeScaleSetting);

            EndCategory();
            CreateCategory("Buttons");

            for (int i = 1; i <= 5; i++)
            {
                Buttons[new FloatSetting($"Speed setting {i}", 0f, 10f, 1f, 3, 0)] = new BindSetting($"Control button {i}");
            }

            foreach (var button in Buttons)
            {
                AddSettings(button.Key, button.Value);
            }

            EndCategory();
            CreateCategory("Extra");

            ResetOnDoubleActive = new BoolSetting("Reset on double click", true);
            DisableOnGamePaused = new BoolSetting("Disable on game paused", true);
            ReEnableAfterPause = new BoolSetting("Re-enable after pause", true);

            AddSettings(ResetOnDoubleActive, DisableOnGamePaused, ReEnableAfterPause);
            EndCategory();
        }

        public override void OnDisable()
        {
            if (!IsGamePaused()) Time.timeScale = 1f;
        }

        public override void OnUpdateActive()
        {
            // Handle keybind shortcuts
            foreach (var button in Buttons)
            {
                if (GetKeyComboDown(button.Value.BindKeys))
                {
                    if (Mathf.Approximately(TargetSpeed, button.Key.Value))
                    {
                        if (ResetOnDoubleActive.Value) SetGameSpeed(1f);
                    }
                    else
                    {
                        SetGameSpeed(button.Key.Value);
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            bool currentlyPaused = IsGamePaused();

            if (wasPausedLastFrame && !currentlyPaused)
            {
                if (Active && ReEnableAfterPause.Value)
                {
                    Time.timeScale = TargetSpeed;
                }
            }

            wasPausedLastFrame = currentlyPaused;

            TimeScaleSetting.Value = Time.timeScale;
        }

        public void SetGameSpeed(float speed)
        {
            TargetSpeed = speed;

            if (DisableOnGamePaused.Value && IsGamePaused()) return;

            Time.timeScale = speed;
        }

        private bool IsGamePaused()
        {
            var status = GameAPP.theGameStatus;
            bool isPausedStatus = (status == GameStatus.OpenOptions || status == GameStatus.Pause);
            return Time.timeScale == 0f && isPausedStatus;
        }

        [HarmonyPatch(typeof(SlowTrigger), nameof(SlowTrigger.Clicking))]
        public static class SlowTriggerPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (instance == null || !instance.Active)
                    return true;

                if (Mathf.Approximately(instance.TargetSpeed, 0.2f))
                {
                    instance.SetGameSpeed(1.0f);
                }
                else
                {
                    instance.SetGameSpeed(0.2f);
                }

                return false;
            }
        }
    }
}
