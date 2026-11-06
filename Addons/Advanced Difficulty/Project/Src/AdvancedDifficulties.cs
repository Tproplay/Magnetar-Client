using System.Collections.Generic;
using AdvancedDifficulty.Config;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;

namespace AdvancedDifficulty;

public class AdvancedDifficulties : Module
{
    // Mod Info
    public override string Name { get; set; } = "Advanced Difficulties";
    public override string Description { get; set; } = "Allows you to modify difficulty settings.";
    public override string SearchHints { get; set; } = "advanceddifficulties advanceddifficuties advanceddifficulty customdifficulty difficultymodifier difficultysettings scaledifficulty" +
        " difficultyeditor game difficultyconfig difficultychanger hardmod easymod difficultybalancer customdifficultycustomizer" +
        " diffeditor difficultyoverride adjustmentmod challengedifficulty difficultyselector diffmanager customchallenge";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Addon;

    // Mod Data
    public static AdvancedDifficulties instance;
    public static int MaxDifficulty = 5;

    public BoolSetting AllowSkinDifficultychange;
    public IntSetting MaxDifficultySetting;

    public LabelSetting AdvancedLabel;
    public SectionSetting CustomDifficultySection;

    public AdvancedDifficulties()
    {
        instance = this;

        CreateCategory("General");

        AllowSkinDifficultychange = new("Allow changing difficulty in skin levels", true);

        MaxDifficultySetting = new("Max Game Difficulty", 0, 15, 6, 0)
        {
            OnValueChanged = (value) =>
            {
                MaxDifficulty = value;
                DifficultyConfigManager.InvalidateCache();
            },
        };

        AddSettings(AllowSkinDifficultychange, MaxDifficultySetting);
        EndCategory();

        CreateCategory("Advanced");

        AdvancedLabel = new(
            "This section allows you to register your own custom difficulty.\n" +
            "Difficulty 6 is already coded, you may overwrite the values\n" +
            "through Custom Difficulty Config.\n" +
            "On Difficulty 5:\n" +
            "   Zombie Insta-kill Threshold % = 40\n" +
            "   Odyssey Health multiplier = 1.5\n" +
            "   Hypnosis Resistance % = 50\n" +
            "On Difficulty 6:\n" +
            "   First Wave Arrival Time = 10\n" +
            "   Zombie Speed Multiplier = 1.4\n" +
            "   Wave Interval = 3");

        CustomDifficultySection = new("Custom Difficulty Config",
            (index) =>
            {
                var settings = new List<Setting>
                {
                    new IntSetting("Difficulty level", 6, 15, 7, 0) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new StringSetting("Difficulty Name", $"Custom difficulty: {index}") { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Zombie Speed Multiplier", 0, 3, -1, 3) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Damage Reduction %", 0, 100, 60, 3, 0, 100) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Zombie Spawn Multiplier", 0.2f, 5, -1, 3, 0) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Zombie Insta-kill Threshold %", 0, 100, 40, 3, 0, 100) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new BoolSetting("Insta-kill ignore armor", false) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("First Wave Arrival Time", 0, 20, -1, 3) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Wave Interval", 0, 20, -1, 3) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Odyssey Health multiplier", 1, 5, 1.5f, 3, 0.01f) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Convyer Interval", 0, 10, -1, 3) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Hypnosis Resistance %", 0, 100, 50, 3, 0, 100) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                    new FloatSetting("Elite Zombie Buff Multiplier", 0.5f, 5f, 1f, 3, 0.01f) { OnValueChanged = _ => DifficultyConfigManager.InvalidateCache() },
                };
                return settings;
            }, 0)
        {
            OnSectionsChanged = DifficultyConfigManager.InvalidateCache
        };

        AddSettings(AdvancedLabel, CustomDifficultySection);
        EndCategory();
    }

    // Mod Logic
    public override void OnEnable()
    {
        MaxDifficulty = MaxDifficultySetting.Value;
        DifficultyConfigManager.InvalidateCache();
    }

    public override void OnDisable()
    {
        MaxDifficulty = 5;
        DifficultyConfigManager.InvalidateCache();
    }

    public override void OnUpdateActive()
    {
    }

    public struct BoardConfig
    {
        public float conveyInterval;
        public float firstWaveArrivedTimer;
        public float zombieSpeedMultiplier;
        public float waveInterval;
        public float zombieCountMultiplier;
    }

    // Compatibility wrappers redirecting to DifficultyConfigManager
    public bool TryGetDifficultyName(int difficulty, out string name)
        => DifficultyConfigManager.TryGetDifficultyName(difficulty, out name);

    public bool TryGetCharredThreshold(int difficulty, out float threshold, out bool ignoreArmor)
        => DifficultyConfigManager.TryGetCharredThreshold(difficulty, out threshold, out ignoreArmor);

    public float GetTravelHealthMultiplier(int difficulty)
        => DifficultyConfigManager.GetTravelHealthMultiplier(difficulty);

    public BoardConfig GetBoardConfig(int difficulty)
        => DifficultyConfigManager.GetBoardConfig(difficulty);

    public double GetDamageReduction(int difficulty)
        => (double)DifficultyConfigManager.GetDamageReduction(difficulty);

    public float GetHypnosisResistance(int difficulty)
        => DifficultyConfigManager.GetHypnosisResistance(difficulty);

    public float GetEliteBuffMultiplier(int difficulty)
        => DifficultyConfigManager.GetEliteBuffMultiplier(difficulty);
}
