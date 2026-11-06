using HarmonyLib;
using AdvancedDifficulty.Config;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#elif BEPINEX || RELEASE_BEPINEX
#endif

namespace AdvancedDifficulty.Patches.GameModes;

[HarmonyPatch(typeof(Lawnf))]
public static class TravelModePatch
{
    [HarmonyPatch(nameof(Lawnf.SetZombieHealth))]
    [HarmonyPrefix]
    public static void SetZombieHealthPrefix(Zombie zombie, ref float ratio)
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return;
        if (GameAPP.config == null) return;

        ratio /= 1.5f;
        ratio *= DifficultyConfigManager.GetTravelHealthMultiplier(GameAPP.config.difficulty);
    }
}
