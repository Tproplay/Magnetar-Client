using Magnetar_Client.Api;
using Magnetar_Client.UI;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using System.Reflection;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

/// <summary>
/// The Core of Magnetar Client that handles everything
/// </summary>
public class Main
{
    /// <summary>
    /// Singleton Instance
    /// </summary>
    public static Main Instance { get; private set; }
    /// <summary>
    /// Harmony in which all of the client's patches are registered
    /// </summary>
    public static HarmonyLib.Harmony HarmonyInstance { get; private set; }
    private bool HasWarmedUp;

    internal static void Initialize()
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

        Api.Actions.Invoke_OnEarlyInitialize();

        // Load the IAddons first so that they can register their actions and sevices
        AddonManager.InitAddons();

        // Apply the harmony patches
        ApplyHarmonyPatches();
        Actions.Invoke_OnInitializeHarmony();

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
        Api.Actions.Invoke_OnEarlyInitializeCore();

        PreferenceManager.InitializePreferences();
        ResourceManager.LoadTextureOverrides();
        GameData.Init();

        TopBar.Init();
        MobileMenuUI.Init();

        // 1. Initialize Built-in Managers
        ModuleManager.Init();
        HUDManager.Init();
        GUIManager.Init();
        NEFManager.Init();
        ProfileGUI.Init();


        // 2. Discover and register Addons
        AddonManager.InitModules();
        AddonManager.InitHUDElements();
        AddonManager.InitServices();

        // 3. Run unified pipeline across all registered services
        ServiceRegistry.InitializeAll();

        Actions.Invoke_OnInitializeCore();

        // 4. Load state, themes, and translations
        SaveLoad.Load();
        Translator.EnsureInitialized();

#if NIGHTLY
        // 5. Check for nightly data-file updates in the background
        NightlyUpdater.CheckAsync();
#endif

        Api.Actions.Invoke_OnLateInitializeCore();

        DebugLogger.Msg("Magnetar Client Loaded!");
    }

    internal void OnUpdate()
    {
        LockUI.BlockKeys = false;

        UI.GUIHelper.OnUpdate();
        AnimationHandler.UpdateTransition();
        Translator.Update();

        // Menu Toggle
        if (Input.GetKeyDown(ClientConfig.MenuOpenKey) && !HUDManager.forceShow)
        {
            if (!ClientConfig.showgui)
            {
                ClientConfig.showgui = true;
                Actions.Invoke_OnMenuOpen();
            }
            else if (ClientConfig.MenuOpenKey != KeyCode.Escape)
            {
                ClientConfig.showgui = false;
                DrawSetting.ResetInputBind();
                SaveLoad.Save();
                Actions.Invoke_OnMenuClosed();
            }
        }

        // Extra controls for GUI
        if (ClientConfig.InGUI && !DrawSetting.IsFocused)
        {
            // Tab cycling (Tab for next, Shift+Tab for previous)
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (SafeToCloseManager.CanSwitchTab())
                {
                    var tabList = TopBar.Order;
                    if (tabList.Count > 0)
                    {
                        int index = tabList.IndexOf(ClientConfig.CurrentTab);
                        bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                        int nextIndex;
                        if (reverse)
                        {
                            nextIndex = (index <= 0) ? tabList.Count - 1 : index - 1;
                        }
                        else
                        {
                            nextIndex = (index == -1 || index >= tabList.Count - 1) ? 0 : index + 1;
                        }

                        TopBar.SwitchTab(tabList[nextIndex]);
                    }
                }
            }
        }

        Api.Actions.Invoke_OnUpdate();
        ServiceRegistry.UpdateAll();

        #region Handle Escape
        if (ClientConfig.showgui)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Check if Escape was consumed by an input field in OnGUI this frame
                bool consumedByTextField = (Time.frameCount == DrawSetting.LastEscapeFrame)
                                        || (Time.unscaledTime - DrawSetting.LastEscapeTime < 0.15f);

                if (consumedByTextField || DrawSetting.IsFocused)
                {
                    DrawSetting.ResetInputBind();
                    DrawSetting.LastEscapeTime = Time.unscaledTime;
                    DrawSetting.LastEscapeFrame = Time.frameCount;
                    Input.ResetInputAxes();
                    return;
                }

                if (SafeToCloseManager.TryInterceptEscape())
                {
                    Input.ResetInputAxes();
                    return;
                }

                if (SafeToCloseManager.CanClose())
                {
                    ClientConfig.showgui = false;
                    SaveLoad.Save();
                    Input.ResetInputAxes();
                }
                DrawSetting.ResetInputBind();
            }
        }
        #endregion

        if (ClientConfig.InGUI) LockUI.BlockKeys = true;
    }

    internal void OnGUI()
    {
        LockUI.BlockKeys = false;

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

            Actions.Invoke_OnEarlyGUI();
            ServiceRegistry.RenderAll();

            // Render menus with smooth fade transition
            if (AnimationHandler.ShouldRenderGUI)
            {
                Color prevGuiColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, AnimationHandler.FadeProgress);

                ClientConfig.CurrentTab?.OnGUI?.Invoke();
                ServiceRegistry.RenderMenuAll();
                Actions.Invoke_OnGUIShow();
                // Render TopBar on top
                TopBar.Render();

                GUI.color = prevGuiColor;
            }

            Actions.Invoke_OnLateGUI();
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[CoreGUI] Render exception: {ex}");
        }
        finally
        {
            GUI.matrix = originalMatrix;
        }

        if (ClientConfig.InGUI) LockUI.BlockKeys = true;
    }

    internal void OnApplicationQuit()
    {
        Api.Actions.Invoke_OnApplicationQuit();
        ServiceRegistry.QuitAll();
        SaveLoad.Save(true);
        DebugLogger.Msg("Preferences Saved!");
    }

    internal static void WarmUp()
    {
        ResourceManager.Init();
        UI.Themes.ThemeManager.Init();

        ServiceRegistry.WarmUpAll();

        Api.Actions.Invoke_OnWarmUp();
    }
}