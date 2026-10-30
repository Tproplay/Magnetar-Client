using HarmonyLib;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel.RogueShooting;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel.RogueShooting;
#endif

namespace Magnetar_Client.Modules;

public class CustomKeybind : Module
{
    // Mod Info
    public override string Name { get; set; } = "Custom Keybind";
    public override string Description { get; set; } = "Allows you to customize all ingame keybinds.";
    public override string SearchHints { get; set; } = "customkeybind keybinds hotkeys keybindings" +
        " setkeys changekeys keymap keymapping keysettings customkeys keybindmanager bindkeys " +
        "keyconfig keyrebind keyboardlayout keybindcustomizer keybindmod rebind keys customkey" +
        " keyremapping settingskey binds bind rhythm movement wasd shovel hammer pause bag";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;
    public override bool EnableInVanillaMode { get; set; } = true;

    // Singleton Instance
    public static CustomKeybind instance;

    // In Game Tools
    public BindSetting PickUpShovel;
    public BindSetting PickUpGlove;
    public BindSetting PickUpHammer;
    public BindSetting ZombieGlove;
    public BindSetting PickUpWheel;
    public BindSetting CoffeeBean;
    public BindSetting RotatePlant;
    public BindSetting OpenBag;
    public BindSetting PauseGame;
    public BindSetting CancelMenu;

    // Movement & Controls
    public BindSetting MoveUp;
    public BindSetting MoveLeft;
    public BindSetting MoveDown;
    public BindSetting MoveRight;

    // Rhythm Game
    public BindSetting Rhythm1;
    public BindSetting Rhythm2;
    public BindSetting Rhythm3;
    public BindSetting Rhythm4;
    public BindSetting RhythmStepForward;
    public BindSetting RhythmStepBackward;
    public BindSetting RhythmDeleteNote;

    // Gods Evolved
    public BindSetting AutoAim;
    public BindSetting ToggleLucky;

    // Seed Slots
    public List<BindSetting> SeedSlot;

    // Dialog & Choices
    public BindSetting Choice1;
    public BindSetting Choice2;
    public BindSetting Choice3;
    public BindSetting Choice4;
    public BindSetting Choice5;
    public BindSetting ChoiceConfirm;

    // PvP & Minigames
    public BindSetting PvPSpeedUp;
    public BindSetting PvPSpeedDown;
    public BindSetting PvPP1Action;
    public BindSetting PvPP2Action;
    public BindSetting PvPSwitchCards1;
    public BindSetting PvPSwitchCards2;
    public BindSetting PvPConfirm;

    // Almanac & Data Menus
    public BindSetting AlmanacLookMix;
    public BindSetting CheckPlantAlmanac;
    public BindSetting LookPlantData;

    // Game Features & Sound
    public BindSetting ShowBulletDamage;
    public BindSetting Ra2Sound;

    // Misc
    public BindSetting SlowMode;
    public BindSetting ShowPlantHP;
    public BindSetting ShowZombieHP;
    public BindSetting ToggleUI;

    // Application
    public BindSetting Fullscreen;
    public BindSetting SmallWindow;

#if MELONLOADER || BEPINEX
    public BoolSetting DebugMode;
#endif

