using HarmonyLib;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
namespace Magnetar_Client.Modules
{
    public class NoLose : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Lose";
        public override string Description { get; set; } = "'Please turn off that' - sincerely ZomBoss";
        public override string SearchHints { get; set; } = "nolose nodefeat winalways neverlose antigameover godmode cheat " +
            "no-lose indestructible cantlose alwayswin winmod losefix stoplose no-die nodie wincheat easywin nolosemod " +
            "eternal survival unkillable endlesswin victoryalways neverdefeat flawless runwon antilose";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static NoLose instance;


        public NoLose() { instance = this; }

        // Mod Logic


        [HarmonyPatch(typeof(GameLose), nameof(GameLose.Update))]
        public static class ConstantStateLockPatch
        {
            [HarmonyPostfix]
            public static void Postfix(GameLose __instance)
            {
                if (__instance == null) return;

                if (instance == null || !instance.Active)
                {
                    // Run Game Logic Normally
                    if (!__instance.canTriggerLose)
                    {
                        __instance.canTriggerLose = true;
                    }
                    return;
                }

                // Don't Trigger lose
                __instance.canTriggerLose = false;
            }
        }
    }
}
