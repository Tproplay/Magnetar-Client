using Magnetar_Client.Core;
using Magnetar_Client.HUDElements;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;

namespace Magnetar_Client.Api;

/// <summary>
/// Magnetar API — provides access to core functionality for use in addons and modules.
/// </summary>
public static class MagnetarApi
{
    // --- Registration ---

    /// <summary>
    /// Registers a client service with the service registry.
    /// </summary>
    public static void RegisterService(IClientService service)
    {
        if (service == null)
        {
            Magnetar_Logger.DebugLogger.Error("[API] Cannot register a null service.");
            return;
        }
        ServiceRegistry.Register(service);
    }

    /// <summary>
    /// Registers a save handler with the SaveLoad system.
    /// </summary>
    public static void RegisterSaveHandler(ISaveHandler handler)
    {
        if (handler == null)
        {
            Magnetar_Logger.DebugLogger.Error("[API] Cannot register a null save handler.");
            return;
        }
        SaveLoad.RegisterHandler(handler);
    }

    /// <summary>
    /// Registers a theme. Optionally applies it immediately.
    /// </summary>
    public static void RegisterTheme(ThemeDefinition theme, bool applyImmediately = false)
    {
        if (theme == null || string.IsNullOrWhiteSpace(theme.Name))
        {
            Magnetar_Logger.DebugLogger.Error("[API] Cannot register a null or unnamed theme.");
            return;
        }

        string themeKey = theme.Name.Trim();
        ThemeData.LoadedThemes[themeKey] = theme;

        if (GUIManager.ThemeSetting != null)
        {
            GUIManager.RefreshThemeOptions();
        }

        if (applyImmediately)
        {
            ThemeManager.ApplyTheme(themeKey);
        }
    }

    /// <summary>
    /// Registers a module by generic type parameter.
    /// </summary>
    public static void RegisterModule<T>() where T : Module, new() => RegisterModule(typeof(T));

    /// <summary>
    /// Registers a module by type.
    /// </summary>
    public static void RegisterModule(Type moduleType)
    {
        if (moduleType == null)
        {
            Magnetar_Logger.DebugLogger.Error("[API] Cannot register a null module type.");
            return;
        }
        ModuleManager.RegisterModule(moduleType);
    }

    /// <summary>
    /// Registers a HUD element by generic type parameter.
    /// </summary>
    public static void RegisterElement<T>() where T : HudElement => RegisterElement(typeof(T));

    /// <summary>
    /// Registers a HUD element by type.
    /// </summary>
    public static void RegisterElement(Type elementType)
    {
        if (elementType == null)
        {
            Magnetar_Logger.DebugLogger.Error("[API] Cannot register a null HUD element type.");
            return;
        }
        HUDManager.RegisterElement(elementType);
    }

    // --- Lookup ---

    /// <summary>
    /// Returns the first registered module of type <typeparamref name="T"/>, or null if not found.
    /// </summary>
    public static T GetModule<T>() where T : Module
        => ModuleManager.Modules.Find(m => m is T) as T;

    /// <summary>
    /// Returns the first registered service of type <typeparamref name="T"/>, or null if not found.
    /// </summary>
    public static T GetService<T>() where T : class, IClientService
    {
        foreach (var svc in ServiceRegistry.Services)
        {
            if (svc is T typed) return typed;
        }
        return null;
    }

    // --- Logging ---

    /// <summary>Logs an informational message.</summary>
    public static void LogMsg(string msg) => Magnetar_Logger.DebugLogger.Msg(msg);

    /// <summary>Logs a warning message.</summary>
    public static void LogWarning(string msg) => Magnetar_Logger.DebugLogger.Warning(msg);

    /// <summary>Logs an error message.</summary>
    public static void LogError(string msg) => Magnetar_Logger.DebugLogger.Error(msg);
}