    public CustomKeybind()
    {
        instance = this;

        RegisterRegexTranslation("Seed Slot (\\d+)", "Seed Slot {1}");

        // In Game Tools
        CreateCategory("In Game");

        PickUpShovel = new BindSetting("Pick up Shovel", new List<KeyCode> { KeyCode.Alpha1 });
        PickUpGlove = new BindSetting("Pick up Glove", new List<KeyCode> { KeyCode.Alpha2 });
        PickUpHammer = new BindSetting("Pick up Hammer", new List<KeyCode> { KeyCode.Alpha4 });
        PickUpWheel = new BindSetting("Pick up Wheel Barrow", new List<KeyCode> { KeyCode.Alpha5 });
        ZombieGlove = new BindSetting("Zombie Glove", new List<KeyCode> { KeyCode.V });
        CoffeeBean = new BindSetting("Coffee Bean", new List<KeyCode> { KeyCode.E });
        RotatePlant = new BindSetting("Rotate Plant (TD)", new List<KeyCode> { KeyCode.R });
        OpenBag = new BindSetting("Open Bag / Inventory", new List<KeyCode> { KeyCode.B });
        PauseGame = new BindSetting("Pause / Resume Game", new List<KeyCode> { KeyCode.Space });
        CancelMenu = new BindSetting("Cancel / Back / Menu", new List<KeyCode> { KeyCode.Escape });

        AddSettings(PickUpShovel, PickUpGlove, PickUpHammer, PickUpWheel, ZombieGlove, CoffeeBean, RotatePlant, OpenBag, PauseGame, CancelMenu);
        EndCategory();

        // Game Features & Sound
        CreateCategory("Game Features", false);

        ShowBulletDamage = new BindSetting("Show Bullet Damage", new List<KeyCode> { KeyCode.T });
        Ra2Sound = new BindSetting("Toggle RA2 Sound", new List<KeyCode> { KeyCode.P });
        SlowMode = new BindSetting("Toggle Slow Mode", new List<KeyCode> { KeyCode.Alpha3 });
        ShowPlantHP = new BindSetting("Show Plant HP", new List<KeyCode> { KeyCode.Q });
        ShowZombieHP = new BindSetting("Show Zombie HP", new List<KeyCode> { KeyCode.W });
        ToggleUI = new BindSetting("Toggle UI", new List<KeyCode> { KeyCode.F1 });

        AddSettings(ShowBulletDamage, Ra2Sound, SlowMode, ShowPlantHP, ShowZombieHP, ToggleUI);
        EndCategory();
        
        // Movement & Controls
        CreateCategory("Movement & Controls");

        MoveUp = new BindSetting("Move Up", new List<KeyCode> { KeyCode.W });
        MoveLeft = new BindSetting("Move Left", new List<KeyCode> { KeyCode.A });
        MoveDown = new BindSetting("Move Down", new List<KeyCode> { KeyCode.S });
        MoveRight = new BindSetting("Move Right", new List<KeyCode> { KeyCode.D });

        AddSettings(MoveUp, MoveLeft, MoveDown, MoveRight);
        EndCategory();


        // Seed Slots
        CreateCategory("Seed Slot", false);

        SeedSlot = new List<BindSetting>(14);
        for (int i = 1; i <= 14; i++)
        {
            SeedSlot.Add(new BindSetting($"Seed Slot {i}") { SaveTranslation = false });
        }
        SeedSlot.ForEach(set => AddSettings(set));
        EndCategory();

        // Choices options (MultipleChoiceMenu)
        CreateCategory("Choices & Buff Selection", false);

        LabelSetting MultipleChoiceLabel = new("These keybinds are used for quickly picking buff choices.");

        Choice1 = new BindSetting("Choice Option 1", new List<KeyCode> { KeyCode.Alpha1 });
        Choice2 = new BindSetting("Choice Option 2", new List<KeyCode> { KeyCode.Alpha2 });
        Choice3 = new BindSetting("Choice Option 3", new List<KeyCode> { KeyCode.Alpha3 });
        Choice4 = new BindSetting("Choice Option 4", new List<KeyCode> { KeyCode.Alpha4 });
        Choice5 = new BindSetting("Choice Option 5", new List<KeyCode> { KeyCode.Alpha5 });
        ChoiceConfirm = new BindSetting("Choice Confirm", new List<KeyCode> { KeyCode.Space });

        AddSettings(MultipleChoiceLabel, Choice1, Choice2, Choice3, Choice4, Choice5, ChoiceConfirm);
        EndCategory();

        // Rhythm Game
        CreateCategory("Rhythm Game");

        Rhythm1 = new BindSetting("Rhythm Track 1", new List<KeyCode> { KeyCode.S });
        Rhythm2 = new BindSetting("Rhythm Track 2", new List<KeyCode> { KeyCode.D });
        Rhythm3 = new BindSetting("Rhythm Track 3", new List<KeyCode> { KeyCode.J });
        Rhythm4 = new BindSetting("Rhythm Track 4", new List<KeyCode> { KeyCode.K });
        RhythmStepForward = new BindSetting("Editor Step Forward", new List<KeyCode> { KeyCode.Space });
        RhythmStepBackward = new BindSetting("Editor Step Backward", new List<KeyCode> { KeyCode.RightArrow });
        RhythmDeleteNote = new BindSetting("Editor Delete Note", new List<KeyCode> { KeyCode.Delete });

        AddSettings(Rhythm1, Rhythm2, Rhythm3, Rhythm4, RhythmStepForward, RhythmStepBackward, RhythmDeleteNote);
        EndCategory();

        // Gods Evolved
        CreateCategory("Gods Evolved");

        AutoAim = new BindSetting("Auto Aim", new List<KeyCode> { KeyCode.Tab });
        ToggleLucky = new BindSetting("Toggle Lucky Display", new List<KeyCode> { KeyCode.LeftAlt });

        AddSettings(AutoAim, ToggleLucky);
        EndCategory();
        
        // PvP
        CreateCategory("PvP", false);

        PvPSpeedUp = new BindSetting("PvP Speed Up", new List<KeyCode> { KeyCode.RightArrow });
        PvPSpeedDown = new BindSetting("PvP Speed Down", new List<KeyCode> { KeyCode.LeftArrow });
        PvPP1Action = new BindSetting("PvP P1 Action", new List<KeyCode> { KeyCode.O });
        PvPP2Action = new BindSetting("PvP P2 Action", new List<KeyCode> { KeyCode.I });
        PvPSwitchCards1 = new BindSetting("PvP Switch Cards 1", new List<KeyCode> { KeyCode.U });
        PvPSwitchCards2 = new BindSetting("PvP Switch Cards 2", new List<KeyCode> { KeyCode.Y });
        PvPConfirm = new BindSetting("PvP Confirm", new List<KeyCode> { KeyCode.Return });

        AddSettings(PvPSpeedUp, PvPSpeedDown, PvPP1Action, PvPP2Action, PvPSwitchCards1, PvPSwitchCards2, PvPConfirm);
        EndCategory();

        // Almanac & Data
        CreateCategory("Almanac & Data", false);

        AlmanacLookMix = new BindSetting("Look Mix (In Almanac)", new List<KeyCode> { KeyCode.U });
        CheckPlantAlmanac = new BindSetting("Check Plant Almanac", new List<KeyCode> { KeyCode.C });
        LookPlantData = new BindSetting("Look Plant Data", new List<KeyCode> { KeyCode.H });

        AddSettings(AlmanacLookMix, CheckPlantAlmanac, LookPlantData);
        EndCategory();


        // Application
        CreateCategory("Application");

        Fullscreen = new BindSetting("Fullscreen", new List<KeyCode> { KeyCode.F });
        SmallWindow = new BindSetting("Small Window", new List<KeyCode> { KeyCode.G });

        AddSettings(Fullscreen, SmallWindow);
        EndCategory();

#if MELONLOADER || BEPINEX
        DebugMode = new BoolSetting("Debug Mode", false);
        AddSettings(DebugMode);
#endif
    }

