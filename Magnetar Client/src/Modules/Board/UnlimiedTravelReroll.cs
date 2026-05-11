using Il2Cpp;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Modules
{
    // TODO: Find where reroll count is saved.
    public class UnlimiedTravelReroll : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimied Modifier Reroll";

        public override string Description { get; set; } = "Allows You to reroll Travel buffs/modifiers infinitely.";
        public override string SearchHints { get; set; } = "unlimitedmodifierreroll unlimitedreroll infinitereroll " +
            "modifierreroll rerollbuffs rerollmodifiers travelreroll travelbuffreroll travelmodifierreroll infinitebuffreroll" +
            " unlimitedrerolls infinitererolls modifierrerolls travelmodifier travelbuff rerollingbuffs rerollingmodifiers" +
            " endlessreroll alwaysreroll rerollutility rerollfeature";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Board;

        // Mod Data

        int originalCount = -1;

#if DEBUG
        public static BoolSetting DebugMode;
        public UnlimiedTravelReroll()
        {
            DebugMode = new BoolSetting("Debug Mode", false);
            Settings.Add(DebugMode);
        }
#endif
        

        // Mod Logic

        public override void OnUpdateActive()
        {
            TravelStore __instance = TravelStore.Instance;
            if (__instance == null || Board.Instance == null) return;

            if (originalCount == -1) originalCount = __instance.refreshCount;

            __instance.refreshCount = 1;
        }

        public override void OnDisable()
        {
            TravelStore __instance = TravelStore.Instance;
            if (__instance == null || Board.Instance == null)
            {
                originalCount = -1;
                return;
            };

            __instance.refreshCount = originalCount;
            originalCount = -1;

        }

#if DEBUG
        public override void OnEnable()
        {
            TravelStore __instance = TravelStore.Instance;
            if (__instance == null || Board.Instance == null) return;

                if (DebugMode != null && DebugMode.Value)
            {
                DebugLogger.Msg("Current refreshCount: " + __instance.refreshCount);
            }
        }
#endif
    }
}
