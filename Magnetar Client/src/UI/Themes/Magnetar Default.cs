using Magnetar_Client.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.UI.Themes;

#region Nested Theme Data Contracts
[Serializable]
public class ColorState
{
    [JsonProperty("normal")]
    public string Normal { get; set; }

    [JsonProperty("hover", NullValueHandling = NullValueHandling.Ignore)]
    public string Hover { get; set; }

    [JsonProperty("active", NullValueHandling = NullValueHandling.Ignore)]
    public string Active { get; set; }

    public ColorState() { }
    public ColorState(string normal, string hover = null, string active = null)
    {
        Normal = normal;
        Hover = hover ?? normal;
        Active = active ?? normal;
    }
}

[Serializable]
public class ElementStyleTheme
{
    [JsonProperty("text")]
    public ColorState Text { get; set; } = new ColorState();

    [JsonProperty("background color")]
    public ColorState BackgroundColor { get; set; } = new ColorState();

    public ElementStyleTheme() { }
    public ElementStyleTheme(ColorState text, ColorState bg)
    {
        Text = text;
        BackgroundColor = bg;
    }
}

[Serializable]
public class WindowStyleTheme
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("background color")]
    public string BackgroundColor { get; set; }

    [JsonProperty("window background", NullValueHandling = NullValueHandling.Ignore)]
    public string WindowBackground { get; set; }

    public WindowStyleTheme() { }
    public WindowStyleTheme(string text, string bg, string windowBg = null)
    {
        Text = text;
        BackgroundColor = bg;
        WindowBackground = windowBg ?? bg;
    }
}

[Serializable]
public class TypographyTheme
{
    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("author")]
    public string Author { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("highlight text")]
    public string HighlightText { get; set; }

    [JsonProperty("highlight background")]
    public string HighlightBackground { get; set; }

    [JsonProperty("secondary", NullValueHandling = NullValueHandling.Ignore)]
    public string Secondary { get; set; }

    [JsonProperty("primary", NullValueHandling = NullValueHandling.Ignore)]
    public string Primary { get => Text; set => Text = value; }
}

[Serializable]
public class NefTheme
{
    [JsonProperty("line color")]
    public string LineColor { get; set; }

    [JsonProperty("node background")]
    public string NodeBackground { get; set; }
}

[Serializable]
public class HudTheme
{
    [JsonProperty("text color")]
    public string TextColor { get; set; }
}

[Serializable]
public class MiscTheme
{
    [JsonProperty("dim background")]
    public string DimBackground { get; set; }

    [JsonProperty("separator")]
    public string Separator { get; set; }

    [JsonProperty("separator text", NullValueHandling = NullValueHandling.Ignore)]
    public string SeparatorText { get; set; }
}

[Serializable]
public class ThemeData
{
    [JsonProperty("name")]
    public string Name { get; set; } = "Custom Theme";

    [JsonProperty("TopBarOff")]
    public ElementStyleTheme TopBarOff { get; set; } = new ElementStyleTheme();

    [JsonProperty("TopBarActive")]
    public ElementStyleTheme TopBarActive { get; set; } = new ElementStyleTheme();

    [JsonProperty("CategoryHeader")]
    public ElementStyleTheme CategoryHeader { get; set; } = new ElementStyleTheme();

    [JsonProperty("CategoryWindow")]
    public WindowStyleTheme CategoryWindow { get; set; } = new WindowStyleTheme();

    [JsonProperty("CategoryModuleOff")]
    public ElementStyleTheme CategoryModuleOff { get; set; } = new ElementStyleTheme();

    [JsonProperty("CategoryModuleOn")]
    public ElementStyleTheme CategoryModuleOn { get; set; } = new ElementStyleTheme();

    [JsonProperty("CloseButton")]
    public ElementStyleTheme CloseButton { get; set; } = new ElementStyleTheme();

    [JsonProperty("SettingsWindow")]
    public WindowStyleTheme SettingsWindow { get; set; } = new WindowStyleTheme();

    [JsonProperty("SettingOff")]
    public ElementStyleTheme SettingOff { get; set; } = new ElementStyleTheme();

    [JsonProperty("SettingOn")]
    public ElementStyleTheme SettingOn { get; set; } = new ElementStyleTheme();

    [JsonProperty("ButtonSetting", NullValueHandling = NullValueHandling.Ignore)]
    public ElementStyleTheme ButtonSetting { get; set; }

