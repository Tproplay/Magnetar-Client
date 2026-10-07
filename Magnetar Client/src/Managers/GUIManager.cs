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

    private const float BaseWidth = 520f;
    private const float BaseHeight = 360f;
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
        UIAnimationHelper.SwitchView(Group, ViewMain);
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
            if (isSelectingSubWindow) return false;

            if (Config.CurrentTab == TabType.GUI)
            {
                return DrawSetting.activeSliderId == -1
                       && DrawSetting.activeDropdownId == -1
                       && DrawSetting.activeTextFieldId == -1;
            }
            return true;
        });

        Api.Actions.OnGUIShow += Render;
        Api.Actions.OnWarmUp += Render;
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

        Config.RescaleAroundCenter(ref windowRect, Config.S(BaseWidth), windowRect.height);

        float targetSelectorWidth = Config.S(BaseSelectorWidth);
        float maxSelectorHeight = Config.NativeHeight * 0.8f;
        float targetSelectorHeight = Mathf.Min(Config.S(BaseSelectorHeight), maxSelectorHeight);
        Config.RescaleAroundCenter(ref selectorRect, targetSelectorWidth, targetSelectorHeight);

        GUIStyle windowBgStyle = ThemeManager.SettingsWndowBgStyle ?? ThemeManager.SettingsWndowStyle;

        // Render both views if their alpha is active
        if (UIAnimationHelper.GetViewAlpha(Group, ViewSelector) > 0.001f)
        {
            selectorRect = GUI.Window(4001, selectorRect, SelectorDelegate, "", windowBgStyle);
        }

        if (UIAnimationHelper.GetViewAlpha(Group, ViewMain) > 0.001f)
        {
            windowRect = GUI.Window(4000, windowRect, GuiControlsDelegate, "", windowBgStyle);
        }
    }

    private static void DrawSelectorModal(int windowID)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;

        float alpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.GetViewAlpha(Group, ViewSelector);
        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * alpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * alpha);

        try
        {
            Event e = Event.current;
            Rect multiSelectRect = new(0, 0, selectorRect.width, selectorRect.height);
            DrawSetting.DrawMultiSelectWindow(multiSelectRect, DrawSetting.activeMultiSelect, _cachedOnClose);

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

        float alpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.GetViewAlpha(Group, ViewMain);
        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * alpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * alpha);

        try
        {
            float w = windowRect.width;
            float indent = Config.indent;
            float rightMargin = Config.S(16f);
            Event e = Event.current;
            float rowSpacing = Config.S(8f);

#if ANDROID
            float headerHeight = Config.S(26f) * 1.30f;
#else
            float headerHeight = Config.S(26f);
#endif
            float y = headerHeight + Config.S(12f);

            Rect headerBgRect = new(0, 0, w, headerHeight);
            GUI.Box(headerBgRect, Translator.Translate("GUI Configuration"), ThemeManager.SettingsWndowStyle);

            float controlWidth = Mathf.Min(Config.SettingWidth, w * 0.45f);
            float controlX = w - rightMargin - controlWidth;
            float labelWidth = controlX - indent - Config.S(10f);

            void DrawButtonRow(string labelText, string btnText, Action onClick, GUIStyle btnStyle = null)
            {
                Rect lblRect = new(indent, y, labelWidth, elementHeight);
                GUI.Label(lblRect, labelText, ThemeManager.SettingLabelStyle);

                Rect btnRect = new(controlX, y, controlWidth, elementHeight);
                bool isHovered = btnRect.Contains(e.mousePosition);

                Color prevBg = GUI.backgroundColor;
                if (isHovered) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);

                GUI.Box(btnRect, btnText, btnStyle ?? ThemeManager.SettingOff);
                GUI.backgroundColor = prevBg;

                if (isHovered && e.type == EventType.MouseDown && e.button == 0)
                {
                    onClick?.Invoke();
                    e.Use();
                }

                y += elementHeight + rowSpacing;
            }

            void DrawSliderRow(FloatSetting setting)
            {
                string labelText = Translator.Translate(setting.Name);
                Rect lblRect = new(indent, y, labelWidth, elementHeight);
                GUI.Label(lblRect, labelText, ThemeManager.SettingLabelStyle);

                float inputW = Config.SettingsInput.NumericInputWidth;
                float gap = Config.SettingsInput.Gap;
                float sliderW = controlWidth - inputW - gap;
                float trackH = Config.SettingsInput.SliderHeight;
                float thumbSize = Config.S(16f);

                Rect sliderRect = new(controlX, y + ((elementHeight - trackH) / 2f), sliderW, trackH);
                Rect inputRect = new(controlX + sliderW + gap, y, inputW, elementHeight);

                float percentage = Mathf.Clamp01((setting.DisplayValue - setting.Min) / (setting.Max - setting.Min));
                float fillWidth = sliderRect.width * percentage;
                float thumbX = sliderRect.x + fillWidth - (thumbSize / 2f);
                float thumbY = sliderRect.y + (trackH / 2f) - (thumbSize / 2f);
                Rect thumbRect = new(thumbX, thumbY, thumbSize, thumbSize);

                GUI.Box(sliderRect, "", ThemeManager.SliderTrackOffStyle);
                if (fillWidth > 0f)
                {
                    GUI.Box(new Rect(sliderRect.x, sliderRect.y, fillWidth, sliderRect.height), "", ThemeManager.SliderTrackOnStyle);
                }
                GUI.Box(thumbRect, "", ThemeManager.SliderThumbStyle);

                int sliderControlId = GUIUtility.GetControlID(setting.Name.GetHashCode(), FocusType.Passive);
                Rect grabHitBox = new(sliderRect.x - 6f, y, sliderW + 12f, elementHeight);

                void ApplyMouseValue(float mouseX)
                {
                    float pct = Mathf.Clamp01((mouseX - sliderRect.x) / sliderRect.width);
                    float newVal = Mathf.Lerp(setting.Min, setting.Max, pct);
                    float rounded = (float)Math.Round(newVal, setting.DecimalPlaces);
                    setting.SetPending(Mathf.Clamp(rounded, setting.Min, setting.Max));
                }

                if (e.type == EventType.MouseDown && e.button == 0 && (grabHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition)))
                {
                    GUIUtility.hotControl = sliderControlId;
                    GUIUtility.keyboardControl = 0;
                    DrawSetting.activeSliderId = sliderControlId;
                    DrawSetting.activeNumericSetting = setting;
                    DrawSetting.focusedControlId = -1;
                    DrawSetting.activeTextFieldId = -1;

                    ApplyMouseValue(e.mousePosition.x);
                    e.Use();
                }

                if (GUIUtility.hotControl == sliderControlId)
                {
                    if (e.type == EventType.MouseDrag)
                    {
                        ApplyMouseValue(e.mousePosition.x);
                        e.Use();
                    }
                    else if (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp)
                    {
                        setting.Commit();
                        GUIUtility.hotControl = 0;
                        DrawSetting.activeSliderId = -1;
                        DrawSetting.activeNumericSetting = null;
                        e.Use();
                    }
                }

                int controlId = inputRect.GetHashCode();
                bool isFocused = (DrawSetting.activeTextFieldId == controlId);
                string formatString = "0." + new string('0', setting.DecimalPlaces);

                if (isFocused && DrawSetting.lastFocusedNumericControlId != controlId)
                {
                    DrawSetting.currentInputBuffer = setting.DisplayValue.ToString(formatString);
                    DrawSetting.lastFocusedNumericControlId = controlId;
                }
                else if (!isFocused && DrawSetting.lastFocusedNumericControlId == controlId)
                {
                    setting.Commit();
                    DrawSetting.lastFocusedNumericControlId = -1;
                }

                string displayStr = isFocused ? DrawSetting.currentInputBuffer : setting.DisplayValue.ToString(formatString);
                string newText = DrawSetting.DrawManualTextField(inputRect, displayStr, "0");

                if (isFocused)
                {
                    DrawSetting.currentInputBuffer = newText;
                    if (double.TryParse(DrawSetting.currentInputBuffer, out double parsed))
                    {
                        setting.SetPending((float)Math.Round(Mathf.Clamp((float)parsed, setting.TrueMin, setting.TrueMax), setting.DecimalPlaces));
                    }
                }

                y += elementHeight + rowSpacing;
            }

            string currentLangName = "English";
            if (LanguageSetting?.SelectedValues != null && LanguageSetting.SelectedValues.Count > 0)
            {
                int selectedId = LanguageSetting.SelectedValues.First();
                if (LanguageSetting.Options.ContainsKey(selectedId))
                    currentLangName = LanguageSetting.Options[selectedId];
            }
            Config.Language = currentLangName;

            DrawButtonRow(
                $"{Translator.Translate("Language")}: <color=yellow>{Config.Language}</color>",
                Translator.Translate("Change"),
                () => OpenSubSelector(LanguageSetting)
            );

            string currentTheme = ThemeManager.CurrentThemeName;
            if (ThemeSetting?.SelectedValues != null && ThemeSetting.SelectedValues.Count > 0)
            {
                int selThemeId = ThemeSetting.SelectedValues.First();
                if (ThemeSetting.Options.ContainsKey(selThemeId))
                    currentTheme = ThemeSetting.Options[selThemeId];
            }
            Config.Theme = currentTheme;

            DrawButtonRow(
                $"{Translator.Translate("Theme")}: <color=yellow>{Config.Theme}</color>",
                Translator.Translate("Change"),
                () =>
                {
                    RefreshThemeOptions();
                    OpenSubSelector(ThemeSetting);
                }
            );

            if (ScaleSetting != null)
            {
                if (DrawSetting.activeSliderId != ScaleSetting.GetHashCode() && Mathf.Abs(ScaleSetting.Value - Config.GUIScale) > 0.001f)
                {
                    ScaleSetting.Value = Config.GUIScale;
                }
                DrawSliderRow(ScaleSetting);
            }

            if (ElementScaleSetting != null)
            {
                if (DrawSetting.activeSliderId != ElementScaleSetting.GetHashCode() && Mathf.Abs(ElementScaleSetting.Value - Config.ElementScale) > 0.001f)
                {
                    ElementScaleSetting.Value = Config.ElementScale;
                }
                DrawSliderRow(ElementScaleSetting);
            }

            DrawButtonRow(
                Translator.Translate("Floating Icon"),
                Config.ShowFloatingIcon ? Translator.Translate("ON") : Translator.Translate("OFF"),
                () => Config.SetFloatingIcon(!Config.ShowFloatingIcon),
                Config.ShowFloatingIcon ? ThemeManager.SettingOn : ThemeManager.SettingOff
            );

            DrawButtonRow(
                Translator.Translate("Mobile Close Buttons"),
                Config.ShowMobileButtons ? Translator.Translate("ON") : Translator.Translate("OFF"),
                () => Config.ShowMobileButtons = !Config.ShowMobileButtons,
                Config.ShowMobileButtons ? ThemeManager.SettingOn : ThemeManager.SettingOff
            );

            DrawButtonRow(
                Translator.Translate("Show Main Menu Credits"),
                Config.ShowMainMenuCredits ? Translator.Translate("ON") : Translator.Translate("OFF"),
                () => Config.ShowMainMenuCredits = !Config.ShowMainMenuCredits,
                Config.ShowMainMenuCredits ? ThemeManager.SettingOn : ThemeManager.SettingOff
            );

            if (Config.ShowFloatingIcon && FloatingIconOpacitySetting != null)
            {
                if (DrawSetting.activeSliderId != FloatingIconOpacitySetting.GetHashCode() && Mathf.Abs(FloatingIconOpacitySetting.Value - Config.FloatingIconOpacity) > 0.001f)
                {
                    FloatingIconOpacitySetting.Value = Config.FloatingIconOpacity;
                }
                DrawSliderRow(FloatingIconOpacitySetting);
            }

            if (DrawSetting.OnPostDraw != null)
            {
                DrawSetting.OnPostDraw.Invoke();
                DrawSetting.OnPostDraw = null;
            }

            windowRect.height = y + Config.S(12f);

            GUI.DragWindow(new Rect(0, 0, w, headerHeight));

            Rect clientArea = new(0, 0, w, windowRect.height);
            if (clientArea.Contains(e.mousePosition) && e.type == EventType.MouseDown)
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
        DrawSetting.activeMultiSelect = setting;
        DrawSetting.multiSelectSearchQuery = "";
        DrawSetting.manualScrollY = 0f;

        float targetW = Config.S(BaseSelectorWidth);
        float targetH = Mathf.Min(Config.S(BaseSelectorHeight), Config.NativeHeight * 0.8f);
        selectorRect = new Rect((Config.NativeWidth - targetW) / 2f, (Config.NativeHeight - targetH) / 2f, targetW, targetH);

        UIAnimationHelper.SwitchView(Group, ViewSelector);
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
            if (isSelectingSubWindow)
            {
                OnClose();
                Main.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            if (Config.CurrentTab == TabType.GUI)
            {
                if (DrawSetting.activeSliderId != -1 || DrawSetting.activeDropdownId != -1 || DrawSetting.activeTextFieldId != -1)
                {
                    Main.ResetInputBind();
                    Input.ResetInputAxes();
                    return true;
                }
            }

            return false;
        }

        public void OnLanguageChanged()
        {
            RefreshThemeOptions();
        }
    }
}