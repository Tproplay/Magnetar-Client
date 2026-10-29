using System.Collections.Generic;
using HarmonyLib;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel.RogueShooting;
using Il2CppUI;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel.RogueShooting;
using UI;
#endif

namespace Magnetar_Client.Modules;

public class GodsEvolvedHack : Module
{
    public override string Name { get; set; } = "Gods Evolved Hack";
    public override string Description { get; set; } = "Various Utils to make Gods Evolved Easier";
    public override string SearchHints { get; set; } = "godsevolvedutil godsevolved helper utility cheats mod godsevolvedhack" +
        "godsevolvedmod trainer easetools assistant godsevolvedcheats godsevolvedtrainer utilitymod mods helpertools" +
        " gameutils godsevolvedhelper godsevolvedutilities qualityoflife qol devutils godsevolvedassist";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    public static GodsEvolvedHack instance;

    // Settings
    public LabelSetting WeightsInfoLabel;
    public BoolSetting LockQualityWeights;

    public FloatSetting QualityDefaultWeight;
    public FloatSetting QualitySilverWeight;
    public FloatSetting QualityGoldtWeight;
    public FloatSetting QualityDiamondWeight;
    public FloatSetting QualityCurseWeight;
    public FloatSetting QualityIridescentWeight;
    public FloatSetting QualityRandomWeight;

    public FloatSetting ShieldValue;
    public FloatSetting ReviveCD;

    private readonly Dictionary<Quality, FloatSetting> weightSliders = new();
    private readonly Dictionary<Quality, float> originalWeights = new();
    private bool isSyncingFromGame;

    public GodsEvolvedHack()
    {
        instance = this;

        string[] strings = new string[]
        {
            "The weight values are synced to level",
            "Weight values are locked to custom settings",
        };
        RegisterTranslations(Translator.CreateDictionary(strings));

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
        WeightsInfoLabel = new("The weight values are synced to level", autoTranslate: false);

        LockQualityWeights = new("Lock Weight Values", false)
        {
            OnValueChanged = locked =>
            {
                UpdateWeightsInfoLabel(locked);
                if (Active && ShootingManager.Instance != null)
                {
                    if (locked)
                    {
                        ApplyWeights(ShootingManager.Instance);
                    }
                    else
                    {
                        SyncWeightsFromGame(ShootingManager.Instance);
                    }
                }
            }
        };
        UpdateWeightsInfoLabel(LockQualityWeights.Value);

        QualityDefaultWeight = CreateWeightSetting("Default Quality Weight", Quality.Default, 50);
        QualitySilverWeight = CreateWeightSetting("Silver Quality Weight", Quality.silver, 50);
        QualityGoldtWeight = CreateWeightSetting("Gold Quality Weight", Quality.gold, 50);
        QualityDiamondWeight = CreateWeightSetting("Diamond Quality Weight", Quality.diamond, 50);
        QualityCurseWeight = CreateWeightSetting("Curse Quality Weight", Quality.curse, 0);
        QualityIridescentWeight = CreateWeightSetting("Iridescent Quality Weight", Quality.iridescent, 0);
        QualityRandomWeight = CreateWeightSetting("Random Quality Weight", Quality.random, 0);

        AddSettings(
            WeightsInfoLabel,
            LockQualityWeights,
            QualityDefaultWeight,
            QualitySilverWeight,
            QualityGoldtWeight,
            QualityDiamondWeight,
            QualityCurseWeight,
            QualityIridescentWeight,
            QualityRandomWeight
        );
        EndCategory();
    }

    private FloatSetting CreateWeightSetting(string name, Quality quality, float def)
    {
        var setting = new FloatSetting(name, 0, 100, def, 2, 0)
        {
            OnValueChanged = val => OnWeightSliderChanged(quality, val)
        };
        weightSliders[quality] = setting;
        return setting;
    }

    private void OnWeightSliderChanged(Quality quality, float value)
    {
        if (isSyncingFromGame) return;

        if (Active && LockQualityWeights.Value && ShootingManager.Instance?.qualityWeights != null)
        {
            ShootingManager.Instance.qualityWeights[quality] = value;
        }
    }

