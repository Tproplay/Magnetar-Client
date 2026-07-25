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

        public static TimeScale Instance;

        public FloatSetting TimeScaleSetting;

        public Dictionary<FloatSetting, BindSetting> Buttons = new Dictionary<FloatSetting, BindSetting>();
        public BoolSetting ResetOnDoubleActive;


        public TimeScale()
        {
            Instance = this;

            CreateCategory("General");

            TimeScaleSetting = new FloatSetting("Time Scale", 0f, 10, 2, 3, 0)
            {
                OnValueChanged = x =>
                {
                    if (Instance.Active) UnityEngine.Time.timeScale = x;
                }
            };
            AddSettings(TimeScaleSetting);

            EndCategory();
            CreateCategory("Buttons");

            for (int i = 1; i <= 5; i++)
            {
                Buttons[new FloatSetting($"Speed setting {i}", 0f, 10, 1, 3, 0)] = new BindSetting($"Control button {i}");
            }

            foreach (var button in Buttons)
            {
                AddSettings(button.Key, button.Value);
            }
            
            EndCategory();
            CreateCategory("Extra");

            ResetOnDoubleActive = new BoolSetting("Reset on double click", true);

            AddSettings(ResetOnDoubleActive);
            EndCategory();

        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            foreach (var button in Buttons)
            {
                if (GetKeyComboDown(button.Value.BindKeys))
                {
                    if (Time.timeScale != button.Key.Value) Time.timeScale = button.Key.Value;
                    else if (ResetOnDoubleActive.Value) Time.timeScale = 1;
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            TimeScaleSetting.Value = Time.timeScale;
        }

    }
}
