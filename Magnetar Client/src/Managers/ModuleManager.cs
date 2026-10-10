using Magnetar_Client.Api;
using Magnetar_Client.Core.ModuleManager_;
using Magnetar_Client.Modules;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
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
    public enum WindowType
    {
        Modules,
        Settings,
        SelectionGUI,
    }

    public const string Group = "ModuleManager";
        private const float _baseSettingsWidth = 630f;

    public static bool IsInitialized { get; private set; }
    public static readonly List<Modules.Module> Modules = new();
    public static WindowType CurrentWindow { get; set; } = WindowType.Modules;
    public static Modules.Module ActiveSettingsModule { get; set; }
        private const float _baseSettingsWidth = 760f; // Wide enough for mobile displays
        public static float SettingsWidth => GUIManager.S(_baseSettingsWidth);
        public static float ScrollSensitivity
        {
            get => GUIManager.S(_baseScrollSensitivity);
            set => _baseScrollSensitivity = value;
        }
        public static float MultiSelectWindowWidth
        {
            get => GUIManager.S(_baseMultiSelectWindowWidth);
            set => _baseMultiSelectWindowWidth = value;
        }
        public static float MultiSelectWindowHeight
        {
            get => Mathf.Min(GUIManager.S(_baseMultiSelectWindowHeight), NativeHeight * 0.8f);
            set => _baseMultiSelectWindowHeight = value;
        }
    internal static bool resetWindowPos;

        public static float PopupSpeed = 13f;

        // Search Window
        public static float SearchAnimationSpeed = 15f;
        public static float SearchWidthMultiplier = 1.5f;

        // Settings Window
        public static float MaxSettingsWindowHeightPct = 0.8f;
        public static float SettingsScrollLerpSpeed = 15f;

        private static float _baseScrollSensitivity = 25f;

        // Multi-Select Window (Capped at 80% of screen height)
        private static float _baseMultiSelectWindowWidth = 500f;

        private static float _baseMultiSelectWindowHeight = 800f;

    public static void Init()
    {
        ModuleCategory.Init();

        #region Register All Internal Modules
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

        ServiceRegistry.Register(new ModuleManagerService());

        AnimationHandler.SetViewImmediate(Group, WindowType.Modules.ToString(), 1f);

        TabType.MODULES.OnDeselected = () =>
        {
            MultiSelectTabStorage.SaveState(TabType.MODULES);
            AnimationHandler.FadeView(Group, CurrentWindow.ToString(), 0f);
        };

        TabType.MODULES.OnSelected = () =>
        {
            MultiSelectTabStorage.RestoreState(TabType.MODULES);
            AnimationHandler.SwitchView(Group, CurrentWindow.ToString());
        };

        SaveLoad.RegisterHandler(new ModuleSaveHandler());

        IsInitialized = true;
        DebugLogger.Msg($"Loaded {Modules.Count} modules");
    }

    public static void RegisterModule(Type type)
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

            CategoryWindowDrawer.EnsureCategoryInitialized(instance.Category);
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[ModuleManager] Failed to instantiate and register '{type.FullName}': {ex}");
        }
    }

    public static void OpenModuleSettings(Modules.Module mod)
    {
        MobileInputHandler.Reset();

        CurrentWindow = WindowType.Settings;
        ActiveSettingsModule = mod;

        mod.ShowSettings = true;
        resetWindowPos = true;

        AnimationHandler.SwitchView(Group, CurrentWindow.ToString());
    }

    public static void Render()
    {
        Event currentEvent = Event.current;
        MobileInputHandler.Update(currentEvent);

        if (AnimationHandler.GetViewAlpha(Group, WindowType.Modules.ToString()) > 0.001f)
        {
            SearchWindowDrawer.Render(currentEvent);
            CategoryWindowDrawer.Render();
        }

        if (AnimationHandler.GetViewAlpha(Group, WindowType.Settings.ToString()) > 0.001f)
        {
            SettingsWindowDrawer.Render(currentEvent);
        }

        if (AnimationHandler.GetViewAlpha(Group, WindowType.SelectionGUI.ToString()) > 0.001f)
        {
            MultiSelectWindowDrawer.Render(currentEvent);
        }
    }

    public static void RenderModulesGUI()
    {
        if (Modules == null) return;
        for (int i = 0; i < Modules.Count; i++)
        {
            Modules[i]?.OnGUI();
        }
    }

    public static void OnUpdate()
    {
        if (!Config.showgui && !HUDManager.forceShow)
        {
            HandleHotkeys();
        }

        if (Modules == null) return;
        for (int i = 0; i < Modules.Count; i++)
        {
            Modules[i]?.OnUpdate();
        }
    }

    private static void HandleHotkeys()
    {
        if (IsFocused) return;

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

    public static void ResetToDefault()
    {
        if (Modules == null) return;

        foreach (var mod in Modules)
        {
            if (mod == null) continue;

            if (mod.Active)
            {
                mod.Active = false;
                try { mod.OnDisable(); } catch { }
               
            }

            mod.KeyBind?.Reset();
            mod.HoldMode = mod.defaultHoldMode;
            if (mod.Active != mod.defaultActive) mod.Toggle();

            if (mod.Settings != null)
            {
                foreach (var setting in mod.Settings)
                {
                    try { setting?.Reset(); } catch { }
                   
                }
            }
        }
    }

    private class ModuleManagerService : IWarmUp, IUpdatable, IRenderable, IMenuRenderable, ICloseHandler, ILanguageAware
    {
        public string Name => "ModuleManager";
        public int Priority => ServicePriority.Modules;

        public void OnWarmUp() => Render();
        public void OnUpdate() => ModuleManager.OnUpdate();
        public void OnGUI() => RenderModulesGUI();

        public void OnMenuGUI()
        {
            AnimationHandler.RenderWithTabAlpha(TabType.MODULES, ModuleManager.Render);
            }

        public bool OnEscapePressed()
        {
            if (Config.CurrentTab != TabType.MODULES) return false;

                if (DrawSetting.IsFocused)
                {
                    DrawSetting.ResetInputBind();
                    return true;
                }

            if (CurrentWindow != WindowType.Modules)
                {
                switch (CurrentWindow)
                    {
                    case WindowType.Settings:
                        if (ActiveSettingsModule != null)
                            {
                            ActiveSettingsModule.ShowSettings = false;
                            }
                        CurrentWindow = WindowType.Modules;
                            break;
                    case WindowType.SelectionGUI:
                        CurrentWindow = WindowType.Settings;
                            break;
                }

                AnimationHandler.SwitchView(Group, CurrentWindow.ToString());
                    DrawSetting.ResetInputBind();
                    return true;
                }

            return false;
        }

        public void OnLanguageChanged() => ModuleTranslationHandler.OnLanguageChanged();

        public bool CanClose()
        {
            if (Config.CurrentTab == TabType.MODULES)
                {
                return CurrentWindow == WindowType.Modules && !DrawSetting.IsFocused;
                }
            return true;
            }
    }
}