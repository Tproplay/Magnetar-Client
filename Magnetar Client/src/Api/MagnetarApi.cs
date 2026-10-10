//
// Magneat Api gives direct access to many functionality
// that makes it easier to interact with the mod
//

using Magnetar_Client.Core;
using Magnetar_Client.HUDElements;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;

namespace Magnetar_Client.Api;

public static class MagnetarApi
{
    // Register
    public static void RegisterService(IClientService service) => ServiceRegistry.Register(service);
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
    public static void RegisterModule<T>() where T : Module, new() => ModuleManager.RegisterModule(typeof(T));
    public static void RegisterModule(Type module) =>  ModuleManager.RegisterModule(module);
    public static void RegisterElement<T>() where T : HudElement => RegisterElement(typeof(T));
    public static void RegisterElement(Type element) => HUDManager.RegisterElement(element);

    // Logging
    public static void LogMsg(string msg) => Magnetar_Logger.DebugLogger.Msg(msg);
    public static void LogWarning(string msg) => Magnetar_Logger.DebugLogger.Warning(msg);
    public static void LogError(string msg) => Magnetar_Logger.DebugLogger.Error(msg);

    
}