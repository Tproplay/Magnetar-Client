using Magnetar_Client.Api;
using Magnetar_Client.Core.GUIManager_;
using Magnetar_Client.Core.Lifecycle;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public static class GUIManager
{
    public const string Group = "GUI";
    public const string ViewMain = "Controls";
    public const string ViewSelector = "Selector";

    public static bool isSelectingSubWindow => UIAnimationHelper.GetViewAlpha(Group, ViewSelector) > 0.001f;

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

    public static FloatSetting ScaleSetting = new("GUI Scale", 0.5f, 2.0f, Config.GUIScale,
        decimalPlaces: 2, trueMin: 0.25f, trueMax: 3.0f)
    {
        OnValueChanged = (val) => Config.GUIScale = val
    };

    public static FloatSetting ElementScaleSetting = new("Element Scale", 0.5f, 2.0f,
        Config.ElementScale, decimalPlaces: 2, trueMin: 0.25f, trueMax: 3.0f)
    {
        OnValueChanged = (val) => Config.ElementScale = val
    };

    public static FloatSetting FloatingIconOpacitySetting = new("Icon Opacity", 0.1f, 1.0f, Config.FloatingIconOpacity,
        decimalPlaces: 2, trueMin: 0.05f, trueMax: 1.0f)
    {
        OnValueChanged = (val) => Config.FloatingIconOpacity = val
    };

    private const float BaseElementHeight = 25f;
    public static float elementHeight => Config.S(BaseElementHeight);

    public static void OnClose()
    {
        UIAnimationHelper.SwitchView(Group, ViewMain);
        GUISelectorDrawer.ActiveSetting = null;
        DrawSetting.activeMultiSelect = null;
    }

    public static void Init()
    {
        UIAnimationHelper.SetViewImmediate(Group, ViewMain, 1.0f);
        UIAnimationHelper.SetViewImmediate(Group, ViewSelector, 0.0f);

        try
        {
            var languageDirs = Directory.GetDirectories(TranslationRootDir);
            int idx = 1;
            int activeIndex = 0;

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
            LanguageSetting.SelectedValues.Add(activeIndex);
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[GUIManager] Error reading translation directories: {ex.Message}");
            LanguageSetting.SelectedValues.Add(0);
        }

        RefreshThemeOptions();

        if (Config.GUIScale <= 0.1f) Config.GUIScale = 1.0f;
        if (Config.ElementScale <= 0.1f) Config.ElementScale = 1.0f;

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
            Main.ResetInputBind();
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
            UIAnimationHelper.RenderWithTabAlpha(TabType.GUI, GUIManager.Render);
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
                Main.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            if (DrawSetting.IsFocused)
            {
                Main.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }

        public void OnLanguageChanged() => RefreshThemeOptions();
    }
}