    public override void OnLanguageChanged()
    {
        if (SeedSlot != null)
        {
            foreach (var set in SeedSlot)
            {
                set.CustomName = Translate(set.Name);
            }
        }
    }

    // Module Lifecycle Callbacks from Module.cs
    public override void OnEnable()
    {
        SyncKeyCodeManager(true);
    }

    public override void OnDisable()
    {
        SyncKeyCodeManager(false);
    }

    /// <summary>
    /// Synchronizes custom keybinds directly into PvZ Fusion's native KeyCodeManager.
    /// This eliminates hook latency and allows the vanilla game to natively react to the remapped keys.
    /// </summary>
    public void SyncKeyCodeManager(bool custom)
    {
        try
        {
            if (custom)
            {
                if (PickUpShovel?.BindKeys?.Count > 0) KeyCodeManager.Shovel = PickUpShovel.BindKeys[0];
                if (PickUpGlove?.BindKeys?.Count > 0) KeyCodeManager.Glove = PickUpGlove.BindKeys[0];
                if (SlowMode?.BindKeys?.Count > 0) KeyCodeManager.SlowTrigger = SlowMode.BindKeys[0];
                if (PickUpHammer?.BindKeys?.Count > 0) KeyCodeManager.Hammer = PickUpHammer.BindKeys[0];
                if (PickUpWheel?.BindKeys?.Count > 0) KeyCodeManager.Wheel = PickUpWheel.BindKeys[0];
                if (ShowPlantHP?.BindKeys?.Count > 0) KeyCodeManager.ShowPlantHealth = ShowPlantHP.BindKeys[0];
                if (ShowZombieHP?.BindKeys?.Count > 0) KeyCodeManager.ShowZombieHealth = ShowZombieHP.BindKeys[0];
                if (CoffeeBean?.BindKeys?.Count > 0) KeyCodeManager.UseGoldBean = CoffeeBean.BindKeys[0];
                if (CheckPlantAlmanac?.BindKeys?.Count > 0) KeyCodeManager.CheckPlantAlmanac = CheckPlantAlmanac.BindKeys[0];
                if (ShowBulletDamage?.BindKeys?.Count > 0) KeyCodeManager.ShowBulletDamage = ShowBulletDamage.BindKeys[0];
                if (ZombieGlove?.BindKeys?.Count > 0) KeyCodeManager.ZombieGlove = ZombieGlove.BindKeys[0];
                if (LookPlantData?.BindKeys?.Count > 0) KeyCodeManager.LookPlantData = LookPlantData.BindKeys[0];
                if (Fullscreen?.BindKeys?.Count > 0) KeyCodeManager.FullScreen = Fullscreen.BindKeys[0];
                if (SmallWindow?.BindKeys?.Count > 0) KeyCodeManager.NormalScreen = SmallWindow.BindKeys[0];
                if (Ra2Sound?.BindKeys?.Count > 0) KeyCodeManager.Ra2Sound = Ra2Sound.BindKeys[0];
                if (ToggleUI?.BindKeys?.Count > 0) KeyCodeManager.HideUI = ToggleUI.BindKeys[0];
            }
            else
            {
                // Restore vanilla defaults
                KeyCodeManager.Shovel = KeyCode.Alpha1;
                KeyCodeManager.Glove = KeyCode.Alpha2;
                KeyCodeManager.SlowTrigger = KeyCode.Alpha3;
                KeyCodeManager.Hammer = KeyCode.Alpha4;
                KeyCodeManager.Wheel = KeyCode.Alpha5;
                KeyCodeManager.ShowPlantHealth = KeyCode.Q;
                KeyCodeManager.ShowZombieHealth = KeyCode.W;
                KeyCodeManager.UseGoldBean = KeyCode.E;
                KeyCodeManager.CheckPlantAlmanac = KeyCode.C;
                KeyCodeManager.ShowBulletDamage = KeyCode.T;
                KeyCodeManager.ZombieGlove = KeyCode.V;
                KeyCodeManager.LookPlantData = KeyCode.H;
                KeyCodeManager.FullScreen = KeyCode.F;
                KeyCodeManager.NormalScreen = KeyCode.G;
                KeyCodeManager.Ra2Sound = KeyCode.P;
                KeyCodeManager.HideUI = KeyCode.F1;
            }
        }
        catch (System.Exception ex)
        {
#if MELONLOADER || BEPINEX
            if (DebugMode?.Value == true)
                DebugLogger.Msg($"[Custom Keybind] KeyCodeManager sync exception: {ex.Message}");
#endif
        }
    }

