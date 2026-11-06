namespace AdvancedDifficulty.Config;

/// <summary>
/// Immutable snapshot of difficulty modifiers for O(1) hot-path lookups.
/// </summary>
public struct DifficultyProfile
{
    public int DifficultyLevel;
    public string Name;
    public float ZombieSpeedMultiplier;
    public float DamageReduction; // 0.0 to 1.0
    public float ZombieSpawnMultiplier;
    public float CharredThreshold; // 0.0 to 1.0
    public bool CharredIgnoreArmor;
    public float FirstWaveArrivalTime;
    public float WaveInterval;
    public float ConveyerInterval;
    public float OdysseyHealthMultiplier;
    public float HypnosisResistance; // 0.0 to 100.0 percentage
    public float EliteZombieBuffMultiplier;

    public static DifficultyProfile CreateDefault(int difficulty)
    {
        var profile = new DifficultyProfile
        {
            DifficultyLevel = difficulty,
            Name = null,
            ZombieSpeedMultiplier = -1f,
            DamageReduction = 0f,
            ZombieSpawnMultiplier = -1f,
            CharredThreshold = -1f,
            CharredIgnoreArmor = false,
            FirstWaveArrivalTime = -1f,
            WaveInterval = -1f,
            ConveyerInterval = -1f,
            OdysseyHealthMultiplier = difficulty < 5 ? 1f : 1.5f,
            HypnosisResistance = difficulty >= 5 ? 50f : 0f,
            EliteZombieBuffMultiplier = 1f
        };

        if (difficulty == 4)
        {
            profile.DamageReduction = 0.3f;
            profile.CharredThreshold = 0.7f;
        }
        else if (difficulty == 5)
        {
            profile.DamageReduction = 0.6f;
            profile.CharredThreshold = 0.4f;
            profile.HypnosisResistance = 50f;
        }
        else if (difficulty == 6)
        {
            profile.Name = "Skin Difficulty";
            profile.WaveInterval = 3f;
            profile.FirstWaveArrivalTime = 10f;
            profile.ZombieSpeedMultiplier = 1.4f;
            profile.DamageReduction = 0.6f;
            profile.CharredThreshold = 0.4f;
            profile.HypnosisResistance = 50f;
            profile.OdysseyHealthMultiplier = 1.5f;
        }
        else if (difficulty > 6)
        {
            profile.Name = $"Difficulty {difficulty}";
            profile.DamageReduction = 0.6f;
            profile.CharredThreshold = 0.4f;
            profile.HypnosisResistance = 50f;
            profile.OdysseyHealthMultiplier = 1.5f;
        }

        return profile;
    }
}
