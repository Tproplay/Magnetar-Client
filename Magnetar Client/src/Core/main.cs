using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Magnetar_Logger;
using System.Reflection;
using Magnetar_Client.UI;

namespace Magnetar_Client.Core;

public class Main
{
    public static Main Instance { get; private set; }
    public static HarmonyLib.Harmony HarmonyInstance { get; private set; }
    public bool HasWarmedUp { get; private set; } = false;

    public static void Initialize()
    {
        if (Instance != null)
        {
            DebugLogger.Error("[Core] Attempted to initialize Main multiple times! Aborting duplicate call.");
            return;
        }
        Instance = new Main();

        // Load the IAddons first so that they can register their actions
        AddonManager.InitAddons();

        Api.Actions.Core.OnEarlyInitialize?.Invoke();

        // Initialize the logger first so subsequent diagnostics are captured
        Utils.Magnetar_Logger.Init();

        DebugLogger.Msg($"[Core] Initializing Magnetar Client with Harmony ID: '{Magnetar_Info.HarmonyId}'...");

        // Apply the harmony patches

        Api.Actions.Core.OnPreApplyHarmonyPatches?.Invoke();

        ApplyHarmonyPatches();

        Api.Actions.Core.OnPostApplyHarmonyPatches?.Invoke();

        // Initialize core systems and modules
        DebugLogger.Msg("[Core] Proceeding to InitializeCore...");
        Instance.InitializeCore();
    }

    static void ApplyHarmonyPatches()
    {
        HarmonyInstance = new HarmonyLib.Harmony(Magnetar_Info.HarmonyId);

        Assembly currentAssembly = typeof(Main).Assembly;

        HarmonyPatchInfo patchInfo = HarmonyManager.HarmonyPatchAll(currentAssembly, HarmonyInstance);

        DebugLogger.Msg($"[Harmony] Total classes checked: {patchInfo.TotalClassesEvaluated} | Succeeded: {patchInfo.SuccessCount} | Failed: {patchInfo.FailCount}");

        // 3. Log detailed failure diagnostics and exceptions if any patch failed
        if (patchInfo.HasFailures)
        {
            DebugLogger.Warning($"[Harmony] {patchInfo.FailCount} patch classes failed to apply:");
            for (int i = 0; i < patchInfo.Failures.Count; i++)
            {
                var failure = patchInfo.Failures[i];
                DebugLogger.Error($"[Harmony] -> Failure #{i + 1} on '{failure.ClassName}':\nException: {failure.Exception.GetType().Name} - {failure.Exception.Message}\nStack: {failure.Exception.StackTrace}");
            }
        }
    }

    void InitializeCore()
    {
        Api.Actions.Core.OnEarlyInitializeCore?.Invoke();

        Preferences.InitializePreferences();

        ModuleManager.Init();
        AddonManager.InitModules();

        HUDRenderer.Init();
        AddonManager.InitHUDElements();

        NEFManager.Init();
        TopBar.Init();
        ProfileManager.Init();

        Translator.LoadTranslations();
        SaveLoad.Load();
        GUIManager.Init();

        Api.Actions.Core.OnLateInitializeCore?.Invoke();

        DebugLogger.Msg("Magnetar Client Loaded!");
    }

