using HarmonyLib;
using UnityEngine;
using AdvancedDifficulty.Config;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#elif BEPINEX || RELEASE_BEPINEX
#endif

namespace AdvancedDifficulty.Patches.Gameplay;

[HarmonyPatch(typeof(Zombie))]
public static class ZombieBehaviorPatch
{
    /// <summary>
    /// Intercepts mind-control attempts. If hypnosis resistance succeeds, the zombie resists being charmed.
    /// </summary>
    [HarmonyPatch(nameof(Zombie.SetMindControl))]
    [HarmonyPrefix]
    public static bool SetMindControlPrefix(Zombie __instance, int controlLevel)
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return true;
        if (GameAPP.config == null) return true;

        // Forced / ultimate mind control bypasses standard resistance checks
        if (controlLevel > 0) return true;

        int difficulty = GameAPP.config.difficulty;
        float resistance = DifficultyConfigManager.GetHypnosisResistance(difficulty);

        if (resistance > 0f)
        {
            float roll = Random.Range(0f, 100f);
            if (roll < resistance)
            {
                // Resisted hypnosis!
                return false;
            }
        }

        return true;
    }
}
