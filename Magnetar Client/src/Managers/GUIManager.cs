using Magnetar_Client.Api;
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
    public static bool isSelectingSubWindow = false;

    public static MultiSelectSetting LanguageSetting { get; } = new("Language")
    {
        MaxSelection = 1,
        Options = new Dictionary<int, string>
        {
            {0,"English" }
        },
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

    private const float BaseWidth = 500f;
    private const float BaseHeight = 340f;
    private const float BaseElementHeight = 25f;
    private const float BaseSelectorWidth = 500f;
    private const float BaseSelectorHeight = 800f;

    public static float elementHeight => Config.S(BaseElementHeight);

    public static Rect windowRect = new(
        (Config.NativeWidth - Config.S(BaseWidth)) / 2,
        (Config.NativeHeight - Config.S(BaseHeight)) / 2,
        Config.S(BaseWidth),
        Config.S(BaseHeight));

    public static Rect selectorRect = new(
        (Config.NativeWidth - Config.S(BaseSelectorWidth)) / 2,
        (Config.NativeHeight - Config.S(BaseSelectorHeight)) / 2,
        Config.S(BaseSelectorWidth),
        Config.S(BaseSelectorHeight));

    private static GUI.WindowFunction _cachedSelector;
    private static GUI.WindowFunction _cachedGuiControls;
    private static readonly Action _cachedOnClose = OnClose;

    private static GUI.WindowFunction SelectorDelegate => _cachedSelector ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((System.Action<int>)DrawSelectorModal);

    private static GUI.WindowFunction GuiControlsDelegate => _cachedGuiControls ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((System.Action<int>)DrawGUIControls);

    public static void OnClose()
    {
        isSelectingSubWindow = false;
        UI.WindowDrawing.DrawSetting.activeMultiSelect = null;
    }

    public static void Init()
    {
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

        // Register to centralized ServiceRegistry and SafeToCloseManager
        ServiceRegistry.Register(new GUIManagerService());

        SafeToCloseManager.RegisterGuard(() =>
        {
            if (Config.CurrentTab == TabType.GUI)
            {
                return !isSelectingSubWindow
                       && DrawSetting.activeSliderId == -1
                       && DrawSetting.activeDropdownId == -1
                       && DrawSetting.activeTextFieldId == -1;
            }
            return true;
        });

        SafeToCloseManager.RegisterInterceptor(() =>
        {
            if (Config.CurrentTab == TabType.GUI && isSelectingSubWindow)
            {
                OnClose();
                UIAnimationHelper.TriggerSubWindowTransition();
                return true;
            }
            return false;
        });

        Api.Actions.Core.OnGUIShow += Render;
        Api.Actions.Core.OnWarmUp += Render;
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
            UIAnimationHelper.TriggerSubWindowTransition();
            e.Use();
            return;
        }

        Config.RescaleAroundCenter(ref windowRect, Config.S(BaseWidth), windowRect.height);

        float targetSelectorWidth = Config.S(BaseSelectorWidth);
        float maxSelectorHeight = Config.NativeHeight * 0.8f;
        float targetSelectorHeight = Mathf.Min(Config.S(BaseSelectorHeight), maxSelectorHeight);
        Config.RescaleAroundCenter(ref selectorRect, targetSelectorWidth, targetSelectorHeight);

        GUIStyle windowBgStyle = ThemeManager.SettingsWndowBgStyle ?? ThemeManager.SettingsWndowStyle;

        if (isSelectingSubWindow)
        {
            selectorRect = GUI.Window(
                4001,
                selectorRect,
                SelectorDelegate,
                "",
                windowBgStyle
            );
        }
        else
        {
            windowRect = GUI.Window(
                4000,
                windowRect,
                GuiControlsDelegate,
                "",
                windowBgStyle
            );
        }
    }

    private static void DrawSelectorModal(int windowID)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.SubWindowAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            Event e = Event.current;
            Rect multiSelectRect = new(0, 0, selectorRect.width, selectorRect.height);
            UI.WindowDrawing.DrawSetting.DrawMultiSelectWindow(multiSelectRect, UI.WindowDrawing.DrawSetting.activeMultiSelect, _cachedOnClose);

#if ANDROID
            float titleHeight = Config.S(25f) * 1.30f;
#else
            float titleHeight = Config.S(25f);