    [JsonProperty("ResetButton", NullValueHandling = NullValueHandling.Ignore)]
    public ElementStyleTheme ResetButton { get; set; }

    [JsonProperty("ListAddButton", NullValueHandling = NullValueHandling.Ignore)]
    public ElementStyleTheme ListAddButton { get; set; }

    [JsonProperty("ListRemoveButton", NullValueHandling = NullValueHandling.Ignore)]
    public ElementStyleTheme ListRemoveButton { get; set; }

    [JsonProperty("Typography")]
    public TypographyTheme Typography { get; set; } = new TypographyTheme();

    [JsonProperty("NEF")]
    public NefTheme NEF { get; set; } = new NefTheme();

    [JsonProperty("HUD")]
    public HudTheme HUD { get; set; } = new HudTheme();

    [JsonProperty("Misc")]
    public MiscTheme Misc { get; set; } = new MiscTheme();

    [JsonProperty("Slider", NullValueHandling = NullValueHandling.Ignore)]
    public SliderTheme Slider { get; set; }
}

[Serializable]
public class SliderTheme
{
    [JsonProperty("track off", NullValueHandling = NullValueHandling.Ignore)]
    public string TrackOff { get; set; }

    [JsonProperty("track on", NullValueHandling = NullValueHandling.Ignore)]
    public string TrackOn { get; set; }

    [JsonProperty("thumb", NullValueHandling = NullValueHandling.Ignore)]
    public string Thumb { get; set; }

    [JsonProperty("thumb hover", NullValueHandling = NullValueHandling.Ignore)]
    public string ThumbHover { get; set; }
}

#endregion

public static class Magnetar_Default
{
    public static bool IsInitialized { get; private set; } = false;

    #region Styles

    public static GUIStyle TopBarStyle;
    public static GUIStyle TopBarActiveStyle;
    public static GUIStyle CategoryWindowStyle;
    public static GUIStyle CategoryHeaderStyle;
    public static GUIStyle CategoryModuleOffStyle;
    public static GUIStyle CategoryModuleOnStyle;
    public static GUIStyle CloseButtonStyle;
    public static GUIStyle SettingsWndowBgStyle;
    public static GUIStyle SettingsWndowStyle;
    public static GUIStyle SettingsDescriptionStyle;
    public static GUIStyle SettingTextStyle;
    public static GUIStyle SettingAuthorStyle;
    public static GUIStyle SettingOff;
    public static GUIStyle SettingOn;
    public static GUIStyle SettingLabelStyle;
    public static GUIStyle ButtonSettingStyle;
    public static GUIStyle ResetButtonStyle;
    public static GUIStyle ListAddButtonStyle;
    public static GUIStyle ListRemoveButtonStyle;

    public static GUIStyle SeparatorStyle;
    public static GUIStyle SeparatorTextStyle;
    public static GUIStyle TextStyle;
    public static GUIStyle TextHighlightedStyle;
    public static GUIStyle DimBackgroundStyle;
    public static GUIStyle HUDElementStyle;
    public static GUIStyle NEFLineStyle;
    public static GUIStyle NEFNodeStyle;
    public static GUIStyle SliderTrackOffStyle;
    public static GUIStyle SliderTrackOnStyle;
    public static GUIStyle SliderThumbStyle;

    #endregion

    #region Dynamic Theme Colors
    public static Color BackgroundColor { get; private set; }
    public static Color AccentColor { get; private set; }
    public static Color AccentHoverColor { get; private set; }
    public static Color LightBackgroundColor { get; private set; }
    public static Color TextWhite { get; private set; }
    public static Color TextDim { get; private set; }
    public static Color HoverColor { get; private set; }
    public static Color ActiveColor { get; private set; }
    public static Color DimColor { get; private set; }
    public static Color NefLineColor { get; private set; }
    public static Color NefNodeColor { get; private set; }
    #endregion

