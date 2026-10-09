using HarmonyLib;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace Magnetar_Client.Modules;


public class CustomDifficulty : Module
{
    // Mod Info
    public override string Name { get; set; } = "Custom Difficulty";
    public override string Description { get; set; } = "Allows you to modify difficulty settings.";
    public override string SearchHints { get; set; } = "customdifficulty difficultymodifier difficultysettings scaledifficulty" +
        " difficultyeditor game difficultyconfig difficultychanger hardmod easymod difficultybalancer customdifficultycustomizer" +
        " diffeditor difficultyoverride adjustmentmod challengedifficulty difficultyselector diffmanager customchallenge";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

    // Mod Data

    public static CustomDifficulty instance;
    public static int MaxDifficulty = 5;
    public BoolSetting AllowSkinDifficultychange;
    public IntSetting MaxDifficultySetting;

    public LabelSetting AdvancedLabel;
    public SectionSetting CustomDifficultySection;

    public CustomDifficulty()
    {
        instance = this;

        CreateCategory("General");

        AllowSkinDifficultychange = new("Allow changing difficulty in skin levels", true);

        MaxDifficultySetting = new("Max Game Difficulty", 0, 10, 6, 0)
        {
            OnValueChanged = (value) => MaxDifficulty = value,
        };

        AddSettings(AllowSkinDifficultychange, MaxDifficultySetting);
        EndCategory();
        CreateCategory("Advanced");

        AdvancedLabel = new(
            "This section allows you to register your own custom difficulty.\n" +
            "Difficulty 6 is already coded, you may over write the values\n" +
            "through Custom Difficulty Config.\n" +
            "On Difficulty 5:\n" +
            "   Zombie Insta-kill Threshold % = 40\n" +
            "   Odyssey Health multiplier = 1.5\n" +
            "On Difficulty 6:\n" +
            "   First Wave Arrival Time = 10\n" +
            "   Zombie Speed Multiplier = 1.4\n" +
            "   Wave Interval = 3");

        CustomDifficultySection = new("Custom Difficulty Config",
            (index) => new List<Setting>
            {
                new IntSetting("Difficulty level",6,15,7,0),
                new StringSetting("Difficulty Name", $"Custom difficulty: {index}"),
                new FloatSetting("Zombie Speed Multiplier", 0, 3, -1, 3),
                new FloatSetting("Damage Reduction %", 0, 100, 60, 3, 0, 100),
                new FloatSetting("Zombie Spawn Multiplier", 0.2f, 5, -1, 3, 0),
                new FloatSetting("Zombie Insta-kill Threshold %", 0, 100, 40, 3, 0, 100),
                new BoolSetting("Insta-kill ignore armor", false),
                new FloatSetting("First Wave Arrival Time", 0, 20, -1, 3),
                new FloatSetting("Wave Interval", 0, 20, -1, 3),
                new FloatSetting("Odyssey Health multiplier", 1, 5, 1.5f, 3, 0.01f),
                new FloatSetting("Convyer Interval", 0, 10, -1, 3),
            }, 0);

        AddSettings(AdvancedLabel, CustomDifficultySection);
        EndCategory();
    }

    // Mod Logic
    public override void OnEnable()
    {
        MaxDifficulty = MaxDifficultySetting.Value;
    }

    public override void OnDisable()
    {
        MaxDifficulty = 5;
    }

    public override void OnUpdateActive()
    {
        
    }

    public bool TryGetDifficultyName(int difficulty, out string name)
    {
        name = null;
        for (int i = CustomDifficultySection.Sections.Count -1; i>=0; i--)
        {
            var section = CustomDifficultySection.Sections[i];

            var sectionDifficulty = section.Find<IntSetting>("Difficulty level").Value;
            
            if (sectionDifficulty == difficulty)
            {
                name = section.Find<StringSetting>("Difficulty Name").Value;
                return true;
            }
        }

        if (difficulty == 6)
        {
            name = "Skin Difficulty";
            return true;
        }

        return false;
    }

