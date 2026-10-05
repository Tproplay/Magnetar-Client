using System;
using Magnetar_Client.Core;
using Magnetar_Client.HUDElements;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;

namespace Magnetar_Client.Api;

public static class MagnetarApi
{
    /// <summary>
    /// Registers a theme definition from an addon or internal module,
    /// synchronizes the active theme options in GUIManager, and optionally applies it immediately.
    /// </summary>
    /// <param name="theme">The theme definition to register.</param>
    /// <param name="applyImmediately">If true, immediately applies and rescales the theme across the client.</param>
    public static void RegisterTheme(ThemeDefinition theme, bool applyImmediately = false)
    {
        if (theme == null || string.IsNullOrWhiteSpace(theme.Name))
        {
            Magnetar_Logger.DebugLogger.Error("[ThemeManager] Cannot register a null or unnamed theme.");
            return;
        }

        string themeKey = theme.Name.Trim();
        ThemeData.LoadedThemes[themeKey] = theme;

        // Refresh dropdown options in GUIManager if it has been initialized
        if (GUIManager.ThemeSetting != null)
        {
            GUIManager.RefreshThemeOptions();
        }

        if (applyImmediately)
        {
            Magnetar_Client.UI.Themes.ThemeManager.ApplyTheme(themeKey);
        }

        Magnetar_Logger.DebugLogger.Msg($"[ThemeManager] Registered theme: '{themeKey}'");
    }

    public static void RegisterModule<T>() where T : Module, new()
    {
        ModuleManager.RegisterModule(typeof(T));
    }

    public static void RegisterModule(Type type)
    {
        ModuleManager.RegisterModule(type);
    }

    public static Module FindModule(string name)
    {
        foreach (Module module in ModuleManager.Modules)
        {
            if (module.Name == name) return module;
        }
        return null;
    }

    public static T FindModule<T>() where T : Module
    {
        foreach (Module module in ModuleManager.Modules)
        {
            if (module is T target)
            {
                return target;
            }
        }
        return null;
    }

    public static void LogMsg(string msg)
    {
        Magnetar_Logger.DebugLogger.Msg(msg);
    }

    public static void LogWarning(string msg)
    {
        Magnetar_Logger.DebugLogger.Warning(msg);
    }

    public static void LogError(string msg)
    {
        Magnetar_Logger.DebugLogger.Error(msg);
    }

    public static void RegisterElement<T>() where T : HudElement => Magnetar_Client.Core.HUDRenderer.RegisterElement(typeof(T));

}