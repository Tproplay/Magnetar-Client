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
    public class ThemeData
    {
        public string Name { get; set; } = "Custom Theme";

        public GeneralColors Colors { get; set; } = new GeneralColors();
        public TypographyColors Typography { get; set; } = new TypographyColors();
        public TopBarTheme TopBar { get; set; } = new TopBarTheme();
        public CategoryTheme CategoryWindow { get; set; } = new CategoryTheme();
        public SettingsTheme SettingsWindow { get; set; } = new SettingsTheme();
        public ControlsTheme Controls { get; set; } = new ControlsTheme();
        public NefTheme NEF { get; set; } = new NefTheme();
        public HudTheme HUD { get; set; } = new HudTheme();
    }

    [Serializable]
    public class GeneralColors
    {
        public string Accent { get; set; }
        public string AccentHover { get; set; }
        public string Background { get; set; }
        public string BackgroundLight { get; set; }
        public string Hover { get; set; }
        public string Active { get; set; }
        public string Dim { get; set; }
        public string Separator { get; set; }
    }

    [Serializable]
    public class TypographyColors
    {
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Description { get; set; }
        public string Label { get; set; }
        public string Author { get; set; }
        public string HighlightText { get; set; }
    }

    [Serializable]
    public class TopBarTheme
    {
        public string Background { get; set; }
        public string ActiveBackground { get; set; }
        public string TextColor { get; set; }
        public string ActiveTextColor { get; set; }
        public string HoverTextColor { get; set; }
    }

    [Serializable]
    public class CategoryTheme
    {
        public string HeaderBackground { get; set; }
        public string HeaderTextColor { get; set; }
        public string ModuleOnBackground { get; set; }
        public string ModuleOnTextColor { get; set; }
        public string ModuleOffBackground { get; set; }
        public string ModuleOffTextColor { get; set; }
    }

    [Serializable]
    public class SettingsTheme
    {
        public string HeaderBackground { get; set; }
        public string HeaderTextColor { get; set; }
        public string SettingOnBackground { get; set; }
        public string SettingOnTextColor { get; set; }
        public string SettingOffBackground { get; set; }
        public string SettingOffTextColor { get; set; }
    }

    [Serializable]
    public class ControlsTheme
    {
        public string CloseButtonBackground { get; set; }
        public string CloseButtonTextColor { get; set; }
    }

    [Serializable]
    public class NefTheme
    {
        public string LineColor { get; set; }
        public string NodeBackground { get; set; }
    }

    [Serializable]
    public class HudTheme
    {
        public string TextColor { get; set; }
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
        /// Style for an inactive module button on the category window (off state).
        /// <para>• Text color: normal (dimmed), hover (highlighted white)</para>
        /// <para>• Bg color: normal (light background), hover (hover color)</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle CategoryModuleOffStyle;

        /// <summary>
        /// Style for an active module button on the category window (on state).
        /// <para>• Text color: normal (contrasting dark)</para>
        /// <para>• Bg color: normal (accent color), hover (active hover color)</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle CategoryModuleOnStyle;

        // --- Mobile Buttons ---

        /// <summary>
        /// Style for the mobile window close buttons and compact icon controls.
        /// <para>• Text color: normal (dark/contrasting)</para>
        /// <para>• Bg color: normal (accent color), hover (active hover color)</para>
        /// <para>• Alignment: MiddleCenter</para>
        /// </summary>
        public static GUIStyle CloseButtonStyle;

        // --- Module Settings and HUD/GUI Managers ---

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
        /// <para>• Text color: normal (dimmed text), hover (white)</para>
        /// <para>• Bg color: normal (light background), hover (hover color)</para>
        /// <para>• Alignment: MiddleLeft</para>
        /// </summary>
        public static GUIStyle SettingOff;

        /// <summary>
        /// Style for setting toggle and action buttons in their active/true state.
        /// <para>• Text color: normal (dark contrasting)</para>
        /// <para>• Bg color: normal (accent background), hover (active hover color)</para>
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
            Colors = new GeneralColors
            {
                Accent = "#FF3D3DFF",
                AccentHover = "#F03333FF",
                Background = "#1A1A1ADC",
                BackgroundLight = "#1C1C1CD6",
                Hover = "#1C1C1CFF",
                Active = "#333333FF",
                Dim = "#1A1A1A66",
                Separator = "#FFFFFFFF"
            },
            Typography = new TypographyColors
            {
                Primary = "#E6E6E6FF",
                Secondary = "#AEAEAEFF",
                Description = "#BFBFBFFF",
                Label = "#E6E6E6FF",
                Author = "#808080FF",
                HighlightText = "#FFFFFFFF"
            },
            TopBar = new TopBarTheme
            {
                Background = "#1A1A1ADC",
                ActiveBackground = "#FF3D3DFF",
                TextColor = "#AEAEAEFF",
                ActiveTextColor = "#FFFFFFFF",
                HoverTextColor = "#FFFFFFFF"
            },
            CategoryWindow = new CategoryTheme
            {
                HeaderBackground = "#1A1A1ADC",
                HeaderTextColor = "#FFFFFFFF",
                ModuleOnBackground = "#FF3D3DFF",
                ModuleOnTextColor = "#000000FF",
                ModuleOffBackground = "#1C1C1CD6",
                ModuleOffTextColor = "#AEAEAEFF"
            },
            SettingsWindow = new SettingsTheme
            {
                HeaderBackground = "#FF3D3DFF",
                HeaderTextColor = "#000000FF",
                SettingOnBackground = "#FF3D3DFF",
                SettingOnTextColor = "#000000FF",
                SettingOffBackground = "#1C1C1CD6",
                SettingOffTextColor = "#AEAEAEFF"
            },
            Controls = new ControlsTheme
            {
                CloseButtonBackground = "#FF3D3DFF",
                CloseButtonTextColor = "#000000FF"
            },
            NEF = new NefTheme
            {
                LineColor = "#FFFFFFFF",
                NodeBackground = "#FF3D3DFF"
            },
            HUD = new HudTheme
            {
                TextColor = "#FFFFFFFF"
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
                            Colors = new GeneralColors
                            {
                                Accent = "#8B0FFFFF",
                                AccentHover = "#A855F7FF",
                                Background = "#11141BDC",
                                BackgroundLight = "#1E1622C8",
                                Hover = "#21262DFF",
                                Active = "#30363DFF",
                                Dim = "#1A1A1A66",
                                Separator = "#FFFFFFFF"
                            },
                            Typography = new TypographyColors
                            {
                                Primary = "#E6EDF3FF",
                                Secondary = "#8B949EFF",
                                Description = "#BFBFBFFF",
                                Label = "#E6E6E6FF",
                                Author = "#808080FF"
                            },
                            TopBar = new TopBarTheme
                            {
                                Background = "#11141BDC",
                                ActiveBackground = "#8B0FFFFF",
                                TextColor = "#8B949EFF",
                                ActiveTextColor = "#FFFFFFFF",
                                HoverTextColor = "#FFFFFFFF"
                            },
                            CategoryWindow = new CategoryTheme
                            {
                                HeaderBackground = "#11141BDC",
                                HeaderTextColor = "#FFFFFFFF",
                                ModuleOnBackground = "#8B0FFFFF",
                                ModuleOnTextColor = "#000000FF",
                                ModuleOffBackground = "#1E1622C8",
                                ModuleOffTextColor = "#8B949EFF"
                            },
                            SettingsWindow = new SettingsTheme
                            {
                                HeaderBackground = "#8B0FFFFF",
                                HeaderTextColor = "#000000FF",
                                SettingOnBackground = "#8B0FFFFF",
                                SettingOnTextColor = "#000000FF",
                                SettingOffBackground = "#1E1622C8",
                                SettingOffTextColor = "#8B949EFF"
                            },
                            Controls = new ControlsTheme
                            {
                                CloseButtonBackground = "#8B0FFFFF",
                                CloseButtonTextColor = "#000000FF"
                            },
                            NEF = new NefTheme
                            {
                                LineColor = "#FFFFFFFF",
                                NodeBackground = "#8B0FFFFF"
                            },
                            HUD = new HudTheme
                            {
                                TextColor = "#FFFFFFFF"
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

            // 1. Resolve Base Colors
            var d = InternalDefaultTheme;
            AccentColor = ParseColor(theme.Colors?.Accent, d.Colors.Accent);
            AccentHoverColor = ParseColor(theme.Colors?.AccentHover, d.Colors.AccentHover);
            BackgroundColor = ParseColor(theme.Colors?.Background, d.Colors.Background);
            LightBackgroundColor = ParseColor(theme.Colors?.BackgroundLight, d.Colors.BackgroundLight);
            HoverColor = ParseColor(theme.Colors?.Hover, d.Colors.Hover);
            ActiveColor = ParseColor(theme.Colors?.Active, d.Colors.Active);
            DimColor = ParseColor(theme.Colors?.Dim, d.Colors.Dim);
            Color separatorColor = ParseColor(theme.Colors?.Separator, d.Colors.Separator);

            // 2. Resolve Typography
            TextWhite = ParseColor(theme.Typography?.Primary, d.Typography.Primary);
            TextDim = ParseColor(theme.Typography?.Secondary, d.Typography.Secondary);
            Color descColor = ParseColor(theme.Typography?.Description, d.Typography.Description);
            Color labelColor = ParseColor(theme.Typography?.Label, d.Typography.Label);
            Color authorColor = ParseColor(theme.Typography?.Author, d.Typography.Author);
            Color highlightTextColor = ParseColor(theme.Typography?.HighlightText, TextWhite);

            // 3. Resolve TopBar
            Color topBarBg = ParseColor(theme.TopBar?.Background, BackgroundColor);
            Color topBarActiveBg = ParseColor(theme.TopBar?.ActiveBackground, AccentColor);
            Color topBarText = ParseColor(theme.TopBar?.TextColor, TextDim);
            Color topBarActiveText = ParseColor(theme.TopBar?.ActiveTextColor, Color.white);
            Color topBarHoverText = ParseColor(theme.TopBar?.HoverTextColor, Color.white);

            // 4. Resolve Category Window
            Color catHeaderBg = ParseColor(theme.CategoryWindow?.HeaderBackground, BackgroundColor);
            Color catHeaderText = ParseColor(theme.CategoryWindow?.HeaderTextColor, Color.white);
            Color catModOnBg = ParseColor(theme.CategoryWindow?.ModuleOnBackground, AccentColor);
            Color catModOnText = ParseColor(theme.CategoryWindow?.ModuleOnTextColor, Color.black);
            Color catModOffBg = ParseColor(theme.CategoryWindow?.ModuleOffBackground, LightBackgroundColor);
            Color catModOffText = ParseColor(theme.CategoryWindow?.ModuleOffTextColor, TextDim);

            // 5. Resolve Settings Window
            Color setHeaderBg = ParseColor(theme.SettingsWindow?.HeaderBackground, AccentColor);
            Color setHeaderText = ParseColor(theme.SettingsWindow?.HeaderTextColor, Color.black);
            Color setOnBg = ParseColor(theme.SettingsWindow?.SettingOnBackground, AccentColor);
            Color setOnText = ParseColor(theme.SettingsWindow?.SettingOnTextColor, Color.black);
            Color setOffBg = ParseColor(theme.SettingsWindow?.SettingOffBackground, LightBackgroundColor);
            Color setOffText = ParseColor(theme.SettingsWindow?.SettingOffTextColor, TextDim);

            // 6. Resolve Controls & Extras
            Color closeBtnBg = ParseColor(theme.Controls?.CloseButtonBackground, AccentColor);
            Color closeBtnText = ParseColor(theme.Controls?.CloseButtonTextColor, Color.black);

            NefLineColor = ParseColor(theme.NEF?.LineColor, d.NEF.LineColor);
            NefNodeColor = ParseColor(theme.NEF?.NodeBackground, AccentColor);
            Color hudTextColor = ParseColor(theme.HUD?.TextColor, Color.white);

            // --- Apply To GUIStyles ---

            // Top Bar
            TopBarStyle.normal.background = GetTex(topBarBg);
            TopBarStyle.hover.background = GetTex(HoverColor);
            TopBarStyle.active.background = GetTex(ActiveColor);
            TopBarStyle.normal.textColor = topBarText;
            TopBarStyle.hover.textColor = topBarHoverText;
            TopBarStyle.active.textColor = topBarActiveText;

            TopBarActiveStyle.normal.background = GetTex(topBarActiveBg);
            TopBarActiveStyle.hover.background = GetTex(topBarActiveBg);
            TopBarActiveStyle.active.background = GetTex(topBarActiveBg);
            TopBarActiveStyle.normal.textColor = topBarActiveText;
            TopBarActiveStyle.hover.textColor = topBarActiveText;
            TopBarActiveStyle.active.textColor = topBarActiveText;

            // Category Window & Modules
            CategoryWindowStyle.normal.background = GetTex(catHeaderBg);
            CategoryWindowStyle.normal.textColor = catHeaderText;

            CategoryModuleOnStyle.normal.background = GetTex(catModOnBg);
            CategoryModuleOnStyle.normal.textColor = catModOnText;
            CategoryModuleOnStyle.hover.background = GetTex(AccentHoverColor);

            CategoryModuleOffStyle.normal.background = GetTex(catModOffBg);
            CategoryModuleOffStyle.normal.textColor = catModOffText;
            CategoryModuleOffStyle.hover.background = GetTex(HoverColor);
            CategoryModuleOffStyle.hover.textColor = Color.white;

            // Close Button
            CloseButtonStyle.normal.background = GetTex(closeBtnBg);
            CloseButtonStyle.normal.textColor = closeBtnText;
            CloseButtonStyle.hover.background = GetTex(AccentHoverColor);

            // Settings Window & Controls
            SettingsWndowStyle.normal.background = GetTex(setHeaderBg);
            SettingsWndowStyle.normal.textColor = setHeaderText;

            SettingOn.normal.background = GetTex(setOnBg);
            SettingOn.normal.textColor = setOnText;
            SettingOn.hover.background = GetTex(AccentHoverColor);

            SettingOff.normal.background = GetTex(setOffBg);
            SettingOff.normal.textColor = setOffText;
            SettingOff.hover.background = GetTex(HoverColor);
            SettingOff.hover.textColor = Color.white;

            // Typography & Descriptions
            SettingsDescriptionStyle.normal.textColor = descColor;
            SettingLabelStyle.normal.textColor = labelColor;
            SettingAuthorStyle.normal.textColor = authorColor;
            SettingTextStyle.normal.textColor = TextWhite;

            // Separators & Backgrounds
            SeparatorStyle.normal.background = GetTex(separatorColor);
            DimBackgroundStyle.normal.background = GetTex(DimColor);

            // HUD & NEF
            HUDElementStyle.normal.textColor = hudTextColor;
            NEFLineStyle.normal.background = GetTex(NefLineColor);
            NEFNodeStyle.normal.background = GetTex(NefNodeColor);

            // Text Fields
            TextStyle.normal.textColor = TextWhite;
            TextHighlightedStyle.normal.textColor = highlightTextColor;
            TextHighlightedStyle.normal.background = GetTex(AccentColor);
        }

        private static Color ParseColor(string hex, string defaultHex)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color col))
                return col;

            ColorUtility.TryParseHtmlString(defaultHex, out Color defCol);
            return defCol;
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color col))
                return col;
            return fallback;
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
            CategoryModuleOffStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft };
            CategoryWindowStyle = new GUIStyle { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
            SettingsWndowStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            SettingOn = new GUIStyle { alignment = TextAnchor.MiddleLeft };
            SettingOff = new GUIStyle { alignment = TextAnchor.MiddleLeft };
            SettingsDescriptionStyle = new GUIStyle { wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
            SettingLabelStyle = new GUIStyle { wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
            SettingAuthorStyle = new GUIStyle { fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleLeft, richText = true };
            SettingTextStyle = new GUIStyle { wordWrap = false, alignment = TextAnchor.MiddleLeft, richText = false, clipping = TextClipping.Clip };
            SeparatorStyle = new GUIStyle();
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
            HUDElementStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(HUDElementFontSize * elementScale));
            SetOffset(HUDElementStyle.padding, 0, 0, 0, 0);

            // NEF & Text
            SetOffset(NEFNodeStyle.padding, S(NEFNodePaddingLR), S(NEFNodePaddingLR), S(NEFNodePaddingTop), S(NEFNodePaddingBottom));
            TextStyle.fontSize = S(TextFontSize);
            TextHighlightedStyle.fontSize = TextStyle.fontSize;
        }
    }
}