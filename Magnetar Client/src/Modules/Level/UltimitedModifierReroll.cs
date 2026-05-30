using HarmonyLib;
using Il2Cpp;

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

        public IntSetting rerollCount;
        private int originalRerollCount = -1;
        public BoolSetting preserveOriginal;


        public UnlimitedModifierReroll() 
        { 
            instance = this;

            rerollCount = new IntSetting("Reroll Count", 0, 99, 99);
            preserveOriginal = new BoolSetting("Preserve Original Count", true);

            Settings.Add(rerollCount);
            Settings.Add(preserveOriginal);

        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (multipleChoiceMenu == null) return;

            if (originalRerollCount == -1)
            {
                originalRerollCount = multipleChoiceMenu.refreshCount;
            }

            if (multipleChoiceMenu.refreshCount != rerollCount.Value-1)
            {
                multipleChoiceMenu.refreshCount = rerollCount.Value;
            }

        }

        public override void OnDisable()
        {
            if (multipleChoiceMenu == null) return;
            if (preserveOriginal.Value && originalRerollCount != -1)
            {
                multipleChoiceMenu.refreshCount = originalRerollCount;
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
            }
            [HarmonyPatch(nameof(MultipleChoiceMenu.OnSelect))]
            [HarmonyPostfix]
            public static void OnSelectPostfix()
            {
                multipleChoiceMenu = null;
            }

            [HarmonyPatch(nameof(MultipleChoiceMenu.Cancel))]
            [HarmonyPostfix]
            public static void CancelPostfix()
            {
                multipleChoiceMenu = null;
            }

            [HarmonyPatch(nameof(MultipleChoiceMenu.SetRefreshable))]
            [HarmonyPostfix]
            public static void SetRefreshablePostfix(ref int refreshCount)
            {
                if (instance == null || !instance.Active) return;

                refreshCount = instance.rerollCount.Value;
            }

        }
    }
}