    private static bool RanbyMod;

    // Active Frame Checks
    public override void OnUpdateActive()
    {
        RanbyMod = true;
        try
        {
            if (!GameData.BoardInstanceIsNull && GameAPP.theGameStatus == GameStatus.InGame)
            {
                // In Game Tools
                if (PickUpShovel?.BindKeys?.Count > 0 && GetKeyComboDown(PickUpShovel.BindKeys) && Shovel.Instance != null)
                    Shovel.Instance.OnClick(Mouse.Instance);

                if (PickUpGlove?.BindKeys?.Count > 0 && GetKeyComboDown(PickUpGlove.BindKeys) && Glove.Instance != null)
                    Glove.Instance.OnClick(Mouse.Instance);

                if (PickUpHammer?.BindKeys?.Count > 0 && GetKeyComboDown(PickUpHammer.BindKeys) && Hammer.Instance != null)
                    Hammer.Instance.OnClick(Mouse.Instance);

                if (PickUpWheel?.BindKeys?.Count > 0 && GetKeyComboDown(PickUpWheel.BindKeys) && GameData.WheelInstance != null)
                    GameData.WheelInstance.OnClick(Mouse.Instance);

                if (SlowMode?.BindKeys?.Count > 0 && GetKeyComboDown(SlowMode.BindKeys))
                {
                    SlowTrigger slowTrigger = GameAPP.board?.GetComponent<SlowTrigger>();
                    if (slowTrigger != null) slowTrigger.TriggerSlow();
                }

                if (ShowPlantHP?.BindKeys?.Count > 0 && GetKeyComboDown(ShowPlantHP.BindKeys) && GameData.BoardInstance != null)
                    GameData.BoardInstance.ShowPlantHealth();

                if (ShowZombieHP?.BindKeys?.Count > 0 && GetKeyComboDown(ShowZombieHP.BindKeys) && GameData.BoardInstance != null)
                    GameData.BoardInstance.ShowZombieHealth();

                if (ShowBulletDamage?.BindKeys?.Count > 0 && GetKeyComboDown(ShowBulletDamage.BindKeys) && GameData.BoardInstance != null)
                    GameData.BoardInstance.showBulletDamage = !GameData.BoardInstance.showBulletDamage;

                if (ToggleUI?.BindKeys?.Count > 0 && GetKeyComboDown(ToggleUI.BindKeys))
                {
                    ToggleUIVisibility();
                }

                // Active Seed Slots (1 - 14)
                for (int i = 0; i < SeedSlot.Count; i++)
                {
                    if (GetKeyComboDown(SeedSlot[i].BindKeys))
                    {
                        ClickSeedSlot(i);
                    }
                }
            }

            // Application Shortcuts (Runs everywhere: In-game, menus, etc.)
            HandleApplicationShortcuts();
        }
        finally
        {
            RanbyMod = false;
        }
    }

