using HarmonyLib;
using Il2Cpp;


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

        public float TimeScaleValue = 2f;
        public FloatSetting TimeScaleValueSetting;

        public TimeScale()
        {
            Instance = this;

            CreateCategory("General");

            TimeScaleValueSetting = new FloatSetting("Time Scale", 0f, 200, TimeScaleValue);
            Settings.Add(TimeScaleValueSetting);

            EndCategory();
        }

        public float originalTimeScale = 1;

        // Mod Logic
        public override void OnEnable()
        {
            originalTimeScale = UnityEngine.Time.timeScale;
        }
        public override void OnDisable()
        {
            UnityEngine.Time.timeScale = originalTimeScale;
            originalTimeScale = 1;
        }

        public override void OnUpdateActive()
        {
            if (UnityEngine.Time.timeScale != TimeScaleValueSetting.Value) UnityEngine.Time.timeScale = TimeScaleValueSetting.Value;
        }

        [HarmonyPatch(typeof(SlowTrigger),nameof(SlowTrigger.Clicking))]
        public static bool SlowTriggerPatch()
        {
            if (Instance == null) return true;
            return !Instance.Active;
        }
    }
}
