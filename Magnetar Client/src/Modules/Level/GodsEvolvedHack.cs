using HarmonyLib;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel.RogueShooting;
using Il2CppUI;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel.RogueShooting;
using UI;

#endif
namespace Magnetar_Client.Modules
{
    public class GodsEvolvedHack : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Gods Evolved Hack";
        public override string Description { get; set; } = "Various Ultils to make Gods Evolved Easier";
        public override string SearchHints { get; set; } = "godsevolvedutil godsevolved helper utility cheats mod godsevolvedhack" +
            "godsevolvedmod trainer easetools assistant godsevolvedcheats godsevolvedtrainer utilitymod mods helpertools" +
            " gameutils godsevolvedhelper godsevolvedutilities qualityoflife qol devutils godsevolvedassist";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static GodsEvolvedHack instance;

        public FloatSetting QualityDefaultWeight;
        public FloatSetting QualitySilverWeight;
        public FloatSetting QualityGoldtWeight;
        public FloatSetting QualityDiamondWeight;

        public FloatSetting ShieldValue;
        public FloatSetting ReviveCD;

        public GodsEvolvedHack() 
        { 
            instance = this;

            CreateCategory("General");

            ShieldValue = new FloatSetting("Shield value", 0, 100000, 0, 2)
            {
                OnValueChanged = x => { if (ShootingManager.Instance != null && Active) ShootingManager.Instance.shieldHealth = x; }
            };

            ReviveCD = new FloatSetting("Revive CD", 0, 3, 3, 3)
            {
                OnValueChanged = x => { if (ShootingManager.Instance != null && Active) ShootingManager.Instance.reviveTimer = x*1000; }
            };

            AddSettings(ShieldValue, ReviveCD);
            EndCategory();

            CreateCategory("Quality Weight");

            QualityDefaultWeight = new FloatSetting("Default Quality Weight", 0, 100, 50, 2, 0)
            {
                OnValueChanged = x => { if (ShootingManager.Instance != null && Active) ShootingManager.Instance.qualityWeights[Quality.Default] = x; }
            };
            QualitySilverWeight = new FloatSetting("Silver Quality Weight", 0, 100, 50, 2, 0)
            {
                OnValueChanged = x => { if (ShootingManager.Instance != null && Active) ShootingManager.Instance.qualityWeights[Quality.silver] = x; }
            };
            QualityGoldtWeight = new FloatSetting("Gold Quality Weight", 0, 100, 50, 2, 0)
            {
                OnValueChanged = x => { if (ShootingManager.Instance != null && Active) ShootingManager.Instance.qualityWeights[Quality.gold] = x; }
            };
            QualityDiamondWeight = new FloatSetting("Diamond Quality Weight", 0, 100, 50, 2, 0)
            {
                OnValueChanged = x => { if (ShootingManager.Instance != null && Active) ShootingManager.Instance.qualityWeights[Quality.diamond] = x; }
            };

            AddSettings(QualityDefaultWeight,QualitySilverWeight,QualityGoldtWeight,QualityDiamondWeight);
            EndCategory();
            
        }

        // Mod Logic

        public override void OnUpdate()
        {
            if (ShootingManager.Instance == null) return;

            // Sync Data
            QualityDefaultWeight.Value = ShootingManager.Instance.qualityWeights[Quality.Default];
            QualitySilverWeight.Value = ShootingManager.Instance.qualityWeights[Quality.silver]; 
            QualityGoldtWeight.Value = ShootingManager.Instance.qualityWeights[Quality.gold];
            QualityDiamondWeight.Value = ShootingManager.Instance.qualityWeights[Quality.diamond];

            ShieldValue.Value = ShootingManager.Instance.shieldHealth;
            ReviveCD.Value = ShootingManager.Instance.reviveTimer/1000f;
        }

        [HarmonyPatch(typeof(ShootingManager))]
        public static class ShootingManagerPatch
        {
            [HarmonyPatch(nameof(ShootingManager.Start))]
            [HarmonyPostfix]
            public static void StartPostfix(ShootingManager __instance)
            {
                if (instance==null || __instance == null) return;

                instance.QualityDefaultWeight.Value = __instance.qualityWeights[Quality.Default];
                instance.QualitySilverWeight.Value = __instance.qualityWeights[Quality.silver];
                instance.QualityGoldtWeight.Value = __instance.qualityWeights[Quality.gold];
                instance.QualityDiamondWeight.Value = __instance.qualityWeights[Quality.diamond];

            }

            [HarmonyPatch(nameof(ShootingManager.GetRandomQuality))]
            [HarmonyPrefix]
            public static bool GetRandomQualityPrefix(ref Quality __result, ShootingManager __instance)
            {
                if (instance == null || !instance.Active) return true;

                float totalWeight = __instance.qualityWeights[Quality.Default] + __instance.qualityWeights[Quality.silver] 
                    + __instance.qualityWeights[Quality.gold] + __instance.qualityWeights[Quality.diamond];

                float random = UnityEngine.Random.RandomRange(0, totalWeight);

                if (random < __instance.qualityWeights[Quality.Default]) { __result = Quality.Default; return false; }
                random -= __instance.qualityWeights[Quality.Default];
                if (random < __instance.qualityWeights[Quality.silver]) { __result = Quality.silver; return false; }
                random -= __instance.qualityWeights[Quality.silver];
                if (random < __instance.qualityWeights[Quality.gold]) { __result = Quality.gold; return false; }

                __result = Quality.diamond;

                return false;

            }

            [HarmonyPatch(nameof(ShootingManager.Update))]
            [HarmonyPrefix]
            public static void UpdatePrefix(ShootingManager __instance)
            {
                if (instance==null || !instance.Active) return;

            }
        }

    }
}
