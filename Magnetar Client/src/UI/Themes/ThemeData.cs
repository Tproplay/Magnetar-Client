using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.Themes;

[Serializable]
public class ThemeDefinition
{
    public string Name { get; set; } = "Magnetar Default";

    // --- TopBar ---
    public string TopBarOffBgHex { get; set; } = "#1A1A1AFF";
    public string TopBarOffTextHex { get; set; } = "#AEAEAEFF";
    public string TopBarActiveBgHex { get; set; } = "#FF3D3DFF";
    public string TopBarActiveTextHex { get; set; } = "#FFFFFFFF";

    // --- Category Windows ---
    public string CategoryWindowBgHex { get; set; } = "#181818FF";
    public string CategoryWindowTextHex { get; set; } = "#FFFFFFFF";
    public string CategoryHeaderBgHex { get; set; } = "#222222FF";
    public string CategoryHeaderTextHex { get; set; } = "#FFFFFFFF";
    public string CategoryModuleOffBgHex { get; set; } = "#1E1E1EFF";
    public string CategoryModuleOffTextHex { get; set; } = "#AEAEAEFF";
    public string CategoryModuleOnBgHex { get; set; } = "#FF3D3DFF";
    public string CategoryModuleOnTextHex { get; set; } = "#000000FF";

    // --- Settings Window ---
    public string SettingsWindowBgHex { get; set; } = "#181818FF";
    public string SettingsHeaderBgHex { get; set; } = "#FF3D3DFF";
    public string SettingsHeaderTextHex { get; set; } = "#000000FF";
    public string CloseBtnBgHex { get; set; } = "#FF3D3DFF";
    public string CloseBtnTextHex { get; set; } = "#000000FF";

    // --- Setting Elements ---
    public string SettingOffBgHex { get; set; } = "#222222FF";
    public string SettingOffTextHex { get; set; } = "#AEAEAEFF";
    public string SettingOnBgHex { get; set; } = "#FF3D3DFF";
    public string SettingOnTextHex { get; set; } = "#000000FF";

    // --- Action Buttons ---
    public string ButtonSettingBgHex { get; set; } = "#262626FF";
    public string ButtonSettingTextHex { get; set; } = "#FFFFFFFF";
    public string ResetBtnBgHex { get; set; } = "#242424FF";
    public string ResetBtnTextHex { get; set; } = "#D0D0D0FF";
    public string ListAddBtnBgHex { get; set; } = "#FF3D3DFF";
    public string ListAddBtnTextHex { get; set; } = "#000000FF";
    public string ListRemoveBtnBgHex { get; set; } = "#2E1A1AFF";
    public string ListRemoveBtnTextHex { get; set; } = "#FF6B6BFF";

    // --- Section Settings ---
    public string SectionGroupHeaderBgHex { get; set; } = "#1C1C1CFF";
    public string SectionGroupHeaderTextHex { get; set; } = "#FFFFFFFF";
    public string SectionHeaderBgHex { get; set; } = "#1E1E1EFF";
    public string SectionHeaderTextHex { get; set; } = "#AEAEAEFF";
    public string SectionRemoveBtnBgHex { get; set; } = "#2E1A1AFF";
    public string SectionRemoveBtnTextHex { get; set; } = "#FF6B6BFF";
    public string SectionAddBtnBgHex { get; set; } = "#FF3D3DFF";
    public string SectionAddBtnTextHex { get; set; } = "#000000FF";

    // --- Typography ---
    public string TypographyDescriptionHex { get; set; } = "#BFBFBFFF";
    public string TypographyLabelHex { get; set; } = "#E6E6E6FF";
    public string TypographyAuthorHex { get; set; } = "#808080FF";
    public string TypographyTextHex { get; set; } = "#FFFFFFFF";
    public string TypographySecondaryHex { get; set; } = "#AEAEAEFF";
    public string TypographyHighlightTextHex { get; set; } = "#FFFFFFFF";
    public string TypographyHighlightBgHex { get; set; } = "#FF3D3DFF";

    // --- NEF & HUD ---
    public string NefLineColorHex { get; set; } = "#FFFFFFFF";
    public string NefNodeBgHex { get; set; } = "#FF3D3DFF";
    public string HudTextColorHex { get; set; } = "#FFFFFFFF";

    // --- Misc & Sliders ---
    public string DimBgHex { get; set; } = "#14141A99"; // Consistent dark veil
    public string SeparatorHex { get; set; } = "#383838FF";
    public string SeparatorTextHex { get; set; } = "#AEAEAEFF";
    public string SliderTrackOffHex { get; set; } = "#22252FFF";
    public string SliderTrackOnHex { get; set; } = "#FF3D3DFF";
    public string SliderThumbHex { get; set; } = "#FF3D3DFF";
}

public static class ThemeData
{
    // Resolved Runtime Colors per Style (Fully opaque panels to allow clean alpha transitions)
    public static Color TopBarOffBg = new(0.10f, 0.10f, 0.10f, 1.0f);
    public static Color TopBarOffText = new(0.68f, 0.68f, 0.68f, 1.0f);
    public static Color TopBarActiveBg = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color TopBarActiveText = Color.white;

