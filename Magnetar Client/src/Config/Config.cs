using System;
#if MELONLOADER || RELEASE_MELON
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx.Configuration;
#endif
using Magnetar_Client.Utils;
using Magnetar_Client.Core;
using UnityEngine;
using Magnetar_Client.Api;

namespace Magnetar_Client;

public static class Config
{
    // Preferences
    public static KeyCode MenuOpenKey => Preferences.ModMenuKeyEntry!=null ? Preferences.ModMenuKeyEntry.Value : KeyCode.RightShift;
    public const string DefaultProfile = "Default";
    public static string CurrentProfile { get; set; } = "Default";
    private static string _theme = "Magnetar Default";
    public static string Theme
    {
        get => _theme;
        set
        {
            if (string.Equals(_theme, value, System.StringComparison.OrdinalIgnoreCase))
                return;

            _theme = value;
            Magnetar_Client.UI.Themes.ThemeManager.ApplyTheme(_theme);
        }
    }
    private static string _language = "English";
    public static string Language
    {
        get => _language;
        set
        {
            if (string.Equals(_language, value, StringComparison.OrdinalIgnoreCase))
                return;

            _language = value;

            _ = Translator.LoadTranslationsAsync(() =>
            {
                ServiceRegistry.NotifyLanguageChanged();
                Actions.OnLanguageChanged?.Invoke();
            });
        }
    }

    // GUI
    public static bool showgui;
    public static bool dimBg;
    public static TabType CurrentTab = TabType.MODULES;


    // GUI Settings
    public static float RainbowSpeed = 0.07f;
    public const float NativeWidth = 1920;
    public const float NativeHeight = 1080;

    // GUI Width
    private const float BaseModuleWindowWidth = 200f;
    public static float ModuleWindowWidth => GUIManager.S(BaseModuleWindowWidth);

    private const float BaseElementHeight = 22f;
    public static float elementHeight => GUIManager.S(BaseElementHeight);

    private const float BaseIndent = 8f;
    public static float indent => GUIManager.S(BaseIndent);

    private const float BaseSpacing = 6f;
    public static float spacing => GUIManager.S(BaseSpacing);

    private const float BaseSelectButtonWidth = 70f;
    public static float selectButtonWidth => GUIManager.S(BaseSelectButtonWidth);


    private const float _baseSettingWidth = 200f;
    public static float SettingWidth => GUIManager.S(_baseSettingWidth);

    /// <summary>
    /// Resizes a Rect to the given width/height while keeping its current
    /// center point fixed. Used for draggable windows so they grow/shrink
    /// around wherever the user last dragged them when GUIScale changes,
    /// instead of always resetting to a default position.
    /// </summary>
    public static void RescaleAroundCenter(ref Rect rect, float newWidth, float newHeight)
    {
        float centerX = rect.x + rect.width / 2f;
        float centerY = rect.y + rect.height / 2f;

        rect.width = newWidth;
        rect.height = newHeight;
        rect.x = centerX - newWidth / 2f;
        rect.y = centerY - newHeight / 2f;
    }
}
