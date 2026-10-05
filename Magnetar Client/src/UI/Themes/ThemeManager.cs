using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Magnetar_Client.Core;
using Magnetar_Client.Core.Lifecycle;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Api.PathsManager;

namespace Magnetar_Client.UI.Themes;

public static class ThemeManager
{
    public static string CurrentThemeName => Config.Theme ?? "Magnetar Default";

    #region Direct Accessors
    public static Color BackgroundColor => ThemeData.CategoryWindowBg;
    public static Color AccentColor => ThemeData.CategoryModuleOnBg;
    public static Color LightBackgroundColor => ThemeData.CategoryModuleOffBg;
    public static Color TextWhite => ThemeData.TypographyText;
    public static Color TextDim => ThemeData.CategoryModuleOffText;
    public static Color DimColor => ThemeData.DimBg;
    public static Color NefLineColor => ThemeData.NefLineColor;
    public static Color NefNodeColor => ThemeData.NefNodeBg;
    public static Color SettingOnColor => ThemeData.SettingOnBg;
    public static Color SettingOffColor => ThemeData.SettingOffBg;
    public static Color SeparatorColor => ThemeData.Separator;

    public static Dictionary<string, ThemeDefinition> LoadedThemes => ThemeData.LoadedThemes;
    public static ThemeDefinition InternalDefaultTheme => ThemeData.InternalDefaultTheme;
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

    public static void Init()
    {
        LoadThemes();
        ApplyTheme(CurrentThemeName);
        Rescale();

        ServiceRegistry.Register(new ThemeService());
    }

    private static void BuildTextures()
    {
        ThemeData.TopBarOffBgTex = ThemeData.Create1x1Tex(ThemeData.TopBarOffBg);
        ThemeData.TopBarActiveBgTex = ThemeData.Create1x1Tex(ThemeData.TopBarActiveBg);
        ThemeData.CategoryWindowBgTex = ThemeData.Create1x1Tex(ThemeData.CategoryWindowBg);
        ThemeData.CategoryHeaderBgTex = ThemeData.Create1x1Tex(ThemeData.CategoryHeaderBg);
        ThemeData.CategoryModuleOffBgTex = ThemeData.Create1x1Tex(ThemeData.CategoryModuleOffBg);
        ThemeData.CategoryModuleOnBgTex = ThemeData.Create1x1Tex(ThemeData.CategoryModuleOnBg);

        ThemeData.SettingsWindowBgTex = ThemeData.Create1x1Tex(ThemeData.SettingsWindowBg);
        ThemeData.SettingsHeaderBgTex = ThemeData.Create1x1Tex(ThemeData.SettingsHeaderBg);
        ThemeData.CloseBtnBgTex = ThemeData.Create1x1Tex(ThemeData.CloseBtnBg);

        ThemeData.SettingOffBgTex = ThemeData.Create1x1Tex(ThemeData.SettingOffBg);
        ThemeData.SettingOnBgTex = ThemeData.Create1x1Tex(ThemeData.SettingOnBg);

        ThemeData.ButtonSettingBgTex = ThemeData.Create1x1Tex(ThemeData.ButtonSettingBg);
        ThemeData.ResetBtnBgTex = ThemeData.Create1x1Tex(ThemeData.ResetBtnBg);
        ThemeData.ListAddBtnBgTex = ThemeData.Create1x1Tex(ThemeData.ListAddBtnBg);
        ThemeData.ListRemoveBtnBgTex = ThemeData.Create1x1Tex(ThemeData.ListRemoveBtnBg);

        ThemeData.SectionGroupHeaderBgTex = ThemeData.Create1x1Tex(ThemeData.SectionGroupHeaderBg);
        ThemeData.SectionHeaderBgTex = ThemeData.Create1x1Tex(ThemeData.SectionHeaderBg);
        ThemeData.SectionRemoveBtnBgTex = ThemeData.Create1x1Tex(ThemeData.SectionRemoveBtnBg);
        ThemeData.SectionAddBtnBgTex = ThemeData.Create1x1Tex(ThemeData.SectionAddBtnBg);

        ThemeData.HighlightBgTex = ThemeData.Create1x1Tex(ThemeData.TypographyHighlightBg);
        ThemeData.NefLineTex = ThemeData.Create1x1Tex(ThemeData.NefLineColor);
        ThemeData.NefNodeTex = ThemeData.Create1x1Tex(ThemeData.NefNodeBg);
        ThemeData.DimBgTex = ThemeData.Create1x1Tex(ThemeData.DimBg);
        ThemeData.SeparatorTex = ThemeData.Create1x1Tex(ThemeData.Separator);

        ThemeData.SliderTrackOffTex = ThemeData.Create1x1Tex(ThemeData.SliderTrackOff);
        ThemeData.SliderTrackOnTex = ThemeData.Create1x1Tex(ThemeData.SliderTrackOn);
        ThemeData.SliderThumbTex = ThemeData.GetCircleTex(ThemeData.SliderThumb);
    }

