using HarmonyLib;
using System.Collections.Generic;
using static Magnetar_Client.Game.AppData;

using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

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

        // Fast O(1) Cache for active card instances
        public static readonly HashSet<CardUI> ActiveCards = new HashSet<CardUI>();

        public BanPlant()
        {
            instance = this;

            CreateCategory("General");

            selectedPlants = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                CustomNames = TranslatedNames(typeof(PlantType)),
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268,
                    246, 247, 3000
                },
                OnSelectionChanged = UpdateSelection
            };

            AddSettings(selectedPlants);
            EndCategory();
        }

        public override void OnLanguageChanged()
        {
            selectedPlants.CustomNames = TranslatedNames(typeof(PlantType));
        }

        private void UpdateSelection(int id, bool val)
        {
            if (!Active) return;

            // Iterate only over cached active cards without scene traversal
            foreach (var card in ActiveCards)
            {
                if (card != null && (int)card.thePlantType == id)
                {
                    BanCard(card, val);
                }
            }
        }

        public override void OnEnable()
        {
            ApplyAllCards(true);
        }

        public override void OnDisable()
        {
            ApplyAllCards(false);
        }

        private void ApplyAllCards(bool isEnabling)
        {
            foreach (var card in ActiveCards)
            {
                if (card == null) continue;

                bool shouldBan = isEnabling && selectedPlants.IsSelected((int)card.thePlantType);
                BanCard(card, shouldBan);
            }
        }

        public static void BanCard(CardUI card, bool value)
        {
            if (card == null) return;

            var shadow = card.transform.Find("Shadow");
            if (shadow != null)
            {
                shadow.gameObject.SetActive(value);
            }
        }

        // ==========================================
        // Harmony Patches
        // ==========================================

        [HarmonyPatch(typeof(CardUI))]
        public static class CardUIPatches
        {
            // Register card into cache on creation and apply ban state immediately
            [HarmonyPatch(nameof(CardUI.Awake))]
            [HarmonyPostfix]
            public static void AwakePostfix(CardUI __instance)
            {
                if (__instance == null) return;

                ActiveCards.Add(__instance);

                if (instance != null && instance.Active)
                {
                    bool shouldBan = instance.selectedPlants.IsSelected((int)__instance.thePlantType);
                    BanCard(__instance, shouldBan);
                }
            }

            // Deregister card from cache when destroyed
            [HarmonyPatch(nameof(CardUI.OnDestroy))]
            [HarmonyPrefix]
            public static void OnDestroyPrefix(CardUI __instance)
            {
                if (__instance != null)
                {
                    ActiveCards.Remove(__instance);
                }
            }

            // Block click execution on banned cards
            [HarmonyPatch(nameof(CardUI.OnMouseDown))]
            [HarmonyPrefix]
            public static bool OnMouseDownPrefix(CardUI __instance)
            {
                if (__instance == null || instance == null || !instance.Active) return true;

                if (instance.selectedPlants.IsSelected((int)__instance.thePlantType))
                {
                    GameAPP.PlaySound(SoundType.Buzzer);
                    return false;
                }

                return true;
            }
        }
    }
}
