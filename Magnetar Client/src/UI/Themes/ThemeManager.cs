using Magnetar_Client.Api;
using Magnetar_Client.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.UI.Themes;

public static class ThemeManager
{
    public static string CurrentThemeName => Config.Theme ?? "Magnetar Default";

    #region Direct Accessors
    public static Color BackgroundColor => ThemeData.BackgroundColor;
    public static Color AccentColor => ThemeData.AccentColor;
    public static Color LightBackgroundColor => ThemeData.LightBackgroundColor;
    public static Color TextWhite => ThemeData.TextWhite;
    public static Color TextDim => ThemeData.TextDim;
    public static Color DimColor => ThemeData.DimColor;
    public static Color NefLineColor => ThemeData.NefLineColor;
    public static Color NefNodeColor => ThemeData.NefNodeColor;
    public static Color SettingOnColor => ThemeData.SettingOnColor;
    public static Color SettingOffColor => ThemeData.SettingOffColor;
    public static Color SeparatorColor => ThemeData.SeparatorColor;

    public static Dictionary<string, ThemeDefinition> LoadedThemes => ThemeData.LoadedThemes;
    public static ThemeDefinition InternalDefaultTheme => ThemeData.InternalDefaultTheme;
    public static ThemeDefinition MeteorPurpleTemplate => ThemeData.MeteorPurpleTemplate;
    #endregion

    #region GUIStyle Registries
    public static GUIStyle TopBarStyle { get; private set; }
    public static GUIStyle TopBarActiveStyle { get; private set; }
    public static GUIStyle CategoryWindowStyle { get; private set; }
    public static GUIStyle CategoryHeaderStyle { get; private set; }
    public static GUIStyle CategoryModuleOffStyle { get; private set; }
    public static GUIStyle CategoryModuleOnStyle { get; private set; }
    public static GUIStyle CloseButtonStyle { get; private set; }
    public static GUIStyle SettingsWndowBgStyle { get; private set; }
    public static GUIStyle SettingsWndowStyle { get; private set; }
    public static GUIStyle SettingsDescriptionStyle { get; private set; }
    public static GUIStyle SettingTextStyle { get; private set; }
    public static GUIStyle SettingAuthorStyle { get; private set; }
    public static GUIStyle SettingOff { get; private set; }
    public static GUIStyle SettingOn { get; private set; }
    public static GUIStyle SettingLabelStyle { get; private set; }
    public static GUIStyle ButtonSettingStyle { get; private set; }
    public static GUIStyle ResetButtonStyle { get; private set; }
    public static GUIStyle ListAddButtonStyle { get; private set; }
    public static GUIStyle ListRemoveButtonStyle { get; private set; }
    public static GUIStyle SeparatorStyle { get; private set; }
    public static GUIStyle SeparatorTextStyle { get; private set; }
    public static GUIStyle TextStyle { get; private set; }
    public static GUIStyle TextHighlightedStyle { get; private set; }
    public static GUIStyle DimBackgroundStyle { get; private set; }
    public static GUIStyle HUDElementStyle { get; private set; }
    public static GUIStyle NEFLineStyle { get; private set; }
    public static GUIStyle NEFNodeStyle { get; private set; }
    public static GUIStyle SliderTrackOffStyle { get; private set; }
    public static GUIStyle SliderTrackOnStyle { get; private set; }
    public static GUIStyle SliderThumbStyle { get; private set; }
    public static GUIStyle SectionGroupHeaderStyle { get; private set; }
    public static GUIStyle SectionHeaderStyle { get; private set; }
    public static GUIStyle SectionRemoveButtonStyle { get; private set; }
    public static GUIStyle SectionAddButtonStyle { get; private set; }
    #endregion

    #region Base Sizing Constants
    private const int TopBarFontSize = 14;
    private const int TopBarPaddingLR = 10;
    private const int TopBarPaddingTB = 5;

    private const int ModuleFontSize = 12;
    private const int ModulePaddingLeft = 10;

    private const int ModuleWindowFontSize = 21;
    private const int ModuleWindowPaddingTop = 3;

    private const int SettingsWindowFontSize = 21;

    private const int SettingFontSize = 12;
    private const int SettingPaddingLeft = 10;

    private const int DescriptionFontSize = 14;
    private const int DescriptionPaddingLR = 5;
    private const int DescriptionPaddingTB = 2;

