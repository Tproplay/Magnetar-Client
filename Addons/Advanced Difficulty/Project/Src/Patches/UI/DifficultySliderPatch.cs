using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using AdvancedDifficulty.Config;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace AdvancedDifficulty.Patches.UI;

[HarmonyPatch(typeof(DifficultyMgr))]
public static class DifficultySliderPatch
{
    private static bool _isSyncing;
    private static DifficultyMgr _lastActiveMgr;

    [HarmonyPatch(nameof(DifficultyMgr.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(DifficultyMgr __instance)
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return;
        SyncSliderToConfig(__instance);
    }

    private static void SyncSliderToConfig(DifficultyMgr difficultyMgr)
    {
        if (difficultyMgr == null) return;
        Slider slider = difficultyMgr.GetComponent<Slider>();
        if (slider != null && GameAPP.config != null)
        {
            _isSyncing = true;
            slider.maxValue = AdvancedDifficulties.MaxDifficulty;
            slider.value = Mathf.Clamp(GameAPP.config.difficulty, 0, AdvancedDifficulties.MaxDifficulty);
            _isSyncing = false;
        }
    }

    [HarmonyPatch(nameof(DifficultyMgr.Update))]
    [HarmonyPrefix]
    public static bool UpdatePrefix(DifficultyMgr __instance)
    {
        if (AdvancedDifficulties.instance == null || !AdvancedDifficulties.instance.Active) return true;

        CustomUpdate(__instance);
        return false;
    }

    public static void CustomUpdate(DifficultyMgr difficultyMgr)
    {
        Slider slider = difficultyMgr.GetComponent<Slider>();
        TextMeshProUGUI label = difficultyMgr.transform.GetChild(3).GetComponent<TextMeshProUGUI>();

        if (slider == null) return;

        slider.maxValue = AdvancedDifficulties.MaxDifficulty;

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
            lockDifficulty = AdvancedDifficulties.instance.AllowSkinDifficultychange.Value ? -1 : 5;
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
            displayText = "Locked";
        }
        else
        {
            targetDifficulty = Mathf.RoundToInt(slider.value);

            if (!DifficultyConfigManager.TryGetDifficultyName(targetDifficulty, out displayText))
            {
                displayText = targetDifficulty switch
                {
                    0 => "<size=20><color=#00C853>0: Easy Mode</color></size>",
                    1 => "<size=20><color=#64DD17>1: Casual Mode</color></size>",
                    2 => "<size=20><color=#AEEA00>2: Normal Mode</color></size>",
                    3 => "<size=20><color=#FFD600>3: Veteran Mode</color></size>",
                    4 => "<size=20><color=#FF6D00>4: Merciless Mode</color></size>",
                    5 => "<size=20><color=#D50000>5: Are You Sure?</color></size>",
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