#endif
            float dragSafeMargin = Config.ShowMobileButtons ? Config.S(35f) : 0f;
            GUI.DragWindow(new Rect(0, 0, selectorRect.width - dragSafeMargin, titleHeight));

            if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
            }
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static void DrawGUIControls(int windowID)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.SubWindowAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            float w = windowRect.width;
            float indent = Config.S(10f);
            Event e = Event.current;
            float y = Config.S(35f);

            Rect headerBgRect = new(0, 0, w, y - indent);
            GUI.Box(headerBgRect, Translator.Translate("GUI Configuration"), ThemeManager.SettingsWndowStyle);

            // --- 1. Language Row ---
            string currentLangName = "English";
            if (LanguageSetting?.SelectedValues != null && LanguageSetting.SelectedValues.Count > 0)
            {
                int selectedId = LanguageSetting.SelectedValues.First();
                if (LanguageSetting.Options.ContainsKey(selectedId))
                    currentLangName = LanguageSetting.Options[selectedId];
            }

            Config.Language = currentLangName;

            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight),
                $"Language: <color=yellow>{Config.Language}</color>",
                ThemeManager.SettingLabelStyle);

            Rect langBtnRect = new(w * 0.5f, y, w * 0.45f, elementHeight);

            if (e.type == EventType.MouseDown && e.button == 0 && langBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                OpenSubSelector(LanguageSetting);
            }
            GUI.Box(langBtnRect, Translator.Translate("Change"), ThemeManager.SettingOff);
            y += elementHeight + Config.S(10f);

            // --- 2. Theme Row ---
            string currentTheme = ThemeManager.CurrentThemeName;
            if (ThemeSetting?.SelectedValues != null && ThemeSetting.SelectedValues.Count > 0)
            {
                int selThemeId = ThemeSetting.SelectedValues.First();
                if (ThemeSetting.Options.ContainsKey(selThemeId))
                {
                    currentTheme = ThemeSetting.Options[selThemeId];
                }
            }

            Config.Theme = currentTheme;

            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight),
                $"Theme: <color=yellow>{Config.Theme}</color>",
                ThemeManager.SettingLabelStyle);

            Rect themeBtnRect = new(w * 0.5f, y, w * 0.45f, elementHeight);

            if (e.type == EventType.MouseDown && e.button == 0 && themeBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                RefreshThemeOptions();
                OpenSubSelector(ThemeSetting);
            }

            GUI.Box(themeBtnRect, Translator.Translate("Change"), ThemeManager.SettingOff);
            y += elementHeight + Config.S(10f);

            // --- 3. GUI Scale Row ---
            if (ScaleSetting != null)
            {
                if (UI.WindowDrawing.DrawSetting.activeSliderId != ScaleSetting.GetHashCode() &&
                    Mathf.Abs(ScaleSetting.Value - Config.GUIScale) > 0.001f)
                {
                    ScaleSetting.Value = Config.GUIScale;
                }
                UI.WindowDrawing.DrawSetting.HandleNumericSetting(ScaleSetting, ref y, w, true);
                y += elementHeight + Config.S(10f);
            }

            // --- 4. Element Scale Row ---
            if (ElementScaleSetting != null)
            {
                if (UI.WindowDrawing.DrawSetting.activeSliderId != ElementScaleSetting.GetHashCode() &&
                    Mathf.Abs(ElementScaleSetting.Value - Config.ElementScale) > 0.001f)
                {
                    ElementScaleSetting.Value = Config.ElementScale;
                }
                UI.WindowDrawing.DrawSetting.HandleNumericSetting(ElementScaleSetting, ref y, w, true);
                y += elementHeight + Config.S(10f);
            }

            // --- 5. Floating Icon Toggle Row ---
            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight), Translator.Translate("Floating Icon"), ThemeManager.SettingLabelStyle);
            Rect floatIconRect = new(w * 0.5f, y, w * 0.45f, elementHeight);

            GUI.Box(floatIconRect,
                Config.ShowFloatingIcon ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Config.ShowFloatingIcon ? ThemeManager.SettingOn : ThemeManager.SettingOff);

            if (floatIconRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                Config.SetFloatingIcon(!Config.ShowFloatingIcon);
                e.Use();
            }
            y += elementHeight + Config.S(10f);

            // --- 6. Mobile Buttons Toggle Row ---
            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight), Translator.Translate("Mobile Close Buttons"), ThemeManager.SettingLabelStyle);
            Rect mobileBtnRect = new(w * 0.5f, y, w * 0.45f, elementHeight);

            GUI.Box(mobileBtnRect,
                Config.ShowMobileButtons ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Config.ShowMobileButtons ? ThemeManager.SettingOn : ThemeManager.SettingOff);

            if (mobileBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                Config.ShowMobileButtons = !Config.ShowMobileButtons;
                e.Use();
            }
            y += elementHeight + Config.S(10f);

            // --- 7. Show Main Menu Credits ---
            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight), Translator.Translate("Show Main Menu Credits"), ThemeManager.SettingLabelStyle);
            Rect showMainMenuCredits = new(w * 0.5f, y, w * 0.45f, elementHeight);

            GUI.Box(showMainMenuCredits,
                Config.ShowMainMenuCredits ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Config.ShowMainMenuCredits ? ThemeManager.SettingOn : ThemeManager.SettingOff);

            if (showMainMenuCredits.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                Config.ShowMainMenuCredits = !Config.ShowMainMenuCredits;
                e.Use();
            }
            y += elementHeight + Config.S(10f);

            if (Config.ShowFloatingIcon && FloatingIconOpacitySetting != null)
            {
                if (UI.WindowDrawing.DrawSetting.activeSliderId != FloatingIconOpacitySetting.GetHashCode() &&
                    Mathf.Abs(FloatingIconOpacitySetting.Value - Config.FloatingIconOpacity) > 0.001f)
                {
                    FloatingIconOpacitySetting.Value = Config.FloatingIconOpacity;
                }
                UI.WindowDrawing.DrawSetting.HandleNumericSetting(FloatingIconOpacitySetting, ref y, w, true);
                y += elementHeight + Config.S(10f);
            }

            if (UI.WindowDrawing.DrawSetting.OnPostDraw != null)
            {
                UI.WindowDrawing.DrawSetting.OnPostDraw.Invoke();
                UI.WindowDrawing.DrawSetting.OnPostDraw = null;
            }

            windowRect.height = y;
            GUI.DragWindow(new Rect(0, 0, w, Config.S(25f)));

            Rect _windowRect = new(0, 0, w, y);
            if (_windowRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
            }
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static void OpenSubSelector(MultiSelectSetting setting)
    {
        UI.WindowDrawing.DrawSetting.activeMultiSelect = setting;
        UI.WindowDrawing.DrawSetting.multiSelectSearchQuery = "";
        UI.WindowDrawing.DrawSetting.manualScrollY = 0f;

        float targetW = Config.S(BaseSelectorWidth);
        float targetH = Mathf.Min(Config.S(BaseSelectorHeight), Config.NativeHeight * 0.8f);
        selectorRect = new Rect((Config.NativeWidth - targetW) / 2f, (Config.NativeHeight - targetH) / 2f, targetW, targetH);

        isSelectingSubWindow = true;
        UIAnimationHelper.TriggerSubWindowTransition();
    }

    private class GUIManagerService : IInitializable, IWarmUp, IMenuRenderable, ICloseHandler, ILanguageAware
    {
        public string Name => "GUIManager";
        public int Priority => ServicePriority.UI;

        public void Initialize() { }
        public void OnWarmUp() => GUIManager.Render();

        public void OnMenuGUI()
        {
            if (Config.CurrentTab == TabType.GUI)
            {
                GUIManager.Render();
            }
        }

        public bool CanClose()
        {
            return !isSelectingSubWindow
                   && DrawSetting.activeSliderId == -1
                   && DrawSetting.activeDropdownId == -1
                   && DrawSetting.activeTextFieldId == -1;
        }

        public bool OnEscapePressed()
        {
            if (Config.CurrentTab != TabType.GUI) return false;

            // 1. If currently inside a sub-selector modal (Language or Theme selector), step back with animation
            if (isSelectingSubWindow)
            {
                OnClose();
                Main.ResetInputBind();
                UIAnimationHelper.TriggerSubWindowTransition();
                Input.ResetInputAxes();
                return true;
            }

            // 2. If an active slider or text field is focused, cancel/unfocus it
            if (DrawSetting.activeSliderId != -1 || DrawSetting.activeDropdownId != -1 || DrawSetting.activeTextFieldId != -1)
            {
                Main.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }

        public void OnLanguageChanged()
        {
            RefreshThemeOptions();
        }
    }
}