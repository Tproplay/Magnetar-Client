using HarmonyLib;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

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
        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data
        public static RePickPlants instance;

        public BoolSetting ForceShowRepickButtton;
        public GameObject showCardsObj;
        public GameObject referenceBtnObj;

        public BoolSetting AllowAllCards;
        public BoolSetting AllowUltimateCards;
#if MELONLOADER || BEPINEX
        public BoolSetting DebugMode;
#endif

        public bool isDefaultEnabled = false;
        public bool RepickenabledByMod = false;

        private bool allCardsEnabledbyMod;
        private bool ultimateCardsEnabledbyMod;

        public RePickPlants()
        {
            instance = this;

            CreateCategory("General");

            ForceShowRepickButtton = new BoolSetting("Force Show Repick button", true);
            ForceShowRepickButtton.OnValueChanged = (val) =>
            {
                if (!val) RemoveRepickButton();
            };

            AddSettings(ForceShowRepickButtton);
            EndCategory();

            CreateCategory("Card Groups");

            AllowAllCards = new BoolSetting("Allow all plant cards", false);
            AllowAllCards.OnValueChanged = (val) =>
            {
                if (!Active || SeedLibrary.Instance == null) return;
                if (val)
                {
                    if (!CheckActiveUIButton("AllCards"))
                    {
                        SeedLibrary.Instance.SetAllCards();
                        allCardsEnabledbyMod = true;
                    }
                }
                else
                {
                    if (CheckActiveUIButton("AllCards") && allCardsEnabledbyMod)
                    {
                        RemoveUIButton("AllCards");
                        allCardsEnabledbyMod = false;
                    }
                }
            };

            AllowUltimateCards = new BoolSetting("Allow Odyssey plant cards (Ensure Odyssey Plant is enabled before entering the level)", false);
            AllowUltimateCards.OnValueChanged = (val) =>
            {
                if (!Active || SeedLibrary.Instance == null) return;
                if (val)
                {
                    if (!CheckActiveUIButton("UltimateCards"))
                    {
                        SeedLibrary.Instance.SetUltimateCards();
                        ultimateCardsEnabledbyMod = true;
                    }
                }
                else
                {
                    if (CheckActiveUIButton("UltimateCards") && ultimateCardsEnabledbyMod)
                    {
                        RemoveUIButton("UltimateCards");
                        ultimateCardsEnabledbyMod = false;
                    }
                }
            };

            AddSettings(AllowAllCards, AllowUltimateCards);
            EndCategory();

#if MELONLOADER || BEPINEX
            DebugMode = new BoolSetting("Debug Mode", false);
            AddSettings(DebugMode);
#endif
        }

        // --- Mod Logic ---
        public void ResetState()
        {
#if MELONLOADER || BEPINEX
            if (DebugMode.Value && Active)
                DebugLogger.Msg("[RePickPlants] Resetting state (Level ended or restarted).");
#endif
            showCardsObj = null;
            referenceBtnObj = null;
            isDefaultEnabled = false;
            RepickenabledByMod = false;
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
#if MELONLOADER || BEPINEX
                    if (DebugMode.Value)
                        DebugLogger.Msg($"[RePickPlants] Turned on the ShowCards button!");
#endif
                }
            }
        }

        public override void OnUpdateActive()
        {
            if (ForceShowRepickButtton.Value)
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
#if MELONLOADER || BEPINEX
                        if (DebugMode.Value)
                            DebugLogger.Msg("[RePickPlants] Other buttons appeared. Activating ShowCards button.");
#endif
                        showCardsObj.SetActive(true);
                        RepickenabledByMod = true;
                    }
                }
                else
                {
                    if (RepickenabledByMod && showCardsObj.activeSelf)
                    {
#if MELONLOADER || BEPINEX
                        if (DebugMode.Value)
                            DebugLogger.Msg("[RePickPlants] Other buttons hid. Hiding ShowCards button.");
#endif
                        showCardsObj.SetActive(false);
                        RepickenabledByMod = false;
                    }
                }
            }
            else RemoveRepickButton();
        }

        private void RemoveRepickButton()
        {
            if (RepickenabledByMod && showCardsObj != null && !isDefaultEnabled)
            {
                showCardsObj.SetActive(false);
            }

            RepickenabledByMod = false;
        }

        public override void OnDisable()
        {
#if MELONLOADER || BEPINEX
            if (DebugMode.Value)
                DebugLogger.Msg("[RePickPlants] Mod disabled");
#endif
            RemoveRepickButton();

            if (SeedLibrary.Instance != null)
            {
                if (CheckActiveUIButton("AllCards") && allCardsEnabledbyMod)
                {
                    RemoveUIButton("AllCards");
                    allCardsEnabledbyMod = false;
                }

                if (CheckActiveUIButton("UltimateCards") && ultimateCardsEnabledbyMod)
                {
                    RemoveUIButton("UltimateCards");
                    ultimateCardsEnabledbyMod = false;
                }
            }
        }

        public override void OnEnable()
        {
            if (SeedLibrary.Instance != null)
            {
                if (AllowAllCards.Value && !CheckActiveUIButton("AllCards"))
                {
                    SeedLibrary.Instance.SetAllCards();
                    allCardsEnabledbyMod = true;
                }

                if (AllowUltimateCards.Value && !CheckActiveUIButton("UltimateCards"))
                {
                    SeedLibrary.Instance.SetUltimateCards();
                    ultimateCardsEnabledbyMod = true;
                }
            }
        }

        private static void RemoveUIButton(string name)
        {
            var buttons = UnityEngine.Object.FindObjectsOfType<UIButton>();
            foreach (var button in buttons)
            {
                if (button.gameObject.name == name)
                {
                    SeedLibrary.Instance.RemoveCardGroup(button);
                    Object.Destroy(button.gameObject);
                }
            }
        }

        private static bool CheckActiveUIButton(string name)
        {
            var buttons = UnityEngine.Object.FindObjectsOfType<UIButton>();
            foreach (var button in buttons)
            {
                if (button.gameObject.name == name) return true;
            }
            return false;
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

        [HarmonyPatch(typeof(SeedLibrary))]
        public static class SeedLibraryPatch
        {
            [HarmonyPatch(nameof(SeedLibrary.Start))]
            [HarmonyPostfix]
            public static void StartPostfix(SeedLibrary __instance)
            {
                if (__instance == null || instance == null || !instance.Active) return;

                if (instance.AllowAllCards.Value && !CheckActiveUIButton("AllCards")) 
                {
                    __instance.SetAllCards();
                    instance.allCardsEnabledbyMod = true;
                }
                if (instance.AllowUltimateCards.Value && !CheckActiveUIButton("UltimateCards")) 
                { 
                    __instance.SetUltimateCards();
                    instance.ultimateCardsEnabledbyMod = true;
                }
            }

        }
    }
}