    private void UpdateWeightsInfoLabel(bool locked)
    {
        WeightsInfoLabel.DisplayText = locked
            ? Translate("Weight values are locked to custom settings")
            : Translate("The weight values are synced to level");
    }

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        UpdateWeightsInfoLabel(LockQualityWeights.Value);
    }

    public void ApplyWeights(ShootingManager manager)
    {
        if (manager?.qualityWeights == null) return;
        foreach (var (q, s) in weightSliders)
        {
            manager.qualityWeights[q] = s.Value;
        }
    }

    public void SyncWeightsFromGame(ShootingManager manager)
    {
        if (manager?.qualityWeights == null) return;
        isSyncingFromGame = true;
        try
        {
            foreach (var (q, s) in weightSliders)
            {
                if (manager.qualityWeights.ContainsKey(q))
                    s.Value = manager.qualityWeights[q];
            }
        }
        finally
        {
            isSyncingFromGame = false;
        }
    }

    public void CaptureOriginalWeights(ShootingManager manager)
    {
        if (manager?.qualityWeights == null) return;
        originalWeights.Clear();
        foreach (var pair in manager.qualityWeights)
        {
            originalWeights[pair.Key] = pair.Value;
        }
    }

    public void RestoreOriginalWeights(ShootingManager manager)
    {
        if (manager?.qualityWeights == null || originalWeights.Count == 0) return;
        foreach (var (q, v) in originalWeights)
        {
            manager.qualityWeights[q] = v;
        }
    }

    public void ApplyToGame(ShootingManager manager)
    {
        if (manager == null) return;

        if (ShieldValue.Value > 0)
            manager.shieldHealth = ShieldValue.Value;

        manager.reviveTimer = ReviveCD.Value * 1000f;

        if (LockQualityWeights.Value)
            ApplyWeights(manager);
    }

    // ==========================================
    // Lifecycle Loops
    // ==========================================

    public override void OnEnable()
    {
        if (ShootingManager.Instance != null)
        {
            CaptureOriginalWeights(ShootingManager.Instance);
            ApplyToGame(ShootingManager.Instance);
        }
    }

    public override void OnDisable()
    {
        if (ShootingManager.Instance != null)
        {
            ShootingManager.Instance.reviveTimer = 3000f;
            RestoreOriginalWeights(ShootingManager.Instance);
        }
    }

    public override void OnUpdateActive()
    {
        if (ShootingManager.Instance == null) return;

        if (ShieldValue.Value > 0)
            ShootingManager.Instance.shieldHealth = ShieldValue.Value;

        ShootingManager.Instance.reviveTimer = ReviveCD.Value * 1000f;
    }

    // Sliders only update while the module's setting window is shown
    public override void OnSettingOpened()
    {
        if (!LockQualityWeights.Value && ShootingManager.Instance != null)
            SyncWeightsFromGame(ShootingManager.Instance);
    }

    public override void OnSettingUpdate()
    {
        if (!LockQualityWeights.Value && ShootingManager.Instance != null)
            SyncWeightsFromGame(ShootingManager.Instance);
    }


    public static void OnBeforeBuffMenu(ShootingManager manager)
    {
        if (instance == null || !instance.Active || manager == null) return;

        if (instance.LockQualityWeights.Value)
        {
            instance.CaptureOriginalWeights(manager);
            instance.ApplyWeights(manager);
        }
    }

    public static void OnAfterBuffMenu(ShootingManager manager)
    {
        if (instance == null || !instance.Active || manager == null) return;

        if (instance.LockQualityWeights.Value)
        {
            instance.RestoreOriginalWeights(manager);
        }
        else
        {
            instance.CaptureOriginalWeights(manager);
            if (instance.ShowSettings)
                instance.SyncWeightsFromGame(manager);
        }
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

            instance.CaptureOriginalWeights(__instance);

            if (instance.Active)
            {
                instance.ApplyToGame(__instance);
            }
        }

        [HarmonyPatch(nameof(ShootingManager.ShowBuff))]
        [HarmonyPrefix]
        public static void ShowBuffPrefix(ShootingManager __instance) => OnBeforeBuffMenu(__instance);

        [HarmonyPatch(nameof(ShootingManager.ShowBuff))]
        [HarmonyPostfix]
        public static void ShowBuffPostfix(ShootingManager __instance) => OnAfterBuffMenu(__instance);

        [HarmonyPatch(nameof(ShootingManager.ShowExpertBuffMenu))]
        [HarmonyPrefix]
        public static void ShowExpertBuffMenuPrefix(ShootingManager __instance) => OnBeforeBuffMenu(__instance);

        [HarmonyPatch(nameof(ShootingManager.ShowExpertBuffMenu))]
        [HarmonyPostfix]
        public static void ShowExpertBuffMenuPostfix(ShootingManager __instance) => OnAfterBuffMenu(__instance);

        [HarmonyPatch(nameof(ShootingManager.GetRandomQuality))]
        [HarmonyPrefix]
        public static bool GetRandomQualityPrefix(ref Quality __result, ShootingManager __instance)
        {
            if (instance == null || !instance.Active || !instance.LockQualityWeights.Value)
                return true;

            float totalWeight = 0f;
            foreach (var (_, setting) in instance.weightSliders)
            {
                if (setting.Value > 0f) totalWeight += setting.Value;
            }

            if (totalWeight <= 0f)
            {
                __result = Quality.Default;
                return false;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            foreach (var (quality, setting) in instance.weightSliders)
            {
                if (setting.Value <= 0f) continue;

                if (roll < setting.Value)
                {
                    __result = quality;
                    return false;
                }

                roll -= setting.Value;
            }

            __result = Quality.Default;
            return false;
        }
    }

    [HarmonyPatch(typeof(MultipleChoiceMenu))]
    public static class MultipleChoiceMenuPatch
    {
        [HarmonyPatch(nameof(MultipleChoiceMenu.OnSelect))]
        [HarmonyPostfix]
        public static void OnSelectPostfix() => OnAfterBuffMenu(ShootingManager.Instance);

        [HarmonyPatch(nameof(MultipleChoiceMenu.Cancel))]
        [HarmonyPostfix]
        public static void CancelPostfix() => OnAfterBuffMenu(ShootingManager.Instance);
    }
}