    #region Hardcoded Default Theme (Safety Fallback)
    public static readonly ThemeData InternalDefaultTheme = new()
    {
        Name = "Magnetar Default",
        TopBarOff = new ElementStyleTheme(
            new ColorState("#AEAEAEFF", "#FFFFFFFF", "#FFFFFFFF"),
            new ColorState("#1A1A1ADC", "#1C1C1CFF", "#333333FF")
        ),
        TopBarActive = new ElementStyleTheme(
            new ColorState("#FFFFFFFF", "#FFFFFFFF", "#FFFFFFFF"),
            new ColorState("#FF3D3DFF", "#FF3D3DFF", "#FF3D3DFF")
        ),
        CategoryHeader = new ElementStyleTheme(
            new ColorState("#FFFFFFFF", "#FFFFFFFF", "#FFFFFFFF"),
            new ColorState("#1A1A1ADC", "#1C1C1CFF", "#333333FF")
        ),
        CategoryWindow = new WindowStyleTheme("#FFFFFFFF", "#1A1A1ADC"),
        CategoryModuleOff = new ElementStyleTheme(
            new ColorState("#AEAEAEFF", "#FFFFFFFF", "#FFFFFFFF"),
            new ColorState("#1C1C1CD6", "#1C1C1CFF", "#1C1C1CFF")
        ),
        CategoryModuleOn = new ElementStyleTheme(
            new ColorState("#000000FF", "#000000FF", "#000000FF"),
            new ColorState("#FF3D3DFF", "#F03333FF", "#F03333FF")
        ),
        CloseButton = new ElementStyleTheme(
            new ColorState("#000000FF", "#000000FF", "#000000FF"),
            new ColorState("#FF3D3DFF", "#F03333FF", "#F03333FF")
        ),
        SettingsWindow = new WindowStyleTheme("#000000FF", "#FF3D3DFF", "#1A1A1ADC"),
        SettingOff = new ElementStyleTheme(
            new ColorState("#AEAEAEFF", "#FFFFFFFF", "#FFFFFFFF"),
            new ColorState("#1C1C1CD6", "#1C1C1CFF", "#1C1C1CFF")
        ),
        SettingOn = new ElementStyleTheme(
            new ColorState("#000000FF", "#000000FF", "#000000FF"),
            new ColorState("#FF3D3DFF", "#F03333FF", "#F03333FF")
        ),
        ButtonSetting = new ElementStyleTheme(
            new ColorState("#FFFFFFFF", "#FFFFFFFF", "#FFFFFFFF"),
            new ColorState("#2A2A2ADD", "#3A3A3AFF", "#222222FF")
        ),
        ResetButton = new ElementStyleTheme(
            new ColorState("#D0D0D0FF", "#FFFFFFFF", "#FF5555FF"),
            new ColorState("#242424DC", "#333333FF", "#1C1C1CFF")
        ),
        ListAddButton = new ElementStyleTheme(
            new ColorState("#FFFFFFFF", "#FFFFFFFF", "#FFFFFFFF"),
            new ColorState("#005213DC", "#00751bFF", "#008f21FF")
        ),
        ListRemoveButton = new ElementStyleTheme(
            new ColorState("#FF6B6BFF", "#FF8E8EFF", "#FF3D3DFF"),
            new ColorState("#2B1818DC", "#3D1E1EFF", "#201212FF")
        ),
        Typography = new TypographyTheme
        {
            Description = "#BFBFBFFF",
            Label = "#E6E6E6FF",
            Author = "#808080FF",
            Text = "#E6E6E6FF",
            Secondary = "#AEAEAEFF",
            HighlightText = "#FFFFFFFF",
            HighlightBackground = "#FF3D3DFF"
        },
        NEF = new NefTheme
        {
            LineColor = "#FFFFFFFF",
            NodeBackground = "#FF3D3DFF"
        },
        HUD = new HudTheme
        {
            TextColor = "#FFFFFFFF"
        },
        Misc = new MiscTheme
        {
            DimBackground = "#1A1A1A66",
            Separator = "#FFFFFFFF",
            SeparatorText = "#FFFFFFFF"
        },
        Slider = new SliderTheme
        {
            TrackOff = "#22252FFF",
            TrackOn = "#FF3D3DFF",
            Thumb = "#FF3D3DFF",
            ThumbHover = "#FF3D3DFF"
        },
    };
    #endregion

    public static readonly Dictionary<string, ThemeData> LoadedThemes = new(StringComparer.OrdinalIgnoreCase);
    public static string CurrentThemeName { get; private set; } = "Magnetar Default";

    private static readonly Dictionary<Color, Texture2D> _texCache = new();
    private static float lastScale = -1f;
    private static float lastElementScale = -1f;

    #region Base Sizes
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

    private const int DescriptionFontSize = 18;
    private const int DescriptionPaddingLR = 5;
    private const int DescriptionPaddingTB = 2;