    public static void LoadThemes()
    {
        ThemeData.LoadedThemes.Clear();
        ThemeData.LoadedThemes[ThemeData.InternalDefaultTheme.Name] = ThemeData.InternalDefaultTheme;

        string themeDir = Path.Combine(DataDir, "Themes");
        try
        {
            if (!Directory.Exists(themeDir))
            {
                Directory.CreateDirectory(themeDir);
                string defaultJson = JsonConvert.SerializeObject(ThemeData.InternalDefaultTheme, Formatting.Indented);
                File.WriteAllText(Path.Combine(themeDir, "Default.json"), defaultJson);
                return;
            }

            string[] files = Directory.GetFiles(themeDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var def = JsonConvert.DeserializeObject<ThemeDefinition>(json);
                    if (def != null && !string.IsNullOrEmpty(def.Name))
                    {
                        ThemeData.LoadedThemes[def.Name] = def;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[ThemeManager] Error loading theme file '{Path.GetFileName(file)}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[ThemeManager] Error reading Themes directory: {ex.Message}");
        }
    }

    public static void ApplyTheme(string themeName)
    {
        if (!ThemeData.LoadedThemes.TryGetValue(themeName, out var def))
        {
            def = ThemeData.InternalDefaultTheme;
        }

        // TopBar
        if (ColorUtility.TryParseHtmlString(def.TopBarOffBgHex, out var tbOffBg)) ThemeData.TopBarOffBg = tbOffBg;
        if (ColorUtility.TryParseHtmlString(def.TopBarOffTextHex, out var tbOffTxt)) ThemeData.TopBarOffText = tbOffTxt;
        if (ColorUtility.TryParseHtmlString(def.TopBarActiveBgHex, out var tbActBg)) ThemeData.TopBarActiveBg = tbActBg;
        if (ColorUtility.TryParseHtmlString(def.TopBarActiveTextHex, out var tbActTxt)) ThemeData.TopBarActiveText = tbActTxt;

        // Categories
        if (ColorUtility.TryParseHtmlString(def.CategoryWindowBgHex, out var catWinBg)) ThemeData.CategoryWindowBg = catWinBg;
        if (ColorUtility.TryParseHtmlString(def.CategoryWindowTextHex, out var catWinTxt)) ThemeData.CategoryWindowText = catWinTxt;
        if (ColorUtility.TryParseHtmlString(def.CategoryHeaderBgHex, out var catHdrBg)) ThemeData.CategoryHeaderBg = catHdrBg;
        if (ColorUtility.TryParseHtmlString(def.CategoryHeaderTextHex, out var catHdrTxt)) ThemeData.CategoryHeaderText = catHdrTxt;
        if (ColorUtility.TryParseHtmlString(def.CategoryModuleOffBgHex, out var catModOffBg)) ThemeData.CategoryModuleOffBg = catModOffBg;
        if (ColorUtility.TryParseHtmlString(def.CategoryModuleOffTextHex, out var catModOffTxt)) ThemeData.CategoryModuleOffText = catModOffTxt;
        if (ColorUtility.TryParseHtmlString(def.CategoryModuleOnBgHex, out var catModOnBg)) ThemeData.CategoryModuleOnBg = catModOnBg;
        if (ColorUtility.TryParseHtmlString(def.CategoryModuleOnTextHex, out var catModOnTxt)) ThemeData.CategoryModuleOnText = catModOnTxt;

        // Settings Window
        if (ColorUtility.TryParseHtmlString(def.SettingsWindowBgHex, out var setWinBg)) ThemeData.SettingsWindowBg = setWinBg;
        if (ColorUtility.TryParseHtmlString(def.SettingsHeaderBgHex, out var setHdrBg)) ThemeData.SettingsHeaderBg = setHdrBg;
        if (ColorUtility.TryParseHtmlString(def.SettingsHeaderTextHex, out var setHdrTxt)) ThemeData.SettingsHeaderText = setHdrTxt;
        if (ColorUtility.TryParseHtmlString(def.CloseBtnBgHex, out var clsBg)) ThemeData.CloseBtnBg = clsBg;
        if (ColorUtility.TryParseHtmlString(def.CloseBtnTextHex, out var clsTxt)) ThemeData.CloseBtnText = clsTxt;

        // Setting Elements
        if (ColorUtility.TryParseHtmlString(def.SettingOffBgHex, out var setOffBg)) ThemeData.SettingOffBg = setOffBg;
        if (ColorUtility.TryParseHtmlString(def.SettingOffTextHex, out var setOffTxt)) ThemeData.SettingOffText = setOffTxt;
        if (ColorUtility.TryParseHtmlString(def.SettingOnBgHex, out var setOnBg)) ThemeData.SettingOnBg = setOnBg;
        if (ColorUtility.TryParseHtmlString(def.SettingOnTextHex, out var setOnTxt)) ThemeData.SettingOnText = setOnTxt;

        // Action Buttons
        if (ColorUtility.TryParseHtmlString(def.ButtonSettingBgHex, out var btnSetBg)) ThemeData.ButtonSettingBg = btnSetBg;
        if (ColorUtility.TryParseHtmlString(def.ButtonSettingTextHex, out var btnSetTxt)) ThemeData.ButtonSettingText = btnSetTxt;
        if (ColorUtility.TryParseHtmlString(def.ResetBtnBgHex, out var rstBg)) ThemeData.ResetBtnBg = rstBg;
        if (ColorUtility.TryParseHtmlString(def.ResetBtnTextHex, out var rstTxt)) ThemeData.ResetBtnText = rstTxt;
        if (ColorUtility.TryParseHtmlString(def.ListAddBtnBgHex, out var addBg)) ThemeData.ListAddBtnBg = addBg;
        if (ColorUtility.TryParseHtmlString(def.ListAddBtnTextHex, out var addTxt)) ThemeData.ListAddBtnText = addTxt;
        if (ColorUtility.TryParseHtmlString(def.ListRemoveBtnBgHex, out var remBg)) ThemeData.ListRemoveBtnBg = remBg;
        if (ColorUtility.TryParseHtmlString(def.ListRemoveBtnTextHex, out var remTxt)) ThemeData.ListRemoveBtnText = remTxt;

        // Section Settings
        if (ColorUtility.TryParseHtmlString(def.SectionGroupHeaderBgHex, out var secGrpBg)) ThemeData.SectionGroupHeaderBg = secGrpBg;
        if (ColorUtility.TryParseHtmlString(def.SectionGroupHeaderTextHex, out var secGrpTxt)) ThemeData.SectionGroupHeaderText = secGrpTxt;
        if (ColorUtility.TryParseHtmlString(def.SectionHeaderBgHex, out var secHdrBg)) ThemeData.SectionHeaderBg = secHdrBg;
        if (ColorUtility.TryParseHtmlString(def.SectionHeaderTextHex, out var secHdrTxt)) ThemeData.SectionHeaderText = secHdrTxt;
        if (ColorUtility.TryParseHtmlString(def.SectionRemoveBtnBgHex, out var secRemBg)) ThemeData.SectionRemoveBtnBg = secRemBg;
        if (ColorUtility.TryParseHtmlString(def.SectionRemoveBtnTextHex, out var secRemTxt)) ThemeData.SectionRemoveBtnText = secRemTxt;
        if (ColorUtility.TryParseHtmlString(def.SectionAddBtnBgHex, out var secAddBg)) ThemeData.SectionAddBtnBg = secAddBg;
        if (ColorUtility.TryParseHtmlString(def.SectionAddBtnTextHex, out var secAddTxt)) ThemeData.SectionAddBtnText = secAddTxt;

        // Typography
        if (ColorUtility.TryParseHtmlString(def.TypographyDescriptionHex, out var typoDesc)) ThemeData.TypographyDescription = typoDesc;
        if (ColorUtility.TryParseHtmlString(def.TypographyLabelHex, out var typoLbl)) ThemeData.TypographyLabel = typoLbl;
        if (ColorUtility.TryParseHtmlString(def.TypographyAuthorHex, out var typoAuth)) ThemeData.TypographyAuthor = typoAuth;
        if (ColorUtility.TryParseHtmlString(def.TypographyTextHex, out var typoTxt)) ThemeData.TypographyText = typoTxt;
        if (ColorUtility.TryParseHtmlString(def.TypographySecondaryHex, out var typoSec)) ThemeData.TypographySecondary = typoSec;
        if (ColorUtility.TryParseHtmlString(def.TypographyHighlightTextHex, out var typoHighTxt)) ThemeData.TypographyHighlightText = typoHighTxt;
        if (ColorUtility.TryParseHtmlString(def.TypographyHighlightBgHex, out var typoHighBg)) ThemeData.TypographyHighlightBg = typoHighBg;

        // NEF & HUD
        if (ColorUtility.TryParseHtmlString(def.NefLineColorHex, out var nefLine)) ThemeData.NefLineColor = nefLine;
        if (ColorUtility.TryParseHtmlString(def.NefNodeBgHex, out var nefNode)) ThemeData.NefNodeBg = nefNode;
        if (ColorUtility.TryParseHtmlString(def.HudTextColorHex, out var hudTxt)) ThemeData.HudTextColor = hudTxt;

        // Misc & Sliders
        if (ColorUtility.TryParseHtmlString(def.DimBgHex, out var dim)) ThemeData.DimBg = dim;
        if (ColorUtility.TryParseHtmlString(def.SeparatorHex, out var sep)) ThemeData.Separator = sep;
        if (ColorUtility.TryParseHtmlString(def.SeparatorTextHex, out var sepTxt)) ThemeData.SeparatorText = sepTxt;
        if (ColorUtility.TryParseHtmlString(def.SliderTrackOffHex, out var sldOff)) ThemeData.SliderTrackOff = sldOff;
        if (ColorUtility.TryParseHtmlString(def.SliderTrackOnHex, out var sldOn)) ThemeData.SliderTrackOn = sldOn;
        if (ColorUtility.TryParseHtmlString(def.SliderThumbHex, out var sldThb)) ThemeData.SliderThumb = sldThb;

        BuildTextures();
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

    public static void Rescale()
    {
        int S(int baseValue) => Mathf.Max(1, Mathf.RoundToInt(Config.S(baseValue)));
        float Sf(float baseValue) => Mathf.Max(0f, Config.S(baseValue));

        // 1. Top Bar
        TopBarStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { background = ThemeData.TopBarOffBgTex, textColor = ThemeData.TopBarOffText },
            fontSize = S(TopBarFontSize),
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(TopBarStyle.padding, S(TopBarPaddingLR), S(TopBarPaddingLR), S(TopBarPaddingTB), S(TopBarPaddingTB));

        TopBarActiveStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { background = ThemeData.TopBarActiveBgTex, textColor = ThemeData.TopBarActiveText },
            fontStyle = FontStyle.Bold,
            fontSize = S(TopBarFontSize),
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(TopBarActiveStyle.padding, S(TopBarPaddingLR), S(TopBarPaddingLR), S(TopBarPaddingTB), S(TopBarPaddingTB));

        // 2. Category Windows
        CategoryWindowStyle = new GUIStyle
        {
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold,
            fontSize = S(ModuleWindowFontSize),
            normal = { background = ThemeData.CategoryWindowBgTex, textColor = ThemeData.CategoryWindowText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(CategoryWindowStyle.padding, 0, 0, S(ModuleWindowPaddingTop), 0);

        CategoryHeaderStyle = new GUIStyle
        {
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold,
            fontSize = S(ModuleWindowFontSize),
            normal = { background = ThemeData.CategoryHeaderBgTex, textColor = ThemeData.CategoryHeaderText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(CategoryHeaderStyle.padding, 0, 0, S(ModuleWindowPaddingTop), 0);

        CategoryModuleOffStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = S(ModuleFontSize),
            normal = { background = ThemeData.CategoryModuleOffBgTex, textColor = ThemeData.CategoryModuleOffText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(CategoryModuleOffStyle.padding, S(ModulePaddingLeft), 0, 0, 0);

        CategoryModuleOnStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = S(ModuleFontSize),
            normal = { background = ThemeData.CategoryModuleOnBgTex, textColor = ThemeData.CategoryModuleOnText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(CategoryModuleOnStyle.padding, S(ModulePaddingLeft), 0, 0, 0);

        // 3. Settings Window & Close Button
        SettingsWndowStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = S(SettingsWindowFontSize),
            normal = { background = ThemeData.SettingsHeaderBgTex, textColor = ThemeData.SettingsHeaderText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SettingsWndowStyle.padding, 0, 0, 0, 0);

        SettingsWndowBgStyle = new GUIStyle
        {
            normal = { background = ThemeData.SettingsWindowBgTex, textColor = ThemeData.SettingsHeaderText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SettingsWndowBgStyle.padding, 0, 0, 0, 0);

        CloseButtonStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = S(ModuleFontSize),
            normal = { background = ThemeData.CloseBtnBgTex, textColor = ThemeData.CloseBtnText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };

        // 4. Setting Toggles
        SettingOff = new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.SettingOffBgTex, textColor = ThemeData.SettingOffText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SettingOff.padding, S(SettingPaddingLeft), 0, 0, 0);

        SettingOn = new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.SettingOnBgTex, textColor = ThemeData.SettingOnText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SettingOn.padding, S(SettingPaddingLeft), 0, 0, 0);

        // 5. Action Buttons
        ButtonSettingStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.ButtonSettingBgTex, textColor = ThemeData.ButtonSettingText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(ButtonSettingStyle.padding, 0, 0, 0, 0);

        ResetButtonStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.ResetBtnBgTex, textColor = ThemeData.ResetBtnText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(ResetButtonStyle.padding, 0, 0, 0, 0);

        ListAddButtonStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.ListAddBtnBgTex, textColor = ThemeData.ListAddBtnText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(ListAddButtonStyle.padding, 0, 0, 0, 0);

        ListRemoveButtonStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.ListRemoveBtnBgTex, textColor = ThemeData.ListRemoveBtnText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(ListRemoveButtonStyle.padding, 0, 0, 0, 0);

        // 6. Section Settings
        SectionGroupHeaderStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = S(SettingDescriptionFontSize),
            normal = { background = ThemeData.SectionGroupHeaderBgTex, textColor = ThemeData.SectionGroupHeaderText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SectionGroupHeaderStyle.padding, S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingLR), 0, 0);

        SectionHeaderStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.SectionHeaderBgTex, textColor = ThemeData.SectionHeaderText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SectionHeaderStyle.padding, S(SettingPaddingLeft), 0, 0, 0);

        SectionRemoveButtonStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.SectionRemoveBtnBgTex, textColor = ThemeData.SectionRemoveBtnText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SectionRemoveButtonStyle.padding, 0, 0, 0, 0);

        SectionAddButtonStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = S(SettingFontSize),
            normal = { background = ThemeData.SectionAddBtnBgTex, textColor = ThemeData.SectionAddBtnText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SectionAddButtonStyle.padding, 0, 0, 0, 0);

        // 7. Typography
        SettingsDescriptionStyle = new GUIStyle
        {
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            richText = true,
            fontSize = S(DescriptionFontSize),
            normal = { textColor = ThemeData.TypographyDescription },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SettingsDescriptionStyle.padding, S(DescriptionPaddingLR), S(DescriptionPaddingLR), S(DescriptionPaddingTB), S(DescriptionPaddingTB));

        SettingLabelStyle = new GUIStyle
        {
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            richText = true,
            fontSize = S(SettingDescriptionFontSize),
            normal = { textColor = ThemeData.TypographyLabel },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SettingLabelStyle.padding, S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingLR), S(SettingDescriptionPaddingTB), S(SettingDescriptionPaddingTB));

        SettingAuthorStyle = new GUIStyle
        {
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            fontSize = S(AuthorFontSize),
            normal = { textColor = ThemeData.TypographyAuthor },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SettingAuthorStyle.padding, S(AuthorPaddingLeft), 0, 0, 0);

        SettingTextStyle = new GUIStyle
        {
            wordWrap = false,
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            clipping = TextClipping.Clip,
            fontSize = S(TextFontSize),
            normal = { textColor = ThemeData.TypographyText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };

        TextStyle = new GUIStyle
        {
            wordWrap = false,
            alignment = TextAnchor.MiddleLeft,
            richText = false,
            clipping = TextClipping.Clip,
            fontSize = S(TextFontSize),
            normal = { textColor = ThemeData.TypographyText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };

        TextHighlightedStyle = new GUIStyle
        {
            wordWrap = false,
            alignment = TextAnchor.MiddleLeft,
            richText = false,
            clipping = TextClipping.Clip,
            fontSize = S(TextFontSize),
            normal = { background = ThemeData.HighlightBgTex, textColor = ThemeData.TypographyHighlightText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };

        // 8. Separators & HUD
        SeparatorStyle = new GUIStyle
        {
            fixedHeight = Sf(SeparatorFixedHeight),
            normal = { background = ThemeData.SeparatorTex },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };

        SeparatorTextStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            richText = true,
            clipping = TextClipping.Overflow,
            fontSize = S(SettingFontSize),
            normal = { textColor = ThemeData.SeparatorText },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SeparatorTextStyle.padding, 0, 0, 0, 0);

        HUDElementStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            richText = true,
            fontSize = Mathf.Max(1, Mathf.RoundToInt(HUDElementFontSize * Config.ElementScale)),
            normal = { textColor = ThemeData.HudTextColor },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(HUDElementStyle.padding, 0, 0, 0, 0);

        // 9. NEF & Overlays
        NEFLineStyle = new GUIStyle
        {
            normal = { background = ThemeData.NefLineTex },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };

        NEFNodeStyle = new GUIStyle
        {
            alignment = TextAnchor.LowerCenter,
            normal = { background = ThemeData.NefNodeTex },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(NEFNodeStyle.padding, S(NEFNodePaddingLR), S(NEFNodePaddingLR), S(NEFNodePaddingTop), S(NEFNodePaddingBottom));

        DimBackgroundStyle = new GUIStyle
        {
            normal = { background = ThemeData.DimBgTex },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };

        // 10. Sliders
        SliderTrackOffStyle = new GUIStyle
        {
            normal = { background = ThemeData.SliderTrackOffTex },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SliderTrackOffStyle.padding, 0, 0, 0, 0);

        SliderTrackOnStyle = new GUIStyle
        {
            normal = { background = ThemeData.SliderTrackOnTex },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SliderTrackOnStyle.padding, 0, 0, 0, 0);

        SliderThumbStyle = new GUIStyle
        {
            normal = { background = ThemeData.SliderThumbTex },
            hover = { background = ThemeData.SliderThumbTex },
            active = { background = ThemeData.SliderThumbTex },
            border = new RectOffset(),
            padding = new RectOffset(),
            margin = new RectOffset(),
            overflow = new RectOffset()
        };
        SetOffset(SliderThumbStyle.padding, 0, 0, 0, 0);
    }

    private class ThemeService : IInitializable, IWarmUp
    {
        public string Name => "ThemeManager";
        public int Priority => ServicePriority.Highest;

        public void Initialize() => ThemeManager.Rescale();
        public void OnWarmUp() => ThemeManager.Rescale();
    }
}