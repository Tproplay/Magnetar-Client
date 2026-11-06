using HarmonyLib;
using UnityEngine;
using AdvancedDifficulty.Config;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#elif BEPINEX || RELEASE_BEPINEX
#endif

namespace AdvancedDifficulty.Patches.Gameplay;

[HarmonyPatch(typeof(Board))]
public static class BoardDifficultyPatch
{
    [HarmonyPatch(nameof(Board.SetUniqueLevel))]
    [HarmonyPrefix]
    public static void SetUniqueLevelPrefix(out int __state)
    {
        __state = -1;
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return;

        if (GameAPP.config != null)
        {
            __state = GameAPP.config.difficulty;
        }
    }

    [HarmonyPatch(nameof(Board.SetUniqueLevel))]
    [HarmonyPostfix]
    public static void SetUniqueLevelPostfix(Board __instance, int __state)
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return;
        if (GameAPP.config == null) return;

        if (GameAPP.theBoardType == LevelType.SkinLevel)
        {
            if (AdvancedDifficulties.instance.AllowSkinDifficultychange.Value && __state != -1)
            {
                GameAPP.config.difficulty = __state;
                __instance.lockedDifficulty = -1;
            }
        }

        var config = DifficultyConfigManager.GetBoardConfig(GameAPP.config.difficulty);

        if (config.conveyInterval > 0) __instance.config.conveyInterval = config.conveyInterval;
        if (config.firstWaveArrivedTimer > 0) __instance.config.firstWaveArrivedTimer = config.firstWaveArrivedTimer;
        if (config.zombieSpeedMultiplier > 0) __instance.config.zombieSpeedMultiplier *= config.zombieSpeedMultiplier;
        if (config.waveInterval > 0) __instance.config.waveInterval = config.waveInterval;
        if (config.zombieCountMultiplier > 0) __instance.config.zombieCountMultiplier = config.zombieCountMultiplier;
    }
}

[HarmonyPatch(typeof(InitZombieList))]
public static class InitZombieListPatch
{
    [HarmonyPatch(nameof(InitZombieList.InitZombie))]
    [HarmonyPostfix]
    public static void InitZombiePostfix()
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return;
        if (GameAPP.config == null) return;

        int diff = GameAPP.config.difficulty;
        var profile = DifficultyConfigManager.GetProfile(diff);
        if (profile.ZombieSpawnMultiplier > 0f)
        {
            InitZombieList.multiplier = Mathf.Max(1, Mathf.RoundToInt(InitZombieList.multiplier * profile.ZombieSpawnMultiplier));
        }
    }
}
