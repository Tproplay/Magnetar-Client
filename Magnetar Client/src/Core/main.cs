using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Magnetar_Logger;
using System.Reflection;
using Magnetar_Client.UI;
using Magnetar_Client.Api;
using Magnetar_Client.Core.Lifecycle;

namespace Magnetar_Client.Core;

public class Main
{
    public static Main Instance { get; private set; }
    public static HarmonyLib.Harmony HarmonyInstance { get; private set; }
    public bool HasWarmedUp { get; private set; } = false;

    public static bool SafeToClose => SafeToCloseManager.CanClose();

    public static void Initialize()
    {
        if (Instance != null)
        {
            DebugLogger.Error("[Core] Attempted to initialize Main multiple times! Aborting duplicate call.");
            return;
        }
        Instance = new Main();

        // Initialize the logger first so subsequent diagnostics are captured
        Utils.Magnetar_Logger.Init();

        // Load the IAddons first so that they can register their actions and sevices
        AddonManager.InitAddons();

        Api.Actions.Core.OnEarlyInitialize?.Invoke();

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

        // Log detailed failure diagnostics and exceptions if any patch failed
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

        // 1. Initialize Built-in Managers (they self-register into ServiceRegistry)
        ModuleManager.Init();
        HUDManager.Init();
        HUDRenderer.Init();
        NEFManager.Init();
        TopBar.Init();
        ProfileGUI.Init();
        MobileMenuUI.Init();
        GUIManager.Init();

        // 2. Discover and register Addon modules, HUD elements, and services
        AddonManager.InitModules();
        AddonManager.InitHUDElements();
        AddonManager.InitServices();

        // 3. Load state, themes, and translations
        Translator.LoadTranslations();
        SaveLoad.Load();

        // 4. Run unified pipeline across all registered services
        ServiceRegistry.InitializeAll();

        Api.Actions.Core.OnLateInitializeCore?.Invoke();

        DebugLogger.Msg("Magnetar Client Loaded!");
    }

    public void OnUpdate()
    {
        LockUI.BlockSKeysPatch.BlockKeys = false;

        UI.GUIHelper._UpdateRainbowColor();
        UIAnimationHelper.UpdateTransition();

        if (Input.GetKeyDown(KeyCode.RightShift) && !HUDManager.forceShow)
        {
            Config.showgui = !Config.showgui;
            if (!Config.showgui) SaveLoad.Save();
        }

        Api.Actions.Core.OnUpdate?.Invoke();
        ServiceRegistry.UpdateAll();

        #region Handle Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SafeToCloseManager.TryInterceptEscape())
            {
                Input.ResetInputAxes();
                return;
            }

            if (SafeToClose)
            {
                Config.showgui = false;
                SaveLoad.Save();
                ResetInputBind();
                Input.ResetInputAxes();
            }
            else
            {
                ResetInputBind();
            }
        }
        #endregion

        if (Config.showgui) LockUI.BlockSKeysPatch.BlockKeys = true;
    }

    public void OnGUI()
    {
        LockUI.BlockSKeysPatch.BlockKeys = false;

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

            // Continuous overlays / HUD
            ServiceRegistry.RenderAll();

            // Render menus with smooth fade transition
            if (UIAnimationHelper.ShouldRenderGUI)
            {
                Color prevGuiColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, UIAnimationHelper.FadeProgress);

                TopBar.Render();
                Config.CurrentTab?.OnGUI?.Invoke();

                ServiceRegistry.RenderMenuAll();

                GUI.color = prevGuiColor;
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
        ServiceRegistry.QuitAll();
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

        ServiceRegistry.WarmUpAll();

        Api.Actions.Core.OnWarmUp?.Invoke();
    }
}