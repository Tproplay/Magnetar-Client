using HarmonyLib;
using System;
using Il2Cpp;
using Il2CppZenGarden;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.Modules
{
    public class AutoCollect : Module
    {
        // Mod Info

        public override string Name { get; set; } = "Auto Collect";
        public override string Description { get; set; } = "Collects the selected item(s) if they apprear in the game.";
        public override string SearchHints { get; set; } = "collectgiftbox giftboxcollector gitbox giftboxcollect getgiftbox giftboxpicker " +
            "autocollectgiftbox giftboxes giftboxs giftboxget gitboxcollect giftboxget giftboxer giftbox-collect giftbix giftvox giftboxe" +
            " giftbx collectgift giftboxauto gift-box giftcollect boxcollect giftboxhunter giftboxgrabber giftboxgrab giftgraber giftgrab " +
            "giftbux";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data

        public static AutoCollect instance;

        public MultiSelectSetting selectedItems;


        public AutoCollect() 
        { 
            instance = this;

            selectedItems = new MultiSelectSetting("Items")
            {
                Options = new Dictionary<int, string>
                {
                    { 0, "Gift Box" },
                    { 1, "Trophy" }
                }
            };
        }

        // Mod Logic

        [HarmonyPatch(typeof(GardenPrize))]
        public static class GardenPrizeCollectPatch
        {
            [HarmonyPatch(nameof(GardenPrize.Awake))]
            [HarmonyPostfix]
            public static void Postfix(GardenPrize __instance)
            {
                if (instance == null || !instance.Active) return;
                if (instance.selectedItems.IsSelected(0))
                    __instance.Active();
            }
        }

        [HarmonyPatch(typeof(PrizeMgr))]
        public static class AutoTrophyCollector
        {
            [HarmonyPatch(nameof(PrizeMgr.Start))]
            [HarmonyPostfix]
            public static void Postfix(PrizeMgr __instance)
            {
                if (instance == null || !instance.Active) return;
                if (instance.selectedItems.IsSelected(1))
                    MelonCoroutines.Start(WaitAndCollectTrophy(__instance));
            }

            private static System.Collections.IEnumerator WaitAndCollectTrophy(PrizeMgr trophyInstance)
            {
                while (trophyInstance != null && !trophyInstance.isLand)
                {
                    yield return new WaitForSeconds(0.1f);
                }

                if (trophyInstance == null) yield break;
                if (!trophyInstance.isClicked)
                {
                    try
                    {
                        trophyInstance.Click();
                        trophyInstance.Clicked();
                    }
                    catch (Exception ex)
                    {
                        trophyInstance.isClicked = true;
                    }
                }
            }
        }
    }
}