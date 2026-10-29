using HarmonyLib;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel.RogueShooting;
using Il2CppUI;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel.RogueShooting;
using UI;
#endif

namespace Magnetar_Client.Modules;

public partial class GodsEvolvedHack
{
    public static void OnBeforeBuffMenu(ShootingManager manager)
    {
        if (instance == null || !instance.Active || manager == null) return;

        foreach (var (_, ls) in instance.qualitySettings)
        {
            if (ls.IsLocked)
            {
                ls.ApplyToGame(manager);
            }
        }
    }

    public static void OnAfterBuffMenu(ShootingManager manager)
    {
        if (instance == null || !instance.Active || manager == null) return;

        foreach (var (_, ls) in instance.qualitySettings)
        {
            if (ls.IsLocked)
            {
                ls.ApplyToGame(manager);
            }
            else
            {
                try
                {
                    ls.CaptureOriginal(manager);
                    if (instance.ShowSettings)
                        ls.SyncFromGame(manager);
                }
                catch { }
            }
        }
    }

    // ==========================================
    // Harmony Patches
    // ==========================================

    [HarmonyPatch(typeof(ShootingManager))]
    public static class ShootingManagerPatch
    {
        [HarmonyPatch(nameof(ShootingManager.Awake))]
        [HarmonyPrefix]
        public static void AwakePrefix(ShootingManager __instance)
        {
            if (instance == null || !instance.Active || __instance == null) return;

            foreach (var ls in instance.allLiveSettings)
            {
                if (ls.IsLocked)
                {
                    ls.ApplyToGame(__instance);
                }
            }
        }

        [HarmonyPatch(nameof(ShootingManager.Awake))]
        [HarmonyPostfix]
        public static void AwakePostfix(ShootingManager __instance)
        {
            if (instance == null || !instance.Active || __instance == null) return;

            foreach (var ls in instance.allLiveSettings)
            {
                if (ls.IsLocked)
                {
                    ls.ApplyToGame(__instance);
                }
            }
        }

        [HarmonyPatch(nameof(ShootingManager.Start))]
        [HarmonyPrefix]
        public static void StartPrefix(ShootingManager __instance)
        {
            if (instance == null || !instance.Active || __instance == null) return;

            foreach (var ls in instance.allLiveSettings)
            {
                if (ls.IsLocked)
                {
                    ls.ApplyToGame(__instance);
                }
            }
        }

        [HarmonyPatch(nameof(ShootingManager.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(ShootingManager __instance)
        {
            if (instance == null || __instance == null) return;

            instance.CaptureOriginals(__instance);

            if (instance.Active)
            {
                foreach (var ls in instance.allLiveSettings)
                {
                    if (ls.IsLocked)
                        ls.ApplyToGame(__instance);
                    else
                        ls.SyncFromGame(__instance);
                }
            }
            else
            {
                foreach (var ls in instance.allLiveSettings)
                    ls.SyncFromGame(__instance);
            }
        }

        [HarmonyPatch("RandomSettings")]
        [HarmonyPostfix]
        public static void RandomSettingsPostfix(ShootingManager __instance)
        {
            if (instance == null || __instance == null) return;

            if (instance.Active)
            {
                foreach (var ls in instance.allLiveSettings)
                {
                    if (ls.IsLocked)
                        ls.ApplyToGame(__instance);
                    else
                        ls.SyncFromGame(__instance);
                }
            }
            else
            {
                foreach (var ls in instance.allLiveSettings)
                    ls.SyncFromGame(__instance);
            }
        }

        [HarmonyPatch("GoNewScene")]
        [HarmonyPrefix]
        public static void GoNewScenePrefix(ShootingManager __instance)
        {
            if (instance == null || !instance.Active || __instance == null) return;

            foreach (var ls in instance.allLiveSettings)
            {
                if (ls.IsLocked)
                {
                    ls.ApplyToGame(__instance);
                }
            }
        }

        [HarmonyPatch(nameof(ShootingManager.ShowBuff))]
        [HarmonyPrefix]
        public static void ShowBuffPrefix(ShootingManager __instance) => OnBeforeBuffMenu(__instance);

        [HarmonyPatch(nameof(ShootingManager.ShowBuff))]
        [HarmonyPostfix]
        public static void ShowBuffPostfix(ShootingManager __instance) => OnAfterBuffMenu(__instance);

        [HarmonyPatch(nameof(ShootingManager.ShowExpertBuffMenu))]
        [HarmonyPrefix]
        public static void ShowExpertBuffMenuPrefix(ShootingManager __instance) => OnBeforeBuffMenu(__instance);

        [HarmonyPatch(nameof(ShootingManager.ShowExpertBuffMenu))]
        [HarmonyPostfix]
        public static void ShowExpertBuffMenuPostfix(ShootingManager __instance) => OnAfterBuffMenu(__instance);

        [HarmonyPatch(nameof(ShootingManager.SuperQualitative), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool SuperQualitativePrefix(ref bool __result)
        {
            if (instance != null && instance.Active && instance.superQualitativeLive != null && instance.superQualitativeLive.IsLocked)
            {
                __result = instance.superQualitativeLive.Setting.Value;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(ShootingManager.LuckyMultiplier), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool LuckyMultiplierPrefix(ref float __result)
        {
            if (instance != null && instance.Active && instance.luckyMultiplierLive != null && instance.luckyMultiplierLive.IsLocked)
            {
                __result = instance.luckyMultiplierLive.Setting.Value;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(ShootingManager.GetRandomQuality))]
        [HarmonyPrefix]
        public static bool GetRandomQualityPrefix(ref Quality __result, ShootingManager __instance)
        {
            if (instance == null || !instance.Active || __instance == null)
                return true;

            bool anyLocked = false;
            foreach (var (_, ls) in instance.qualitySettings)
            {
                if (ls.IsLocked) { anyLocked = true; break; }
            }

            if (!anyLocked) return true;

            float totalWeight = 0f;
            foreach (var (q, ls) in instance.qualitySettings)
            {
                float w = ls.IsLocked ? ls.Setting.Value : ls.Getter(__instance);
                if (w > 0f) totalWeight += w;
            }

            if (totalWeight <= 0f)
            {
                __result = Quality.Default;
                return false;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            foreach (var (q, ls) in instance.qualitySettings)
            {
                float w = ls.IsLocked ? ls.Setting.Value : ls.Getter(__instance);
                if (w <= 0f) continue;

                if (roll < w)
                {
                    __result = q;
                    return false;
                }
                roll -= w;
            }

            __result = Quality.Default;
            return false;
        }
    }

    [HarmonyPatch(typeof(MultipleChoiceMenu))]
    public static class MultipleChoiceMenuPatch
    {
        [HarmonyPatch(nameof(MultipleChoiceMenu.OnSelect))]
        [HarmonyPostfix]
        public static void OnSelectPostfix() => OnAfterBuffMenu(ShootingManager.Instance);

        [HarmonyPatch(nameof(MultipleChoiceMenu.Cancel))]
        [HarmonyPostfix]
        public static void CancelPostfix() => OnAfterBuffMenu(ShootingManager.Instance);
    }
}