    public bool TryGetCharredThreshold(int difficulty, out float threshold, out bool ignoreArmor)
    {
        threshold = 0;
        ignoreArmor = false;
        for (int i = CustomDifficultySection.Sections.Count - 1; i >= 0; i--)
        {
            var section = CustomDifficultySection.Sections[i];

            var sectionDifficulty = section.Find<IntSetting>("Difficulty level").Value;

            if (sectionDifficulty == difficulty)
            {
                var value = section.Find<FloatSetting>("Zombie Insta-kill Threshold %").Value;
                threshold = value/100f;
                ignoreArmor = section.Find<BoolSetting>("Insta-kill ignore armor").Value;
                return true;
            }
        }
        return false;
    }

    public float GetTravelHealthMultiplier(int difficulty)
    {
        for (int i = CustomDifficultySection.Sections.Count - 1; i >= 0; i--)
        {
            var section = CustomDifficultySection.Sections[i];

            var sectionDifficulty = section.Find<IntSetting>("Difficulty level").Value;

            if (sectionDifficulty == difficulty)
            {
                return section.Find<FloatSetting>("Odyssey Health multiplier").Value;
            }
        }

        // fallback
        if (difficulty < 5) return 1f;
        else return 1.5f;
    }

    public struct BoardConfig
    {
        public float conveyInterval;
        public float firstWaveArrivedTimer;
        public float zombieSpeedMultiplier;
        public float waveInterval;
        public float zombieCountMultiplier;
    }

    public BoardConfig GetBoardConfig(int difficulty)
    {
        var config = new BoardConfig();

        config.conveyInterval = -1;
        config.firstWaveArrivedTimer = -1;
        config.zombieSpeedMultiplier = -1;
        config.waveInterval = -1;
        config.zombieCountMultiplier = -1;

        for (int i = CustomDifficultySection.Sections.Count - 1; i >= 0; i--)
        {
            var section = CustomDifficultySection.Sections[i];

            var sectionDifficulty = section.Find<IntSetting>("Difficulty level").Value;

            if (sectionDifficulty == difficulty)
            {

                config.conveyInterval = section.Find<FloatSetting>("Convyer Interval").Value;
                config.firstWaveArrivedTimer = section.Find<FloatSetting>("First Wave Arrival Time").Value;
                config.zombieSpeedMultiplier = section.Find<FloatSetting>("Zombie Speed Multiplier").Value;
                config.waveInterval = section.Find<FloatSetting>("Wave Interval").Value;
                config.zombieCountMultiplier = section.Find<FloatSetting>("Zombie Spawn Multiplier").Value;

                return config;
            }
        }

        if (difficulty == 6)
        {
            config.waveInterval = 3;
            config.firstWaveArrivedTimer = 10;
            config.zombieSpeedMultiplier = 1.4f;
        }

        return config;
    }

    public double GetDamageReduction(int difficulty)
    {
        for (int i = CustomDifficultySection.Sections.Count - 1; i >= 0; i--)
        {
            var section = CustomDifficultySection.Sections[i];

            var sectionDifficulty = section.Find<IntSetting>("Difficulty level").Value;

            if (sectionDifficulty == difficulty)
            {
                var value = section.Find<FloatSetting>("Damage Reduction %").Value;
                var val = (double)value;
                return val / 100.0;
            }
        }

        if (difficulty == 4) return 0.3;
        if (difficulty == 5) return 0.6;
        return 0;
    }

    [HarmonyPatch(typeof(DifficultyMgr))]
    public static class DifficultyMgrPatch
    {
        private static bool _isSyncing;
        private static DifficultyMgr _lastActiveMgr;

        [HarmonyPatch(nameof(DifficultyMgr.Start))]
        [HarmonyPostfix]
        public static void AwakePostfix(DifficultyMgr __instance)
        {
            // Sync slider value as soon as the menu object wakes up
            SyncSliderToConfig(__instance);
        }

        private static void SyncSliderToConfig(DifficultyMgr difficultyMgr)
        {
            if (difficultyMgr == null) return;
            Slider slider = difficultyMgr.GetComponent<Slider>();
            if (slider != null && GameAPP.config != null)
            {
                _isSyncing = true;
                slider.maxValue = MaxDifficulty;
                slider.value = Mathf.Clamp(GameAPP.config.difficulty, 0, MaxDifficulty);
                _isSyncing = false;
            }
        }

