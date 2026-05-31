using UnityEngine;

namespace Magnetar_Client.Modules
{
    public class FPSLimit : Module
    {
        // Mod Info
        public override string Name { get; set; } = "FPS Limit";
        public override string Description { get; set; } = "Set a custom FPS limit or break the current fps limit";
        public override string SearchHints { get; set; } = "fpslimit fpscap limitfps framespersecond fpsbreaker unlockfps capfps customfps " +
            "maxfps fpsfix fpsunlocked framecap framepersecond fpslimiter fpxlimit fpaslimit fps-limit fpsbypass bypassfps nofpslimit " +
            "unlimitedfps fpsset setfps fps-cap morefps fpsboost lagfix frameslimit";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;

        // Mod Data

        public static FPSLimit instance;

        public IntSetting FpsSetting;

        private int originalTargetFrameRate;
        private int originalVSyncCount;

        public FPSLimit()
        {
            instance = this;
            FpsSetting = new IntSetting("Max FPS", 1, 400, 60);
            AddSettings(FpsSetting);
        }

        public override void OnEnable()
        {
            originalTargetFrameRate = Application.targetFrameRate;
            originalVSyncCount = QualitySettings.vSyncCount;
        }

        public override void OnUpdateActive()
        {
            int targetVal = FpsSetting.Value;

            // If slider is exactly at 400, use -1 to tell Unity to uncap the framerate
            int appliedTarget = (targetVal >= 400) ? -1 : targetVal;

            if (Application.targetFrameRate != appliedTarget)
            {
                // VSync must be 0 for Application.targetFrameRate to be respected unconditionally
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = appliedTarget;
            }
        }

        public override void OnDisable()
        {
            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.vSyncCount = originalVSyncCount;
        }
    }
}