    private const int SettingDescriptionFontSize = 14;
    private const int SettingDescriptionPaddingLR = 5;
    private const int SettingDescriptionPaddingTB = 2;

    private const int AuthorFontSize = 15;
    private const int AuthorPaddingLeft = 10;

    private const float SeparatorFixedHeight = 1;

    private const int HUDElementFontSize = 18;

    private const int NEFNodePaddingLR = 2;
    private const int NEFNodePaddingTop = 2;
    private const int NEFNodePaddingBottom = 5;

    private const int TextFontSize = 13;
    #endregion

    public static void Init()
    {
        LoadThemesFromJson();
        BuildEmptyStyles();

        IsInitialized = true;

        string requestedTheme = Config.Theme;
        if (string.IsNullOrEmpty(requestedTheme) || !LoadedThemes.ContainsKey(requestedTheme))
        {
            requestedTheme = InternalDefaultTheme.Name;
        }

        ApplyTheme(requestedTheme);

        lastScale = -1f;
        lastElementScale = -1f;
        Rescale();

        DebugLogger.Msg($"[Themes] Initialized with theme: '{CurrentThemeName}'");
    }

    public static void LoadThemesFromJson()
    {
        LoadedThemes.Clear();
        LoadedThemes[InternalDefaultTheme.Name] = InternalDefaultTheme;

        string dataDir = Path.Combine(SaveLoad.ModsDir, "Magnetar Data");
        string themePath = Path.Combine(dataDir, "themes.json");

        try
        {
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

            if (!File.Exists(themePath))
            {
                var templateList = new List<ThemeData>
                {
                    new() {
                        Name = "Meteor Purple",
                        TopBarOff = new ElementStyleTheme(
                            new ColorState("#e8e8e8", "#e8e8e8", "#e8e8e8"),
                            new ColorState("#11141b94", "#131721ca", "#131721ca")
                        ),
                        TopBarActive = new ElementStyleTheme(
                            new ColorState("#e8e8e8", "#e8e8e8", "#e8e8e8"),
                            new ColorState("#131721ca", "#131721ca", "#131721ca")
                        ),
                        CategoryHeader = new ElementStyleTheme(
                            new ColorState("#E6EDF3FF", "#FFFFFFFF", "#FFFFFFFF"),
                            new ColorState("#7d00f1", "#9a2eff", "#9a2eff")
                        ),
                        CategoryWindow = new WindowStyleTheme("#FFFFFFFF", "#100c14c0"),
                        CategoryModuleOff = new ElementStyleTheme(
                            new ColorState("#8B949EFF", "#e6e6e6ea", "#e6e6e6ea"),
                            new ColorState("#00000000", "#22142594", "#22142594")
                        ),
                        CategoryModuleOn = new ElementStyleTheme(
                            new ColorState("#8B949EFF", "#e6e6e6ea", "#e6e6e6ea"),
                            new ColorState("#22142594", "#22142594", "#22142594")
                        ),
                        CloseButton = new ElementStyleTheme(
                            new ColorState("#000000FF", "#000000FF", "#000000FF"),
                            new ColorState("#7d00f1", "#A855F7FF", "#A855F7FF")
                        ),
                        SettingsWindow = new WindowStyleTheme("#FFFFFFFF", "#7d00f1", "#11141bb6"),
                        SettingOff = new ElementStyleTheme(
                            new ColorState("#8B949EFF", "#FFFFFFFF", "#FFFFFFFF"),
                            new ColorState("#02010271", "#0000009d", "#0000009d")
                        ),
                        SettingOn = new ElementStyleTheme(
                            new ColorState("#000000FF", "#000000FF", "#000000FF"),
                            new ColorState("#8B0FFFFF", "#992cff", "#8B0FFFFF")
                        ),
                        ButtonSetting = new ElementStyleTheme(
                            new ColorState("#FFFFFFFF", "#FFFFFFFF", "#FFFFFFFF"),
                            new ColorState("#2A123DCC", "#441D63FF", "#1F0A2FFF")
                        ),
                        ResetButton = new ElementStyleTheme(
                            new ColorState("#D4C2F0FF", "#FFFFFFFF", "#A855F7FF"),
                            new ColorState("#1D1226CC", "#2C173DFF", "#150B1EFF")
                        ),
                        ListAddButton = new ElementStyleTheme(
                            new ColorState("#E6EDF3FF", "#FFFFFFFF", "#FFFFFFFF"),
                            new ColorState("#1E1128CC", "#2F1940FF", "#140A1CFF")
                        ),
                        ListRemoveButton = new ElementStyleTheme(
                            new ColorState("#FF77BCFF", "#FFA6D2FF", "#E11D48FF"),
                            new ColorState("#2D1022CC", "#4A1835FF", "#1F0A17FF")
                        ),
                        Typography = new TypographyTheme
                        {
                            Description = "#ec45ff",
                            Label = "#E6E6E6FF",
                            Author = "#808080FF",
                            Text = "#E6EDF3FF",
                            Secondary = "#8B949EFF",
                            HighlightText = "#FFFFFFFF",
                            HighlightBackground = "#203e6ec4"
                        },
                        NEF = new NefTheme
                        {
                            LineColor = "#FFFFFFFF",
                            NodeBackground = "#8B0FFFFF"
                        },
                        HUD = new HudTheme
                        {
                            TextColor = "#FFFFFFFF"
                        },
                        Misc = new MiscTheme
                        {
                            DimBackground = "#1a1a1a5c",
                            Separator = "#ffffff",
                            SeparatorText = "#ffffff"
                        },
                        Slider = new SliderTheme
                        {
                            TrackOff = "#1D212BFF",
                            TrackOn = "#00ff9d",
                            Thumb = "#00ff9d",
                            ThumbHover = "#00ff9d"
                        }
                    }
                };

                string jsonTemplate = JsonConvert.SerializeObject(templateList, Formatting.Indented);
                File.WriteAllText(themePath, jsonTemplate);
            }

            string rawJson = File.ReadAllText(themePath);
            var parsedThemes = JsonConvert.DeserializeObject<List<ThemeData>>(rawJson);

            if (parsedThemes != null)
            {
                foreach (var th in parsedThemes)
                {
                    if (!string.IsNullOrEmpty(th.Name))
                    {
                        LoadedThemes[th.Name] = th;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[Themes] Error loading themes.json, falling back to internal default: {ex.Message}");
        }
    }

    public static void ApplyTheme(string themeName)
    {
        if (!IsInitialized)
        {
            CurrentThemeName = themeName;
            return;
        }

        if (!LoadedThemes.TryGetValue(themeName, out var theme))
        {
            DebugLogger.Warning($"[Themes] Theme '{themeName}' not found. Falling back to default.");
            theme = InternalDefaultTheme;
            themeName = InternalDefaultTheme.Name;
        }

        CurrentThemeName = themeName;
        var d = InternalDefaultTheme;

        void ApplyElement(GUIStyle style, ElementStyleTheme t, ElementStyleTheme fallback)
        {
            var text = ResolveState(t?.Text, fallback?.Text);
            var bg = ResolveState(t?.BackgroundColor, fallback?.BackgroundColor);
            style.normal.textColor = text.normal;
            style.hover.textColor = text.hover;
            style.active.textColor = text.active;
            style.normal.background = GetTex(bg.normal);
            style.hover.background = GetTex(bg.hover);
            style.active.background = GetTex(bg.active);
        }

        // --- 1. Top Bar ---
        ApplyElement(TopBarStyle, theme.TopBarOff, d.TopBarOff);
        ApplyElement(TopBarActiveStyle, theme.TopBarActive, d.TopBarActive);

        // --- 2. Category Header & Window ---
        ApplyElement(CategoryHeaderStyle, theme.CategoryHeader, d.CategoryHeader);
        CategoryWindowStyle.normal.textColor = ParseColor(theme.CategoryWindow?.Text, d.CategoryWindow.Text);
        CategoryWindowStyle.normal.background = GetTex(ParseColor(theme.CategoryWindow?.BackgroundColor, d.CategoryWindow.BackgroundColor));

        // --- 3. Category Modules ---
        ApplyElement(CategoryModuleOffStyle, theme.CategoryModuleOff, d.CategoryModuleOff);
        ApplyElement(CategoryModuleOnStyle, theme.CategoryModuleOn, d.CategoryModuleOn);

        // --- 4. Close Button ---
        ApplyElement(CloseButtonStyle, theme.CloseButton, d.CloseButton);

        // --- 5. Settings Window ---
        string rawWindowBg = theme.SettingsWindow?.WindowBackground
                             ?? theme.CategoryWindow?.BackgroundColor
                             ?? d.SettingsWindow.WindowBackground;
        SettingsWndowBgStyle.normal.background = GetTex(ParseColor(rawWindowBg, d.SettingsWindow.WindowBackground));

        SettingsWndowStyle.normal.textColor = ParseColor(theme.SettingsWindow?.Text, d.SettingsWindow.Text);
        SettingsWndowStyle.normal.background = GetTex(ParseColor(theme.SettingsWindow?.BackgroundColor, d.SettingsWindow.BackgroundColor));

        // --- 6. Setting Toggles ---
        ApplyElement(SettingOff, theme.SettingOff, d.SettingOff);
        ApplyElement(SettingOn, theme.SettingOn, d.SettingOn);

        // --- 7. Button Settings, Reset, and List Operations ---
        ApplyElement(ButtonSettingStyle, theme.ButtonSetting, d.ButtonSetting);
        ApplyElement(ResetButtonStyle, theme.ResetButton, d.ResetButton);
        ApplyElement(ListAddButtonStyle, theme.ListAddButton, d.ListAddButton);
        ApplyElement(ListRemoveButtonStyle, theme.ListRemoveButton, d.ListRemoveButton);

        // --- 8. Typography ---
        SettingsDescriptionStyle.normal.textColor = ParseColor(theme.Typography?.Description, d.Typography.Description);
        SettingLabelStyle.normal.textColor = ParseColor(theme.Typography?.Label, d.Typography.Label);
        SettingAuthorStyle.normal.textColor = ParseColor(theme.Typography?.Author, d.Typography.Author);

        Color baseText = ParseColor(theme.Typography?.Text, d.Typography.Text);
        SettingTextStyle.normal.textColor = baseText;
        TextStyle.normal.textColor = baseText;

        TextHighlightedStyle.normal.textColor = ParseColor(theme.Typography?.HighlightText, d.Typography.HighlightText);
        TextHighlightedStyle.normal.background = GetTex(ParseColor(theme.Typography?.HighlightBackground, d.Typography.HighlightBackground));

        // --- 9. Separator & Dim ---
        SeparatorStyle.normal.background = GetTex(ParseColor(theme.Misc?.Separator, d.Misc.Separator));

        Color sepTextColor = ParseColor(theme.Misc?.SeparatorText ?? theme.Typography?.Secondary, d.Typography.Secondary);
        SeparatorTextStyle.normal.textColor = sepTextColor;

        DimBackgroundStyle.normal.background = GetTex(ParseColor(theme.Misc?.DimBackground, d.Misc.DimBackground));

        // --- 10. NEF & HUD ---
        NEFLineStyle.normal.background = GetTex(ParseColor(theme.NEF?.LineColor, d.NEF.LineColor));
        NEFNodeStyle.normal.background = GetTex(ParseColor(theme.NEF?.NodeBackground, d.NEF.NodeBackground));
        HUDElementStyle.normal.textColor = ParseColor(theme.HUD?.TextColor, d.HUD.TextColor);

        // --- 11. Slider ---
        Color trackOffColor = ParseColor(theme.Slider?.TrackOff ?? theme.SettingOff?.BackgroundColor?.Normal, d.Slider.TrackOff);
        Color trackOnColor = ParseColor(theme.Slider?.TrackOn ?? theme.SettingOn?.BackgroundColor?.Normal, d.Slider.TrackOn);
        Color thumbColor = ParseColor(theme.Slider?.Thumb ?? theme.SettingOn?.BackgroundColor?.Normal, d.Slider.Thumb);
        Color thumbHoverColor = ParseColor(theme.Slider?.ThumbHover ?? theme.Slider?.Thumb ?? theme.SettingOn?.BackgroundColor?.Hover, d.Slider.ThumbHover);

        SliderTrackOffStyle.normal.background = GetTex(trackOffColor);
        SliderTrackOnStyle.normal.background = GetTex(trackOnColor);

        SliderThumbStyle.normal.background = GetCircleTex(thumbColor);
        SliderThumbStyle.hover.background = GetCircleTex(thumbHoverColor);
        SliderThumbStyle.active.background = GetCircleTex(thumbHoverColor);

        // Exposed dynamic properties
        var catModOffBg = ResolveState(theme.CategoryModuleOff?.BackgroundColor, d.CategoryModuleOff.BackgroundColor);
        var catModOnBg = ResolveState(theme.CategoryModuleOn?.BackgroundColor, d.CategoryModuleOn.BackgroundColor);
        var catModOffText = ResolveState(theme.CategoryModuleOff?.Text, d.CategoryModuleOff.Text);
        var tbOffBg = ResolveState(theme.TopBarOff?.BackgroundColor, d.TopBarOff.BackgroundColor);

        AccentColor = catModOnBg.normal;
        AccentHoverColor = catModOnBg.hover;
        BackgroundColor = ParseColor(theme.CategoryWindow?.BackgroundColor, d.CategoryWindow.BackgroundColor);
        LightBackgroundColor = catModOffBg.normal;
        TextWhite = baseText;
        TextDim = catModOffText.normal;
        HoverColor = catModOffBg.hover;
        ActiveColor = tbOffBg.active;
        DimColor = ParseColor(theme.Misc?.DimBackground, d.Misc.DimBackground);
        NefLineColor = ParseColor(theme.NEF?.LineColor, d.NEF.LineColor);
        NefNodeColor = ParseColor(theme.NEF?.NodeBackground, d.NEF.NodeBackground);
    }

    private static (Color normal, Color hover, Color active) ResolveState(ColorState state, ColorState fallback)
    {
        string normHex = !string.IsNullOrEmpty(state?.Normal) ? state.Normal : fallback?.Normal;
        string hovHex = !string.IsNullOrEmpty(state?.Hover) ? state.Hover : (!string.IsNullOrEmpty(state?.Normal) ? state.Normal : fallback?.Hover);
        string actHex = !string.IsNullOrEmpty(state?.Active) ? state.Active : (!string.IsNullOrEmpty(state?.Normal) ? state.Normal : fallback?.Active);

        Color normal = ParseColor(normHex, fallback?.Normal ?? "#FFFFFFFF");
        Color hover = ParseColor(hovHex, fallback?.Hover ?? normHex);
        Color active = ParseColor(actHex, fallback?.Active ?? normHex);

        return (normal, hover, active);
    }

    private static Color ParseColor(string hex, string defaultHex)
    {
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color col))
            return col;

        ColorUtility.TryParseHtmlString(defaultHex, out Color defCol);
        return defCol;
    }

    private static Texture2D GetTex(Color col)
    {
        if (_texCache.TryGetValue(col, out var tex) && tex != null)
            return tex;

        Texture2D newTex = new(1, 1, TextureFormat.RGBA32, false);
        newTex.SetPixel(0, 0, col);
        newTex.Apply();
        _texCache[col] = newTex;
        return newTex;
    }

    private static readonly Dictionary<string, Texture2D> _circleTextureCache = new();

    public static Texture2D GetCircleTex(Color color, int size = 64)
    {
        string key = $"{color.r}_{color.g}_{color.b}_{color.a}_{size}";
        if (_circleTextureCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) / 2f;
        float radius = size / 2f;
        float edgeThickness = 1.25f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) / edgeThickness);
                Color pixelColor = new(color.r, color.g, color.b, color.a * alpha);
                tex.SetPixel(x, y, pixelColor);
            }
        }

        tex.Apply(false);
        _circleTextureCache[key] = tex;
        return tex;
    }

    private static void BuildEmptyStyles()
    {
        TopBarStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        TopBarActiveStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        CategoryModuleOnStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft };
        CloseButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        CategoryHeaderStyle = new GUIStyle { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
        CategoryModuleOffStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft };
        CategoryWindowStyle = new GUIStyle { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
        SettingsWndowBgStyle = new GUIStyle();
        SettingsWndowStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        SettingOn = new GUIStyle { alignment = TextAnchor.MiddleLeft };
        SettingOff = new GUIStyle { alignment = TextAnchor.MiddleLeft };

        // Action Buttons
        ButtonSettingStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        ResetButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        ListAddButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        ListRemoveButtonStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

        SettingsDescriptionStyle = new GUIStyle { wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
        SettingLabelStyle = new GUIStyle { wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
        SettingAuthorStyle = new GUIStyle { fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleLeft, richText = true };
        SettingTextStyle = new GUIStyle { wordWrap = false, alignment = TextAnchor.MiddleLeft, richText = true, clipping = TextClipping.Clip };
        SeparatorStyle = new GUIStyle();
        SeparatorTextStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            richText = true,
            clipping = TextClipping.Overflow
        };
        DimBackgroundStyle = new GUIStyle();
        HUDElementStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = true };
        NEFLineStyle = new GUIStyle();
        NEFNodeStyle = new GUIStyle { alignment = TextAnchor.LowerCenter };
        TextStyle = new GUIStyle { wordWrap = false, alignment = TextAnchor.MiddleLeft, richText = false, clipping = TextClipping.Clip };
        TextHighlightedStyle = new GUIStyle { wordWrap = false, alignment = TextAnchor.MiddleLeft, richText = false, clipping = TextClipping.Clip };
        SliderTrackOffStyle = new GUIStyle();
        SliderTrackOnStyle = new GUIStyle();
        SliderThumbStyle = new GUIStyle();
    }

    private static void SetOffset(RectOffset ro, int left, int right, int top, int bottom)
    {
        if (ro == null) return;
        ro.left = left;
        ro.right = right;
        ro.top = top;
        ro.bottom = bottom;
    }

    public static void Rescale()
    {
        if (!Magnetar_Client.Core.main.Instance.hasWarmedUp || !IsInitialized) return;

        float scale = Config.GUIScale;
        float elementScale = Config.ElementScale;

        if (Mathf.Approximately(scale, lastScale) && Mathf.Approximately(elementScale, lastElementScale))
            return;

        lastScale = scale;
        lastElementScale = elementScale;

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
        SetOffset(SettingsWndowBgStyle.padding, 0, 0, 0, 0);

        CategoryWindowStyle.fontSize = S(ModuleWindowFontSize);
        SetOffset(CategoryWindowStyle.padding, 0, 0, S(ModuleWindowPaddingTop), 0);

        SettingsWndowStyle.fontSize = S(SettingsWindowFontSize);
        SetOffset(SettingsWndowStyle.padding, 0, 0, 0, 0);

        // Controls
        SettingOn.fontSize = S(SettingFontSize);
        SetOffset(SettingOn.padding, S(SettingPaddingLeft), 0, 0, 0);

        SettingOff.fontSize = S(SettingFontSize);
        SetOffset(SettingOff.padding, S(SettingPaddingLeft), 0, 0, 0);

        // New Action Buttons font scaling
        ButtonSettingStyle.fontSize = S(SettingFontSize);
        SetOffset(ButtonSettingStyle.padding, 0, 0, 0, 0);

        ResetButtonStyle.fontSize = S(SettingFontSize);
        SetOffset(ResetButtonStyle.padding, 0, 0, 0, 0);

        ListAddButtonStyle.fontSize = S(SettingFontSize);
        SetOffset(ListAddButtonStyle.padding, 0, 0, 0, 0);

        ListRemoveButtonStyle.fontSize = S(SettingFontSize);
        SetOffset(ListRemoveButtonStyle.padding, 0, 0, 0, 0);

        // Typography
        SettingsDescriptionStyle.fontSize = S(DescriptionFontSize);
        SetOffset(SettingsDescriptionStyle.padding, S(DescriptionPaddingLR), S(DescriptionPaddingLR), S(DescriptionPaddingTB), S(DescriptionPaddingTB));

        SettingLabelStyle.fontSize = S(SettingDescriptionFontSize);
        SetOffset(SettingLabelStyle.padding, S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingTB), S(SettingDescriptionPaddingTB));

        SettingAuthorStyle.fontSize = S(AuthorFontSize);
        SetOffset(SettingAuthorStyle.padding, S(AuthorPaddingLeft), 0, 0, 0);

        SettingTextStyle.fontSize = S(TextFontSize);

        // Separators & HUD
        SeparatorStyle.fixedHeight = Sf(SeparatorFixedHeight);
        SeparatorTextStyle.fontSize = S(SettingFontSize);
        SetOffset(SeparatorTextStyle.padding, 0, 0, 0, 0);
        HUDElementStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(HUDElementFontSize * elementScale));
        SetOffset(HUDElementStyle.padding, 0, 0, 0, 0);

        // NEF & Text
        SetOffset(NEFNodeStyle.padding, S(NEFNodePaddingLR), S(NEFNodePaddingLR), S(NEFNodePaddingTop), S(NEFNodePaddingBottom));
        TextStyle.fontSize = S(TextFontSize);
        TextHighlightedStyle.fontSize = TextStyle.fontSize;

        // Slider
        SetOffset(SliderTrackOffStyle.padding, 0, 0, 0, 0);
        SetOffset(SliderTrackOnStyle.padding, 0, 0, 0, 0);
        SetOffset(SliderThumbStyle.padding, 0, 0, 0, 0);
    }
}