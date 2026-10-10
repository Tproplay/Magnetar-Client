using Il2CppSystem.Threading.Tasks;
using Magnetar_Client.Api;
using Magnetar_Client.Core.GUIManager_;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public static class GUIManager
{
    public const string Group = "GUI";
    public const string ViewMain = "Controls";
    public const string ViewSelector = "Selector";

    public static bool isSelectingSubWindow => AnimationHandler.GetViewAlpha(Group, ViewSelector) > 0.001f;

    public static MultiSelectSetting LanguageSetting { get; } = new("Language")
    {
        MaxSelection = 1,
        Options = new Dictionary<int, string> { { 0, "English" } },
        CustomNames = new Dictionary<int, string>()
    };

    public static MultiSelectSetting ThemeSetting { get; } = new("Theme")
    {
        MaxSelection = 1,
        Options = new Dictionary<int, string>(),
        CustomNames = new Dictionary<int, string>()
    };

    public static FloatSetting ScaleSetting = new("GUI Scale", 0.5f, 2.0f,
#if ANDROID
        1.5f;
#else
        1,
#endif
        decimalPlaces: 2, trueMin: 0.25f, trueMax: 3.0f);

    public static FloatSetting ElementScaleSetting = new("Element Scale", 0.5f, 2.0f, 1,
        decimalPlaces: 2, trueMin: 0.25f, trueMax: 3.0f);

    public static FloatSetting FloatingIconOpacitySetting = new("Icon Opacity", 0.1f, 1.0f, 0.85f,
        decimalPlaces: 3, trueMin: 0.05f, trueMax: 1.0f);

    private const float BaseElementHeight = 25f;
    public static float elementHeight => GUIManager.S(BaseElementHeight);

    static bool _showMobileButtons;

    public static bool ShowFloatingIcon;
    public static float FloatingIconIdleTimeout = 4f;
    public static bool ShowMobileButtons
    {
        get => _showMobileButtons;
        set
        {
            if (_showMobileButtons == value) return;
            _showMobileButtons = value;

#if MELONLOADER || RELEASE_MELON
            if (Preferences.ShowMobileButtonsEntry != null)
            {
                Preferences.ShowMobileButtonsEntry.Value = value;
            }
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
            if (Preferences.ShowMobileButtonsEntry != null)
            {
                Preferences .ShowMobileButtonsEntry.Value = value;
                Preferences.BepInExConfig?.Save();
            }
#endif
        }
    }

    public static bool ShowMainMenuCredits;

    public static void OnClose()
    {
        AnimationHandler.SwitchView(Group, ViewMain);
        GUISelectorDrawer.ActiveSetting = null;
        DrawSetting.activeMultiSelect = null;
    }

    public static void Init()
    {
        AnimationHandler.SetViewImmediate(Group, ViewMain, 1.0f);
        AnimationHandler.SetViewImmediate(Group, ViewSelector, 0.0f);

        // Reset language options & selections before discovering available languages
        LanguageSetting.Options.Clear();
        LanguageSetting.SelectedValues.Clear();
        LanguageSetting.AddOption(0, "English");

        int activeIndex = 0;
        try
        {
            var languageDirs = Directory.GetDirectories(TranslationRootDir);
            int idx = 1;

            foreach (var dir in languageDirs)
            {
                string langName = Path.GetFileName(dir);
                if (string.Equals(langName, "English", StringComparison.OrdinalIgnoreCase)) continue;

                LanguageSetting.AddOption(idx, langName);
                if (string.Equals(Config.Language, langName, StringComparison.OrdinalIgnoreCase))
                {
                    activeIndex = idx;
                }
                idx++;
                TranslatorLogger.Msg($"Found language: {langName}");
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[GUIManager] Error reading translation directories: {ex.Message}");
        }

        LanguageSetting.SelectedValues.Add(activeIndex);

        // Handle user selection changes and sync with Preferences + Config
        LanguageSetting.OnSelectionChanged = (selectedId, isSelected) =>
        {
            if (!isSelected) return;

            if (LanguageSetting.Options.TryGetValue(selectedId, out string chosenLang))
            {
                Config.Language = chosenLang;
                if (Preferences.LanguageEntry != null)
                {
                    Preferences.LanguageEntry.Value = chosenLang;
                    Preferences.LanguageEntry.Save();
                }
            }
        };

        RefreshThemeOptions();

        if (ScaleSetting.Value <= 0.1f) ScaleSetting.Value = 1.0f;
        if (ElementScaleSetting.Value <= 0.1f) ElementScaleSetting.Value = 1.0f;

        ServiceRegistry.Register(new GUIManagerService());

        SafeToCloseManager.RegisterGuard(() =>
        {
            if (Config.CurrentTab == TabType.GUI)
            {
                if (isSelectingSubWindow) return false;
                if (DrawSetting.IsFocused) return false;
            }
            return true;
        });

        Api.Actions.OnGUIShow += Render;
        Api.Actions.OnWarmUp += Render;

        TabType.GUI.OnDeselected = () =>
        {
            MultiSelectTabStorage.SaveState(TabType.GUI);
        };

        TabType.GUI.OnSelected = () =>
        {
            MultiSelectTabStorage.RestoreState(TabType.GUI);
        };

        // SaveLoad entries
        SaveLoad.RegisterEntry("Language", () => Config.Language, val =>
        {
            if (!string.Equals(Config.Language, val, StringComparison.OrdinalIgnoreCase))
            {
                Config.Language = val;
            }

            if (LanguageSetting?.Options != null)
            {
                foreach (var kvp in LanguageSetting.Options)
                {
                    if (string.Equals(kvp.Value, val, StringComparison.OrdinalIgnoreCase))
                    {
                        LanguageSetting.SelectedValues.Clear();
                        LanguageSetting.SelectedValues.Add(kvp.Key);
                        break;
                    }
                }
            }
        }, "English");

        SaveLoad.RegisterEntry("Theme", () => Config.Theme, val => Config.Theme = val, "Default");
        SaveLoad.RegisterEntry("GUIScale", () => ScaleSetting.Value, val => ScaleSetting.Value = val, 1.0f);
        SaveLoad.RegisterEntry("ElementScale", () => ElementScaleSetting.Value, val => ElementScaleSetting.Value = val, 1.0f);
        SaveLoad.RegisterEntry("ShowFloatingIcon", () => ShowFloatingIcon, val => ShowFloatingIcon = val, true);
        SaveLoad.RegisterEntry("ShowMainMenuCredits", () => ShowMainMenuCredits, val => ShowMainMenuCredits = val, true);
    }

    public static void RefreshThemeOptions()
    {
        if (ThemeSetting == null) return;

        if (ThemeManager.LoadedThemes == null || ThemeManager.LoadedThemes.Count == 0)
        {
            ThemeManager.LoadThemes();
        }

        ThemeSetting.Options.Clear();
        ThemeSetting.SelectedValues.Clear();

        int tIdx = 0;
        int activeThemeIdx = 0;

        foreach (var kvp in ThemeManager.LoadedThemes)
        {
            ThemeSetting.AddOption(tIdx, kvp.Key);
            if (string.Equals(Config.Theme, kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                activeThemeIdx = tIdx;
            }
            tIdx++;
        }

        if (ThemeSetting.Options.Count == 0)
        {
            ThemeSetting.AddOption(0, ThemeManager.InternalDefaultTheme.Name);
            activeThemeIdx = 0;
        }

        ThemeSetting.SelectedValues.Add(activeThemeIdx);
    }

    public static void Render()
    {
        ThemeManager.Rescale();

        Event e = Event.current;

        if (isSelectingSubWindow && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            OnClose();
            DrawSetting.ResetInputBind();
            Input.ResetInputAxes();
            e.Use();
            return;
        }

        GUIStyle windowBgStyle = ThemeManager.SettingsWndowBgStyle ?? ThemeManager.SettingsWndowStyle;

        GUISelectorDrawer.Render(windowBgStyle);
        GUIControlsDrawer.Render(windowBgStyle);
    }

    private class GUIManagerService : IWarmUp, IMenuRenderable, ICloseHandler, ILanguageAware
    {
        public string Name => "GUIManager";
        public int Priority => ServicePriority.UI;

        public void OnWarmUp() => GUIManager.Render();

        public void OnMenuGUI()
        {
            AnimationHandler.RenderWithTabAlpha(TabType.GUI, GUIManager.Render);
        }

        public bool CanClose()
        {
            if (Config.CurrentTab == TabType.GUI)
            {
                return !isSelectingSubWindow && !DrawSetting.IsFocused;
            }
            return true;
        }

        public bool OnEscapePressed()
        {
            if (Config.CurrentTab != TabType.GUI) return false;

            if (isSelectingSubWindow)
            {
                OnClose();
                DrawSetting.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            if (DrawSetting.IsFocused)
            {
                DrawSetting.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }

        public void OnLanguageChanged() => RefreshThemeOptions();
    }

    private static void DumpEnglishTemplates(string domainDir)
    {
        var template = new string[]
        {
            "GUI Configuration", "Language", "Theme", "Change", "GUI Scale", "Element Scale",
            "Floating Icon", "Mobile Close Buttons", "Show Main Menu Credits", "Icon Opacity",
            "ON", "OFF",
        };

        string filePath = Path.Combine(domainDir, "translation_strings.json");
        Translator.SaveJson(filePath, Translator.CreateDictionary(template));
    }

    public static float S(float value) => value * ScaleSetting.Value;
    public static float ES(float value) => value * ElementScaleSetting.Value;
    public static void SetFloatingIcon(bool enabled)
    {
        ShowFloatingIcon = enabled;
#if MELONLOADER || RELEASE_MELON
    if (Preferences.ShowFloatingIconEntry != null)
    {
        Preferences.ShowFloatingIconEntry.Value = enabled;
    }
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
        if (Preferences.ShowFloatingIconEntry != null)
        {
            Preferences.ShowFloatingIconEntry.Value = enabled;
            Preferences.BepInExConfig?.Save();
        }
#endif
        Utils.SaveLoad.Save();
    }
}