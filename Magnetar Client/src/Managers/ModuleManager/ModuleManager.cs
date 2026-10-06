using Magnetar_Client.Api;
using Magnetar_Client.Core.Lifecycle;
using Magnetar_Client.Modules;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Magnetar_Client.UI.WindowDrawing.DrawSetting;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public static class ModuleManager
{
    public static bool IsInitialized { get; private set; } = false;
    public static List<Modules.Module> Modules = new();
    public static bool showModules = true;
    public static bool showSettings = false;
    public static bool showSelectionGui = false;
    public static Modules.Module activeSettingsModule = null;

    public static bool resetWindowPos = false;
    public static int bindingModuleId = -1;

    public static Dictionary<ModuleCategory, Rect> windowPositions => CategoryWindowDrawer.WindowPositions;
    public static string ModuleSearchQuery
    {
        get => SearchWindowDrawer.SearchQuery;
        set => SearchWindowDrawer.SearchQuery = value;
    }

    /// <summary>
    /// Computes whether ModuleManager is in a clean root state where closing the GUI is safe.
    /// </summary>
    public static bool SafeToClose
    {
        get
        {
            return showModules
                   && bindingModuleId == -1
                   && focusedControlId == -1
                   && activeTextFieldId == -1
                   && activeDropdownId == -1
                   && activeSliderId == -1;
        }
    }

    public static void Init()
    {
        ModuleCategory.Init();

        #region Register All Interal Modules
        Type[] exportedTypes;
        try
        {
            exportedTypes = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            exportedTypes = ex.Types.Where(t => t != null).ToArray();
        }

        var moduleTypes = exportedTypes
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && typeof(Modules.Module).IsAssignableFrom(t)
                        && t.Namespace != null
                        && t.Namespace.StartsWith("Magnetar_Client.Modules", StringComparison.Ordinal))
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var type in moduleTypes)
        {
            RegisterModule(type);
        }

        #endregion

        CategoryWindowDrawer.InitializeLayout();
        MultiSelectWindowDrawer.InitializeLayout();
        SearchWindowDrawer.Initialize();

        showModules = true;
        showSettings = false;
        showSelectionGui = false;

        // Register to centralized ServiceRegistry and SafeToCloseManager
        ServiceRegistry.Register(new ModuleManagerService());

        SafeToCloseManager.RegisterGuard(() =>
        {
            if (Config.CurrentTab == TabType.MODULES)
            {
                return SafeToClose;
            }
            return true;
        });

        SafeToCloseManager.RegisterInterceptor(() =>
        {
            if (Config.CurrentTab != TabType.MODULES) return false;

            // 1. Cancel active binding or text input
            if (bindingModuleId != -1 || focusedControlId != -1 || activeTextFieldId != -1)
            {
                Main.ResetInputBind();
                return true;
            }

            // 2. Step back from Selection GUI to Settings
            if (showSelectionGui)
            {
                showSettings = true;
                showSelectionGui = false;
                Main.ResetInputBind();
                return true;
            }

            // 3. Step back from Settings to Modules root
            if (showSettings)
            {
                showModules = true;
                showSettings = false;
                showSelectionGui = false;

                if (Modules != null)
                {
                    for (int i = 0; i < Modules.Count; i++)
                    {
                        if (Modules[i] != null)
                            Modules[i].ShowSettings = false;
                    }
                }

                Main.ResetInputBind();
                return true;
            }

            return false;
        });

        IsInitialized = true;
        DebugLogger.Msg($"Loaded {Modules.Count} modules");
    }

    internal static void RegisterModule(Type type)
    {
        if (type == null)
        {
            DebugLogger.Error("[ModuleManager] Cannot register a null module type.");
            return;
        }

        if (!typeof(Modules.Module).IsAssignableFrom(type) || type.IsAbstract || !type.IsClass)
        {
            DebugLogger.Error($"[ModuleManager] Type '{type.FullName}' must be a non-abstract class derived from '{nameof(Magnetar_Client.Modules.Module)}'.");
            return;
        }

        if (Modules.Exists(m => m.GetType() == type))
        {
            DebugLogger.Warning($"[ModuleManager] Module '{type.Name}' is already registered. Skipping.");
            return;
        }

        try
        {
            var instance = (Modules.Module)Activator.CreateInstance(type);
            Modules.Add(instance);

            // Automatically ensure the category window is mapped
            if (instance.Category != null)
            {
                CategoryWindowDrawer.EnsureCategoryInitialized(instance.Category);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[ModuleManager] Failed to instantiate and register '{type.FullName}': {ex}");
        }
    }

    public static void OpenModuleSettings(Modules.Module mod)
    {
        MobileInputHandler.Reset();
        showModules = false;
        showSelectionGui = false;
        showSettings = true;
        activeSettingsModule = mod;

        foreach (var m in Modules)
            m.ShowSettings = false;

        mod.ShowSettings = true;
        resetWindowPos = true;

        // Trigger smooth alpha dip and glide
        UIAnimationHelper.TriggerSubWindowTransition();
    }

    public static void RenderCheck()
    {
        if (Config.CurrentTab == TabType.MODULES)
        {
            Render();
        }
    }

    public static void Render()
    {
        Event currentEvent = Event.current;
        MobileInputHandler.Update(currentEvent);

        if (showModules)
        {
            resetWindowPos = true;
            SearchWindowDrawer.Render(currentEvent);
            CategoryWindowDrawer.Render();
        }
        else if (showSettings)
        {
            SettingsWindowDrawer.Render(currentEvent);
        }
        else if (showSelectionGui)
        {
            MultiSelectWindowDrawer.Render(currentEvent);
        }
    }

    public static void RenderModulesGUI()
    {
        if (Modules != null)
        {
            for (int i = 0; i < Modules.Count; i++)
            {
                Modules[i]?.OnGUI();
            }
        }
    }

    public static void OnUpdate()
    {
        if (!Config.showgui && !HUDManager.forceShow)
        {
            HandleHotkeys();
        }

        for (int i = 0; i < Modules.Count; i++)
        {
            Modules[i]?.OnUpdate();
        }
    }

    static void HandleHotkeys()
    {
        if (focusedControlId != -1 || bindingModuleId != -1) return;

        foreach (var mod in Modules)
        {
            if (mod.BindKeys == null || mod.BindKeys.Count == 0) continue;

            bool allKeysHeld = true;
            bool anyKeyJustPressed = false;
            bool anyKeyJustReleased = false;

            foreach (KeyCode key in mod.BindKeys)
            {
                if (!Input.GetKey(key)) allKeysHeld = false;
                if (Input.GetKeyDown(key)) anyKeyJustPressed = true;
                if (Input.GetKeyUp(key)) anyKeyJustReleased = true;
            }

            if (mod.HoldMode)
            {
                if (allKeysHeld && anyKeyJustPressed && !mod.Active)
                {
                    if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                }
                else if (mod.Active && anyKeyJustReleased)
                {
                    if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                }
            }
            else
            {
                if (allKeysHeld && anyKeyJustPressed)
                {
                    if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                }
            }
        }
    }

    private class ModuleManagerService : IInitializable, IWarmUp, IUpdatable, IRenderable, IMenuRenderable, ICloseHandler, ILanguageAware
    {
        public string Name => "ModuleManager";
        public int Priority => ServicePriority.Modules;

        public void Initialize() { }
        public void OnWarmUp() => Render();
        public void OnUpdate() => ModuleManager.OnUpdate();
        public void OnGUI() => RenderModulesGUI();

        public void OnMenuGUI()
        {
            if (Config.CurrentTab == TabType.MODULES)
            {
                Render();
            }
        }

        public bool CanClose() => ModuleManager.SafeToClose;

        public bool OnEscapePressed()
        {
            // Inside your Escape back-handler when closing Settings:
            if (ModuleManager.showSettings)
            {
                ModuleManager.showModules = true;
                ModuleManager.showSettings = false;
                ModuleManager.showSelectionGui = false;

                if (ModuleManager.Modules != null)
                {
                    for (int i = 0; i < ModuleManager.Modules.Count; i++)
                    {
                        if (ModuleManager.Modules[i] != null)
                            ModuleManager.Modules[i].ShowSettings = false;
                    }
                }

                Main.ResetInputBind();
                UIAnimationHelper.TriggerSubWindowTransition(); // Smoothly blends back into Category Windows
                return true;
            }
            return false;
        }

        public void OnLanguageChanged()
        {
            if (Modules == null) return;
            for (int i = 0; i < Modules.Count; i++)
            {
                Modules[i]?.OnLanguageChanged();
            }
        }
    }
}