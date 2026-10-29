using System.Collections.Generic;
using Magnetar_Client.UI.Setting;
using static Magnetar_Client.Utils.Translator;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel.RogueShooting;
using Il2CppUI;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel.RogueShooting;
using UI;
#endif

namespace Magnetar_Client.Modules;

public partial class GodsEvolvedHack : Module
{
    public override string Name { get; set; } = "Gods Evolved Hack";
    public override string Description { get; set; } = "Various Utils to make Gods Evolved Easier";
    public override string SearchHints { get; set; } = "godsevolvedutil godsevolved helper utility cheats mod godsevolvedhack" +
        "godsevolvedmod trainer easetools assistant godsevolvedcheats godsevolvedtrainer utilitymod mods helpertools" +
        " gameutils godsevolvedhelper godsevolvedutilities qualityoflife qol devutils godsevolvedassist";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    public static GodsEvolvedHack instance;

    private readonly List<ILiveSetting> allLiveSettings = new();
    private readonly Dictionary<Quality, LiveFloatSetting> qualitySettings = new();

    // Quality Weight Settings
    public FloatSetting QualityDefaultWeight;
    public FloatSetting QualitySilverWeight;
    public FloatSetting QualityGoldtWeight;
    public FloatSetting QualityDiamondWeight;
    public FloatSetting QualityCurseWeight;
    public FloatSetting QualityIridescentWeight;
    public FloatSetting QualityRandomWeight;

    // Multiplier Settings
    public FloatSetting BaseDamageMultiplierSetting;
    public FloatSetting DamageMultiplier1Setting;
    public FloatSetting DamageMultiplier2Setting;
    public FloatSetting DamageMultiplier3Setting;
    public FloatSetting BaseHealthMultiplierSetting;
    public FloatSetting HealthMultiplier1Setting;
    public FloatSetting HealthMultiplier2Setting;
    public FloatSetting HealthMultiplier3Setting;
    public FloatSetting BaseSpeedMultiplierSetting;
    public FloatSetting SpeedMultiplier1Setting;
    public FloatSetting SpeedMultiplier2Setting;
    public FloatSetting SpeedMultiplier3Setting;

    // Live Combat & Stats Settings
    public BoolSetting UncrashableSetting;
    public FloatSetting ShieldHealthSetting;
    public FloatSetting ReviveCDSetting;
    public FloatSetting MaxPlantCountSetting;
    public FloatSetting DebuffPointSetting;
    public FloatSetting RefreshCountSetting;

    // Live Lucky & Pity Settings
    public FloatSetting LuckySetting;
    public FloatSetting LuckyMultiplierSetting;
    public FloatSetting MaxLuckySetting;
    public FloatSetting MinLuckySetting;
    public BoolSetting LuckyShowSetting;
    public FloatSetting PityThresholdSetting;
    public BoolSetting PityEnabledSetting;
    public BoolSetting SuperQualitativeSetting;

    // Live Level & Modes Settings
    public SelectSetting RandomTypeSetting;
    public MultiSelectSetting Scene1Setting;
    public MultiSelectSetting Scene2Setting;
    public MultiSelectSetting Scene3Setting;
    public FloatSetting MaxStageSetting;
    public FloatSetting StageSetting;
    public FloatSetting DifficultySetting;
    public BoolSetting EndlessSetting;
    public BoolSetting ContinuousMoveSetting;
    public BoolSetting CanTabSetting;
    public BoolSetting CheatHardSetting;
    public BoolSetting HellModeSetting;
    public BoolSetting SuperUpgradeSetting;
    public FloatSetting GodCoinRewardsSetting;

    private LiveBoolSetting superQualitativeLive;
    private LiveFloatSetting luckyMultiplierLive;