    private void HandleApplicationShortcuts()
    {
        if (Fullscreen?.BindKeys?.Count > 0 && GetKeyComboDown(Fullscreen.BindKeys))
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

        if (SmallWindow?.BindKeys?.Count > 0 && GetKeyComboDown(SmallWindow.BindKeys))
        {
            Screen.SetResolution(1280, 720, fullscreen: false);
        }

        if (Ra2Sound?.BindKeys?.Count > 0 && GetKeyComboDown(Ra2Sound.BindKeys))
        {
            if (GameAPP.config != null)
            {
                GameAPP.config.ra2Sound = !GameAPP.config.ra2Sound;
            }
        }
    }

    private void ToggleUIVisibility()
    {
        if (GameAPP.canvas != null && GameAPP.canvasUp != null)
        {
            CanvasGroup canvasGroup = GameAPP.canvas.GetComponent<CanvasGroup>();
            CanvasGroup canvasGroupUp = GameAPP.canvasUp.GetComponent<CanvasGroup>();

            if (canvasGroup != null && canvasGroupUp != null)
            {
                float newAlpha = (canvasGroup.alpha == 1f) ? 0f : 1f;
                canvasGroup.alpha = newAlpha;
                canvasGroupUp.alpha = newAlpha;
            }
        }
    }

    #region SeedSlot

    public static void ClickSeedSlot(int slot)
    {
        if (Mouse.Instance?.theItemOnMouse != null) return;
        if (InGameUI.Instance == null) return;
        if (slot < 0 || slot >= InGameUI.Instance.Cards.Count) return;

        CardUI targetCard = InGameUI.Instance.Cards[slot];
        if (targetCard == null || Mouse.Instance == null) return;

        if (AutoPlant.Instance == null || !AutoPlant.Instance.Active)
        {
            Mouse.Instance.ClickOnCard(targetCard);
        }
        else
        {
            bool originalAvailable = targetCard.isAvailable;
            int originalSun = targetCard.theSeedCost;

            targetCard.isAvailable = true;
            targetCard.theSeedCost = 0;

            Mouse.Instance.ClickOnCard(targetCard);

            targetCard.isAvailable = originalAvailable;
            targetCard.theSeedCost = originalSun;
        }
    }

    #endregion

    #region UI & Tools Patches (Raw Input Handling)

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

    [HarmonyPatch(typeof(Shovel))]
    public static class ShovelPatch
    {
        [HarmonyPatch(nameof(Shovel.OnUpdate))]
        [HarmonyPrefix]
        private static bool PickPrefix(Shovel __instance)
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

