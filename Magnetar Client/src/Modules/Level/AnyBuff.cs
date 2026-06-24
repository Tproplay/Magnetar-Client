using HarmonyLib;
using System.Threading;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
namespace Magnetar_Client.Modules
{
    public class AnyBuff : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Any Buff";
        public override string Description { get; set; } = "Allows you to pick any buff from the modifiers menu";
        public override string SearchHints { get; set; } = "anybuff buffpicker selectbuff pickbuff modifierpicker " +
            "modpicker buffmenu buffselector custombuffs bufflist modlist buffmod unlockbuffs buffchoice " +
            "pickmodifiers anymodifier buffaccess modbuffs buffselection buffunlocker buffcustomizer pickanybuff" +
            " buffhack modmenu pickany";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static AnyBuff instance;


        public AnyBuff() { instance = this; }

        // Mod Logic
        bool wasAlreadyShowAll = false;
        bool changed = false;
        public override void OnUpdateActive()
        {
            if (!changed && TravelLookMenu.Instance != null)
            {
                changed = true;
                wasAlreadyShowAll = TravelLookMenu.Instance.showAll;
                TravelLookMenu.Instance.showAll = true;
            }
        }

        public override void OnDisable()
        {
            if (changed && TravelLookMenu.Instance != null)
            {
                changed = false;
                TravelLookMenu.Instance.showAll = wasAlreadyShowAll;
            }
        }

        [HarmonyPatch(typeof(TravelLookMenu))]
        public static class TravelLookMenuPatch
        {
            [HarmonyPatch(nameof(TravelLookMenu.Start))]
            [HarmonyPostfix]
            public static void StartPostfix()
            {
                if (instance == null || !instance.Active) return;
                instance.changed = false;
            }
        }



    }
}
