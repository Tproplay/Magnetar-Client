using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.Modules
{
    public class BanPlant : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Ban Plants";
        public override string Description { get; set; } = "Bans the selected Plant(s) in the seed selection phase.";
        public override string SearchHints { get; set; } = "banplants plantban seedban banseed disableplants " +
            "removeplants plantremoval plantblock blockplants seedblock banplant plantbanmod seedfilter" +
            " filterplants disableseed blockseed plantselectionban banlist plantblacklist banselections " +
            "seedbanmod banplantsmod plantblocker banselected removeplant seedlimit banplantselect banfeature";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static BanPlant instance;

        public MultiSelectSetting selectedPlants;

        public BanPlant() 
        { 
            instance = this;

            CreateCategory("General");

            selectedPlants = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                CustomNames = TranslatedNames(typeof(PlantType)),
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,3000,
                }
            };

            Settings.Add(selectedPlants);

            EndCategory();

        }

        public override void OnLanguageChanged()
        {
            selectedPlants.CustomNames = TranslatedNames(typeof(PlantType));
        }

        // Mod Logic

        [HarmonyPatch(typeof(CardUI), nameof(CardUI.OnMouseDown))]
        public static class CardUIClickPatch
        {
            public static bool Prefix(CardUI __instance)
            {
                if (__instance == null || __instance.Pointer == System.IntPtr.Zero) return true;

                if (instance == null || !instance.Active) return true;

                if (instance.selectedPlants.IsSelected((int)__instance.thePlantType))
                {
                    return false;
                }

                return true;
            }
        }

        private static readonly Color BannedColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        [HarmonyPatch(typeof(CardUI), nameof(CardUI.Awake))]
        public static class CardUIVisualLockout
        {
            public static void Postfix(CardUI __instance)
            {
                // Safety Check
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                if (instance == null || !instance.Active) return;

                if (instance.selectedPlants.IsSelected((int)__instance.thePlantType))
                {
                    // 1. Force the internal game flag for disabled cards
                    __instance.disabled = true;

                    // 2. Desaturate the Card visuals
                    var renderers = __instance.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sr in renderers)
                    {
                        sr.color = BannedColor;
                    }

                    // 3. Update the text
                    if (__instance.text != null)
                    {
                        __instance.text.color = Color.red;
                        __instance.text.text = "X";
                    }
                }
            }
        }
    }
}