    [HarmonyPatch(typeof(GameAPP))]
    public static class GameAPPPatch
    {
        [HarmonyPatch(nameof(GameAPP.Update))]
        [HarmonyPrefix]
        public static void UpdatePrefix()
        {
            if (instance == null || !instance.Active)
                return;
            InputPatch.GameAPPBlockKeys = true;
        }

        [HarmonyPatch(nameof(GameAPP.Update))]
        [HarmonyPostfix]
        public static void UpdatePostfix()
        {
            InputPatch.GameAPPBlockKeys = false;
        }
    }

    [HarmonyPatch(typeof(Board))]
    public static class BoardPatch
    {
        [HarmonyPatch(nameof(Board.Update))]
        [HarmonyPrefix]
        public static void BoardPrefix()
        {
            if (instance == null || !instance.Active)
                return;
            InputPatch.BoardBlockKeys = true;
        }

        [HarmonyPatch(nameof(Board.Update))]
        [HarmonyPostfix]
        public static void BoardPostfix()
        {
            InputPatch.BoardBlockKeys = false;
        }
    }

    #endregion

    #region ShootingManager Patches

    [HarmonyPatch(typeof(ShootingManager))]
    public static class ShootingManagerKeyPatch
    {
        [HarmonyPatch(nameof(ShootingManager.Update))]
        [HarmonyPrefix]
        public static void UpdatePrefix()
        {
            if (instance?.Active == true)
                InputPatch.ShootingManagerBlockKeys = true;
        }

        [HarmonyPatch(nameof(ShootingManager.Update))]
        [HarmonyPostfix]
        public static void UpdatePostfix()
        {
            InputPatch.ShootingManagerBlockKeys = false;
        }
    }

    #endregion

    #region Lawnf Input Redirection (Movement / WASD)

    [HarmonyPatch(typeof(Lawnf))]
    public static class LawnfPatch
    {
        [HarmonyPatch(nameof(Lawnf.GetKey), new System.Type[] { typeof(KeyCode) })]
        [HarmonyPrefix]
        public static bool LawnfGetKey(KeyCode keyCode, ref bool __result)
        {
            if (instance == null || !instance.Active) return true;

            if (keyCode == KeyCode.W && instance.MoveUp != null)
            {
                __result = GetKeyCombo(instance.MoveUp.BindKeys);
                return false;
            }
            if (keyCode == KeyCode.A && instance.MoveLeft != null)
            {
                __result = GetKeyCombo(instance.MoveLeft.BindKeys);
                return false;
            }
            if (keyCode == KeyCode.S && instance.MoveDown != null)
            {
                __result = GetKeyCombo(instance.MoveDown.BindKeys);
                return false;
            }
            if (keyCode == KeyCode.D && instance.MoveRight != null)
            {
                __result = GetKeyCombo(instance.MoveRight.BindKeys);
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(Lawnf.GetKeyDown), new System.Type[] { typeof(KeyCode) })]
        [HarmonyPrefix]
        public static bool LawnfGetKeyDown(KeyCode keyCode, ref bool __result)
        {
            if (instance == null || !instance.Active) return true;

            if (keyCode == KeyCode.W && instance.MoveUp != null)
            {
                __result = GetKeyComboDown(instance.MoveUp.BindKeys);
                return false;
            }
            if (keyCode == KeyCode.A && instance.MoveLeft != null)
            {
                __result = GetKeyComboDown(instance.MoveLeft.BindKeys);
                return false;
            }
            if (keyCode == KeyCode.S && instance.MoveDown != null)
            {
                __result = GetKeyComboDown(instance.MoveDown.BindKeys);
                return false;
            }
            if (keyCode == KeyCode.D && instance.MoveRight != null)
            {
                __result = GetKeyComboDown(instance.MoveRight.BindKeys);
                return false;
            }

            return true;
        }
    }

    #endregion

    #region Universal Input Redirection

    [HarmonyPatch(typeof(Input))]
    public static class InputPatch
    {
        public static bool GameAPPBlockKeys;
        public static bool BoardBlockKeys;
        public static bool ShootingManagerBlockKeys;

        private static bool isResolvingKey;
        private static bool isResolvingKeyDown;
        private static bool isResolvingKeyUp;

