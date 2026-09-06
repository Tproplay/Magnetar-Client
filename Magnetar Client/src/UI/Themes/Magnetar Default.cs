using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.UI.Themes
{

    public static class Magnetar_Default
    {
        public static GUIStyle TopBar;
        public static GUIStyle TopBarActive;

        public static GUIStyle ModuleWindow;
        public static GUIStyle ModuleOn;
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

        private static Texture2D BgTex;
        private static Texture2D BgLightTex;
        private static Texture2D AccentTex;
        private static Texture2D HoverTex;
        private static Texture2D ActiveTex;
        private static Texture2D ActiveHoverTex;

        private static Texture2D TransparentTex;
        private static Texture2D DimTex;

        // Tracks the GUIScale that styles were last rescaled for, so Rescale()
        // is a no-op (aside from the float compare) when nothing changed.
        private static float lastScale = -1f;

        #region Colors
        public static readonly Color BackgroundColor = new Color(26 / 255f, 26 / 255f, 26 / 255f, 220 / 255f);
        public static readonly Color AccentColor = new Color(255 / 255f, 61 / 255f, 61 / 255f, 255 / 255f);
        public static readonly Color LightBackgroundColor = new Color(28 / 255f, 28 / 255f, 28 / 255f, 214 / 255f);
        public static readonly Color TextWhite = new Color(230 / 255f, 230 / 255f, 230 / 255f, 255 / 255f);
        public static readonly Color TextDim = new Color(174 / 255f, 174 / 255f, 174 / 255f, 255 / 255f);
        public static readonly Color HoverColor = new Color(28 / 255f, 28 / 255f, 28 / 255f, 255 / 255f);
        public static readonly Color ActiveColor = new Color(51 / 255f, 51 / 255f, 51 / 255f, 255 / 255f);
        public static readonly Color ActiveHoverColor = new Color(240 / 255f, 51 / 255f, 51 / 255f, 255 / 255f);
        public static readonly Color Transparent = new Color(128 / 255f, 128 / 255f, 128 / 255f, 0 / 255f);
        public static readonly Color DimColor = new Color(26 / 255f, 26 / 255f, 26 / 255f, 102 / 255f);
        #endregion

        // Base (unscaled, GUIScale == 1) sizes. Init() builds every style from
        // these, and Rescale() re-derives fontSize/padding/fixedHeight from
        // these same numbers whenever Config.GUIScale changes, so there's a
        // single source of truth for "what size is this at 1x".
        #region Base Sizes
        private const int TopBarFontSize = 14;
        private const int TopBarPaddingLR = 10;
        private const int TopBarPaddingTB = 5;

        private const int ModuleFontSize = 12;
        private const int ModulePaddingLeft = 10;

        private const int ModuleWindowFontSize = 21;
        private const int ModuleWindowPaddingTop = 3;

        private const int SettingsWindowFontSize = 21;
        private const int SettingsWindowPaddingTop = 1;

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
            // Setup Textures
            BgTex = CreateTex(BackgroundColor);
            AccentTex = CreateTex(AccentColor);
            HoverTex = CreateTex(HoverColor);
            ActiveTex = CreateTex(ActiveColor);

            ActiveHoverTex = CreateTex(ActiveHoverColor);
            TransparentTex = CreateTex(Transparent);
            BgLightTex = CreateTex(LightBackgroundColor);

            DimTex = CreateTex(DimColor);

            #region TopBar
            TopBar = new GUIStyle();
            TopBar.normal.textColor = TextDim;
            TopBar.hover.textColor = Color.white;
            TopBar.active.textColor = Color.white;
            TopBar.normal.background = BgTex;
            TopBar.hover.background = HoverTex;
            TopBar.active.background = ActiveTex;
            TopBar.alignment = TextAnchor.MiddleCenter;
            TopBar.fontSize = TopBarFontSize;

            TopBar.padding = new RectOffset();
            TopBar.padding.left = TopBarPaddingLR;
            TopBar.padding.right = TopBarPaddingLR;
            TopBar.padding.top = TopBarPaddingTB;
            TopBar.padding.bottom = TopBarPaddingTB;

            // Style for the active tab in the top bar (when it's selected)
            TopBarActive = new GUIStyle();
            TopBarActive.normal.textColor = Color.white;
            TopBarActive.hover.textColor = Color.white;
            TopBarActive.active.textColor = Color.white;
            TopBarActive.normal.background = AccentTex;
            TopBarActive.hover.background = AccentTex;
            TopBarActive.active.background = AccentTex;
            TopBarActive.alignment = TextAnchor.MiddleCenter;
            TopBarActive.fontSize = TopBarFontSize;

            TopBarActive.padding = new RectOffset();
            TopBarActive.padding.left = TopBarPaddingLR;
            TopBarActive.padding.right = TopBarPaddingLR;
            TopBarActive.padding.top = TopBarPaddingTB;
            TopBarActive.padding.bottom = TopBarPaddingTB;

            #endregion

            #region ModuleOn
            ModuleOn = new GUIStyle();
            ModuleOn.normal.background = AccentTex;
            ModuleOn.normal.textColor = Color.black;
            ModuleOn.fontSize = ModuleFontSize;
            ModuleOn.alignment = TextAnchor.MiddleLeft;
            ModuleOn.hover.background = ActiveHoverTex;
            ModuleOn.padding = new RectOffset();
            ModuleOn.padding.left = ModulePaddingLeft;
            #endregion

            #region ModuleOff
            ModuleOff = new GUIStyle();
            ModuleOff.normal.background = BgLightTex;
            ModuleOff.normal.textColor = TextDim;
            ModuleOff.fontSize = ModuleFontSize;
            ModuleOff.hover.background = HoverTex;
            ModuleOff.hover.textColor = Color.white;

            ModuleOff.alignment = TextAnchor.MiddleLeft;
            ModuleOff.padding = new RectOffset();
            ModuleOff.padding.left = ModulePaddingLeft;
            #endregion

            #region ModuleWindow
            ModuleWindow = new GUIStyle();
            ModuleWindow.normal.background = BgTex;
            ModuleWindow.normal.textColor = Color.white;

            ModuleWindow.alignment = TextAnchor.UpperCenter;
            ModuleWindow.fontSize = ModuleWindowFontSize;
            ModuleWindow.fontStyle = FontStyle.Bold;

            ModuleWindow.padding = new RectOffset();
            ModuleWindow.padding.top = ModuleWindowPaddingTop;
            ModuleWindow.padding.bottom = 0;
            ModuleWindow.padding.left = 0;
            ModuleWindow.padding.right = 0;
            #endregion

            #region SettingsWindow
            SettingsWindow = new GUIStyle();
            SettingsWindow.normal.background = AccentTex;
            SettingsWindow.normal.textColor = Color.black;

            SettingsWindow.alignment = TextAnchor.UpperCenter;
            SettingsWindow.fontSize = SettingsWindowFontSize;
            SettingsWindow.fontStyle = FontStyle.Bold;

            SettingsWindow.padding = new RectOffset();
            SettingsWindow.padding.top = SettingsWindowPaddingTop;
            SettingsWindow.padding.bottom = 0;
            SettingsWindow.padding.left = 0;
            SettingsWindow.padding.right = 0;
            #endregion

            #region SettingOn
            SettingOn = new GUIStyle();
            SettingOn.normal.background = AccentTex;
            SettingOn.normal.textColor = Color.black;
            SettingOn.fontSize = SettingFontSize;
            SettingOn.alignment = TextAnchor.MiddleLeft;
            SettingOn.hover.background = ActiveHoverTex;
            SettingOn.padding = new RectOffset();
            SettingOn.padding.left = SettingPaddingLeft;
            #endregion

            #region SettingOff
            SettingOff = new GUIStyle();
            SettingOff.normal.background = BgLightTex;
            SettingOff.normal.textColor = TextDim;
            SettingOff.fontSize = SettingFontSize;
            SettingOff.hover.background = HoverTex;
            SettingOff.hover.textColor = Color.white;

            SettingOff.alignment = TextAnchor.MiddleLeft;
            SettingOff.padding = new RectOffset();
            SettingOff.padding.left = SettingPaddingLeft;
            #endregion

            #region Description
            DescriptionStyle = new GUIStyle();
            DescriptionStyle.fontSize = DescriptionFontSize;
            DescriptionStyle.wordWrap = true;
            DescriptionStyle.alignment = TextAnchor.UpperLeft;
            DescriptionStyle.richText = true;

            DescriptionStyle.normal = new GUIStyleState();
            DescriptionStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            DescriptionStyle.padding = new RectOffset();
            DescriptionStyle.padding.left = DescriptionPaddingLR;
            DescriptionStyle.padding.right = DescriptionPaddingLR;
            DescriptionStyle.padding.top = DescriptionPaddingTB;
            DescriptionStyle.padding.bottom = DescriptionPaddingTB;

            #endregion

            #region SettingDescriptionStyle
            SettingDescriptionStyle = new GUIStyle();
            SettingDescriptionStyle.fontSize = SettingDescriptionFontSize;
            SettingDescriptionStyle.wordWrap = true;
            SettingDescriptionStyle.alignment = TextAnchor.UpperLeft;
            SettingDescriptionStyle.richText = true;

            SettingDescriptionStyle.normal = new GUIStyleState();
            SettingDescriptionStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            SettingDescriptionStyle.padding = new RectOffset();
            SettingDescriptionStyle.padding.left = SettingDescriptionPaddingLR;
            SettingDescriptionStyle.padding.right = SettingDescriptionPaddingLR;
            SettingDescriptionStyle.padding.top = SettingDescriptionPaddingTB;
            SettingDescriptionStyle.padding.bottom = SettingDescriptionPaddingTB;

            #endregion

            #region Author Style
            AuthorStyle = new GUIStyle();
            AuthorStyle.fontSize = AuthorFontSize;
            AuthorStyle.fontStyle = FontStyle.Italic;
            AuthorStyle.alignment = TextAnchor.MiddleLeft;
            AuthorStyle.padding = new RectOffset();
            AuthorStyle.padding.left = AuthorPaddingLeft;
            AuthorStyle.richText = true;

            AuthorStyle.normal = new GUIStyleState();
            AuthorStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);

            #endregion

            #region Seperator Style
            SeparatorStyle = new GUIStyle();
            SeparatorStyle.fixedHeight = SeparatorFixedHeight;
            SeparatorStyle.margin = new RectOffset();
            SeparatorStyle.padding = new RectOffset();
            SeparatorStyle.normal = new GUIStyleState();

            SeparatorStyle.normal.background = Texture2D.whiteTexture;

            #endregion

            #region DimStyle

            DimStyle = new GUIStyle();
            DimStyle.normal.background = DimTex;

            #endregion

            #region HudElement
            HUDElementStyle = new GUIStyle();

            HUDElementStyle.fontSize = HUDElementFontSize;
            HUDElementStyle.alignment = TextAnchor.UpperLeft;
            HUDElementStyle.wordWrap = false;
            HUDElementStyle.richText = true;

            HUDElementStyle.normal = new GUIStyleState();
            HUDElementStyle.normal.textColor = Color.white;



            #endregion

            #region NEF Node Connection line

            NEFLineStyle = new GUIStyle();
            NEFLineStyle.normal.background = Texture2D.whiteTexture;

            RectOffset offset = new RectOffset();
            offset.left = 0; offset.right = 0; offset.top = 0; offset.bottom = 0;
            NEFLineStyle.border = offset; NEFLineStyle.margin = offset;
            NEFLineStyle.padding = offset; NEFLineStyle.overflow = offset;

            #endregion

            #region NEF Node Style
            NEFNodeStyle = new GUIStyle();

            NEFNodeStyle.normal.background = AccentTex;

            NEFNodeStyle.font = Magnetar_Default.SettingOn.font;
            NEFNodeStyle.wordWrap = Magnetar_Default.SettingOn.wordWrap;

            NEFNodeStyle.alignment = TextAnchor.LowerCenter;
            NEFNodeStyle.padding = new RectOffset();
            NEFNodeStyle.padding.left = NEFNodePaddingLR;
            NEFNodeStyle.padding.right = NEFNodePaddingLR;
            NEFNodeStyle.padding.top = NEFNodePaddingTop;
            NEFNodeStyle.padding.bottom = NEFNodePaddingBottom;
            #endregion

            #region TextStyle Normal

            TextStyle = new GUIStyle();
            TextStyle.wordWrap = false;
            TextStyle.alignment = TextAnchor.MiddleLeft;
            TextStyle.richText = false;
            TextStyle.clipping = TextClipping.Clip;
            TextStyle.fontSize = TextFontSize;

            TextStyle.normal.textColor = Color.white;

            #endregion

            #region TextStyle Highlighted

            TextHighlightedStyle = new GUIStyle();
            TextHighlightedStyle.wordWrap = TextStyle.wordWrap;
            TextHighlightedStyle.alignment = TextStyle.alignment;
            TextHighlightedStyle.richText = TextStyle.richText;
            TextHighlightedStyle.clipping = TextStyle.clipping;
            TextHighlightedStyle.fontSize = TextStyle.fontSize;
            TextHighlightedStyle.normal.textColor = TextStyle.normal.textColor;

            TextHighlightedStyle.normal.background = AccentTex;

            #endregion

            // Apply current GUIScale immediately in case it isn't 1 at startup.
            lastScale = -1f;
            Rescale();

            DebugLogger.Msg("Initialized the Theme 'Magnetar_Default'");

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
            float scale = Config.GUIScale;
            if (Mathf.Approximately(scale, lastScale)) return;
            lastScale = scale;

            int S(int baseValue) => Mathf.Max(1, Mathf.RoundToInt(Config.S(baseValue)));
            float Sf(float baseValue) => Mathf.Max(0f, Config.S(baseValue));

            // TopBar / TopBarActive
            TopBar.fontSize = S(TopBarFontSize);
            TopBar.padding.left = S(TopBarPaddingLR);
            TopBar.padding.right = S(TopBarPaddingLR);
            TopBar.padding.top = S(TopBarPaddingTB);
            TopBar.padding.bottom = S(TopBarPaddingTB);

            TopBarActive.fontSize = S(TopBarFontSize);
            TopBarActive.padding.left = S(TopBarPaddingLR);
            TopBarActive.padding.right = S(TopBarPaddingLR);
            TopBarActive.padding.top = S(TopBarPaddingTB);
            TopBarActive.padding.bottom = S(TopBarPaddingTB);

            // ModuleOn / ModuleOff
            ModuleOn.fontSize = S(ModuleFontSize);
            ModuleOn.padding.left = S(ModulePaddingLeft);

            ModuleOff.fontSize = S(ModuleFontSize);
            ModuleOff.padding.left = S(ModulePaddingLeft);

            // ModuleWindow
            ModuleWindow.fontSize = S(ModuleWindowFontSize);
            ModuleWindow.padding.top = S(ModuleWindowPaddingTop);

            // SettingsWindow
            SettingsWindow.fontSize = S(SettingsWindowFontSize);
            SettingsWindow.padding.top = S(SettingsWindowPaddingTop);

            // SettingOn / SettingOff
            SettingOn.fontSize = S(SettingFontSize);
            SettingOn.padding.left = S(SettingPaddingLeft);

            SettingOff.fontSize = S(SettingFontSize);
            SettingOff.padding.left = S(SettingPaddingLeft);

            // DescriptionStyle
            DescriptionStyle.fontSize = S(DescriptionFontSize);
            DescriptionStyle.padding.left = S(DescriptionPaddingLR);
            DescriptionStyle.padding.right = S(DescriptionPaddingLR);
            DescriptionStyle.padding.top = S(DescriptionPaddingTB);
            DescriptionStyle.padding.bottom = S(DescriptionPaddingTB);

            // SettingDescriptionStyle
            SettingDescriptionStyle.fontSize = S(SettingDescriptionFontSize);
            SettingDescriptionStyle.padding.left = S(SettingDescriptionPaddingLR);
            SettingDescriptionStyle.padding.right = S(SettingDescriptionPaddingLR);
            SettingDescriptionStyle.padding.top = S(SettingDescriptionPaddingTB);
            SettingDescriptionStyle.padding.bottom = S(SettingDescriptionPaddingTB);

            // AuthorStyle
            AuthorStyle.fontSize = S(AuthorFontSize);
            AuthorStyle.padding.left = S(AuthorPaddingLeft);

            // SeparatorStyle
            SeparatorStyle.fixedHeight = Sf(SeparatorFixedHeight);

            // HUDElementStyle
            HUDElementStyle.fontSize = S(HUDElementFontSize);

            // NEFNodeStyle
            NEFNodeStyle.padding.left = S(NEFNodePaddingLR);
            NEFNodeStyle.padding.right = S(NEFNodePaddingLR);
            NEFNodeStyle.padding.top = S(NEFNodePaddingTop);
            NEFNodeStyle.padding.bottom = S(NEFNodePaddingBottom);

            // TextStyle, then mirror into TextHighlightedStyle
            TextStyle.fontSize = S(TextFontSize);
            TextHighlightedStyle.fontSize = TextStyle.fontSize;
        }

        private static Texture2D CreateTex(Color col)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }
    }
}