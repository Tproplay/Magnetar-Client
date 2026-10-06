using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

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

[Serializable]
public class SectionSettingTheme
{
    [JsonProperty("group header", NullValueHandling = NullValueHandling.Ignore)]
    public ElementStyleTheme GroupHeader { get; set; }

    [JsonProperty("section header", NullValueHandling = NullValueHandling.Ignore)]
    public ElementStyleTheme SectionHeader { get; set; }

    [JsonProperty("remove button", NullValueHandling = NullValueHandling.Ignore)]
    public ElementStyleTheme RemoveButton { get; set; }

    [JsonProperty("add button", NullValueHandling = NullValueHandling.Ignore)]
    public ElementStyleTheme AddButton { get; set; }
}

[Serializable]
public class ThemeDefinition
{
    [JsonProperty("name")]
    public string Name { get; set; } = "Magnetar Default";

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

    [JsonProperty("SettingsWindow")]
    public WindowStyleTheme SettingsWindow { get; set; } = new WindowStyleTheme();

    [JsonProperty("CloseButton")]
    public ElementStyleTheme CloseButton { get; set; } = new ElementStyleTheme();

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

    [JsonProperty("Section", NullValueHandling = NullValueHandling.Ignore)]
    public SectionSettingTheme Section { get; set; }
}
#endregion

public static class ThemeData
{
    // Resolved Runtime Colors per Style
    public static Color BackgroundColor { get; set; }
    public static Color AccentColor { get; set; }
    public static Color AccentHoverColor { get; set; }
    public static Color LightBackgroundColor { get; set; }
    public static Color TextWhite { get; set; }
    public static Color TextDim { get; set; }
    public static Color HoverColor { get; set; }
    public static Color ActiveColor { get; set; }
    public static Color DimColor { get; set; }
    public static Color NefLineColor { get; set; }
    public static Color NefNodeColor { get; set; }
    public static Color SettingOnColor { get; set; }
    public static Color SettingOffColor { get; set; }
    public static Color SeparatorColor { get; set; }

    // --- Hardcoded Default Theme (Safety Fallback) ---
    public static readonly ThemeDefinition InternalDefaultTheme = new()
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
        SettingsWindow = new WindowStyleTheme("#000000FF", "#FF3D3DFF", "#1A1A1AEE"),
        SettingOff = new ElementStyleTheme(
            new ColorState("#AEAEAEFF", "#FFFFFFFF", "#FFFFFFFF"),
            new ColorState("#242424DC", "#333333FF", "#1C1C1CFF")
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
            new ColorState("#000000FF", "#000000FF", "#000000FF"),
            new ColorState("#FF3D3DFF", "#F03333FF", "#F03333FF")
        ),
        ListRemoveButton = new ElementStyleTheme(
            new ColorState("#FF6B6BFF", "#FF8E8EFF", "#FF3D3DFF"),
            new ColorState("#2B1818DC", "#3D1E1EFF", "#201212FF")
        ),
        Section = new SectionSettingTheme
        {
            GroupHeader = new ElementStyleTheme(
                new ColorState("#FFFFFFFF", "#FFFFFFFF", "#FFFFFFFF"),
                new ColorState("#1A1A1ADC", "#242424FF", "#181818FF")
            ),
            SectionHeader = new ElementStyleTheme(
                new ColorState("#AEAEAEFF", "#FFFFFFFF", "#FFFFFFFF"),
                new ColorState("#1C1C1CD6", "#2A2A2AFF", "#161616FF")
            ),
            RemoveButton = new ElementStyleTheme(
                new ColorState("#FF6B6BFF", "#FF8E8EFF", "#FF3D3DFF"),
                new ColorState("#2B1818DC", "#3D1E1EFF", "#201212FF")
            ),
            AddButton = new ElementStyleTheme(
                new ColorState("#000000FF", "#000000FF", "#000000FF"),
                new ColorState("#FF3D3DFF", "#F03333FF", "#F03333FF")
            )
        },
        Typography = new TypographyTheme
        {
            Description = "#BFBFBFFF",
            Label = "#E6E6E6FF",
            Author = "#808080FF",
            Text = "#FFFFFFFF",
            Secondary = "#FFFFFFFF",
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
        }
    };

    // --- Hardcoded Template for Meteor Purple (Dumped to Disk) ---
    public static readonly ThemeDefinition MeteorPurpleTemplate = new()
    {
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
        Section = new SectionSettingTheme
        {
            GroupHeader = new ElementStyleTheme(
                new ColorState("#E6EDF3FF", "#FFFFFFFF", "#FFFFFFFF"),
                new ColorState("#7d00f1", "#9a2eff", "#9a2eff")
            ),
            SectionHeader = new ElementStyleTheme(
                new ColorState("#8B949EFF", "#e6e6e6ea", "#e6e6e6ea"),
                new ColorState("#1D1226CC", "#2C173DFF", "#22142594")
            ),
            RemoveButton = new ElementStyleTheme(
                new ColorState("#FF77BCFF", "#FFA6D2FF", "#E11D48FF"),
                new ColorState("#2D1022CC", "#4A1835FF", "#1F0A17FF")
            ),
            AddButton = new ElementStyleTheme(
                new ColorState("#000000FF", "#000000FF", "#000000FF"),
                new ColorState("#8B0FFFFF", "#992cff", "#8B0FFFFF")
            )
        },
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
    };

    public static readonly Dictionary<string, ThemeDefinition> LoadedThemes = new(StringComparer.OrdinalIgnoreCase);

    #region Color Parsing & Texture Cache Helpers
    public static (Color normal, Color hover, Color active) ResolveState(ColorState state, ColorState fallback)
    {
        string normHex = !string.IsNullOrEmpty(state?.Normal) ? state.Normal : fallback?.Normal;
        string hovHex = !string.IsNullOrEmpty(state?.Hover) ? state.Hover : (!string.IsNullOrEmpty(state?.Normal) ? state.Normal : fallback?.Hover);
        string actHex = !string.IsNullOrEmpty(state?.Active) ? state.Active : (!string.IsNullOrEmpty(state?.Normal) ? state.Normal : fallback?.Active);

        Color normal = ParseColor(normHex, fallback?.Normal ?? "#FFFFFFFF");
        Color hover = ParseColor(hovHex, fallback?.Hover ?? normHex);
        Color active = ParseColor(actHex, fallback?.Active ?? normHex);

        return (normal, hover, active);
    }

    public static Color ParseColor(string hex, string defaultHex)
    {
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color col))
            return col;

        ColorUtility.TryParseHtmlString(defaultHex, out Color defCol);
        return defCol;
    }

    private static readonly Dictionary<Color, Texture2D> _texCache = new();

    public static Texture2D GetTex(Color col)
    {
        if (_texCache.TryGetValue(col, out var tex) && tex != null)
            return tex;

        Texture2D newTex = new(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.DontSave
        };
        Color[] pixels = new Color[4] { col, col, col, col };
        newTex.SetPixels(pixels);
        newTex.Apply(false, false);

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
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
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

        tex.Apply(false, false);
        _circleTextureCache[key] = tex;
        return tex;
    }
    #endregion
}