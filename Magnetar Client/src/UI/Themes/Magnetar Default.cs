using Magnetar_Client.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.UI.Themes
{
    [Serializable]
    public struct ThemeData
    {
        public string Name;
        public string BackgroundColor;
        public string AccentColor;
        public string LightBackgroundColor;
        public string TextWhite;
        public string TextDim;
        public string HoverColor;
        public string ActiveColor;
        public string ActiveHoverColor;
        public string DimColor;
        public string NEFLineColor;
        public string NEFNodeColor;
    }

    public static class Magnetar_Default
    {
        public static bool IsInitialized { get; private set; } = false;

        #region Styles
        public static GUIStyle TopBar;
        public static GUIStyle TopBarButtonActive;

        public static GUIStyle ModuleWindow;
        public static GUIStyle ModuleOn;
        public static GUIStyle ModuleOnCentralized;
        public static GUIStyle ModuleOff;

        public static GUIStyle SettingsWindow;
        public static GUIStyle SettingsText;
        public static GUIStyle DescriptionStyle;
        public static GUIStyle AuthorStyle;

        public static GUIStyle SettingOn;
        public static GUIStyle SettingOff;
        public static GUIStyle SettingDescriptionStyle;

        public static GUIStyle SeparatorStyle;
        public static GUIStyle TextStyle;
        public static GUIStyle TextHighlightedStyle;

        public static GUIStyle DimStyle;
        public static GUIStyle HUDElementStyle;

        public static GUIStyle NEFLineStyle;
        public static GUIStyle NEFNodeStyle;
        #endregion

        #region Textures
        private static Texture2D BgTex;
        private static Texture2D BgLightTex;
        private static Texture2D AccentTex;
        private static Texture2D HoverTex;
        private static Texture2D ActiveTex;
        private static Texture2D ActiveHoverTex;
        private static Texture2D DimTex;
        private static Texture2D NefNodeTex;
        private static Texture2D NefLineTex;
        #endregion

        #region Dynamic Theme Colors
        public static Color BackgroundColor { get; private set; }
        public static Color AccentColor { get; private set; }
        public static Color LightBackgroundColor { get; private set; }
        public static Color TextWhite { get; private set; }
        public static Color TextDim { get; private set; }
        public static Color HoverColor { get; private set; }
        public static Color ActiveColor { get; private set; }
        public static Color ActiveHoverColor { get; private set; }
        public static Color DimColor { get; private set; }
        public static Color NefLineColor { get; private set; }
        public static Color NefNodeColor { get; private set; }
        #endregion

        #region Hardcoded Default Theme (Safety Fallback)
        public static readonly ThemeData InternalDefaultTheme = new ThemeData
        {
            Name = "Magnetar Default",
            BackgroundColor = "#1A1A1ADC",
            AccentColor = "#FF3D3DFF",
            LightBackgroundColor = "#1C1C1CD6",
            TextWhite = "#E6E6E6FF",
            TextDim = "#AEAEAEFF",
            HoverColor = "#1C1C1CFF",
            ActiveColor = "#333333FF",
            ActiveHoverColor = "#F03333FF",
            DimColor = "#1A1A1A66",
            NEFLineColor = "#FFFFFFFF",
            NEFNodeColor = "#FF3D3DFF"
        };
        #endregion

        public static readonly Dictionary<string, ThemeData> LoadedThemes = new Dictionary<string, ThemeData>(StringComparer.OrdinalIgnoreCase);
        public static string CurrentThemeName { get; private set; } = "Magnetar Default";

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
            // 1. Load custom JSON themes from disk
            LoadThemesFromJson();

            // 2. Set up all GUIStyle instances first so BindStyles doesn't hit null references
            BuildEmptyStyles();

            // 3. Mark ready before calling ApplyTheme
            IsInitialized = true;

            // 4. Resolve theme from Config (or fall back to default)
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
                            BackgroundColor = "#11141BDC",
                            AccentColor = "#A855F7FF",
                            LightBackgroundColor = "#161B22D6",
                            TextWhite = "#E6EDF3FF",
                            TextDim = "#8B949EFF",
                            HoverColor = "#21262DFF",
                            ActiveColor = "#30363DFF",
                            ActiveHoverColor = "#C084FCFF",
                            DimColor = "#11141B66",
                            NEFLineColor = "#FFFFFFFF",
                            NEFNodeColor = "#A855F7FF"
                        },
                        new ThemeData
                        {
                            Name = "Cyber Green",
                            BackgroundColor = "#0D1117DC",
                            AccentColor = "#2EA043FF",
                            LightBackgroundColor = "#161B22D6",
                            TextWhite = "#F0F6FCFF",
                            TextDim = "#7D8590FF",
                            HoverColor = "#21262DFF",
                            ActiveColor = "#30363DFF",
                            ActiveHoverColor = "#3FB950FF",
                            DimColor = "#0D111766",
                            NEFLineColor = "#FFFFFFFF",
                            NEFNodeColor = "#2EA043FF"
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

            BackgroundColor = ParseColor(theme.BackgroundColor, InternalDefaultTheme.BackgroundColor);
            AccentColor = ParseColor(theme.AccentColor, InternalDefaultTheme.AccentColor);
            LightBackgroundColor = ParseColor(theme.LightBackgroundColor, InternalDefaultTheme.LightBackgroundColor);
            TextWhite = ParseColor(theme.TextWhite, InternalDefaultTheme.TextWhite);
            TextDim = ParseColor(theme.TextDim, InternalDefaultTheme.TextDim);
            HoverColor = ParseColor(theme.HoverColor, InternalDefaultTheme.HoverColor);
            ActiveColor = ParseColor(theme.ActiveColor, InternalDefaultTheme.ActiveColor);
            ActiveHoverColor = ParseColor(theme.ActiveHoverColor, InternalDefaultTheme.ActiveHoverColor);
            DimColor = ParseColor(theme.DimColor, InternalDefaultTheme.DimColor);
            NefLineColor = ParseColor(theme.NEFLineColor, InternalDefaultTheme.NEFLineColor);
            NefNodeColor = ParseColor(theme.NEFNodeColor, AccentColor);

            UpdateTextures();
            BindStyles();
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

        private static void UpdateTextures()
        {
            SetPixel(ref BgTex, BackgroundColor);
            SetPixel(ref AccentTex, AccentColor);
            SetPixel(ref HoverTex, HoverColor);
            SetPixel(ref ActiveTex, ActiveColor);
            SetPixel(ref ActiveHoverTex, ActiveHoverColor);
            SetPixel(ref BgLightTex, LightBackgroundColor);
            SetPixel(ref DimTex, DimColor);
            SetPixel(ref NefNodeTex, NefNodeColor);
            SetPixel(ref NefLineTex, NefLineColor);
        }

        private static void SetPixel(ref Texture2D tex, Color col)
        {
            if (tex == null)
            {
                tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            }
            tex.SetPixel(0, 0, col);
            tex.Apply();
        }

        private static void BuildEmptyStyles()
        {
            TopBar = new GUIStyle();
            TopBarButtonActive = new GUIStyle();
            ModuleOn = new GUIStyle();
            ModuleOnCentralized = new GUIStyle();
            ModuleOff = new GUIStyle();
            ModuleWindow = new GUIStyle();
            SettingsWindow = new GUIStyle();
            SettingOn = new GUIStyle();
            SettingOff = new GUIStyle();
            DescriptionStyle = new GUIStyle();
            SettingDescriptionStyle = new GUIStyle();
            AuthorStyle = new GUIStyle();
            SeparatorStyle = new GUIStyle();
            DimStyle = new GUIStyle();
            HUDElementStyle = new GUIStyle();
            NEFLineStyle = new GUIStyle();
            NEFNodeStyle = new GUIStyle();
            TextStyle = new GUIStyle();
            TextHighlightedStyle = new GUIStyle();
        }

        private static void BindStyles()
        {
            if (TopBar == null) BuildEmptyStyles();

            TopBar.normal.textColor = TextDim;
            TopBar.hover.textColor = Color.white;
            TopBar.active.textColor = Color.white;
            TopBar.normal.background = BgTex;
            TopBar.hover.background = HoverTex;
            TopBar.active.background = ActiveTex;
            TopBar.alignment = TextAnchor.MiddleCenter;

            TopBarButtonActive.normal.textColor = Color.white;
            TopBarButtonActive.hover.textColor = Color.white;
            TopBarButtonActive.active.textColor = Color.white;
            TopBarButtonActive.normal.background = AccentTex;
            TopBarButtonActive.hover.background = AccentTex;
            TopBarButtonActive.active.background = AccentTex;
            TopBarButtonActive.alignment = TextAnchor.MiddleCenter;

            ModuleOn.normal.background = AccentTex;
            ModuleOn.normal.textColor = Color.black;
            ModuleOn.alignment = TextAnchor.MiddleLeft;
            ModuleOn.hover.background = ActiveHoverTex;

            ModuleOnCentralized.normal.background = AccentTex;
            ModuleOnCentralized.normal.textColor = Color.black;
            ModuleOnCentralized.alignment = TextAnchor.MiddleCenter;
            ModuleOnCentralized.hover.background = ActiveHoverTex;

            ModuleOff.normal.background = BgLightTex;
            ModuleOff.normal.textColor = TextDim;
            ModuleOff.hover.background = HoverTex;
            ModuleOff.hover.textColor = Color.white;
            ModuleOff.alignment = TextAnchor.MiddleLeft;

            ModuleWindow.normal.background = BgTex;
            ModuleWindow.normal.textColor = Color.white;
            ModuleWindow.alignment = TextAnchor.UpperCenter;
            ModuleWindow.fontStyle = FontStyle.Bold;

            SettingsWindow.normal.background = AccentTex;
            SettingsWindow.normal.textColor = Color.black;
            SettingsWindow.alignment = TextAnchor.MiddleCenter;
            SettingsWindow.fontStyle = FontStyle.Bold;

            SettingOn.normal.background = AccentTex;
            SettingOn.normal.textColor = Color.black;
            SettingOn.alignment = TextAnchor.MiddleLeft;
            SettingOn.hover.background = ActiveHoverTex;

            SettingOff.normal.background = BgLightTex;
            SettingOff.normal.textColor = TextDim;
            SettingOff.hover.background = HoverTex;
            SettingOff.hover.textColor = Color.white;
            SettingOff.alignment = TextAnchor.MiddleLeft;

            DescriptionStyle.wordWrap = true;
            DescriptionStyle.alignment = TextAnchor.UpperLeft;
            DescriptionStyle.richText = true;
            DescriptionStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            SettingDescriptionStyle.wordWrap = true;
            SettingDescriptionStyle.alignment = TextAnchor.UpperLeft;
            SettingDescriptionStyle.richText = true;
            SettingDescriptionStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            AuthorStyle.fontStyle = FontStyle.Italic;
            AuthorStyle.alignment = TextAnchor.MiddleLeft;
            AuthorStyle.richText = true;
            AuthorStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);

            SeparatorStyle.normal.background = Texture2D.whiteTexture;
            DimStyle.normal.background = DimTex;

            HUDElementStyle.alignment = TextAnchor.MiddleCenter;
            HUDElementStyle.wordWrap = false;
            HUDElementStyle.richText = true;
            HUDElementStyle.normal.textColor = Color.white;

            NEFLineStyle.normal.background = NefLineTex;
            NEFNodeStyle.normal.background = NefNodeTex;
            NEFNodeStyle.alignment = TextAnchor.LowerCenter;

            TextStyle.wordWrap = false;
            TextStyle.alignment = TextAnchor.MiddleLeft;
            TextStyle.richText = false;
            TextStyle.clipping = TextClipping.Clip;
            TextStyle.normal.textColor = Color.white;

            TextHighlightedStyle.wordWrap = TextStyle.wordWrap;
            TextHighlightedStyle.alignment = TextStyle.alignment;
            TextHighlightedStyle.richText = TextStyle.richText;
            TextHighlightedStyle.clipping = TextStyle.clipping;
            TextHighlightedStyle.normal.textColor = TextStyle.normal.textColor;
            TextHighlightedStyle.normal.background = AccentTex;
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

            // TopBar / TopBarButtonActive
            TopBar.fontSize = S(TopBarFontSize);
            SetOffset(TopBar.padding, S(TopBarPaddingLR), S(TopBarPaddingLR), S(TopBarPaddingTB), S(TopBarPaddingTB));

            TopBarButtonActive.fontSize = S(TopBarFontSize);
            SetOffset(TopBarButtonActive.padding, S(TopBarPaddingLR), S(TopBarPaddingLR), S(TopBarPaddingTB), S(TopBarPaddingTB));

            // ModuleOn / ModuleOff
            ModuleOn.fontSize = S(ModuleFontSize);
            SetOffset(ModuleOn.padding, S(ModulePaddingLeft), 0, 0, 0);

            ModuleOnCentralized.fontSize = S(ModuleFontSize);

            ModuleOff.fontSize = S(ModuleFontSize);
            SetOffset(ModuleOff.padding, S(ModulePaddingLeft), 0, 0, 0);

            // ModuleWindow / SettingsWindow
            ModuleWindow.fontSize = S(ModuleWindowFontSize);
            SetOffset(ModuleWindow.padding, 0, 0, S(ModuleWindowPaddingTop), 0);

            SettingsWindow.fontSize = S(SettingsWindowFontSize);
            SetOffset(SettingsWindow.padding, 0, 0, 0, 0);

            // SettingOn / SettingOff
            SettingOn.fontSize = S(SettingFontSize);
            SetOffset(SettingOn.padding, S(SettingPaddingLeft), 0, 0, 0);

            SettingOff.fontSize = S(SettingFontSize);
            SetOffset(SettingOff.padding, S(SettingPaddingLeft), 0, 0, 0);

            // Descriptions & Author
            DescriptionStyle.fontSize = S(DescriptionFontSize);
            SetOffset(DescriptionStyle.padding, S(DescriptionPaddingLR), S(DescriptionPaddingLR), S(DescriptionPaddingTB), S(DescriptionPaddingTB));

            SettingDescriptionStyle.fontSize = S(SettingDescriptionFontSize);
            SetOffset(SettingDescriptionStyle.padding, S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingTB), S(SettingDescriptionPaddingTB));

            AuthorStyle.fontSize = S(AuthorFontSize);
            SetOffset(AuthorStyle.padding, S(AuthorPaddingLeft), 0, 0, 0);

            // Separator & HUD
            SeparatorStyle.fixedHeight = Sf(SeparatorFixedHeight);

            HUDElementStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(HUDElementFontSize * elementScale));
            SetOffset(HUDElementStyle.padding, 0, 0, 0, 0);

            // NEF Nodes & Text
            SetOffset(NEFNodeStyle.padding, S(NEFNodePaddingLR), S(NEFNodePaddingLR), S(NEFNodePaddingTop), S(NEFNodePaddingBottom));

            TextStyle.fontSize = S(TextFontSize);
            TextHighlightedStyle.fontSize = TextStyle.fontSize;
        }
    }
}