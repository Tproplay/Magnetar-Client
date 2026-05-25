using UnityEngine;
using HarmonyLib;
using Il2Cpp;

namespace Magnetar_Client.Modules
{
    public class RePickPlants : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Repick Plants";
        public override string Description { get; set; } = "Allows you to repick you plants mid-game.";
        public override string SearchHints { get; set; } = "repickplants reselectplants changeplants midgameplants" +
            " plantswitch plantrepick reselect seedbankreset pickagain repickseed re-pick changeseed midgameselector " +
            "plantchange plantmenu seedreselect selectagain reselectseeds plantselection repickmode changeplant" +
            " midgamepick resetplants repickselection seedsrepick re-select repick plantreselector";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static RePickPlants instance;

        public GameObject showCardsObj;
        public GameObject menuBtnObj;

        public bool isDefaultEnabled = false;
        public bool enabledByMod = false;

        public RePickPlants()
        {
            instance = this;
        }

        public void ResetState()
        {
            showCardsObj = null;
            menuBtnObj = null;
            isDefaultEnabled = false;
            enabledByMod = false;
        }

        public void CaptureReferences()
        {
            if (showCardsObj == null)
            {
                var bags = UnityEngine.Resources.FindObjectsOfTypeAll<InGame_openBag>();
                foreach (var bag in bags)
                {
                    if (bag.gameObject.transform.parent != null)
                    {
                        showCardsObj = bag.gameObject;
                        isDefaultEnabled = showCardsObj.activeSelf;
                        break;
                    }
                }
            }
            if (menuBtnObj == null)
            {
                var btns = UnityEngine.Resources.FindObjectsOfTypeAll<InGameBtn>();
                foreach (var btn in btns)
                {
                    if (btn.gameObject.name == "Menu" && btn.gameObject.transform.parent != null)
                    {
                        menuBtnObj = btn.gameObject;
                        break;
                    }
                }
            }
        }

        // --- Mod Logic ---
        public override void OnUpdateActive()
        {
            if (showCardsObj == null || menuBtnObj == null)
            {
                CaptureReferences();
                if (showCardsObj == null || menuBtnObj == null) return;
            }
            if (menuBtnObj.activeInHierarchy)
            {
                if (!showCardsObj.activeSelf)
                {
                    showCardsObj.SetActive(true);
                    enabledByMod = true;
                }
            }
            else
            {
                if (enabledByMod && showCardsObj.activeSelf)
                {
                    showCardsObj.SetActive(false);
                    enabledByMod = false;
                }
            }
        }

        public override void OnDisable()
        {
            if (enabledByMod && showCardsObj != null && !isDefaultEnabled)
            {
                showCardsObj.SetActive(false);
            }

            enabledByMod = false;
        }

        [HarmonyPatch(typeof(Board))]
        public static class BoardPatches
        {
            [HarmonyPatch(nameof(Board.Awake))]
            [HarmonyPostfix]
            public static void AwakePostfix()
            {
                if (instance != null) instance.ResetState();
            }

            [HarmonyPatch(nameof(Board.Die))]
            [HarmonyPostfix]
            public static void DiePostfix()
            {
                if (instance != null) instance.ResetState();
            }
        }
    }
}
