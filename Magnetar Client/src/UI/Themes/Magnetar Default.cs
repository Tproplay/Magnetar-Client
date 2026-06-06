using MelonLoader;
using UnityEngine;

namespace Magnetar_Client.UI.Themes
{

    public static class Magnetar_Default
    {
        public static GUIStyle TopBar;
        public static GUIStyle TopBarActive;
        public static GUIStyle ModuleWindow;
        public static GUIStyle ModuleOn;
        public static GUIStyle ModuleOff;
        public static GUIStyle DescriptionStyle;
        public static GUIStyle AuthorStyle;
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
            TopBar.fontSize = 12;

            TopBar.padding = new RectOffset();
            TopBar.padding.left = 10;
            TopBar.padding.right = 10;
            TopBar.padding.top = 5;
            TopBar.padding.bottom = 5;

            // Style for the active tab in the top bar (when it's selected)
            TopBarActive = new GUIStyle();
            TopBarActive.normal.textColor = Color.white;
            TopBarActive.hover.textColor = Color.white;
            TopBarActive.active.textColor = Color.white;
            TopBarActive.normal.background = AccentTex;
            TopBarActive.hover.background = AccentTex;
            TopBarActive.active.background = AccentTex;
            TopBarActive.alignment = TextAnchor.MiddleCenter;
            TopBarActive.fontSize = 12;

            TopBarActive.padding = new RectOffset();
            TopBarActive.padding.left = 10;
            TopBarActive.padding.right = 10;
            TopBarActive.padding.top = 5;
            TopBarActive.padding.bottom = 5;

            #endregion

            #region ModuleWindow
            ModuleWindow = new GUIStyle();
            ModuleWindow.normal.background = BgTex;
            ModuleWindow.normal.textColor = Color.white;

            ModuleWindow.alignment = TextAnchor.UpperCenter;
            ModuleWindow.fontSize = 20;
            ModuleWindow.fontStyle = FontStyle.Bold;

            ModuleWindow.padding = new RectOffset();
            ModuleWindow.padding.top = 3;
            ModuleWindow.padding.bottom = 0;
            ModuleWindow.padding.left = 0;
            ModuleWindow.padding.right = 0;
            #endregion

            #region ModuleOn
            ModuleOn = new GUIStyle();
            ModuleOn.normal.background = AccentTex;
            ModuleOn.normal.textColor = Color.black;
            ModuleOn.alignment = TextAnchor.MiddleLeft;
            ModuleOn.hover.background = ActiveHoverTex;
            ModuleOn.padding = new RectOffset();
            ModuleOn.padding.left = 10;
            #endregion

            #region ModuleOff
            ModuleOff = new GUIStyle();
            ModuleOff.normal.background = BgLightTex;
            ModuleOff.normal.textColor = TextDim;
            ModuleOff.hover.background = HoverTex;
            ModuleOff.hover.textColor = Color.white;

            ModuleOff.alignment = TextAnchor.MiddleLeft;
            ModuleOff.padding = new RectOffset();
            ModuleOff.padding.left = 10;
            #endregion

            #region Description
            DescriptionStyle = new GUIStyle();
            DescriptionStyle.fontSize = 16;
            DescriptionStyle.wordWrap = true;
            DescriptionStyle.alignment = TextAnchor.UpperLeft;
            DescriptionStyle.richText = true;

            DescriptionStyle.normal = new GUIStyleState();
            DescriptionStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            DescriptionStyle.padding = new RectOffset();
            DescriptionStyle.padding.left = 5;
            DescriptionStyle.padding.right = 5;
            DescriptionStyle.padding.top = 2;
            DescriptionStyle.padding.bottom = 2;

            #endregion

            #region Author Style
            AuthorStyle = new GUIStyle();
            AuthorStyle.fontSize = 15;
            AuthorStyle.fontStyle = FontStyle.Italic;
            AuthorStyle.alignment = TextAnchor.MiddleLeft;
            AuthorStyle.padding = new RectOffset();
            AuthorStyle.padding.left = 10;

            AuthorStyle.normal = new GUIStyleState();
            AuthorStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);

            #endregion

            #region Seperator Style
            SeparatorStyle = new GUIStyle();
            SeparatorStyle.fixedHeight = 1; 
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

            HUDElementStyle.fontSize = 18;
            HUDElementStyle.alignment = TextAnchor.UpperLeft;
            HUDElementStyle.wordWrap = false;
            HUDElementStyle.richText = true;

            HUDElementStyle.normal = new GUIStyleState();
            HUDElementStyle.normal.textColor = new Color(1f, 1f, 1f);



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

            NEFNodeStyle.font = Magnetar_Default.ModuleOn.font;
            NEFNodeStyle.wordWrap = Magnetar_Default.ModuleOn.wordWrap;

            NEFNodeStyle.alignment = TextAnchor.LowerCenter;
            NEFNodeStyle.padding = new RectOffset();
            NEFNodeStyle.padding.left = 2;
            NEFNodeStyle.padding.right = 2;
            NEFNodeStyle.padding.top = 2;
            NEFNodeStyle.padding.bottom = 5;
            #endregion

            #region TextStyle Normal

            TextStyle = new GUIStyle();
            TextStyle.wordWrap = false;
            TextStyle.alignment = TextAnchor.MiddleLeft;
            TextStyle.richText = false;
            TextStyle.clipping = TextClipping.Clip;

            TextStyle.normal.textColor = Color.white;

            #endregion

            #region TextStyle Highlighted

            TextHighlightedStyle = new GUIStyle();
            TextHighlightedStyle.wordWrap = false;
            TextHighlightedStyle.alignment = TextAnchor.MiddleLeft;
            TextHighlightedStyle.richText = false;
            TextHighlightedStyle.clipping = TextClipping.Clip;

            TextHighlightedStyle.normal.textColor = Color.white;
            TextHighlightedStyle.normal.background = AccentTex;

            #endregion

            MelonLogger.Msg("Initialized the Theme 'Magnetar_Default'");

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
