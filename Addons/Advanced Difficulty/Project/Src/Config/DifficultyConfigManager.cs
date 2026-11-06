using System.Collections.Generic;
using Magnetar_Client.UI.Setting;

namespace AdvancedDifficulty.Config;

/// <summary>
/// Fast O(1) lookup cache and resolution manager for active difficulty profiles.
/// Eliminates repeated UI section list scans on combat hot-paths.
/// </summary>
public static class DifficultyConfigManager
{
    private static readonly Dictionary<int, DifficultyProfile> _profileCache = new();
    private static bool _isCacheDirty = true;
    private static readonly object _lock = new();

    /// <summary>
    /// Flags the cache to be rebuilt on the next query (called on UI setting change).
    /// </summary>
    public static void InvalidateCache()
    {
        lock (_lock)
        {
            _isCacheDirty = true;
        }
    }

    private static void EnsureCache()
    {
        lock (_lock)
        {
            if (!_isCacheDirty) return;

            _profileCache.Clear();

            var instance = AdvancedDifficulties.instance;
            if (instance?.CustomDifficultySection?.Sections != null)
            {
                var sections = instance.CustomDifficultySection.Sections;
                for (int i = 0; i < sections.Count; i++)
                {
                    var section = sections[i];
                    var diffSetting = section.Find<IntSetting>("Difficulty level");
                    if (diffSetting == null) continue;

                    int diff = diffSetting.Value;
                    var profile = DifficultyProfile.CreateDefault(diff);

                    var nameSetting = section.Find<StringSetting>("Difficulty Name");
                    if (nameSetting != null && !string.IsNullOrWhiteSpace(nameSetting.Value))
                    {
                        profile.Name = nameSetting.Value;
                    }

                    var speedSetting = section.Find<FloatSetting>("Zombie Speed Multiplier");
                    if (speedSetting != null) profile.ZombieSpeedMultiplier = speedSetting.Value;

                    var dmgRedSetting = section.Find<FloatSetting>("Damage Reduction %");
                    if (dmgRedSetting != null) profile.DamageReduction = dmgRedSetting.Value / 100f;

                    var spawnSetting = section.Find<FloatSetting>("Zombie Spawn Multiplier");
                    if (spawnSetting != null) profile.ZombieSpawnMultiplier = spawnSetting.Value;

                    var charValSetting = section.Find<FloatSetting>("Zombie Insta-kill Threshold %");
                    if (charValSetting != null) profile.CharredThreshold = charValSetting.Value / 100f;

                    var armorSetting = section.Find<BoolSetting>("Insta-kill ignore armor");
                    if (armorSetting != null) profile.CharredIgnoreArmor = armorSetting.Value;

                    var firstWaveSetting = section.Find<FloatSetting>("First Wave Arrival Time");
                    if (firstWaveSetting != null) profile.FirstWaveArrivalTime = firstWaveSetting.Value;

                    var waveIntervalSetting = section.Find<FloatSetting>("Wave Interval");
                    if (waveIntervalSetting != null) profile.WaveInterval = waveIntervalSetting.Value;

                    var conveyerSetting = section.Find<FloatSetting>("Convyer Interval");
                    if (conveyerSetting != null) profile.ConveyerInterval = conveyerSetting.Value;

                    var odysseySetting = section.Find<FloatSetting>("Odyssey Health multiplier");
                    if (odysseySetting != null) profile.OdysseyHealthMultiplier = odysseySetting.Value;

                    var hypnoSetting = section.Find<FloatSetting>("Hypnosis Resistance %");
                    if (hypnoSetting != null) profile.HypnosisResistance = hypnoSetting.Value;

                    var eliteSetting = section.Find<FloatSetting>("Elite Zombie Buff Multiplier");
                    if (eliteSetting != null) profile.EliteZombieBuffMultiplier = eliteSetting.Value;

                    // Latest section overrides earlier ones for the same difficulty level
                    _profileCache[diff] = profile;
                }
            }

            _isCacheDirty = false;
        }
    }

    public static DifficultyProfile GetProfile(int difficulty)
    {
        EnsureCache();

        lock (_lock)
        {
            if (_profileCache.TryGetValue(difficulty, out var profile))
            {
                return profile;
            }
        }

        return DifficultyProfile.CreateDefault(difficulty);
    }

    public static bool TryGetDifficultyName(int difficulty, out string name)
    {
        var profile = GetProfile(difficulty);
        name = profile.Name;
        return !string.IsNullOrEmpty(name);
    }

    public static bool TryGetCharredThreshold(int difficulty, out float threshold, out bool ignoreArmor)
    {
        var profile = GetProfile(difficulty);
        threshold = profile.CharredThreshold;
        ignoreArmor = profile.CharredIgnoreArmor;
        return threshold >= 0f;
    }

    public static float GetDamageReduction(int difficulty)
    {
        return GetProfile(difficulty).DamageReduction;
    }

    public static float GetTravelHealthMultiplier(int difficulty)
    {
        return GetProfile(difficulty).OdysseyHealthMultiplier;
    }

    public static float GetHypnosisResistance(int difficulty)
    {
        return GetProfile(difficulty).HypnosisResistance;
    }

    public static float GetEliteBuffMultiplier(int difficulty)
    {
        return GetProfile(difficulty).EliteZombieBuffMultiplier;
    }

    public static AdvancedDifficulties.BoardConfig GetBoardConfig(int difficulty)
    {
        var profile = GetProfile(difficulty);
        return new AdvancedDifficulties.BoardConfig
        {
            conveyInterval = profile.ConveyerInterval,
            firstWaveArrivedTimer = profile.FirstWaveArrivalTime,
            zombieSpeedMultiplier = profile.ZombieSpeedMultiplier,
            waveInterval = profile.WaveInterval,
            zombieCountMultiplier = profile.ZombieSpawnMultiplier
        };
    }
}
