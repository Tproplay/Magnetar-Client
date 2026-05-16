using HarmonyLib;
using Il2CppZenGarden;

namespace Magnetar_Client.Modules
{
    public class AutoCollectGiftBoxes : Module
    {
        // Mod Info

        public override string Name { get; set; } = "Collect GiftBox";
        public override string Description { get; set; } = "Collects any gitbox that apprears in the game.";
        public override string SearchHints { get; set; } = "collectgiftbox giftboxcollector gitbox giftboxcollect getgiftbox giftboxpicker " +
            "autocollectgiftbox giftboxes giftboxs giftboxget gitboxcollect giftboxget giftboxer giftbox-collect giftbix giftvox giftboxe" +
            " giftbx collectgift giftboxauto gift-box giftcollect boxcollect giftboxhunter giftboxgrabber giftboxgrab giftgraber giftgrab " +
            "giftbux";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data

        public static AutoCollectGiftBoxes instance;

        public AutoCollectGiftBoxes() { instance = this; }

        // Mod Logic

        [HarmonyPatch(typeof(GardenPrize))]
        public static class GardenPrizeCollectPatch
        {
            [HarmonyPatch(nameof(GardenPrize.Awake))]
            [HarmonyPostfix]
            public static void Postfix(GardenPrize __instance)
            {
                if (instance == null || !instance.Active) return;

                __instance.Active();
            }
        }
    }
}