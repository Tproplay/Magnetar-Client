using HarmonyLib;
using UnityEngine;
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

        public FloatSetting TimeScaleValueSetting;

        public BoolSetting ResumeAfterPaused;

        private bool paused = false;

        public TimeScale()
        {
            Instance = this;

            CreateCategory("General");

            TimeScaleValueSetting = new FloatSetting("Time Scale", 0f, 200, 2);
            AddSettings(TimeScaleValueSetting);

            EndCategory();
            CreateCategory("Extra");

            ResumeAfterPaused = new BoolSetting("Auto Resume After Game Pause", true);
            AddSettings(ResumeAfterPaused);

            EndCategory();

        }

        public float originalTimeScale = 1;

        // Mod Logic
        public override void OnEnable()
        {
            originalTimeScale = UnityEngine.Time.timeScale == 0 ? 1 : UnityEngine.Time.timeScale;
        }
        public override void OnDisable()
        {
            paused = false;
            if (GameAPP.theGameStatus!=GameStatus.Pause)
                UnityEngine.Time.timeScale = originalTimeScale;
            originalTimeScale = 1;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (paused && ResumeAfterPaused.Value)
            {
                if (UnityEngine.Time.timeScale != 0)
                {
                    Active = true;
                    paused = false;
                }
            }
        }

        public override void OnUpdateActive()
        {
            if (HoldMode)
            {
                bool allKeysHeld = true;

                foreach (KeyCode key in BindKeys)
                {
                    if (!Input.GetKey(key)) allKeysHeld = false; // Is this key currently down?
                }

                if (!allKeysHeld) { Active = false; OnDisable(); return; }
            }
            float timeScale = UnityEngine.Time.timeScale;
            if (timeScale != TimeScaleValueSetting.Value)
            {
                if (GameAPP.theGameStatus == GameStatus.Pause)
                    { Active = false; paused = true; return; }
                UnityEngine.Time.timeScale = TimeScaleValueSetting.Value; 
            }
        }

        [HarmonyPatch(typeof(SlowTrigger),nameof(SlowTrigger.Clicking))]
        public static bool SlowTriggerPatch()
        {
            if (Instance == null) return true;
            return !Instance.Active;
        }
    }
}
