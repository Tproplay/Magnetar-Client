using System;
using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;

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

}