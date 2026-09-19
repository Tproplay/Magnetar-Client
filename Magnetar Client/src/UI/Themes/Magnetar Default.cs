using Magnetar_Client.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.UI.Themes
{
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

        [JsonProperty("Typography")]
        public TypographyTheme Typography { get; set; } = new TypographyTheme();

        [JsonProperty("NEF")]
        public NefTheme NEF { get; set; } = new NefTheme();

        [JsonProperty("HUD")]
        public HudTheme HUD { get; set; } = new HudTheme();

        [JsonProperty("Misc")]
        public MiscTheme Misc { get; set; } = new MiscTheme();
    }
    #endregion

    public static class Magnetar_Default
    {
        public static bool IsInitialized { get; private set; } = false;

        #region Styles

        // --- Top Bar ---

        /// <summary>
        /// TopBar buttons style configurations.
        /// <para>• Text color: normal, hover, active</para>
        /// <para>• Bg color: normal, hover, active</para>
        /// </summary>
        public static GUIStyle TopBarStyle;

        /// <summary>
        /// TopBar active button style configurations (when selected/on).
        /// <para>• Text color: normal, hover, active (highlighted)</para>
        /// <para>• Bg color: normal, hover, active (accent background)</para>
        /// </summary>
        public static GUIStyle TopBarActiveStyle;

        // --- Category Window ---

        /// <summary>
        /// Category Window container and header title style configurations.
        /// <para>• Text color: normal (title text)</para>
        /// <para>• Bg color: normal (window background)</para>
        /// <para>• Alignment: UpperCenter, Bold</para>
        /// </summary>
        public static GUIStyle CategoryWindowStyle;

        /// <summary>
        /// Style for bg of name of the category window.
        /// <para>• Text color: normal (dimmed), hover (highlighted white), active</para>
        /// <para>• Bg color: normal (light background), hover (hover color), active</para>
        /// <para>• Alignment: UpperCenter, Bold</para>
        /// </summary>
        public static GUIStyle CategoryHeaderStyle;

        /// <summary>
        /// Style for an inactive module button on the category window (off state).
        /// <para>• Text color: normal (dimmed), hover (highlighted white), active</para>
        /// <para>• Bg color: normal (light background), hover (hover color), active</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle CategoryModuleOffStyle;

        /// <summary>
        /// Style for an active module button on the category window (on state).
        /// <para>• Text color: normal (contrasting dark), hover, active</para>
        /// <para>• Bg color: normal (accent color), hover (active hover color), active</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle CategoryModuleOnStyle;

        // --- Mobile Buttons ---

        /// <summary>
        /// Style for the mobile window close buttons and compact icon controls.
        /// <para>• Text color: normal (dark/contrasting), hover, active</para>
        /// <para>• Bg color: normal (accent color), hover (active hover color), active</para>
        /// <para>• Alignment: MiddleCenter</para>
        /// </summary>
        public static GUIStyle CloseButtonStyle;

        // --- Module Settings and HUD/GUI Managers ---

        /// <summary>
        /// Style for the base Module Setting background.
        /// <para>• Bg color: normal</para>
        /// </summary>
        public static GUIStyle SettingsWndowBgStyle;

        /// <summary>
        /// Style for the base Module Setting header banner and popup titles.
        /// <para>• Text color: normal (dark contrasting)</para>
        /// <para>• Bg color: normal (accent header background)</para>
        /// <para>• Alignment: MiddleCenter, Bold</para>
        /// </summary>
        public static GUIStyle SettingsWndowStyle;

        /// <summary>
        /// Style for the Module Setting's long-form description text at the top of setting windows.
        /// <para>• Text color: normal (soft description color)</para>
        /// <para>• Formatting: WordWrap enabled, RichText enabled</para>
        /// <para>• Alignment: UpperLeft</para>
        /// </summary>
        public static GUIStyle SettingsDescriptionStyle;

        /// <summary>
        /// Style for general read-only setting text and configuration values.
        /// <para>• Text color: normal (primary text white)</para>
        /// <para>• Formatting: Clipping enabled, RichText disabled</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle SettingTextStyle;

        /// <summary>
        /// Style for the module author credit string.
        /// <para>• Text color: normal (author/credit grey)</para>
        /// <para>• Formatting: Italic, RichText enabled</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle SettingAuthorStyle;

        /// <summary>
        /// Style for setting toggle and action buttons in their inactive/false state.
        /// <para>• Text color: normal (dimmed text), hover (white), active</para>
        /// <para>• Bg color: normal (light background), hover (hover color), active</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle SettingOff;

        /// <summary>
        /// Style for setting toggle and action buttons in their active/true state.
        /// <para>• Text color: normal (dark contrasting), hover, active</para>
        /// <para>• Bg color: normal (accent background), hover (active hover color), active</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle SettingOn;

        /// <summary>
        /// Style for individual setting option labels and names.
        /// <para>• Text color: normal (label foreground)</para>
        /// <para>• Formatting: WordWrap enabled, RichText enabled</para>
        /// <para>• Alignment: UpperLeft</para>
        /// </summary>
        public static GUIStyle SettingLabelStyle;

        /// <summary>
        /// Style for divider rules and layout separators.
        /// <para>• Bg color: normal (solid separator color)</para>
        /// <para>• Height: Fixed 1px scaled</para>
        /// </summary>
        public static GUIStyle SeparatorStyle;

        /// <summary>
        /// Style for the text label rendered inside partitioned separators.
        /// <para>• Text color: normal (separator label text color)</para>
        /// <para>• Alignment: MiddleCenter</para>
        /// </summary>
        public static GUIStyle SeparatorTextStyle;

        /// <summary>
        /// Style for text input fields and manual value editors.
        /// <para>• Text color: normal (primary white)</para>
        /// <para>• Formatting: Clipping enabled, WordWrap disabled</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle TextStyle;

        /// <summary>
        /// Style for text input fields when selecting text or focusing via cursor.
        /// <para>• Text color: normal (highlight text color)</para>
        /// <para>• Bg color: normal (accent selection highlight)</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle TextHighlightedStyle;

        /// <summary>
        /// Style for the fullscreen overlay background when Dim Background is enabled.
        /// <para>• Bg color: normal (translucent dim color)</para>
        /// </summary>
        public static GUIStyle DimBackgroundStyle;

        /// <summary>
        /// Style for on-screen HUD text overlays, coordinates, and statistics.
        /// <para>• Text color: normal (HUD text color)</para>
        /// <para>• Formatting: RichText enabled, WordWrap disabled</para>
        /// <para>• Alignment: MiddleCenter</para>
        /// </summary>
        public static GUIStyle HUDElementStyle;

        /// <summary>
        /// Style for connector lines inside the NEF node diagram view.
        /// <para>• Bg color: normal (NEF line texture)</para>
        /// </summary>
        public static GUIStyle NEFLineStyle;

        /// <summary>
        /// Style for individual node frames and entity backgrounds in the NEF tree.
        /// <para>• Bg color: normal (NEF node texture)</para>
        /// <para>• Alignment: LowerCenter</para>
        /// </summary>
        public static GUIStyle NEFNodeStyle;
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
        public static readonly ThemeData InternalDefaultTheme = new ThemeData
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
                Separator = "#FFFFFFFF"
            }
        };
        #endregion

        public static readonly Dictionary<string, ThemeData> LoadedThemes = new Dictionary<string, ThemeData>(StringComparer.OrdinalIgnoreCase);
        public static string CurrentThemeName { get; private set; } = "Magnetar Default";

        private static readonly Dictionary<Color, Texture2D> _texCache = new Dictionary<Color, Texture2D>();
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
                        new ThemeData
                        {
                            Name = "Meteor Purple",
                            TopBarOff = new ElementStyleTheme(
                                new ColorState("#8B949EFF", "#FFFFFFFF", "#FFFFFFFF"),
                                new ColorState("#11141BDC", "#21262DFF", "#30363DFF")
                            ),
                            TopBarActive = new ElementStyleTheme(
                                new ColorState("#FFFFFFFF", "#FFFFFFFF", "#FFFFFFFF"),
                                new ColorState("#8B0FFFFF", "#8B0FFFFF", "#8B0FFFFF")
                            ),
                            CategoryHeader = new ElementStyleTheme(
                                new ColorState("#E6EDF3FF", "#FFFFFFFF", "#FFFFFFFF"),
                                new ColorState("#161B22E6", "#21262DFF", "#30363DFF")
                            ),
                            CategoryWindow = new WindowStyleTheme("#FFFFFFFF", "#11141BDC"),
                            CategoryModuleOff = new ElementStyleTheme(
                                new ColorState("#8B949EFF", "#FFFFFFFF", "#FFFFFFFF"),
                                new ColorState("#1E1622C8", "#21262DFF", "#21262DFF")
                            ),
                            CategoryModuleOn = new ElementStyleTheme(
                                new ColorState("#000000FF", "#000000FF", "#000000FF"),
                                new ColorState("#8B0FFFFF", "#A855F7FF", "#A855F7FF")
                            ),
                            CloseButton = new ElementStyleTheme(
                                new ColorState("#000000FF", "#000000FF", "#000000FF"),
                                new ColorState("#8B0FFFFF", "#A855F7FF", "#A855F7FF")
                            ),
                            SettingsWindow = new WindowStyleTheme("#000000FF", "#8B0FFFFF"),
                            SettingOff = new ElementStyleTheme(
                                new ColorState("#8B949EFF", "#FFFFFFFF", "#FFFFFFFF"),
                                new ColorState("#1E1622C8", "#21262DFF", "#21262DFF")
                            ),
                            SettingOn = new ElementStyleTheme(
                                new ColorState("#000000FF", "#000000FF", "#000000FF"),
                                new ColorState("#8B0FFFFF", "#A855F7FF", "#A855F7FF")
                            ),
                            Typography = new TypographyTheme
                            {
                                Description = "#BFBFBFFF",
                                Label = "#E6E6E6FF",
                                Author = "#808080FF",
                                Text = "#E6EDF3FF",
                                HighlightText = "#FFFFFFFF",
                                HighlightBackground = "#8B0FFFFF"
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
                                DimBackground = "#1A1A1A66",
                                Separator = "#FFFFFFFF"
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

            // --- 1. Top Bar Off ---
            var tbOffText = ResolveState(theme.TopBarOff?.Text, d.TopBarOff.Text);
            var tbOffBg = ResolveState(theme.TopBarOff?.BackgroundColor, d.TopBarOff.BackgroundColor);
            TopBarStyle.normal.textColor = tbOffText.normal;
            TopBarStyle.hover.textColor = tbOffText.hover;
            TopBarStyle.active.textColor = tbOffText.active;
            TopBarStyle.normal.background = GetTex(tbOffBg.normal);
            TopBarStyle.hover.background = GetTex(tbOffBg.hover);
            TopBarStyle.active.background = GetTex(tbOffBg.active);

            // --- 2. Top Bar Active ---
            var tbActText = ResolveState(theme.TopBarActive?.Text, d.TopBarActive.Text);
            var tbActBg = ResolveState(theme.TopBarActive?.BackgroundColor, d.TopBarActive.BackgroundColor);
            TopBarActiveStyle.normal.textColor = tbActText.normal;
            TopBarActiveStyle.hover.textColor = tbActText.hover;
            TopBarActiveStyle.active.textColor = tbActText.active;
            TopBarActiveStyle.normal.background = GetTex(tbActBg.normal);
            TopBarActiveStyle.hover.background = GetTex(tbActBg.hover);
            TopBarActiveStyle.active.background = GetTex(tbActBg.active);

            // --- 3. Category Header (Unique Color Entry) ---
            var catHeadText = ResolveState(theme.CategoryHeader?.Text, d.CategoryHeader.Text);
            var catHeadBg = ResolveState(theme.CategoryHeader?.BackgroundColor, d.CategoryHeader.BackgroundColor);
            CategoryHeaderStyle.normal.textColor = catHeadText.normal;
            CategoryHeaderStyle.hover.textColor = catHeadText.hover;
            CategoryHeaderStyle.active.textColor = catHeadText.active;
            CategoryHeaderStyle.normal.background = GetTex(catHeadBg.normal);
            CategoryHeaderStyle.hover.background = GetTex(catHeadBg.hover);
            CategoryHeaderStyle.active.background = GetTex(catHeadBg.active);

            // --- 4. Category Window ---
            CategoryWindowStyle.normal.textColor = ParseColor(theme.CategoryWindow?.Text, d.CategoryWindow.Text);
            CategoryWindowStyle.normal.background = GetTex(ParseColor(theme.CategoryWindow?.BackgroundColor, d.CategoryWindow.BackgroundColor));

            // --- 5. Category Module Off ---
            var catModOffText = ResolveState(theme.CategoryModuleOff?.Text, d.CategoryModuleOff.Text);
            var catModOffBg = ResolveState(theme.CategoryModuleOff?.BackgroundColor, d.CategoryModuleOff.BackgroundColor);
            CategoryModuleOffStyle.normal.textColor = catModOffText.normal;
            CategoryModuleOffStyle.hover.textColor = catModOffText.hover;
            CategoryModuleOffStyle.active.textColor = catModOffText.active;
            CategoryModuleOffStyle.normal.background = GetTex(catModOffBg.normal);
            CategoryModuleOffStyle.hover.background = GetTex(catModOffBg.hover);
            CategoryModuleOffStyle.active.background = GetTex(catModOffBg.active);

            // --- 6. Category Module On ---
            var catModOnText = ResolveState(theme.CategoryModuleOn?.Text, d.CategoryModuleOn.Text);
            var catModOnBg = ResolveState(theme.CategoryModuleOn?.BackgroundColor, d.CategoryModuleOn.BackgroundColor);
            CategoryModuleOnStyle.normal.textColor = catModOnText.normal;
            CategoryModuleOnStyle.hover.textColor = catModOnText.hover;
            CategoryModuleOnStyle.active.textColor = catModOnText.active;
            CategoryModuleOnStyle.normal.background = GetTex(catModOnBg.normal);
            CategoryModuleOnStyle.hover.background = GetTex(catModOnBg.hover);
            CategoryModuleOnStyle.active.background = GetTex(catModOnBg.active);

            // --- 7. Close Button ---
            var closeText = ResolveState(theme.CloseButton?.Text, d.CloseButton.Text);
            var closeBg = ResolveState(theme.CloseButton?.BackgroundColor, d.CloseButton.BackgroundColor);
            CloseButtonStyle.normal.textColor = closeText.normal;
            CloseButtonStyle.hover.textColor = closeText.hover;
            CloseButtonStyle.active.textColor = closeText.active;
            CloseButtonStyle.normal.background = GetTex(closeBg.normal);
            CloseButtonStyle.hover.background = GetTex(closeBg.hover);
            CloseButtonStyle.active.background = GetTex(closeBg.active);

            // --- 8. Settings Window ---

            string rawWindowBg = theme.SettingsWindow?.WindowBackground
                                 ?? theme.CategoryWindow?.BackgroundColor
                                 ?? d.SettingsWindow.WindowBackground;
            SettingsWndowBgStyle.normal.background = GetTex(ParseColor(rawWindowBg, d.SettingsWindow.WindowBackground));

            SettingsWndowStyle.normal.textColor = ParseColor(theme.SettingsWindow?.Text, d.SettingsWindow.Text);
            SettingsWndowStyle.normal.background = GetTex(ParseColor(theme.SettingsWindow?.BackgroundColor, d.SettingsWindow.BackgroundColor));

            // --- 9. Setting Off ---
            var setOffText = ResolveState(theme.SettingOff?.Text, d.SettingOff.Text);
            var setOffBg = ResolveState(theme.SettingOff?.BackgroundColor, d.SettingOff.BackgroundColor);
            SettingOff.normal.textColor = setOffText.normal;
            SettingOff.hover.textColor = setOffText.hover;
            SettingOff.active.textColor = setOffText.active;    
            SettingOff.normal.background = GetTex(setOffBg.normal);
            SettingOff.hover.background = GetTex(setOffBg.hover);
            SettingOff.active.background = GetTex(setOffBg.active);

            // --- 10. Setting On ---
            var setOnText = ResolveState(theme.SettingOn?.Text, d.SettingOn.Text);
            var setOnBg = ResolveState(theme.SettingOn?.BackgroundColor, d.SettingOn.BackgroundColor);
            SettingOn.normal.textColor = setOnText.normal;
            SettingOn.hover.textColor = setOnText.hover;
            SettingOn.active.textColor = setOnText.active;
            SettingOn.normal.background = GetTex(setOnBg.normal);
            SettingOn.hover.background = GetTex(setOnBg.hover);
            SettingOn.active.background = GetTex(setOnBg.active);

            // --- 11. Typography ---
            SettingsDescriptionStyle.normal.textColor = ParseColor(theme.Typography?.Description, d.Typography.Description);
            SettingLabelStyle.normal.textColor = ParseColor(theme.Typography?.Label, d.Typography.Label);
            SettingAuthorStyle.normal.textColor = ParseColor(theme.Typography?.Author, d.Typography.Author);

            Color baseText = ParseColor(theme.Typography?.Text, d.Typography.Text);
            SettingTextStyle.normal.textColor = baseText;
            TextStyle.normal.textColor = baseText;

            TextHighlightedStyle.normal.textColor = ParseColor(theme.Typography?.HighlightText, d.Typography.HighlightText);
            TextHighlightedStyle.normal.background = GetTex(ParseColor(theme.Typography?.HighlightBackground, d.Typography.HighlightBackground));

            // --- 12. Separator & Dim ---
            SeparatorStyle.normal.background = GetTex(ParseColor(theme.Misc?.Separator, d.Misc.Separator));

            Color sepTextColor = ParseColor(theme.Misc?.SeparatorText ?? theme.Typography?.Secondary, d.Typography.Secondary);
            SeparatorTextStyle.normal.textColor = sepTextColor;

            DimBackgroundStyle.normal.background = GetTex(ParseColor(theme.Misc?.DimBackground, d.Misc.DimBackground));

            // --- 13. NEF & HUD ---
            NEFLineStyle.normal.background = GetTex(ParseColor(theme.NEF?.LineColor, d.NEF.LineColor));
            NEFNodeStyle.normal.background = GetTex(ParseColor(theme.NEF?.NodeBackground, d.NEF.NodeBackground));
            HUDElementStyle.normal.textColor = ParseColor(theme.HUD?.TextColor, d.HUD.TextColor);

            // Update exposed dynamic properties
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

            Texture2D newTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            newTex.SetPixel(0, 0, col);
            newTex.Apply();
            _texCache[col] = newTex;
            return newTex;
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
            SettingsDescriptionStyle = new GUIStyle { wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
            SettingLabelStyle = new GUIStyle { wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
            SettingAuthorStyle = new GUIStyle { fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleLeft, richText = true };
            SettingTextStyle = new GUIStyle { wordWrap = false, alignment = TextAnchor.MiddleLeft, richText = false, clipping = TextClipping.Clip };
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
        }

        private static void SetOffset(RectOffset ro, int left, int right, int top, int bottom)
        {
            if (ro == null) return;
            ro.left = left;
            ro.right = right;
            ro.top = top;
            ro.bottom = bottom;
        }

        /// <summary>
        /// Re-derives every style's fontSize/padding/fixedHeight from the base
        /// (1x) sizes using the current Config.GUIScale. Cheap to call every
        /// frame - it only mutates plain int/float fields on existing GUIStyle
        /// instances, it never allocates new styles or textures, and it bails
        /// out immediately if the scale hasn't changed since the last call.
        /// </summary>
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
        }
    }
}