        [HarmonyPatch(nameof(DifficultyMgr.Update))]
        [HarmonyPrefix]
        public static bool UpdatePrefix(DifficultyMgr __instance)
        {
            if (instance == null || !instance.Active) return true;

            CustomUpdate(__instance);

            return false;
        }

        public static void CustomUpdate(DifficultyMgr difficultyMgr)
        {
            Slider slider = difficultyMgr.GetComponent<Slider>();
            TextMeshProUGUI label = difficultyMgr.transform.GetChild(3).GetComponent<TextMeshProUGUI>();

            if (slider == null) return;

            slider.maxValue = MaxDifficulty;

            // Ensure slider matches config when switching or freshly activating
            if (_lastActiveMgr != difficultyMgr)
            {
                _lastActiveMgr = difficultyMgr;
                SyncSliderToConfig(difficultyMgr);
            }

            // 1. Determine lock state
            int lockDifficulty = -1;
            if (GameAPP.theBoardType == LevelType.SkinLevel)
            {
                lockDifficulty = instance.AllowSkinDifficultychange.Value ? -1 : 5;
            }
            else if (Board.Instance != null)
            {
                lockDifficulty = Board.Instance.lockedDifficulty;
            }

            bool isLocked = lockDifficulty != -1;
            slider.interactable = !isLocked;

            // 2. Resolve target difficulty and display text
            int targetDifficulty;
            string displayText;

            if (isLocked)
            {
                targetDifficulty = lockDifficulty;
                if (!_isSyncing)
                {
                    _isSyncing = true;
                    slider.value = lockDifficulty;
                    _isSyncing = false;
                }
                displayText = "已锁定";
            }
            else
            {
                targetDifficulty = Mathf.RoundToInt(slider.value);

                if (!instance.TryGetDifficultyName(targetDifficulty, out displayText))
                {
                    displayText = targetDifficulty switch
                    {
                        0 => "休闲模式",
                        1 => "简单模式",
                        2 => "正常模式",
                        3 => "困难模式",
                        4 => "极难模式",
                        5 => "你确定？",
                        _ => $"Difficulty: {targetDifficulty}",
                    };
                }
            }

            // 3. Apply state
            if (GameAPP.config != null)
            {
                GameAPP.config.difficulty = targetDifficulty;
            }

            if (label != null)
            {
                label.text = displayText;
            }
        }
    }

    [HarmonyPatch(typeof(Zombie))]
    public static class ZombiePatch
    {
        [HarmonyPatch(nameof(Zombie.Charred))]
        [HarmonyPrefix]
        public static bool Charred(Zombie __instance, int damage, PlantType reportType, bool fix, DamageType damageType)
        {
            if (instance == null || !instance.Active) return true;

            // 1. Adventure passive check
            if (AdvantureConfig.data != null &&
                AdvantureConfig.data.GetResult((AdvantureLevel)6, (MissionResult)1))
            {
                damage = (int)(damage * 1.1f);
            }

            GameConfig config = GameAPP.config;
            if (config == null) return true;

            int difficulty = config.difficulty;
            float totalZombieHp = __instance.theFirstArmorHealth + __instance.theHealth;

            // 2. Difficulty thresholds for instant char/ash kill
            bool canBeCharred = false;

            if (instance.TryGetCharredThreshold(difficulty, out float threshold, out bool ignoreArmor))
            {
                if (!ignoreArmor && totalZombieHp <= damage * threshold)
                {
                    canBeCharred = true;
                }
                else if (damage * threshold >= __instance.theHealth)
                {
                    canBeCharred = true;
                }
            }

            // fallback to original game's code
            else if (difficulty <= 3)
            {
                if (damage >= __instance.theHealth)
                {
                    canBeCharred = true;
                }
            }
            else if (difficulty == 4)
            {
                if (totalZombieHp <= damage * 0.7f)
                {
                    canBeCharred = true;
                }
            }
            else if (difficulty == 5)
            {
                if (totalZombieHp <= damage * 0.4f)
                {
                    canBeCharred = true;
                }
            }


            if (canBeCharred)
            {
                if (Board.Instance != null && Board.Instance.damageReporter != null)
                {
                    float finalDamage = Mathf.Max(0f, totalZombieHp);
                    Vector3 centerPos = __instance.centerPosition;
                    Il2CppSystem.Nullable<Color> reportColor = new(new Color(1f, 1f, 1f, 0.55f));

                    Board.Instance.damageReporter.Report(reportType, (long)finalDamage, centerPos, reportColor);
                    __instance.SetCarred();
                    AdvantureMission.TryAddCount((AdvantureLevel)18, 0); // Mission 0x12
                    return false;
                }
            }

            // If char condition fails, fall back to standard damage with matching parameters
            __instance.TakeDamage(damage, null, damageType, reportType, fix);

            return false;
        }