    private const int SettingDescriptionFontSize = 14;
    private const int SettingDescriptionPaddingLR = 5;
    private const int SettingDescriptionPaddingTB = 2;

    private const int AuthorFontSize = 15;
    private const int AuthorPaddingLeft = 10;

    private const float SeparatorFixedHeight = 1f;
    private const int HUDElementFontSize = 18;

    private const int NEFNodePaddingLR = 2;
    private const int NEFNodePaddingTop = 2;
    private const int NEFNodePaddingBottom = 5;

    private const int TextFontSize = 13;
    #endregion

    private static float _lastScale = -1f;
    private static float _lastElementScale = -1f;

    private static bool _initialized;

    public static void Init()
    {
        _initialized = true;
        BuildEmptyStyles();
        LoadThemes();
        ApplyTheme(CurrentThemeName);
        _lastScale = -1f;
        _lastElementScale = -1f;
        Rescale();

        ServiceRegistry.Register(new ThemeService());
    }

    public static void LoadThemes()
    {
        ThemeData.LoadedThemes.Clear();

        // 1. Magnetar Default is permanently hardcoded and immutable
        ThemeData.LoadedThemes[ThemeData.InternalDefaultTheme.Name] = ThemeData.InternalDefaultTheme;

        try
        {

            // 2. Dump Meteor Purple as template if file does not exist
            string templatePath = Path.Combine(ThemesDir, "Meteor Purple.json");
            if (!File.Exists(templatePath))
            {
                string json = JsonConvert.SerializeObject(ThemeData.MeteorPurpleTemplate, Formatting.Indented);
                File.WriteAllText(templatePath, json);
                ThemeData.LoadedThemes[ThemeData.MeteorPurpleTemplate.Name] = ThemeData.MeteorPurpleTemplate;
                DebugLogger.Msg("[ThemeManager] Dumped 'Meteor Purple.json' template to disk.");
            }

            // 3. Load all themes from the Themes directory
            string[] files = Directory.GetFiles(ThemesDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var def = JsonConvert.DeserializeObject<ThemeDefinition>(json);
                    if (def != null && !string.IsNullOrWhiteSpace(def.Name))
                    {
                        string themeKey = def.Name.Trim();

                        // Guard: Default theme is strictly immutable
                        if (string.Equals(themeKey, ThemeData.InternalDefaultTheme.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            DebugLogger.Warning($"[ThemeManager] Ignoring '{Path.GetFileName(file)}': 'Magnetar Default' is hardcoded and cannot be modified.");
                            continue;
                        }

                        ThemeData.LoadedThemes[themeKey] = def;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[ThemeManager] Error loading theme file '{Path.GetFileName(file)}': {ex.Message}");
                }
            }

            // Fallback ensure Meteor Purple is registered even if disk loading failed
            if (!ThemeData.LoadedThemes.ContainsKey(ThemeData.MeteorPurpleTemplate.Name))
            {
                ThemeData.LoadedThemes[ThemeData.MeteorPurpleTemplate.Name] = ThemeData.MeteorPurpleTemplate;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[ThemeManager] Error handling Themes directory: {ex.Message}");
            ThemeData.LoadedThemes[ThemeData.MeteorPurpleTemplate.Name] = ThemeData.MeteorPurpleTemplate;
        }
    }

    public static void RegisterTheme(ThemeDefinition theme, bool applyImmediately = false)
    {
        if (theme == null || string.IsNullOrWhiteSpace(theme.Name))
        {
            DebugLogger.Error("[ThemeManager] Cannot register a null or unnamed theme.");
            return;
        }

        string themeKey = theme.Name.Trim();

        if (string.Equals(themeKey, ThemeData.InternalDefaultTheme.Name, StringComparison.OrdinalIgnoreCase))
        {
            DebugLogger.Warning("[ThemeManager] 'Magnetar Default' is immutable and cannot be overwritten.");
            return;
        }

        ThemeData.LoadedThemes[themeKey] = theme;

        if (GUIManager.ThemeSetting != null)
        {
            GUIManager.RefreshThemeOptions();
        }

        if (applyImmediately)
        {
            ApplyTheme(themeKey);
        }

        DebugLogger.Msg($"[ThemeManager] Registered theme: '{themeKey}'");
    }

    public static void ApplyTheme(string themeName)
    {
        if (!_initialized) return;

        if (!ThemeData.LoadedThemes.TryGetValue(themeName, out var theme))
        {
            theme = ThemeData.InternalDefaultTheme;
            themeName = ThemeData.InternalDefaultTheme.Name;
        }

        var d = ThemeData.InternalDefaultTheme;

        void ApplyElement(GUIStyle style, ElementStyleTheme t, ElementStyleTheme fallback)
        {
            var text = ThemeData.ResolveState(t?.Text, fallback?.Text);
            var bg = ThemeData.ResolveState(t?.BackgroundColor, fallback?.BackgroundColor);

            Texture2D normBg = ThemeData.GetTex(bg.normal);
            Texture2D hovBg = ThemeData.GetTex(bg.hover);
            Texture2D actBg = ThemeData.GetTex(bg.active);

            style.normal.textColor = text.normal;
            style.hover.textColor = text.hover;
            style.active.textColor = text.active;
            style.focused.textColor = text.hover;
            style.onNormal.textColor = text.normal;
            style.onHover.textColor = text.hover;
            style.onActive.textColor = text.active;
            style.onFocused.textColor = text.hover;

            style.normal.background = normBg;
            style.hover.background = hovBg;
            style.active.background = actBg;
            style.focused.background = hovBg;
            style.onNormal.background = normBg;
            style.onHover.background = hovBg;
            style.onActive.background = actBg;
            style.onFocused.background = hovBg;
        }

        void ApplyWindow(GUIStyle style, WindowStyleTheme w, WindowStyleTheme fallback)
        {
            Color txt = ThemeData.ParseColor(w?.Text, fallback?.Text);
            Color bg = ThemeData.ParseColor(w?.BackgroundColor, fallback?.BackgroundColor);
            Texture2D bgTex = ThemeData.GetTex(bg);

            style.normal.textColor = txt;
            style.hover.textColor = txt;
            style.active.textColor = txt;
            style.focused.textColor = txt;
            style.onNormal.textColor = txt;
            style.onHover.textColor = txt;
            style.onActive.textColor = txt;
            style.onFocused.textColor = txt;

            style.normal.background = bgTex;
            style.hover.background = bgTex;
            style.active.background = bgTex;
            style.focused.background = bgTex;
            style.onNormal.background = bgTex;
            style.onHover.background = bgTex;
            style.onActive.background = bgTex;
            style.onFocused.background = bgTex;
        }

        // 1. Top Bar
        ApplyElement(TopBarStyle, theme.TopBarOff, d.TopBarOff);
        ApplyElement(TopBarActiveStyle, theme.TopBarActive, d.TopBarActive);

        // 2. Category Windows
        ApplyWindow(CategoryWindowStyle, theme.CategoryWindow, d.CategoryWindow);
        ApplyElement(CategoryHeaderStyle, theme.CategoryHeader, d.CategoryHeader);
        ApplyElement(CategoryModuleOffStyle, theme.CategoryModuleOff, d.CategoryModuleOff);
        ApplyElement(CategoryModuleOnStyle, theme.CategoryModuleOn, d.CategoryModuleOn);

        // 3. Settings Window & Close Button
        ApplyElement(CloseButtonStyle, theme.CloseButton, d.CloseButton);

        string rawWindowBg = theme.SettingsWindow?.WindowBackground
                             ?? theme.CategoryWindow?.BackgroundColor
                             ?? d.SettingsWindow.WindowBackground;
        Texture2D winBgTex = ThemeData.GetTex(ThemeData.ParseColor(rawWindowBg, d.SettingsWindow.WindowBackground));
        SettingsWndowBgStyle.normal.background = winBgTex;
        SettingsWndowBgStyle.hover.background = winBgTex;
        SettingsWndowBgStyle.active.background = winBgTex;
        SettingsWndowBgStyle.focused.background = winBgTex;
        SettingsWndowBgStyle.onNormal.background = winBgTex;
        SettingsWndowBgStyle.onHover.background = winBgTex;
        SettingsWndowBgStyle.onActive.background = winBgTex;
        SettingsWndowBgStyle.onFocused.background = winBgTex;

        ApplyWindow(SettingsWndowStyle, theme.SettingsWindow, d.SettingsWindow);

        // 4. Setting Toggles
        ApplyElement(SettingOff, theme.SettingOff, d.SettingOff);
        ApplyElement(SettingOn, theme.SettingOn, d.SettingOn);

        // 5. Action Buttons
        ApplyElement(ButtonSettingStyle, theme.ButtonSetting, d.ButtonSetting);
        ApplyElement(ResetButtonStyle, theme.ResetButton, d.ResetButton);
        ApplyElement(ListAddButtonStyle, theme.ListAddButton, d.ListAddButton);
        ApplyElement(ListRemoveButtonStyle, theme.ListRemoveButton, d.ListRemoveButton);

        // 6. Section Settings
        ApplyElement(SectionGroupHeaderStyle, theme.Section?.GroupHeader, d.Section?.GroupHeader ?? d.CategoryHeader);
        ApplyElement(SectionHeaderStyle, theme.Section?.SectionHeader, d.Section?.SectionHeader ?? d.CategoryModuleOff);
        ApplyElement(SectionRemoveButtonStyle, theme.Section?.RemoveButton, d.Section?.RemoveButton ?? d.ListRemoveButton ?? d.SettingOff);
        ApplyElement(SectionAddButtonStyle, theme.Section?.AddButton, d.Section?.AddButton ?? d.SettingOn);

        // 7. Typography
        SettingsDescriptionStyle.normal.textColor = ThemeData.ParseColor(theme.Typography?.Description, d.Typography.Description);
        SettingLabelStyle.normal.textColor = ThemeData.ParseColor(theme.Typography?.Label, d.Typography.Label);
        SettingAuthorStyle.normal.textColor = ThemeData.ParseColor(theme.Typography?.Author, d.Typography.Author);

        Color baseText = ThemeData.ParseColor(theme.Typography?.Text, d.Typography.Text);
        SettingTextStyle.normal.textColor = baseText;
        TextStyle.normal.textColor = baseText;

        TextHighlightedStyle.normal.textColor = ThemeData.ParseColor(theme.Typography?.HighlightText, d.Typography.HighlightText);
        Texture2D hiBgTex = ThemeData.GetTex(ThemeData.ParseColor(theme.Typography?.HighlightBackground, d.Typography.HighlightBackground));
        TextHighlightedStyle.normal.background = hiBgTex;
        TextHighlightedStyle.hover.background = hiBgTex;

        // 8. Separators & HUD
        SeparatorStyle.normal.background = ThemeData.GetTex(ThemeData.ParseColor(theme.Misc?.Separator, d.Misc.Separator));
        Color sepTextColor = ThemeData.ParseColor(theme.Misc?.SeparatorText ?? theme.Typography?.Secondary, d.Typography.Secondary);
        SeparatorTextStyle.normal.textColor = sepTextColor;
        DimBackgroundStyle.normal.background = ThemeData.GetTex(ThemeData.ParseColor(theme.Misc?.DimBackground, d.Misc.DimBackground));
        HUDElementStyle.normal.textColor = ThemeData.ParseColor(theme.HUD?.TextColor, d.HUD.TextColor);

        // 9. NEF
        NEFLineStyle.normal.background = ThemeData.GetTex(ThemeData.ParseColor(theme.NEF?.LineColor, d.NEF.LineColor));
        NEFNodeStyle.normal.background = ThemeData.GetTex(ThemeData.ParseColor(theme.NEF?.NodeBackground, d.NEF.NodeBackground));

        // 10. Slider
        Color trackOffColor = ThemeData.ParseColor(theme.Slider?.TrackOff ?? theme.SettingOff?.BackgroundColor?.Normal, d.Slider.TrackOff);
        Color trackOnColor = ThemeData.ParseColor(theme.Slider?.TrackOn ?? theme.SettingOn?.BackgroundColor?.Normal, d.Slider.TrackOn);
        Color thumbColor = ThemeData.ParseColor(theme.Slider?.Thumb ?? theme.SettingOn?.BackgroundColor?.Normal, d.Slider.Thumb);
        Color thumbHoverColor = ThemeData.ParseColor(theme.Slider?.ThumbHover ?? theme.Slider?.Thumb ?? theme.SettingOn?.BackgroundColor?.Hover, d.Slider.ThumbHover);

        SliderTrackOffStyle.normal.background = ThemeData.GetTex(trackOffColor);
        SliderTrackOnStyle.normal.background = ThemeData.GetTex(trackOnColor);

        SliderThumbStyle.normal.background = ThemeData.GetCircleTex(thumbColor);
        SliderThumbStyle.hover.background = ThemeData.GetCircleTex(thumbHoverColor);
        SliderThumbStyle.active.background = ThemeData.GetCircleTex(thumbHoverColor);

        // Update Runtime Colors on ThemeData
        var catModOffBg = ThemeData.ResolveState(theme.CategoryModuleOff?.BackgroundColor, d.CategoryModuleOff.BackgroundColor);
        var catModOnBg = ThemeData.ResolveState(theme.CategoryModuleOn?.BackgroundColor, d.CategoryModuleOn.BackgroundColor);
        var catModOffText = ThemeData.ResolveState(theme.CategoryModuleOff?.Text, d.CategoryModuleOff.Text);
        var tbOffBg = ThemeData.ResolveState(theme.TopBarOff?.BackgroundColor, d.TopBarOff.BackgroundColor);

        ThemeData.AccentColor = catModOnBg.normal;
        ThemeData.AccentHoverColor = catModOnBg.hover;
        ThemeData.BackgroundColor = ThemeData.ParseColor(theme.CategoryWindow?.BackgroundColor, d.CategoryWindow.BackgroundColor);
        ThemeData.LightBackgroundColor = catModOffBg.normal;
        ThemeData.TextWhite = baseText;
        ThemeData.TextDim = catModOffText.normal;
        ThemeData.HoverColor = catModOffBg.hover;
        ThemeData.ActiveColor = tbOffBg.active;
        ThemeData.DimColor = ThemeData.ParseColor(theme.Misc?.DimBackground, d.Misc.DimBackground);
        ThemeData.NefLineColor = ThemeData.ParseColor(theme.NEF?.LineColor, d.NEF.LineColor);
        ThemeData.NefNodeColor = ThemeData.ParseColor(theme.NEF?.NodeBackground, d.NEF.NodeBackground);
        ThemeData.SettingOnColor = catModOnBg.normal;
        ThemeData.SettingOffColor = catModOffBg.normal;
        ThemeData.SeparatorColor = ThemeData.ParseColor(theme.Misc?.Separator, d.Misc.Separator);

        _lastScale = -1f;
        _lastElementScale = -1f;
        Rescale();
    }

    private static void SetOffset(RectOffset ro, int left, int right, int top, int bottom)
    {
        if (ro == null) return;
        ro.left = left;
        ro.right = right;
        ro.top = top;
        ro.bottom = bottom;
    }

    private static void BuildEmptyStyles()
    {
        TopBarStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        TopBarActiveStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        CategoryWindowStyle = new GUIStyle { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
        CategoryHeaderStyle = new GUIStyle { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
        CategoryModuleOffStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft };
        CategoryModuleOnStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft };
        CloseButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        SettingsWndowBgStyle = new GUIStyle();
        SettingsWndowStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        SettingOff = new GUIStyle { alignment = TextAnchor.MiddleLeft };
        SettingOn = new GUIStyle { alignment = TextAnchor.MiddleLeft };

        ButtonSettingStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        ResetButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        ListAddButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        ListRemoveButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

        SettingsDescriptionStyle = new GUIStyle { wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
        SettingLabelStyle = new GUIStyle { wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
        SettingAuthorStyle = new GUIStyle { fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleLeft, richText = true };
        SettingTextStyle = new GUIStyle { wordWrap = false, alignment = TextAnchor.MiddleLeft, richText = true, clipping = TextClipping.Clip };
        TextStyle = new GUIStyle { wordWrap = false, alignment = TextAnchor.MiddleLeft, richText = false, clipping = TextClipping.Clip };
        TextHighlightedStyle = new GUIStyle { wordWrap = false, alignment = TextAnchor.MiddleLeft, richText = false, clipping = TextClipping.Clip };

        SeparatorStyle = new GUIStyle();
        SeparatorTextStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = true, clipping = TextClipping.Overflow };
        HUDElementStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = true };

        NEFLineStyle = new GUIStyle();
        NEFNodeStyle = new GUIStyle { alignment = TextAnchor.LowerCenter };
        DimBackgroundStyle = new GUIStyle();

        SliderTrackOffStyle = new GUIStyle();
        SliderTrackOnStyle = new GUIStyle();
        SliderThumbStyle = new GUIStyle();

        SectionGroupHeaderStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        SectionHeaderStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft };
        SectionRemoveButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        SectionAddButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
    }

    public static void Rescale()
    {
        if (!_initialized) return;

        float scale = Config.GUIScale;
        float elementScale = Config.ElementScale;

        if (Mathf.Approximately(scale, _lastScale) && Mathf.Approximately(elementScale, _lastElementScale))
            return;

        _lastScale = scale;
        _lastElementScale = elementScale;

        int S(int baseValue) => Mathf.Max(1, Mathf.RoundToInt(Config.S(baseValue)));
        float Sf(float baseValue) => Mathf.Max(0f, Config.S(baseValue));

        // TopBar
        TopBarStyle.fontSize = S(TopBarFontSize);
        SetOffset(TopBarStyle.padding, S(TopBarPaddingLR), S(TopBarPaddingLR), S(TopBarPaddingTB), S(TopBarPaddingTB));

        TopBarActiveStyle.fontSize = S(TopBarFontSize);
        SetOffset(TopBarActiveStyle.padding, S(TopBarPaddingLR), S(TopBarPaddingLR), S(TopBarPaddingTB), S(TopBarPaddingTB));

        // Category Modules
        CategoryHeaderStyle.fontSize = S(ModuleWindowFontSize);
        SetOffset(CategoryHeaderStyle.padding, 0, 0, S(ModuleWindowPaddingTop), 0);

        CategoryModuleOnStyle.fontSize = S(ModuleFontSize);
        SetOffset(CategoryModuleOnStyle.padding, S(ModulePaddingLeft), 0, 0, 0);

        CloseButtonStyle.fontSize = S(ModuleFontSize);

        CategoryModuleOffStyle.fontSize = S(ModuleFontSize);
        SetOffset(CategoryModuleOffStyle.padding, S(ModulePaddingLeft), 0, 0, 0);

        // Windows
        CategoryWindowStyle.fontSize = S(ModuleWindowFontSize);
        SetOffset(CategoryWindowStyle.padding, 0, 0, S(ModuleWindowPaddingTop), 0);

        SettingsWndowStyle.fontSize = S(SettingsWindowFontSize);
        SetOffset(SettingsWndowStyle.padding, 0, 0, 0, 0);

        // Controls
        SettingOn.fontSize = S(SettingFontSize);
        SetOffset(SettingOn.padding, S(SettingPaddingLeft), 0, 0, 0);

        SettingOff.fontSize = S(SettingFontSize);
        SetOffset(SettingOff.padding, S(SettingPaddingLeft), 0, 0, 0);

        ButtonSettingStyle.fontSize = S(SettingFontSize);
        ResetButtonStyle.fontSize = S(SettingFontSize);
        ListAddButtonStyle.fontSize = S(SettingFontSize);
        ListRemoveButtonStyle.fontSize = S(SettingFontSize);

        // Typography
        SettingsDescriptionStyle.fontSize = S(DescriptionFontSize);
        SetOffset(SettingsDescriptionStyle.padding, S(DescriptionPaddingLR), S(DescriptionPaddingLR), S(DescriptionPaddingTB), S(DescriptionPaddingTB));

        SettingLabelStyle.fontSize = S(SettingDescriptionFontSize);
        SetOffset(SettingLabelStyle.padding, S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingTB), S(SettingDescriptionPaddingTB));

        SettingAuthorStyle.fontSize = S(AuthorFontSize);
        SetOffset(SettingAuthorStyle.padding, S(AuthorPaddingLeft), 0, 0, 0);

        SettingTextStyle.fontSize = S(TextFontSize);
        TextStyle.fontSize = S(TextFontSize);
        TextHighlightedStyle.fontSize = TextStyle.fontSize;

        // Separators & HUD
        SeparatorStyle.fixedHeight = Sf(SeparatorFixedHeight);
        SeparatorTextStyle.fontSize = S(SettingFontSize);
        HUDElementStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(HUDElementFontSize * elementScale));

        SetOffset(NEFNodeStyle.padding, S(NEFNodePaddingLR), S(NEFNodePaddingLR), S(NEFNodePaddingTop), S(NEFNodePaddingBottom));

        // Section Settings
        SectionGroupHeaderStyle.fontSize = S(SettingDescriptionFontSize);
        SetOffset(SectionGroupHeaderStyle.padding, S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingLR), 0, 0);

        SectionHeaderStyle.fontSize = S(SettingFontSize);
        SetOffset(SectionHeaderStyle.padding, S(SettingPaddingLeft), 0, 0, 0);

        SectionRemoveButtonStyle.fontSize = S(SettingFontSize);
        SectionAddButtonStyle.fontSize = S(SettingFontSize);
    }

    private class ThemeService : IWarmUp
    {
        public string Name => "ThemeManager";
        public int Priority => ServicePriority.Highest;

        public void OnWarmUp() => ThemeManager.Rescale();
    }
}