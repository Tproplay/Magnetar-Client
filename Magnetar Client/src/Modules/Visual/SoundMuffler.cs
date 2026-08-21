using HarmonyLib;
using Il2Cpp;
using System;

namespace Magnetar_Client.Modules
{
    public class SoundMuffler : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Sound Muffler";
        public override string Description { get; set; } = "Disable certain sounds from being played.";
        public override string SearchHints { get; set; } = "soundmuffler audiofilter soundmuffle muteaudio " +
            "soundblocker audiosettings volumecontrol soundmute noisecontrol disablesounds soundcleaner audioblock " +
            "soundfix muteeffects audiomute soundmanager customaudio soundfilter audiodampener soundkiller";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;

        // Mod Data

        public static SoundMuffler instance;

        public MultiSelectSetting blacklistedTypes;

        public SoundMuffler()
        {
            instance = this;

            CreateCategory("General");

            blacklistedTypes = new MultiSelectSetting("Blacklisted", typeof(SoundType))
            {
                CustomNames = TranslatedNames(typeof(SoundType))
            };
            AddSettings(blacklistedTypes);

            EndCategory();
        }

        public override void OnLanguageChanged()
        {
            blacklistedTypes.CustomNames = TranslatedNames(typeof(SoundType));
        }

        // Mod Logic

        [HarmonyPatch(typeof(GameAPP))]
        public static class GameAppPatch
        {
            [HarmonyPatch(nameof(GameAPP.PlaySound), new Type[] { typeof(int), typeof(float), typeof(float) })]
            [HarmonyPrefix]
            public static bool PlaySoundIntPatch(int theSoundID, float theVolume, float pitch)
            {
                if (instance == null || !instance.Active ||
                    !instance.blacklistedTypes.IsSelected(theSoundID)) return true;

                return false;

                
            }
        }
        
    }
}