        [HarmonyPatch(nameof(Zombie.GetDamage))]
        [HarmonyPostfix]
        public static void GetDamagePostfix(ref long __result)
        {
            if (instance == null || !instance.Active) return;

            long baseDmg;
            if (GameAPP.config.difficulty == 4) baseDmg = (long)(__result / 0.7f);
            else if (GameAPP.config.difficulty == 5) baseDmg = (long)(__result / 0.4f);
            else baseDmg = __result;

            __result = (long)(baseDmg * (1-instance.GetDamageReduction(GameAPP.config.difficulty)));
        }
    }

    [HarmonyPatch(typeof(UIDifficulty))]
    public static class UIDiffPatch
    {
        [HarmonyPatch(nameof(UIDifficulty.Update))]
        [HarmonyPostfix]
        public static void UpdatePostfix(UIDifficulty __instance)
        {
            if (instance == null || !instance.Active) return;

            var text = __instance.GetComponent<TextMeshProUGUI>();

            if (text == null) return;

            if (GameAPP.developerMode)
            {
                return;
            }

            if (GameAPP.theBoardType == LevelType.SkinLevel && instance.AllowSkinDifficultychange.Value)
            {
                text.text = text.text.Split("：")[0] + $"：{GameAPP.config.difficulty}";
            }

        }
    }

    [HarmonyPatch(typeof(Lawnf))]
    public static class LawnfPatch
    {
        [HarmonyPatch(nameof(Lawnf.SetZombieHealth))]
        [HarmonyPrefix]
        public static void SetZombieHealthPrefix(Zombie zombie, ref float ratio)
        {
            if (instance==null || !instance.Active) return;

            ratio /= 1.5f;
            ratio *= instance.GetTravelHealthMultiplier(GameAPP.config.difficulty);

        }
    }

    [HarmonyPatch(typeof(Board))]
    public static class BoardPatch
    {
        [HarmonyPatch(nameof(Board.SetUniqueLevel))]
        [HarmonyPrefix]
        public static void SetUniqueLevelPrefix(out int __state)
        {
            __state = -1;
            if (instance == null || !instance.Active) return;

            __state = GameAPP.config.difficulty;

        }

        [HarmonyPatch(nameof(Board.SetUniqueLevel))]
        [HarmonyPostfix]
        public static void SetUniqueLevelPostfix(Board __instance, int __state)
        {
            if (instance==null || !instance.Active) return;

            if (GameAPP.theBoardType == LevelType.SkinLevel)
            {
                if (instance.AllowSkinDifficultychange.Value && __state != -1)
                {
                    if (GameAPP.config != null)
                    {
                        GameAPP.config.difficulty = __state;
                    }
                    __instance.lockedDifficulty = -1;
                }
            }

            var config = instance.GetBoardConfig(GameAPP.config.difficulty);

            if (config.conveyInterval > 0) __instance.config.conveyInterval = config.conveyInterval;
            if (config.firstWaveArrivedTimer > 0) __instance.config.firstWaveArrivedTimer = config.firstWaveArrivedTimer;
            if (config.zombieSpeedMultiplier > 0) __instance.config.zombieSpeedMultiplier *= config.zombieSpeedMultiplier;
            if (config.waveInterval > 0) __instance.config.waveInterval = config.waveInterval;
            if (config.zombieCountMultiplier > 0) __instance.config.zombieCountMultiplier = config.zombieCountMultiplier;

        }
    }
}
