using Magnetar_Client.Api;
using Magnetar_Client.UI;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using System.Reflection;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public class Main
{
    public static Main Instance { get; private set; }
    public static HarmonyLib.Harmony HarmonyInstance { get; private set; }
    public bool HasWarmedUp { get; private set; }

    public static bool SafeToClose => SafeToCloseManager.CanClose();

    public static void Initialize()
    {
        if (Instance != null)
        {
            DebugLogger.Error("Attempted to initialize Main multiple times! Aborting duplicate call.");
            return;
        }
        Instance = new Main();

        // Initialize the logger first so subsequent diagnostics are captured
        Utils.Magnetar_Logger.Init();

        DebugLogger.Msg("Initializing Magnetar Client...");

        // Load the IAddons first so that they can register their actions and sevices
        AddonManager.InitAddons();

        Api.Actions.OnEarlyInitialize?.Invoke();

        // Apply the harmony patches
        ApplyHarmonyPatches();

        // Initialize core systems and modules
        Instance.InitializeCore();
    }

    static void ApplyHarmonyPatches()
    {
        HarmonyInstance = new HarmonyLib.Harmony(MagnetarInfo.HarmonyId);

        Assembly currentAssembly = typeof(Main).Assembly;

        HarmonyPatchInfo patchInfo = HarmonyManager.HarmonyPatchAll(currentAssembly, HarmonyInstance);

        DebugLogger.Msg($"Applied all Harmony patches - Succeeded: {patchInfo.SuccessCount} | Failed: {patchInfo.FailCount}");

        // Log detailed failure diagnostics and exceptions if any patch failed
        if (patchInfo.HasFailures)
        {
            DebugLogger.Warning($"{patchInfo.FailCount} patch classes failed to apply:");
            for (int i = 0; i < patchInfo.Failures.Count; i++)
            {
                var failure = patchInfo.Failures[i];
                DebugLogger.Error($"-> Failure #{i + 1} on '{failure.ClassName}':\nException: {failure.Exception.GetType().Name} - {failure.Exception.Message}\nStack: {failure.Exception.StackTrace}");
            }
        }
    }

    void InitializeCore()
    {
        Api.Actions.OnEarlyInitializeCore?.Invoke();

        PreferenceManager.InitializePreferences();

        TopBar.Init();
        MobileMenuUI.Init();

        // 1. Initialize Built-in Managers (they self-register into ServiceRegistry)
        ModuleManager.Init();
        HUDManager.Init();
        GUIManager.Init();
        NEFManager.Init();
        ProfileGUI.Init();
        

        // 2. Discover and register Addon modules, HUD elements, and services
        AddonManager.InitModules();
        AddonManager.InitHUDElements();
        AddonManager.InitServices();

        // 3. Run unified pipeline across all registered services
        ServiceRegistry.InitializeAll();

        // 4. Load state, themes, and translations
        SaveLoad.Load();

        Api.Actions.OnLateInitializeCore?.Invoke();

        DebugLogger.Msg("Magnetar Client Loaded!");
    }

    public void OnUpdate()
    {
        LockUI.BlockSKeysPatch.BlockKeys = false;

        UI.GUIHelper.OnUpdate();
        AnimationHandler.UpdateTransition();
        Translator.Update();

        if (Input.GetKeyDown(ClientConfig.MenuOpenKey) && !HUDManager.forceShow)
        {
            ClientConfig.showgui = !ClientConfig.showgui;
            if (!ClientConfig.showgui) SaveLoad.Save();
        }

        Api.Actions.OnUpdate?.Invoke();
        ServiceRegistry.UpdateAll();

        #region Handle Escape
        if (ClientConfig.showgui)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (SafeToCloseManager.TryInterceptEscape())
                {
                    Input.ResetInputAxes();
                    return;
                }

                if (SafeToClose)
                {
                    ClientConfig.showgui = false;
                    SaveLoad.Save();
                    Input.ResetInputAxes();
                }
                DrawSetting.ResetInputBind();
            }
        }
        
        #endregion

        if (ClientConfig.InGUI) LockUI.BlockSKeysPatch.BlockKeys = true;
    }

    public void OnGUI()
    {
        LockUI.BlockSKeysPatch.BlockKeys = false;

        Matrix4x4 originalMatrix = GUI.matrix;

        try
        {
            float scaleX = Screen.width / ClientConfig.NativeWidth;
            float scaleY = Screen.height / ClientConfig.NativeHeight;
            float uniformScale = Mathf.Min(scaleX, scaleY);

            float offsetX = (Screen.width - (ClientConfig.NativeWidth * uniformScale)) * 0.5f;
            float offsetY = (Screen.height - (ClientConfig.NativeHeight * uniformScale)) * 0.5f;

            GUI.matrix = Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0),
                Quaternion.identity,
                new Vector3(uniformScale, uniformScale, 1)
            );

            // On first frame we call the WarmUp method
            if (!HasWarmedUp)
            {
                WarmUp();
                HasWarmedUp = true;
            }

            UI.Themes.ThemeManager.Rescale();

            Actions.OnGUI?.Invoke();
            ServiceRegistry.RenderAll();

            // Render menus with smooth fade transition
            if (AnimationHandler.ShouldRenderGUI)
            {
                Color prevGuiColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, AnimationHandler.FadeProgress);
                
                ClientConfig.CurrentTab?.OnGUI?.Invoke();
                ServiceRegistry.RenderMenuAll();

                // Render TopBar on top
                TopBar.Render();
                
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

        if (ClientConfig.showgui) LockUI.BlockSKeysPatch.BlockKeys = true;
    }

    public void OnApplicationQuit()
    {
        Api.Actions.OnApplicationQuit?.Invoke();
        ServiceRegistry.QuitAll();
        SaveLoad.Save(true);
        DebugLogger.Msg("Preferences Saved!");
    }

    public static void WarmUp()
    {
        ResourceManager.Init();
        UI.Themes.ThemeManager.Init();

        ServiceRegistry.WarmUpAll();

        Api.Actions.OnWarmUp?.Invoke();
    }
}