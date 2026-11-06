using HarmonyLib;
using UnityEngine;
using AdvancedDifficulty.Config;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#elif BEPINEX || RELEASE_BEPINEX
#endif

namespace AdvancedDifficulty.Patches.Gameplay;

[HarmonyPatch(typeof(Zombie))]
public static class ZombieDamagePatch
{
    [HarmonyPatch(nameof(Zombie.Charred))]
    [HarmonyPrefix]
    public static bool CharredPrefix(Zombie __instance, int damage, PlantType reportType, bool fix, DamageType damageType)
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return true;

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

        if (DifficultyConfigManager.TryGetCharredThreshold(difficulty, out float threshold, out bool ignoreArmor))
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
        else if (difficulty >= 5)
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
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return;
        if (GameAPP.config == null) return;

        int difficulty = GameAPP.config.difficulty;

        // Base damage normalization
        long baseDmg;
        if (difficulty == 4) baseDmg = (long)(__result / 0.7f);
        else if (difficulty == 5) baseDmg = (long)(__result / 0.4f);
        else baseDmg = __result;

        float reduction = DifficultyConfigManager.GetDamageReduction(difficulty);
        __result = (long)(baseDmg * (1f - reduction));
    }
}
