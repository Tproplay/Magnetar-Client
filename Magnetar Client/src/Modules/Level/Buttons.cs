using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Modules
{
    public class RePickPlants : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Repick Plants";
        public override string Description { get; set; } = "Allows you to repick your plants mid-game.";
        public override string SearchHints { get; set; } = "repickplants reselectplants changeplants midgameplants" +
            " plantswitch plantrepick reselect seedbankreset pickagain repickseed re-pick changeseed midgameselector " +
            "plantchange plantmenu seedreselect selectagain reselectseeds plantselection repickmode changeplant" +
            " midgamepick resetplants repickselection seedsrepick re-select repick plantreselector";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static RePickPlants instance;

        public GameObject showCardsObj;
        public GameObject referenceBtnObj;

#if DEBUG
        public BoolSetting DebugMode;
#endif

        public bool isDefaultEnabled = false;
        public bool enabledByMod = false;

        public RePickPlants()
        {
            instance = this;

#if DEBUG
            DebugMode = new BoolSetting("Debug Mode", false);
            AddSettings(DebugMode);
#endif

        }

        public void ResetState()
        {
#if DEBUG
            if (DebugMode.Value && Active)
                DebugLogger.Msg("[RePickPlants] Resetting state (Level ended or restarted).");
#endif
            showCardsObj = null;
            referenceBtnObj = null;
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
                    if (bag.gameObject.transform.parent != null && bag.gameObject.transform.parent.name == "LeftButtons")
                    {
                        showCardsObj = bag.gameObject;
                        isDefaultEnabled = showCardsObj.activeSelf;
                        break;
                    }
                }
            }

            if (referenceBtnObj == null && showCardsObj != null)
            {
                Transform parent = showCardsObj.transform.parent;

                Transform siblingBtn = parent.Find("BackToMainMenu");
                if (siblingBtn == null) siblingBtn = parent.Find("SlowTrigger");

                if (siblingBtn != null)
                {
                    referenceBtnObj = siblingBtn.gameObject;
#if DEBUG
                    if (DebugMode.Value)
                        DebugLogger.Msg($"[RePickPlants] Turned on the ShowCards button!");
#endif
                }
            }
        }

        // --- Mod Logic ---
        public override void OnUpdateActive()
        {
            if (showCardsObj == null || referenceBtnObj == null)
            {
                CaptureReferences();
                if (showCardsObj == null || referenceBtnObj == null) return;
            }

            if (referenceBtnObj.activeInHierarchy)
            {
                if (!showCardsObj.activeSelf)
                {
#if DEBUG
                    if (DebugMode.Value)
                        DebugLogger.Msg("[RePickPlants] Other buttons appeared. Activating ShowCards button.");
#endif
                    showCardsObj.SetActive(true);
                    enabledByMod = true;
                }
            }
            else
            {
                if (enabledByMod && showCardsObj.activeSelf)
                {
#if DEBUG
                    if (DebugMode.Value)
                        DebugLogger.Msg("[RePickPlants] Other buttons hid. Hiding ShowCards button.");
#endif
                    showCardsObj.SetActive(false);
                    enabledByMod = false;
                }
            }
        }

        public override void OnDisable()
        {
#if DEBUG
            if (DebugMode.Value)
                DebugLogger.Msg("[RePickPlants] Mod disabled");
#endif
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
