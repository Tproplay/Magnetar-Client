using HarmonyLib;
using static Magnetar_Client.Utils.Magnetar_Logger;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class UnlimitedModifierReroll : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimited Modifier Reroll";
        public override string Description { get; set; } = "Allows unlimited rerolls of modifiers";
        public override string SearchHints { get; set; } = "unlimitedmodifierreroll modifierreroll infinite reroll" +
            " buffs rerollbuffs buffreroll rerollmods modreroll modreroller unlimitedreroll infinitereoll " +
            "rerollunlimited randombuff rerollability rerolloptions buffmod rerollcheat betterbuffs buffrandomizer " +
            "freeroll endlessreroll modchange rerollpower rerollstats rerollfix modifierchange rerollanybuff" +
            " infinitestats modrerollcheat";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static UnlimitedModifierReroll instance;
        public static MultipleChoiceMenu multipleChoiceMenu;
        public static TravelRefresh travelRefresh;
        public static TravelStore travelStore;

        public IntSetting rerollCount;
        private int originalRerollCount = -1;
        public BoolSetting preserveOriginal;

#if MELONLOADER || BEPINEX
        public BoolSetting DebugMode;
#endif

        public UnlimitedModifierReroll()
        {
            instance = this;

            CreateCategory("General");

            rerollCount = new IntSetting("Reroll Count", 0, 50, 50, 0);
            preserveOriginal = new BoolSetting("Preserve Original Count", true);

            Settings.Add(rerollCount);
            Settings.Add(preserveOriginal);

            EndCategory();

#if MELONLOADER || BEPINEX
            DebugMode = new BoolSetting("Debug Mode", false);
            AddSettings(DebugMode);
#endif

        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (multipleChoiceMenu != null)
            {
                if (originalRerollCount == -1)
                {
                    originalRerollCount = multipleChoiceMenu.refreshCount;
#if MELONLOADER || BEPINEX
                    if (DebugMode.Value)
                        DebugLogger.Msg("[Unlimited Modifier] OriginalRerollCount (MultipleChoiceMenu) set to " + originalRerollCount);
#endif
                }

                if (multipleChoiceMenu.refreshCount != rerollCount.Value - 1)
                {
#if MELONLOADER || BEPINEX
                    if (DebugMode.Value)
                        DebugLogger.Msg($"[Unlimited Modifier] Forcing MultipleChoiceMenu count from {multipleChoiceMenu.refreshCount} to {rerollCount.Value - 1}");
#endif
                    multipleChoiceMenu.refreshCount = rerollCount.Value;
                }
            }

            if (travelRefresh != null)
            {
                if (originalRerollCount == -1)
                {
                    originalRerollCount = travelRefresh.refreshTimes;
#if MELONLOADER || BEPINEX
                    if (DebugMode.Value)
                        DebugLogger.Msg("[Unlimited Modifier] OriginalRerollCount (TravelRefresh) set to " + originalRerollCount);
#endif
                }

                if (travelRefresh.refreshTimes != rerollCount.Value)
                {
#if MELONLOADER || BEPINEX
                    if (DebugMode.Value)
                        DebugLogger.Msg($"[Unlimited Modifier] Forcing TravelRefresh count from {travelRefresh.refreshTimes} to {rerollCount.Value}");
#endif
                    travelRefresh.SetRefrashTimes(rerollCount.Value);
                    travelRefresh.UpdateText();
                }
            }

            if (travelStore != null)
            {
                if (originalRerollCount == -1)
                {
                    originalRerollCount = travelStore.refreshCount;
#if MELONLOADER || BEPINEX
                    if (DebugMode.Value)
                        DebugLogger.Msg("[Unlimited Modifier] OriginalRerollCount (TravelStore) set to " + originalRerollCount);
#endif
                }

                if (travelStore.refreshCount > 0)
                {
#if MELONLOADER || BEPINEX
                    if (DebugMode.Value)
                        DebugLogger.Msg($"[Unlimited Modifier] Forcing TravelStore count from {travelStore.refreshCount} to 0 to prevent cost scaling.");
#endif
                    travelStore.refreshCount = 0;
                }
            }
        }

        public override void OnDisable()
        {
#if MELONLOADER || BEPINEX
            if (DebugMode.Value) DebugLogger.Msg("[Unlimited Modifier] Mod disabled.");
#endif
            if (preserveOriginal.Value && originalRerollCount != -1)
            {
#if MELONLOADER || BEPINEX
                if (DebugMode.Value) DebugLogger.Msg($"[Unlimited Modifier] Restoring original reroll count: {originalRerollCount}");
#endif
                if (multipleChoiceMenu != null)
                    multipleChoiceMenu.refreshCount = originalRerollCount;
                if (travelRefresh != null)
                    travelRefresh.SetRefrashTimes(originalRerollCount);
                if (travelStore != null)
                    travelStore.refreshCount = originalRerollCount;
            }
            originalRerollCount = -1;
        }


        [HarmonyPatch(typeof(MultipleChoiceMenu))]
        public static class MultipleChoiceMenuPatch
        {
            [HarmonyPatch(nameof(MultipleChoiceMenu.Start))]
            [HarmonyPostfix]
            public static void OnEnablePostfix(MultipleChoiceMenu __instance)
            {
                if (__instance == null) return;
                multipleChoiceMenu = __instance;
#if MELONLOADER || BEPINEX
                if (instance == null || !instance.DebugMode.Value) return;
                DebugLogger.Msg("[Unlimited Modifier] Found and registered MultipleChoiceMenu instance");
#endif
            }

            [HarmonyPatch(nameof(MultipleChoiceMenu.OnSelect))]
            [HarmonyPostfix]
            public static void OnSelectPostfix()
            {
#if MELONLOADER || BEPINEX
                if (instance != null && instance.DebugMode.Value) DebugLogger.Msg("[Unlimited Modifier] MultipleChoiceMenu selection made. Clearing instance.");
#endif
                multipleChoiceMenu = null;
            }

            [HarmonyPatch(nameof(MultipleChoiceMenu.Cancel))]
            [HarmonyPostfix]
            public static void CancelPostfix()
            {
#if MELONLOADER || BEPINEX
                if (instance != null && instance.DebugMode.Value) DebugLogger.Msg("[Unlimited Modifier] MultipleChoiceMenu cancelled. Clearing instance.");
#endif
                multipleChoiceMenu = null;
            }

            [HarmonyPatch(nameof(MultipleChoiceMenu.SetRefreshable))]
            [HarmonyPostfix]
            public static void SetRefreshablePostfix(ref int refreshCount)
            {
                if (instance == null || !instance.Active) return;

#if MELONLOADER || BEPINEX
                if (instance.DebugMode.Value) DebugLogger.Msg($"[Unlimited Modifier] Intercepting SetRefreshable. Changing {refreshCount} to {instance.rerollCount.Value}");
#endif
                refreshCount = instance.rerollCount.Value;
            }
        }

        [HarmonyPatch(typeof(TravelRefresh))]
        public static class TravelRefreshPatch
        {
            [HarmonyPatch(nameof(TravelRefresh.Awake))]
            [HarmonyPostfix]
            public static void RefreshPostfix(TravelRefresh __instance)
            {
#if MELONLOADER || BEPINEX
                if (instance != null && instance.DebugMode.Value) DebugLogger.Msg("[Unlimited Modifier] Found and registered TravelRefresh instance.");
#endif
                travelRefresh = __instance;
            }

            [HarmonyPatch(nameof(TravelRefresh.ModifyRefrashTimes))]
            [HarmonyPrefix]
            public static bool ModifyRefreshTimesPrefix()
            {
                if (instance != null && instance.Active)
                {
#if MELONLOADER || BEPINEX
                    if (instance.DebugMode.Value) DebugLogger.Msg("[Unlimited Modifier] Blocked game from manually modifying TravelRefresh times.");
#endif
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(TravelStore))]
        public static class TravelStorePatch
        {
            [HarmonyPatch(nameof(TravelStore.Awake))]
            [HarmonyPostfix]
            public static void AwakePostfix(TravelStore __instance)
            {
#if MELONLOADER || BEPINEX
                if (instance != null && instance.DebugMode.Value) DebugLogger.Msg("[Unlimited Modifier] Found and registered TravelStore instance (Awake).");
#endif
                travelStore = __instance;
            }

            [HarmonyPatch(nameof(TravelStore.Start))]
            [HarmonyPostfix]
            public static void StartPostfix(TravelStore __instance)
            {
#if MELONLOADER || BEPINEX
                if (instance != null && instance.DebugMode.Value && travelStore == null) DebugLogger.Msg("[Unlimited Modifier] Found and registered TravelStore instance (Start).");
#endif
                travelStore = __instance;
            }

            [HarmonyPatch(nameof(TravelStore.Exit))]
            [HarmonyPostfix]
            public static void ExitPostfix()
            {
#if MELONLOADER || BEPINEX
                if (instance != null && instance.DebugMode.Value) DebugLogger.Msg("[Unlimited Modifier] TravelStore exited. Clearing instance.");
#endif
                travelStore = null;
            }
        }
    }
}