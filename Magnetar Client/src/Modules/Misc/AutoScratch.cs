using HarmonyLib;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class AutoScratch : Module
{
    // Mod Info
    public override string Name { get; set; } = "Auto Scratch";
    public override string Description { get; set; } = "Automatically reveals all unlocked scratch cells on Scratch-Off tickets.";
    public override string SearchHints { get; set; } = "autoscratch scratchauto autoreveal scratchticket scratchoff fastscratch " +
        "quickscratch scratchsolver scratchclearer ticketreveal scratchbot autouncover scratchhelper scratchmod scratchassist" +
        " instantscratch scratchcells scratchautomation ticketbot scratchcheat";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;
    public override bool EnableInVanillaMode { get; set; } = true;

    // Singleton Instance
    public static AutoScratch instance;

    // Settings
    public BoolSetting AutoRevealOnOpen;
    public BoolSetting AutoClaim;
    public BindSetting ManualScratchKey;

    public static ScratchOffMenu CurrentMenu;

    public AutoScratch()
    {
        instance = this;

        CreateCategory("General");

        AutoRevealOnOpen = new BoolSetting("Auto Reveal on Open", true);
        AutoClaim = new BoolSetting("Auto Claim Prize", false);
        ManualScratchKey = new BindSetting("Manual Scratch Key", new List<KeyCode> { KeyCode.None });

        AddSettings(AutoRevealOnOpen, AutoClaim, ManualScratchKey);
        EndCategory();
    }

    public override void OnUpdateActive()
    {
        base.OnUpdateActive();

        // 1. Keep track of current ScratchOffMenu if open
        if (CurrentMenu == null || CurrentMenu.gameObject == null || !CurrentMenu.gameObject.activeInHierarchy)
        {
            CurrentMenu = Object.FindObjectOfType<ScratchOffMenu>();
        }

        // 2. If menu is open and AutoRevealOnOpen is active, check for any unscratched cells
        if (CurrentMenu != null && CurrentMenu.gameObject != null && CurrentMenu.gameObject.activeInHierarchy)
        {
            if (AutoRevealOnOpen != null && AutoRevealOnOpen.Value && CurrentMenu.cells != null)
            {
                bool hasUnscratched = false;
                for (int i = 0; i < CurrentMenu.cells.Count; i++)
                {
                    var cell = CurrentMenu.cells[i];
                    if (cell != null && cell.Unlocked && cell.scratchProgress < 0.7f)
                    {
                        hasUnscratched = true;
                        break;
                    }
                }

                if (hasUnscratched)
                {
                    ScratchAllUnlocked(CurrentMenu, AutoClaim != null && AutoClaim.Value);
                }
            }
        }

        // 3. Manual Scratch Hotkey
        if (ManualScratchKey?.BindKeys?.Count > 0 && GetKeyComboDown(ManualScratchKey.BindKeys))
        {
            if (CurrentMenu != null)
            {
                ScratchAllUnlocked(CurrentMenu, AutoClaim != null && AutoClaim.Value);
            }
        }
    }

    public static void RevealCell(ScratchCell cell)
    {
        if (cell == null) return;

        try
        {
            // Draw brush across the cell using the game's built-in DrawAtUV method
            // Temporarily enlarge brushSize to cover the entire mask texture in a few stamps
            float savedSize = cell.brushSize;
            try
            {
                cell.brushSize = 1.2f;
                cell.DrawAtUV(new Vector2(0.5f, 0.5f));
                cell.DrawAtUV(new Vector2(0.2f, 0.2f));
                cell.DrawAtUV(new Vector2(0.8f, 0.2f));
                cell.DrawAtUV(new Vector2(0.2f, 0.8f));
                cell.DrawAtUV(new Vector2(0.8f, 0.8f));
            }
            finally
            {
                cell.brushSize = savedSize;
            }
        }
        catch { }

        // Set scratchProgress to 1f so game logic sees 100% scratched
        cell.scratchProgress = 1f;
    }

    public static void ScratchAllUnlocked(ScratchOffMenu menu, bool autoClaim = false)
    {
        if (menu == null || menu.cells == null) return;

        for (int i = 0; i < menu.cells.Count; i++)
        {
            var cell = menu.cells[i];
            if (cell != null && cell.Unlocked)
            {
                RevealCell(cell);
            }
        }

        if (autoClaim && instance != null && instance.Active && instance.AutoClaim.Value)
        {
            try
            {
                menu.CloseMenu();
            }
            catch { }
        }
    }

    #region Harmony Patches

    [HarmonyPatch(typeof(ScratchOffMenu))]
    public static class ScratchOffMenuPatch
    {
        [HarmonyPatch(nameof(ScratchOffMenu.Awake))]
        [HarmonyPostfix]
        private static void AwakePostfix(ScratchOffMenu __instance)
        {
            CurrentMenu = __instance;
        }

        [HarmonyPatch(nameof(ScratchOffMenu.Start))]
        [HarmonyPostfix]
        private static void StartPostfix(ScratchOffMenu __instance)
        {
            CurrentMenu = __instance;
            if (instance != null && instance.Active && instance.AutoRevealOnOpen.Value)
            {
                ScratchAllUnlocked(__instance, autoClaim: instance.AutoClaim.Value);
            }
        }

        [HarmonyPatch(nameof(ScratchOffMenu.SetLevel))]
        [HarmonyPostfix]
        private static void SetLevelPostfix(ScratchOffMenu __instance)
        {
            CurrentMenu = __instance;
            if (instance != null && instance.Active && instance.AutoRevealOnOpen.Value)
            {
                ScratchAllUnlocked(__instance, autoClaim: instance.AutoClaim.Value);
            }
        }

        [HarmonyPatch(nameof(ScratchOffMenu.Refresh))]
        [HarmonyPostfix]
        private static void RefreshPostfix(ScratchOffMenu __instance)
        {
            CurrentMenu = __instance;
            if (instance != null && instance.Active && instance.AutoRevealOnOpen.Value)
            {
                ScratchAllUnlocked(__instance, autoClaim: false);
            }
        }

        [HarmonyPatch(nameof(ScratchOffMenu.CloseMenu))]
        [HarmonyPrefix]
        private static void CloseMenuPrefix(ScratchOffMenu __instance)
        {
            if (instance == null || !instance.Active) return;

            if (__instance.cells != null)
            {
                for (int i = 0; i < __instance.cells.Count; i++)
                {
                    var cell = __instance.cells[i];
                    if (cell != null && cell.Unlocked)
                    {
                        cell.scratchProgress = 1f;
                    }
                }
            }
        }

        [HarmonyPatch(nameof(ScratchOffMenu.OnExit))]
        [HarmonyPostfix]
        private static void OnExitPostfix(ScratchOffMenu __instance)
        {
            if (CurrentMenu == __instance)
                CurrentMenu = null;
        }
    }

    #endregion
}
