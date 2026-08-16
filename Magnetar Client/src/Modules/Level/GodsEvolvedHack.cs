using HarmonyLib;
using UnityEngine;

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
        public override string Description { get; set; } = "Various Utils to make Gods Evolved Easier";
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
        public FloatSetting QualityCurseWeight;
        public FloatSetting QualityIridescentWeight;
        public FloatSetting QualityRandomWeight;

        public FloatSetting ShieldValue;
        public FloatSetting ReviveCD;

        public GodsEvolvedHack()
        {
            instance = this;

            CreateCategory("General");

            ShieldValue = new FloatSetting("Shield value", 0, 100000, 0, 2)
            {
                OnValueChanged = x =>
                {
                    if (Active && ShootingManager.Instance != null)
                        ShootingManager.Instance.shieldHealth = x;
                }
            };

            ReviveCD = new FloatSetting("Revive CD", 0, 3, 3, 3)
            {
                OnValueChanged = x =>
                {
                    if (Active && ShootingManager.Instance != null)
                        ShootingManager.Instance.reviveTimer = x * 1000f;
                }
            };

            AddSettings(ShieldValue, ReviveCD);
            EndCategory();

            CreateCategory("Quality Weight");

            QualityDefaultWeight = new FloatSetting("Default Quality Weight", 0, 100, 50, 2, 0)
            {
                OnValueChanged = x => ApplySingleWeight(Quality.Default, x)
            };
            QualitySilverWeight = new FloatSetting("Silver Quality Weight", 0, 100, 50, 2, 0)
            {
                OnValueChanged = x => ApplySingleWeight(Quality.silver, x)
            };
            QualityGoldtWeight = new FloatSetting("Gold Quality Weight", 0, 100, 50, 2, 0)
            {
                OnValueChanged = x => ApplySingleWeight(Quality.gold, x)
            };
            QualityDiamondWeight = new FloatSetting("Diamond Quality Weight", 0, 100, 50, 2, 0)
            {
                OnValueChanged = x => ApplySingleWeight(Quality.diamond, x)
            };
            QualityCurseWeight = new FloatSetting("Curse Quality Weight", 0, 100, 0, 2, 0)
            {
                OnValueChanged = x => ApplySingleWeight(Quality.curse, x)
            };
            QualityIridescentWeight = new FloatSetting("Iridescent Quality Weight", 0, 100, 0, 2, 0)
            {
                OnValueChanged = x => ApplySingleWeight(Quality.iridescent, x)
            };
            QualityRandomWeight = new FloatSetting("Random Quality Weight", 0, 100, 0, 2, 0)
            {
                OnValueChanged = x => ApplySingleWeight(Quality.random, x)
            };

            AddSettings(QualityDefaultWeight, QualitySilverWeight, QualityGoldtWeight, QualityDiamondWeight,
                QualityCurseWeight, QualityIridescentWeight, QualityRandomWeight);
            EndCategory();
        }


        private static float GetWeight(ShootingManager manager, Quality quality, float fallback = 0f)
        {
            if (manager == null || manager.qualityWeights == null) return fallback;
            return manager.qualityWeights.ContainsKey(quality) ? manager.qualityWeights[quality] : fallback;
        }

        private static void SetWeight(ShootingManager manager, Quality quality, float value)
        {
            if (manager == null || manager.qualityWeights == null) return;
            manager.qualityWeights[quality] = value;
        }

        private void ApplySingleWeight(Quality quality, float value)
        {
            if (!Active || ShootingManager.Instance == null) return;
            SetWeight(ShootingManager.Instance, quality, value);
        }

        /// <summary>
        /// Pushes GUI values into the game.
        /// </summary>
        public void ApplyToGame(ShootingManager manager)
        {
            if (manager == null || manager.qualityWeights == null) return;

            SetWeight(manager, Quality.Default, QualityDefaultWeight.Value);
            SetWeight(manager, Quality.silver, QualitySilverWeight.Value);
            SetWeight(manager, Quality.gold, QualityGoldtWeight.Value);
            SetWeight(manager, Quality.diamond, QualityDiamondWeight.Value);
            SetWeight(manager, Quality.curse, QualityCurseWeight.Value);
            SetWeight(manager, Quality.iridescent, QualityIridescentWeight.Value);
            SetWeight(manager, Quality.random, QualityRandomWeight.Value);

            if (ShieldValue.Value > 0)
                manager.shieldHealth = ShieldValue.Value;

            manager.reviveTimer = ReviveCD.Value * 1000f;
        }

        /// <summary>
        /// Reads game values into the GUI sliders.
        /// </summary>
        public void SyncFromGame(ShootingManager manager)
        {
            if (manager == null || manager.qualityWeights == null) return;

            QualityDefaultWeight.Value = GetWeight(manager, Quality.Default, 50f);
            QualitySilverWeight.Value = GetWeight(manager, Quality.silver, 50f);
            QualityGoldtWeight.Value = GetWeight(manager, Quality.gold, 50f);
            QualityDiamondWeight.Value = GetWeight(manager, Quality.diamond, 50f);
            QualityCurseWeight.Value = GetWeight(manager, Quality.curse, 0f);
            QualityIridescentWeight.Value = GetWeight(manager, Quality.iridescent, 0f);
            QualityRandomWeight.Value = GetWeight(manager, Quality.random, 0f);

            ShieldValue.Value = manager.shieldHealth;
            ReviveCD.Value = manager.reviveTimer / 1000f;
        }

        // ==========================================
        // Lifecycle Loops
        // ==========================================

        public override void OnEnable()
        {
            if (ShootingManager.Instance != null)
            {
                ApplyToGame(ShootingManager.Instance);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (ShootingManager.Instance == null) return;

            SyncFromGame(ShootingManager.Instance);
        }

        public override void OnUpdateActive()
        {
            if (ShootingManager.Instance == null) return;

            if (ShieldValue.Value > 0)
                ShootingManager.Instance.shieldHealth = ShieldValue.Value;
        }

        // ==========================================
        // Harmony Patches
        // ==========================================

        [HarmonyPatch(typeof(ShootingManager))]
        public static class ShootingManagerPatch
        {
            [HarmonyPatch(nameof(ShootingManager.Start))]
            [HarmonyPostfix]
            public static void StartPostfix(ShootingManager __instance)
            {
                if (instance == null || __instance == null) return;

                if (instance.Active)
                {
                    instance.ApplyToGame(__instance);
                }
                else
                {
                    instance.SyncFromGame(__instance);
                }
            }

            [HarmonyPatch(nameof(ShootingManager.GetRandomQuality))]
            [HarmonyPrefix]
            public static bool GetRandomQualityPrefix(ref Quality __result, ShootingManager __instance)
            {
                if (instance == null || !instance.Active || __instance == null || __instance.qualityWeights == null)
                    return true;

                float totalWeight = 0f;
                foreach (var pair in __instance.qualityWeights)
                {
                    if (pair.Value > 0f) totalWeight += pair.Value;
                }

                if (totalWeight <= 0f)
                {
                    __result = Quality.Default;
                    return false;
                }

                float roll = UnityEngine.Random.Range(0f, totalWeight);

                foreach (var pair in __instance.qualityWeights)
                {
                    if (pair.Value <= 0f) continue;

                    if (roll < pair.Value)
                    {
                        __result = pair.Key;
                        return false;
                    }

                    roll -= pair.Value;
                }

                __result = Quality.Default;
                return false;
            }
        }
    }
}