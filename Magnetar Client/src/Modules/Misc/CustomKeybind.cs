using HarmonyLib;
using Magnetar_Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Utils.Magnetar_Logger;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class CustomKeybind : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Custom Keybind";
        public override string Description { get; set; } = "Allows you to customize the ingame keybinds.";
        public override string SearchHints { get; set; } = "customkeybind keybinds hotkeys keybindings" +
            " setkeys changekeys keymap keymapping keysettings customkeys keybindmanager bindkeys " +
            "keyconfig keyrebind keyboardlayout keybindcustomizer keybindmod rebind keys customkey" +
            " keyremapping settingskey binds bind";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data

        public static CustomKeybind instance;

        public List<BindSetting> SeedSlot;

        public BindSetting SlowMode;
        public BindSetting ShowPlantHP;
        public BindSetting ShowZombieHP;

        public BindSetting PickUpShovel;
        public BindSetting PickUpGlove;
        public BindSetting PickUpHammer;
        public BindSetting PickUpWheel;
        public BindSetting CoffeeBean;

        public BindSetting Fullscreen;
        public BindSetting SmallWindow;

#if MELONLOADER || BEPINEX
        public BoolSetting DebugMode;
#endif
        public CustomKeybind()
        {
            instance = this;

            CreateCategory("In Game");

            PickUpShovel = new BindSetting("Pick up Shovel", new List<KeyCode> { KeyCode.Alpha1 });
            PickUpGlove = new BindSetting("Pick up Glove", new List<KeyCode> { KeyCode.Alpha2 });
            PickUpHammer = new BindSetting("Pick up Hammer", new List<KeyCode> { KeyCode.Alpha4 });
            PickUpWheel = new BindSetting("Pick up Wheel Barrow", new List<KeyCode> { KeyCode.Alpha5 });
            CoffeeBean = new BindSetting("Coffee Bean", new List<KeyCode> { KeyCode.E });

            AddSettings(PickUpShovel,PickUpGlove,PickUpHammer,PickUpWheel,CoffeeBean);

            EndCategory();

            CreateCategory("Seed Slot",false);

            SeedSlot = new List<BindSetting>();
            for (int i=1; i<=14; i++)
            {
                SeedSlot.Add(new BindSetting($"Seed Slot {i}"));
            }

            SeedSlot.ForEach(set => AddSettings(set));

            EndCategory();


            CreateCategory("Misc");

            SlowMode = new BindSetting("Toggle Slow Mode",new List<KeyCode>{ KeyCode.Alpha3 });
            ShowPlantHP = new BindSetting("Show Plant HP", new List<KeyCode> {KeyCode.Q});
            ShowZombieHP = new BindSetting("Show Zombie HP", new List<KeyCode> { KeyCode.W });

            AddSettings(SlowMode, ShowPlantHP,ShowZombieHP);

            CreateCategory("Application");

            Fullscreen = new BindSetting("Fullscreen",new List<KeyCode>{ KeyCode.F });
            SmallWindow = new BindSetting("Small Window", new List<KeyCode> { KeyCode.G });

            AddSettings(Fullscreen, SmallWindow);
            EndCategory();
#if MELONLOADER || BEPINEX
            DebugMode = new BoolSetting("Debug Mode", false);
            AddSettings(DebugMode);
#endif
            
        }


        // Mod Logic

        

        private static bool RanbyMod = false;

        public override void OnUpdateActive()
        {
            RanbyMod = true;
            
            if (!BoardInstanceIsNull && GameAPP.theGameStatus == GameStatus.InGame)
            {
                // In Game

                if (GetKeyComboDown(PickUpShovel.BindKeys) && Shovel.Instance != null)
                    Shovel.Instance.OnClick(Mouse.Instance);
                if (GetKeyComboDown(PickUpGlove.BindKeys) && Glove.Instance != null)
                    Glove.Instance.OnClick(Mouse.Instance);
                if (GetKeyComboDown(PickUpHammer.BindKeys) && Hammer.Instance != null)
                    Hammer.Instance.OnClick(Mouse.Instance);
                if (GetKeyComboDown(PickUpWheel.BindKeys) && wheel != null)
                    wheel.OnClick(Mouse.Instance);

                if (GetKeyComboDown(CoffeeBean.BindKeys) && itemBtn != null) 
                    Mouse.Instance.theItemOnMouse = itemBtn.Clicked();

                // Seed Slots
            
                for (int i = 0; i <= SeedSlot.Count - 1; i++)
                {
                    if (GetKeyComboDown(SeedSlot[i].BindKeys)) ClickSeedSlot(i);
                }

                // Misc

                if (GetKeyComboDown(SlowMode.BindKeys)) 
                {
                    if (inGameBtn != null) inGameBtn.SpeedTrigger();
                    else
                    {
#if MELONLOADER || RELEASE_MELON
                        var slowTrigger = UnityEngine.Object.FindAnyObjectByType<SlowTrigger>();
                        if (slowTrigger != null) slowTrigger.TriggerSlow();
#elif BEPINEX || RELEASE_BEPINEX
                        // Call the non-generic signature passing the C++ class identifier pointer
                        var rawSlowTrigger = UnityEngine.Object.FindAnyObjectByType(Il2CppInterop.Runtime.Il2CppType.Of<SlowTrigger>());
                        var slowTrigger = rawSlowTrigger != null ? rawSlowTrigger.TryCast<SlowTrigger>() : null;

                        if (slowTrigger != null) slowTrigger.TriggerSlow();
#endif
                    }


                }  
                if (GetKeyComboDown(ShowPlantHP.BindKeys)) board.ShowPlantHealth();
                if (GetKeyComboDown(ShowZombieHP.BindKeys)) board.ShowZombieHealth();

            }

            // Application

            if (GetKeyComboDown(Fullscreen.BindKeys))
            {
                if (GameAPP.isFullScreen)
                {
                    Screen.SetResolution(1920, 1080, fullscreen: false);
                }
                else
                {
                    Screen.SetResolution(1920, 1080, fullscreen: true);
                }
                GameAPP.isFullScreen = !GameAPP.isFullScreen;
            }

            if (GetKeyComboDown(SmallWindow.BindKeys))
            {
                Screen.SetResolution(1280, 720, fullscreen: false);
            }
            RanbyMod = false;
        }

        #region SeedSlot

        public static void ClickSeedSlot(int slot)
        {
            if (Mouse.Instance?.theItemOnMouse != null) return;
            if (InGameUI.Instance == null) return;
            if (slot < 0 || slot >= InGameUI.Instance.Cards.Count) return;

            CardUI targetCard = InGameUI.Instance.Cards[slot];

            if (targetCard != null && Mouse.Instance != null)
            {
                if (AutoPlant.instance == null || !AutoPlant.instance.Active)
                {
                    Mouse.Instance.ClickOnCard(targetCard);
                }
                else
                {
                    var originalAvailable = targetCard.isAvailable;
                    var originalSun = targetCard.theSeedCost;

                    targetCard.isAvailable = true;
                    targetCard.theSeedCost = 0;

                    Mouse.Instance.ClickOnCard(targetCard);
                    
                    targetCard.isAvailable = originalAvailable;
                    targetCard.theSeedCost = originalSun;

                }
            }

        }


        #endregion


        #region UI Buttons

        [HarmonyPatch(typeof(SlowTrigger))]
        public static class SlowTriggerPatch
        {

            [HarmonyPatch(nameof(SlowTrigger.Update))]
            [HarmonyPrefix]
            private static bool UpdatePrefix()
            {
                if (instance == null || !instance.Active || RanbyMod || Input.GetMouseButtonDown(0))
                    return true;
                return false;
            }
        }

        public static InGameBtn inGameBtn;

        [HarmonyPatch(typeof(InGameBtn))]
        public static class InGameBtnPatch
        {
            [HarmonyPatch(nameof(InGameBtn.Start))]
            [HarmonyPostfix]
            public static void AwakePostfix(InGameBtn __instance)
            {
                if (__instance != null)
                    inGameBtn = __instance;
            }


            [HarmonyPatch(nameof(InGameBtn.SpeedTrigger))]
            [HarmonyPrefix]
            private static bool SpeedTriggerPrefix()
            {
                if (instance == null || !instance.Active || RanbyMod || Input.GetMouseButtonUp(0))
                    return true;
                return false;
            }
        }

        public static ItemBtn itemBtn;

        [HarmonyPatch(typeof(ItemBtn))]
        public static class ItemBtnPatch
        {
            [HarmonyPatch(nameof(ItemBtn.Update))]
            [HarmonyPrefix]
            private static bool ItemBtnPrefix(ItemBtn __instance)
            {
                itemBtn = __instance;
                if (instance == null || !instance.Active || RanbyMod || Input.GetMouseButtonDown(0))
                    return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Board))]
        public static class BoardPatch
        {
            [HarmonyPatch(nameof(Board.Die))]
            [HarmonyPostfix]
            public static void DiePostfix()
            {
                inGameBtn = null;
                itemBtn = null;
            }
        }


        #endregion

        #region Plant/Zombie HP

        [HarmonyPatch(typeof(Board))]
        public static class BoardHPPatch
        {
            [HarmonyPatch(nameof(Board.ShowPlantHealth))]
            [HarmonyPrefix]
            public static bool ShowPlantHealthPatch()
            {
                if (instance==null || !instance.Active || RanbyMod) return true;
#if MELONLOADER || BEPINEX
                if (instance.DebugMode.Value)
                    DebugLogger.Msg("[Custom Keybind] Patched ShowPlantHealth");
#endif
                return false;
            }

            [HarmonyPatch(nameof(Board.ShowZombieHealth))]
            [HarmonyPrefix]
            public static bool ShowZombieHealthPatch()
            {
                if (instance == null || !instance.Active || RanbyMod) return true;
#if MELONLOADER || BEPINEX
                if (instance.DebugMode.Value)
                    DebugLogger.Msg("[Custom Keybind] Patched ShowZombieHealth");
#endif
                return false;

            }
        }

        #endregion

        #region Shovel Glove Hammer Wheel

        [HarmonyPatch(typeof(Shovel))]
        public static class ShovelPatch
        {
            [HarmonyPatch(nameof(Shovel.OnClick))]
            [HarmonyPrefix]
            private static bool PickPrefix()
            {
                if (instance == null || !instance.Active || RanbyMod || Input.GetMouseButtonDown(0))
                    return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Glove))]
        public static class GlovePatch
        {
            [HarmonyPatch(nameof(Glove.OnUpdate))]
            [HarmonyPrefix]
            private static bool PickPrefix(Glove __instance)
            {
                if (instance == null || !instance.Active || RanbyMod || Input.GetMouseButtonDown(0))
                    return true;
                __instance.CDUpdate();
                return false;
            }
        }

        [HarmonyPatch(typeof(Hammer))]
        public static class HammerPatch
        {
            [HarmonyPatch(nameof(Hammer.OnUpdate))]
            [HarmonyPrefix]
            private static bool PickPrefix(Hammer __instance)
            {
                if (instance == null || !instance.Active || RanbyMod || Input.GetMouseButtonDown(0))
                    return true;
                __instance.CDUpdate();
                return false;
            }
        }

        [HarmonyPatch(typeof(Wheel))]
        public static class WheelPatch
        {
            [HarmonyPatch(nameof(Wheel.OnUpdate))]
            [HarmonyPrefix]
            private static bool PickPrefix(Wheel __instance)
            {
                if (instance == null || !instance.Active || RanbyMod || Input.GetMouseButtonDown(0))
                    return true;
                __instance.CDUpdate();
                return false;
            }
        }
        #endregion

        #region GameAPP

        [HarmonyPatch(typeof(GameAPP))]
        public static class GameAPPPatch
        {
            [HarmonyPatch(nameof(GameAPP.Update))]
            [HarmonyPrefix]
            public static void UpdatePrefix()
            {
                if (instance == null || !instance.Active)
                    return;
                InputPatch.blockKeys = true;
            }

            [HarmonyPatch(nameof(GameAPP.Update))]
            [HarmonyPostfix]
            public static void UpdatePostfix()
            {
                InputPatch.blockKeys = false;
            }
        }

        [HarmonyPatch(typeof(Input))]
        public static class InputPatch
        {
            public static bool blockKeys;

            [HarmonyPatch(nameof(Input.GetKeyDown), new System.Type[] { typeof(KeyCode) })]
            [HarmonyPrefix]
            public static bool GetKeyDown(KeyCode key)
            {
                if (blockKeys && (key == KeyCode.F || key == KeyCode.G))
                {
                    return false;
                }
                return true;
            }
        }

        #endregion
    }
}