        [HarmonyPatch(nameof(Input.GetKey), new System.Type[] { typeof(KeyCode) })]
        [HarmonyPrefix]
        public static bool InputGetKey(KeyCode key, ref bool __result)
        {
            if (isResolvingKey || instance == null || !instance.Active) return true;

            // Gods Evolved Tab (Auto Aim continuous check)
            if (ShootingManagerBlockKeys && key == KeyCode.Tab && instance.AutoAim != null)
            {
                isResolvingKey = true;
                try
                {
                    __result = GetKeyCombo(instance.AutoAim.BindKeys);
                    return false;
                }
                finally
                {
                    isResolvingKey = false;
                }
            }

            return true;
        }

        [HarmonyPatch(nameof(Input.GetKeyDown), new System.Type[] { typeof(KeyCode) })]
        [HarmonyPrefix]
        public static bool InputGetKeyDown(KeyCode key, ref bool __result)
        {
            if (isResolvingKeyDown) return true;

            // Block vanilla GameAPP raw keys so HandleApplicationShortcuts handles them
            if (GameAPPBlockKeys)
            {
                if (key == KeyCode.F || key == KeyCode.G || key == KeyCode.P ||
                    (instance != null && (key == KeyCodeManager.FullScreen || key == KeyCodeManager.NormalScreen || key == KeyCodeManager.Ra2Sound)))
                {
                    __result = false;
                    return false;
                }
            }

            // Block vanilla Board raw keys so our mod handles them
            if (BoardBlockKeys)
            {
                if (key == KeyCode.F1 || key == KeyCode.T || key == KeyCode.Q || key == KeyCode.W || key == KeyCode.F2 || key == KeyCode.X ||
                    (instance != null && (key == KeyCodeManager.HideUI || key == KeyCodeManager.ShowBulletDamage || key == KeyCodeManager.ShowPlantHealth || key == KeyCodeManager.ShowZombieHealth || key == KeyCodeManager.DevalopMode)))
                {
                    __result = false;
                    return false;
                }
            }

            if (instance == null || !instance.Active) return true;

            isResolvingKeyDown = true;
            try
            {
                // Gods Evolved
                if (ShootingManagerBlockKeys && (key == KeyCode.LeftAlt || key == KeyCode.LeftShift) && instance.ToggleLucky != null)
                {
                    __result = GetKeyComboDown(instance.ToggleLucky.BindKeys);
                    return false;
                }

                // Pause & Cancel Menus
                if (key == KeyCode.Escape && instance.CancelMenu != null)
                {
                    __result = GetKeyComboDown(instance.CancelMenu.BindKeys);
                    return false;
                }
                if (key == KeyCode.Space && instance.PauseGame != null)
                {
                    __result = GetKeyComboDown(instance.PauseGame.BindKeys);
                    return false;
                }

                // Inventory & Plant Preview
                if (key == KeyCode.B && instance.OpenBag != null)
                {
                    __result = GetKeyComboDown(instance.OpenBag.BindKeys);
                    return false;
                }
                if (key == KeyCode.R && instance.RotatePlant != null)
                {
                    __result = GetKeyComboDown(instance.RotatePlant.BindKeys);
                    return false;
                }
                if (key == KeyCode.V && instance.ZombieGlove != null)
                {
                    __result = GetKeyComboDown(instance.ZombieGlove.BindKeys);
                    return false;
                }
                if (key == KeyCode.E && instance.CoffeeBean != null)
                {
                    __result = GetKeyComboDown(instance.CoffeeBean.BindKeys);
                    return false;
                }

                // Rhythm Game Tracks
                if (key == KeyCode.S && instance.Rhythm1 != null)
                {
                    __result = GetKeyComboDown(instance.Rhythm1.BindKeys);
                    return false;
                }
                if (key == KeyCode.D && instance.Rhythm2 != null)
                {
                    __result = GetKeyComboDown(instance.Rhythm2.BindKeys);
                    return false;
                }
                if (key == KeyCode.J && instance.Rhythm3 != null)
                {
                    __result = GetKeyComboDown(instance.Rhythm3.BindKeys);
                    return false;
                }
                if (key == KeyCode.K && instance.Rhythm4 != null)
                {
                    __result = GetKeyComboDown(instance.Rhythm4.BindKeys);
                    return false;
                }

                // Rhythm Game Editor Navigation
                if (key == KeyCode.RightArrow && instance.RhythmStepBackward != null)
                {
                    __result = GetKeyComboDown(instance.RhythmStepBackward.BindKeys);
                    return false;
                }
                if ((key == KeyCode.Delete || key == KeyCode.Backspace) && instance.RhythmDeleteNote != null)
                {
                    __result = GetKeyComboDown(instance.RhythmDeleteNote.BindKeys);
                    return false;
                }

                // Almanac & Data Menus
                if (key == KeyCode.U && instance.AlmanacLookMix != null)
                {
                    __result = GetKeyComboDown(instance.AlmanacLookMix.BindKeys);
                    return false;
                }
                if (key == KeyCode.C && instance.CheckPlantAlmanac != null)
                {
                    __result = GetKeyComboDown(instance.CheckPlantAlmanac.BindKeys);
                    return false;
                }
                if (key == KeyCode.H && instance.LookPlantData != null)
                {
                    __result = GetKeyComboDown(instance.LookPlantData.BindKeys);
                    return false;
                }

                // Dialog Choices (1 - 5 + Confirm)
                if (key == KeyCode.Alpha1 && instance.Choice1 != null)
                {
                    __result = GetKeyComboDown(instance.Choice1.BindKeys);
                    return false;
                }
                if (key == KeyCode.Alpha2 && instance.Choice2 != null)
                {
                    __result = GetKeyComboDown(instance.Choice2.BindKeys);
                    return false;
                }
                if (key == KeyCode.Alpha3 && instance.Choice3 != null)
                {
                    __result = GetKeyComboDown(instance.Choice3.BindKeys);
                    return false;
                }
                if (key == KeyCode.Alpha4 && instance.Choice4 != null)
                {
                    __result = GetKeyComboDown(instance.Choice4.BindKeys);
                    return false;
                }
                if (key == KeyCode.Alpha5 && instance.Choice5 != null)
                {
                    __result = GetKeyComboDown(instance.Choice5.BindKeys);
                    return false;
                }
                if (key == KeyCode.Space && instance.ChoiceConfirm != null)
                {
                    __result = GetKeyComboDown(instance.ChoiceConfirm.BindKeys);
                    return false;
                }

                // PvP Actions
                if (key == KeyCode.O && instance.PvPP1Action != null)
                {
                    __result = GetKeyComboDown(instance.PvPP1Action.BindKeys);
                    return false;
                }
                if (key == KeyCode.I && instance.PvPP2Action != null)
                {
                    __result = GetKeyComboDown(instance.PvPP2Action.BindKeys);
                    return false;
                }
                if (key == KeyCode.Y && instance.PvPSwitchCards2 != null)
                {
                    __result = GetKeyComboDown(instance.PvPSwitchCards2.BindKeys);
                    return false;
                }
                if (key == KeyCode.Return && instance.PvPConfirm != null)
                {
                    __result = GetKeyComboDown(instance.PvPConfirm.BindKeys);
                    return false;
                }
            }
            finally
            {
                isResolvingKeyDown = false;
            }

            return true;
        }

        [HarmonyPatch(nameof(Input.GetKeyUp), new System.Type[] { typeof(KeyCode) })]
        [HarmonyPrefix]
        public static bool InputGetKeyUp(KeyCode key, ref bool __result)
        {
            if (isResolvingKeyUp || instance == null || !instance.Active) return true;

            isResolvingKeyUp = true;
            try
            {
                // Rhythm Game Note Release (Tracks 1 - 4)
                if (key == KeyCode.S && instance.Rhythm1 != null)
                {
                    __result = GetKeyComboUp(instance.Rhythm1.BindKeys);
                    return false;
                }
                if (key == KeyCode.D && instance.Rhythm2 != null)
                {
                    __result = GetKeyComboUp(instance.Rhythm2.BindKeys);
                    return false;
                }
                if (key == KeyCode.J && instance.Rhythm3 != null)
                {
                    __result = GetKeyComboUp(instance.Rhythm3.BindKeys);
                    return false;
                }
                if (key == KeyCode.K && instance.Rhythm4 != null)
                {
                    __result = GetKeyComboUp(instance.Rhythm4.BindKeys);
                    return false;
                }
            }
            finally
            {
                isResolvingKeyUp = false;
            }

            return true;
        }
    }

    #endregion
}
