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

        public BoolSetting StopWhenPaused;
        public BoolSetting ResumeAfterPaused;

        private bool paused = false;

        public TimeScale()
        {
            Instance = this;

            CreateCategory("General");

            TimeScaleValueSetting = new FloatSetting("Time Scale", 0f, 200, TimeScaleValue);
            AddSettings(TimeScaleValueSetting);

            EndCategory();
            CreateCategory("Extra");

            StopWhenPaused = new BoolSetting("Stop When Game Paused",true);
            AddSettings(StopWhenPaused);

            ResumeAfterPaused = new BoolSetting("Auto Resume After Game Pause", true);
            AddSettings(ResumeAfterPaused);

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
            float timeScale = UnityEngine.Time.timeScale;
            if (timeScale != TimeScaleValueSetting.Value)
            {
                if ((timeScale == 0) && (StopWhenPaused.Value))
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