    public static Color CategoryWindowBg = new(0.09f, 0.09f, 0.09f, 1.0f);
    public static Color CategoryWindowText = Color.white;
    public static Color CategoryHeaderBg = new(0.13f, 0.13f, 0.13f, 1.0f);
    public static Color CategoryHeaderText = Color.white;
    public static Color CategoryModuleOffBg = new(0.12f, 0.12f, 0.12f, 1.0f);
    public static Color CategoryModuleOffText = new(0.68f, 0.68f, 0.68f, 1.0f);
    public static Color CategoryModuleOnBg = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color CategoryModuleOnText = Color.black;

    public static Color SettingsWindowBg = new(0.09f, 0.09f, 0.09f, 1.0f);
    public static Color SettingsHeaderBg = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color SettingsHeaderText = Color.black;
    public static Color CloseBtnBg = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color CloseBtnText = Color.black;

    public static Color SettingOffBg = new(0.13f, 0.13f, 0.13f, 1.0f);
    public static Color SettingOffText = new(0.68f, 0.68f, 0.68f, 1.0f);
    public static Color SettingOnBg = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color SettingOnText = Color.black;

    public static Color ButtonSettingBg = new(0.15f, 0.15f, 0.15f, 1.0f);
    public static Color ButtonSettingText = Color.white;
    public static Color ResetBtnBg = new(0.14f, 0.14f, 0.14f, 1.0f);
    public static Color ResetBtnText = new(0.82f, 0.82f, 0.82f, 1.0f);
    public static Color ListAddBtnBg = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color ListAddBtnText = Color.black;
    public static Color ListRemoveBtnBg = new(0.18f, 0.10f, 0.10f, 1.0f);
    public static Color ListRemoveBtnText = new(1.0f, 0.42f, 0.42f, 1.0f);

    public static Color SectionGroupHeaderBg = new(0.11f, 0.11f, 0.11f, 1.0f);
    public static Color SectionGroupHeaderText = Color.white;
    public static Color SectionHeaderBg = new(0.12f, 0.12f, 0.12f, 1.0f);
    public static Color SectionHeaderText = new(0.68f, 0.68f, 0.68f, 1.0f);
    public static Color SectionRemoveBtnBg = new(0.18f, 0.10f, 0.10f, 1.0f);
    public static Color SectionRemoveBtnText = new(1.0f, 0.42f, 0.42f, 1.0f);
    public static Color SectionAddBtnBg = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color SectionAddBtnText = Color.black;

    public static Color TypographyDescription = new(0.75f, 0.75f, 0.75f, 1.0f);
    public static Color TypographyLabel = new(0.90f, 0.90f, 0.90f, 1.0f);
    public static Color TypographyAuthor = new(0.50f, 0.50f, 0.50f, 1.0f);
    public static Color TypographyText = Color.white;
    public static Color TypographySecondary = new(0.68f, 0.68f, 0.68f, 1.0f);
    public static Color TypographyHighlightText = Color.white;
    public static Color TypographyHighlightBg = new(1.0f, 0.24f, 0.24f, 1.0f);

    public static Color NefLineColor = Color.white;
    public static Color NefNodeBg = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color HudTextColor = Color.white;

    // Overlay remains the sole translucent texture
    public static Color DimBg = new(0.08f, 0.08f, 0.10f, 0.60f);
    public static Color Separator = new(0.22f, 0.22f, 0.22f, 1.0f);
    public static Color SeparatorText = new(0.68f, 0.68f, 0.68f, 1.0f);
    public static Color SliderTrackOff = new(0.13f, 0.15f, 0.18f, 1.0f);
    public static Color SliderTrackOn = new(1.0f, 0.24f, 0.24f, 1.0f);
    public static Color SliderThumb = new(1.0f, 0.24f, 0.24f, 1.0f);

    // Default Fallback Template
    public static readonly ThemeDefinition InternalDefaultTheme = new();

    // Procedural Solid Textures per Style
    public static Texture2D TopBarOffBgTex;
    public static Texture2D TopBarActiveBgTex;
    public static Texture2D CategoryWindowBgTex;
    public static Texture2D CategoryHeaderBgTex;
    public static Texture2D CategoryModuleOffBgTex;
    public static Texture2D CategoryModuleOnBgTex;
    public static Texture2D SettingsWindowBgTex;
    public static Texture2D SettingsHeaderBgTex;
    public static Texture2D CloseBtnBgTex;
    public static Texture2D SettingOffBgTex;
    public static Texture2D SettingOnBgTex;
    public static Texture2D ButtonSettingBgTex;
    public static Texture2D ResetBtnBgTex;
    public static Texture2D ListAddBtnBgTex;
    public static Texture2D ListRemoveBtnBgTex;
    public static Texture2D SectionGroupHeaderBgTex;
    public static Texture2D SectionHeaderBgTex;
    public static Texture2D SectionRemoveBtnBgTex;
    public static Texture2D SectionAddBtnBgTex;
    public static Texture2D HighlightBgTex;
    public static Texture2D NefLineTex;
    public static Texture2D NefNodeTex;
    public static Texture2D DimBgTex;
    public static Texture2D SeparatorTex;
    public static Texture2D SliderTrackOffTex;
    public static Texture2D SliderTrackOnTex;
    public static Texture2D SliderThumbTex;

    // Registry of loaded themes
    public static readonly Dictionary<string, ThemeDefinition> LoadedThemes = new(StringComparer.OrdinalIgnoreCase);

    public static Texture2D Create1x1Tex(Color color)
    {
        Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
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
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
            }
        }

        tex.Apply(false);
        _circleTextureCache[key] = tex;
        return tex;
    }
}