using HarmonyLib;
using UnityEngine;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace AdvancedDifficulty.Patches.UI;

[HarmonyPatch(typeof(UIDifficulty))]
public static class UIDiffPatch
{
    [HarmonyPatch(nameof(UIDifficulty.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(UIDifficulty __instance)
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return;

        var text = __instance.GetComponent<TextMeshProUGUI>();
        if (text == null) return;

        if (GameAPP.developerMode)
        {
            return;
        }

        if (GameAPP.theBoardType == LevelType.SkinLevel && AdvancedDifficulties.instance.AllowSkinDifficultychange.Value)
        {
            text.text = text.text.Split("：")[0] + $"：{GameAPP.config.difficulty}";
        }
    }
}

[HarmonyPatch(typeof(AdvantureMenu2))]
public static class AdvantureMenu2Patch
{
    [HarmonyPatch(nameof(AdvantureMenu2.AdjustDifficulty))]
    [HarmonyPrefix]
    public static bool AdjustDifficultyPrefix(AdvantureMenu2 __instance, int value)
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return true;
        if (AdvantureConfig.data == null) return true;

        int newDiff = AdvantureConfig.data.gameDifficulty + value;
        int maxDiff = Mathf.Max(10, AdvancedDifficulties.MaxDifficulty);
        newDiff = Mathf.Clamp(newDiff, 0, maxDiff);
        AdvantureConfig.data.gameDifficulty = newDiff;

        if (__instance.difficulty != null)
        {
            string text = $"Difficulty Level: {newDiff}\n(Click for Info)";
            if (AdvantureConfig.data.hardMode)
            {
                text += "+2";
            }
            __instance.difficulty.text = text;
        }

        return false;
    }
}
