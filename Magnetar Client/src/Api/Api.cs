using System;
using Magnetar_Client.Core;
using Magnetar_Client.HUDElements;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;

namespace Magnetar_Client.Api;

public static class MagnetarApi
{
    public static void RegisterTheme(ThemeData themeData)
    {
        ThemeManager.LoadedThemes[themeData.Name] = themeData;
    }

    public static void RegisterModule<T>() where T : Module, new()
    {
        ModuleManager.RegisterModule(typeof(T));
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

    public static void RegisterHUDElement(HudElement hudElement)
    {
        HUDRenderer.RegisterElement(hudElement);
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

    public static string ModsDir => PathsManager.ModsDir;
    public static string AddonsDir => PathsManager.AddonsDir;


}