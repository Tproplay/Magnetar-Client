using HarmonyLib;

using Magnetar_Client.UI.Setting;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppZenGarden;
#elif BEPINEX || RELEASE_BEPINEX
using ZenGarden;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Modules;

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
    public override bool Active { get; set; } = true;
    public override bool defaultActive { get; } = true;
    public override bool enableInVanillaMode { get; set; } = true;

    // Mod Data

    public static AutoCollect instance;

    public MultiSelectSetting selectedItems;


    public AutoCollect()
    {
        instance = this;

        CreateCategory("General");

        selectedItems = new MultiSelectSetting("Items")
        {
            Options = new Dictionary<int, string>
            {
                { 0, "Gift Box" },
                { 1, "Trophy" }
            },
            SelectedValues = new HashSet<int> { 0 },
        };
        selectedItems.SetCurrentAsDefault();

        AddSettings(selectedItems);
        EndCategory();
    }

    // Mod Logic

    public override void OnEnable()
    {
        if (selectedItems.IsSelected(0))
        {
            var objects = UnityEngine.Object.FindObjectsOfType<GardenPrize>();
            foreach (GardenPrize obj in objects)
            {
                if (obj != null) obj.Active();
            }
        }

        if (selectedItems.IsSelected(1))
        {
            var objects = UnityEngine.Object.FindObjectsOfType<PrizeMgr>();
            foreach (PrizeMgr obj in objects)
            {
                if (obj != null)
                {
                    CoroutineManager.Start(AutoTrophyCollector.WaitAndCollectTrophy(obj));
                }
            }
        }
    }


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
            {
                CoroutineManager.Start(WaitAndCollectTrophy(__instance));
            }
        }

        public static System.Collections.IEnumerator WaitAndCollectTrophy(PrizeMgr trophyInstance)
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
                    DebugLogger.Error($"[AutoCollect] Failed to auto-click Trophy: {ex}");
                    trophyInstance.isClicked = true;
                }
            }
        }
    }
}