    public void OnUpdate()
    {
        LockUI.BlockSKeysPatch.BlockKeys = false;
        UI.GUIHelper._UpdateRainbowColor();

        HUDRenderer.UpdateElements();

        if (Input.GetKeyDown(KeyCode.RightShift) && !HUDManager.forceShow)
        {
            Config.showgui = !Config.showgui;
            if (!Config.showgui) SaveLoad.Save();
        }

        ModuleManager.OnUpdate();

        if (!HasWarmedUp) return;

        #region Handle Escape Key
        if (Input.GetKeyDown(KeyCode.Escape) && Config.CurrentTab == TabType.MODULES)
        {
            bool isInputBlocked = ModuleManager.bindingModuleId != -1
                                  || UI.WindowDrawing.DrawSetting.focusedControlId != -1
                                  || UI.WindowDrawing.DrawSetting.activeTextFieldId != -1;

            if (ModuleManager.showModules)
            {
                Config.showgui = false;
                SaveLoad.Save();
                ResetInputBind();
                Input.ResetInputAxes();
            }
            else if (!isInputBlocked)
            {
                if (ModuleManager.showSettings)
                {
                    ModuleManager.showModules = true;
                    ModuleManager.showSettings = false;
                    ModuleManager.showSelectionGui = false;

                    if (ModuleManager.Modules != null)
                    {
                        foreach (var m in ModuleManager.Modules)
                        {
                            m.ShowSettings = false;
                        }
                    }

                    ResetInputBind();
                    Input.ResetInputAxes();
                }
                else if (ModuleManager.showSelectionGui)
                {
                    ModuleManager.showSettings = true;
                    ModuleManager.showSelectionGui = false;

                    ResetInputBind();
                    Input.ResetInputAxes();
                }
            }
        }
        #endregion

        if (Config.showgui) LockUI.BlockSKeysPatch.BlockKeys = true;
    }

    public void OnGUI()
    {
        LockUI.BlockSKeysPatch.BlockKeys = false;
        if (!ModuleManager.IsInitialized) return;

        Event e = Event.current;
        if (e == null) return;

        Matrix4x4 originalMatrix = GUI.matrix;

        try
        {
            float scaleX = (float)Screen.width / Config.NativeWidth;
            float scaleY = (float)Screen.height / Config.NativeHeight;
            float uniformScale = Mathf.Min(scaleX, scaleY);

            float offsetX = (Screen.width - (Config.NativeWidth * uniformScale)) * 0.5f;
            float offsetY = (Screen.height - (Config.NativeHeight * uniformScale)) * 0.5f;

            GUI.matrix = Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0),
                Quaternion.identity,
                new Vector3(uniformScale, uniformScale, 1)
            );

            if (!HasWarmedUp)
            {
                WarmUp();
                HasWarmedUp = true;
            }

            UI.Themes.ThemeManager.Rescale();

            MobileMenuUI.Render();
            HUDManager.Render();

            foreach (var mod in ModuleManager.Modules)
            {
                mod.OnGUI();
            }


            if (Config.showgui)
            {
                TopBar.Render();

                if (Config.CurrentTab == TabType.MODULES) ModuleManager.Render();
                if (Config.CurrentTab == TabType.NEF) NEFManager.Render();
                if (Config.CurrentTab == TabType.GUI) GUIManager.Render();
                if (Config.CurrentTab == TabType.PROFILE) ProfileGUI.Render();
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[CoreGUI] Render exception: {ex}");
        }
        finally
        {
            GUI.matrix = originalMatrix;
        }

        if (Config.showgui) LockUI.BlockSKeysPatch.BlockKeys = true;
    }

    public void OnApplicationQuit()
    {
        Api.Actions.Core.OnEarlyApplicationQuit?.Invoke();
        SaveLoad.Save(true);
        Api.Actions.Core.OnLateApplicationQuit?.Invoke();
        DebugLogger.Msg("Magnetar Preferences Saved!");
    }

    public static void ResetInputBind()
    {
        UI.WindowDrawing.DrawSetting.activeDropdownId = -1;
        UI.WindowDrawing.DrawSetting.activeSliderId = -1;
        UI.WindowDrawing.DrawSetting.activeTextFieldId = -1;
        ModuleManager.bindingModuleId = -1;
    }

    public static void WarmUp()
    {
        LoadFont.Init();
        UI.Themes.ThemeManager.Init();

        ModuleManager.Render();

        Magnetar_Client.NEF.Data.NEFBanned.InitBan();
        Magnetar_Client.NEF.Data.NEFBanned.InitHidden();
        Magnetar_Client.NEF.Data.NEFRecipes.InitRecipes();

        NEFManager.Render();
        GUIManager.Render();


    }

}