    public GodsEvolvedHack()
    {
        instance = this;

        CreateCategory("Quality Weight");
        QualityDefaultWeight = CreateWeightSetting("Default Quality Weight", Quality.Default, 50).Setting;
        QualitySilverWeight = CreateWeightSetting("Silver Quality Weight", Quality.silver, 50).Setting;
        QualityGoldtWeight = CreateWeightSetting("Gold Quality Weight", Quality.gold, 50).Setting;
        QualityDiamondWeight = CreateWeightSetting("Diamond Quality Weight", Quality.diamond, 50).Setting;
        QualityCurseWeight = CreateWeightSetting("Curse Quality Weight", Quality.curse, 0).Setting;
        QualityIridescentWeight = CreateWeightSetting("Iridescent Quality Weight", Quality.iridescent, 0).Setting;
        QualityRandomWeight = CreateWeightSetting("Random Quality Weight", Quality.random, 0).Setting;

        AddSettings(
            QualityDefaultWeight,
            QualitySilverWeight,
            QualityGoldtWeight,
            QualityDiamondWeight,
            QualityCurseWeight,
            QualityIridescentWeight,
            QualityRandomWeight
        );
        EndCategory();

        CreateCategory("Multipliers");
        BaseDamageMultiplierSetting = CreateLiveFloatSetting("Base Damage Multiplier", 0, 10, 1, 2, 0, 100,
            m => m.BaseDamageMultiplier, (m, v) => m.BaseDamageMultiplier = v).Setting;
        DamageMultiplier1Setting = CreateLiveFloatSetting("Damage Multiplier 1", 0, 10, 1, 2, 0, 100,
            m => m.damageMultiplier1, (m, v) => m.damageMultiplier1 = v).Setting;
        DamageMultiplier2Setting = CreateLiveFloatSetting("Damage Multiplier 2", 0, 10, 1, 2, 0, 100,
            m => m.damageMultiplier2, (m, v) => m.damageMultiplier2 = v).Setting;
        DamageMultiplier3Setting = CreateLiveFloatSetting("Damage Multiplier 3", 0, 10, 1, 2, 0, 100,
            m => m.damageMultiplier3, (m, v) => m.damageMultiplier3 = v).Setting;

        BaseHealthMultiplierSetting = CreateLiveFloatSetting("Base Health Multiplier", 0, 10, 1, 2, 0, 100,
            m => m.BaseHealthMulitpier, (m, v) => m.BaseHealthMulitpier = v).Setting;
        HealthMultiplier1Setting = CreateLiveFloatSetting("Health Multiplier 1", 0, 10, 1, 2, 0, 100,
            m => m.healthMultiplier1, (m, v) => m.healthMultiplier1 = v).Setting;
        HealthMultiplier2Setting = CreateLiveFloatSetting("Health Multiplier 2", 0, 10, 1, 2, 0, 100,
            m => m.healthMultiplier2, (m, v) => m.healthMultiplier2 = v).Setting;
        HealthMultiplier3Setting = CreateLiveFloatSetting("Health Multiplier 3", 0, 10, 1, 2, 0, 100,
            m => m.healthMultiplier3, (m, v) => m.healthMultiplier3 = v).Setting;

        BaseSpeedMultiplierSetting = CreateLiveFloatSetting("Base Speed Multiplier", 0, 10, 1, 2, 0, 100,
            m => m.BaseSpeedMultiplier, (m, v) => m.BaseSpeedMultiplier = v).Setting;
        SpeedMultiplier1Setting = CreateLiveFloatSetting("Speed Multiplier 1", 0, 10, 1, 2, 0, 100,
            m => m.speedMultiplier1, (m, v) => m.speedMultiplier1 = v).Setting;
        SpeedMultiplier2Setting = CreateLiveFloatSetting("Speed Multiplier 2", 0, 10, 1, 2, 0, 100,
            m => m.speedMultiplier2, (m, v) => m.speedMultiplier2 = v).Setting;
        SpeedMultiplier3Setting = CreateLiveFloatSetting("Speed Multiplier 3", 0, 10, 1, 2, 0, 100,
            m => m.speedMultiplier3, (m, v) => m.speedMultiplier3 = v).Setting;

        AddSettings(
            BaseDamageMultiplierSetting,
            DamageMultiplier1Setting,
            DamageMultiplier2Setting,
            DamageMultiplier3Setting,
            BaseHealthMultiplierSetting,
            HealthMultiplier1Setting,
            HealthMultiplier2Setting,
            HealthMultiplier3Setting,
            BaseSpeedMultiplierSetting,
            SpeedMultiplier1Setting,
            SpeedMultiplier2Setting,
            SpeedMultiplier3Setting
        );
        EndCategory();

        CreateCategory("Combat & Stats");
        UncrashableSetting = CreateLiveBoolSetting("Uncrashable", false,
            m => m.uncrashable, (m, v) => m.uncrashable = v).Setting;
        ShieldHealthSetting = CreateLiveFloatSetting("Shield Health", 0, 100000, 0, 2, 0, float.MaxValue,
            m => m.shieldHealth, (m, v) => m.shieldHealth = v).Setting;
        ReviveCDSetting = CreateLiveFloatSetting("Revive CD", 0, 3, 3, 3, 0, 60,
            m => m.reviveTimer / 1000f, (m, v) => m.reviveTimer = v * 1000f).Setting;
        MaxPlantCountSetting = CreateLiveFloatSetting("Max Plant Count", 1, 5, 5, 0, 1, int.MaxValue,
            m => m.maxPlantCount, (m, v) => m.maxPlantCount = (int)v).Setting;
        DebuffPointSetting = CreateLiveFloatSetting("Debuff Point", 0, 2000, 0, 0, 0, int.MaxValue,
            m => m.debuffPoint, (m, v) => m.debuffPoint = (int)v).Setting;
        RefreshCountSetting = CreateLiveFloatSetting("Refresh Count", 0, 10, 0, 0, 0, int.MaxValue,
            m => m.refreshCount, (m, v) => m.refreshCount = (int)v).Setting;

        AddSettings(UncrashableSetting, ShieldHealthSetting, ReviveCDSetting, MaxPlantCountSetting, DebuffPointSetting, RefreshCountSetting);
        EndCategory();

        CreateCategory("Lucky & Pity");
        LuckySetting = CreateLiveFloatSetting("Lucky", -100, 100, 0, 2, -10000, 10000,
            m => m.Lucky, (m, v) => m.Lucky = v).Setting;
        luckyMultiplierLive = CreateLiveFloatSetting("Lucky Multiplier", 0, 10, 1, 2, 0, 1000,
            m => m.LuckyMultiplier, null);
        LuckyMultiplierSetting = luckyMultiplierLive.Setting;
        MaxLuckySetting = CreateLiveFloatSetting("Max Lucky", 0, 100, 10, 2, 0, 10000,
            m => m.maxLucky, (m, v) => m.maxLucky = v).Setting;
        MinLuckySetting = CreateLiveFloatSetting("Min Lucky", -100, 100, -10, 2, -10000, 10000,
            m => m.minLucky, (m, v) => m.minLucky = v).Setting;
        LuckyShowSetting = CreateLiveBoolSetting("Lucky Show", false,
            m => m.luckyShow, (m, v) => m.luckyShow = v).Setting;
        PityThresholdSetting = CreateLiveFloatSetting("Pity Threshold", 0, 100, 10, 0, 0, 10000,
            m => m.pityThreshold, (m, v) => m.pityThreshold = (int)v).Setting;
        PityEnabledSetting = CreateLiveBoolSetting("Pity Enabled", true,
            m => m.pityEnabled, (m, v) => m.pityEnabled = v).Setting;
        superQualitativeLive = CreateLiveBoolSetting("Super Qualitative", false,
            m => m.SuperQualitative, null);
        SuperQualitativeSetting = superQualitativeLive.Setting;

        AddSettings(LuckySetting, LuckyMultiplierSetting, MaxLuckySetting, MinLuckySetting, LuckyShowSetting, PityThresholdSetting, PityEnabledSetting, SuperQualitativeSetting);
        EndCategory();

        CreateCategory("Level & Modes");
        RandomTypeSetting = CreateLiveSelectSetting("Random Zombie Type", typeof(RandomZombieType), (int)RandomZombieType.Default,
            m => (int)ShootingManager.randomType, (m, v) => ShootingManager.randomType = (RandomZombieType)v).Setting;
        Scene1Setting = CreateLiveMultiSelectSetting("Scene 1", typeof(SceneType), (int)SceneType.Day,
            m => (int)m.scene1, (m, v) => m.scene1 = (SceneType)v, 120f).Setting;
        Scene2Setting = CreateLiveMultiSelectSetting("Scene 2", typeof(SceneType), (int)SceneType.Pool,
            m => (int)m.scene2, (m, v) => m.scene2 = (SceneType)v, 120f).Setting;
        Scene3Setting = CreateLiveMultiSelectSetting("Scene 3", typeof(SceneType), (int)SceneType.NormalBeach,
            m => (int)m.scene3, (m, v) => m.scene3 = (SceneType)v, 120f).Setting;
        MaxStageSetting = CreateLiveFloatSetting("Max Stage", 1, 3, 3, 0, 1, 3,
            m => m.maxStage, (m, v) => m.maxStage = (int)v).Setting;
        StageSetting = CreateLiveFloatSetting("Current Stage", 1, 3, 1, 0, 1, 10,
            m => m.stage, (m, v) => m.stage = (int)v).Setting;
        DifficultySetting = CreateLiveFloatSetting("Difficulty", 0, 10, 0, 0, 0, 100,
            m => m.difficulty, (m, v) => m.difficulty = (int)v).Setting;
        EndlessSetting = CreateLiveBoolSetting("Endless Mode", false,
            m => m.endless, (m, v) => m.endless = v).Setting;
        ContinuousMoveSetting = CreateLiveBoolSetting("Continuous Move", false,
            m => m.continuousMove, (m, v) => m.continuousMove = v).Setting;
        CanTabSetting = CreateLiveBoolSetting("Can Tab", false,
            m => m.canTab, (m, v) => m.canTab = v).Setting;
        CheatHardSetting = CreateLiveBoolSetting("Cheat Hard", false,
            m => m.cheatHard, (m, v) => m.cheatHard = v).Setting;
        HellModeSetting = CreateLiveBoolSetting("Hell Mode", false,
            m => m.hellMode, (m, v) => m.hellMode = v).Setting;
        SuperUpgradeSetting = CreateLiveBoolSetting("Super Upgrade", false,
            m => m.superUpgrade, (m, v) => m.superUpgrade = v).Setting;
        GodCoinRewardsSetting = CreateLiveFloatSetting("God Coin Rewards", 0, 100000, 0, 0, 0, 9999999,
            GetGodCoinRewards, SetGodCoinRewards).Setting;

        AddSettings(
            RandomTypeSetting,
            Scene1Setting,
            Scene2Setting,
            Scene3Setting,
            MaxStageSetting,
            StageSetting,
            DifficultySetting,
            EndlessSetting,
            ContinuousMoveSetting,
            CanTabSetting,
            CheatHardSetting,
            HellModeSetting,
            SuperUpgradeSetting,
            GodCoinRewardsSetting
        );
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        if (RandomTypeSetting != null)
            RandomTypeSetting.CustomNames = TranslateEnum(typeof(RandomZombieType));
        if (Scene1Setting != null)
            Scene1Setting.CustomNames = TranslateEnum(typeof(SceneType));
        if (Scene2Setting != null)
            Scene2Setting.CustomNames = TranslateEnum(typeof(SceneType));
        if (Scene3Setting != null)
            Scene3Setting.CustomNames = TranslateEnum(typeof(SceneType));
    }

    public override void OnEnable()
    {
        foreach (var ls in allLiveSettings)
        {
            ls.SetDisabled(false);
            ls.IsLocked = false;
        }

        if (ShootingManager.Instance != null)
        {
            CaptureOriginals(ShootingManager.Instance);
            SyncUnlockedFromGame();
        }
    }

    public override void OnDisable()
    {
        if (ShootingManager.Instance != null)
        {
            RestoreOriginals(ShootingManager.Instance);
        }

        foreach (var ls in allLiveSettings)
        {
            ls.IsLocked = false;
            ls.SetDisabled(true);
            ls.ResetSlider();
        }
    }

    public override void OnUpdateActive()
    {
        if (ShootingManager.Instance == null) return;

        foreach (var ls in allLiveSettings)
        {
            if (ls.IsLocked)
            {
                ls.ApplyToGame(ShootingManager.Instance);
            }
        }
    }

    public override void OnSettingOpened() => SyncUnlockedFromGame();
    public override void OnSettingUpdate() => SyncUnlockedFromGame();
}
