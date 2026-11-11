using System;
using System.Collections.Generic;
using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// Bitmask flags determining which style properties are exposed and configurable in a <see cref="StyleSetting"/>.
/// </summary>
[Flags]
public enum StyleFeatures
{
    None = 0,
    TextColor = 1 << 0,
    FontSize = 1 << 1,
    Alignment = 1 << 2,
    FontStyle = 1 << 3,
    Outline = 1 << 4,
    BackgroundColor = 1 << 5,
    WordWrap = 1 << 6,
    All = TextColor | FontSize | Alignment | FontStyle | Outline | BackgroundColor | WordWrap
}

/// <summary>
/// Default baseline configuration values used when initializing or resetting a <see cref="StyleSetting"/>.
/// </summary>
public class StyleDefaults
{
    public Color TextColor { get; set; } = Color.white;
    public float FontSize { get; set; } = 20f;
    public float MinFontSize { get; set; } = 8f;
    public float MaxFontSize { get; set; } = 60f;
    public int FontSizePrecision { get; set; } = 1;
    public TextAnchor Alignment { get; set; } = TextAnchor.MiddleCenter;
    public FontStyle FontStyle { get; set; } = FontStyle.Normal;
    public bool OutlineEnabled { get; set; } = false;
    public Color OutlineColor { get; set; } = Color.black;
    public float OutlineWidth { get; set; } = 0.25f;
    public float MinOutlineWidth { get; set; } = 0f;
    public float MaxOutlineWidth { get; set; } = 0.5f;
    public int OutlineWidthPrecision { get; set; } = 2;
    public Color BackgroundColor { get; set; } = Color.clear;
    public bool WordWrap { get; set; } = false;
}

/// <summary>
/// A modular composite setting that encapsulates visual typography, color, alignment, outline,
/// and layout properties. Can generate and update an IMGUI <see cref="UnityEngine.GUIStyle"/>
/// or directly configure a <see cref="TextMeshProUGUI"/> component.
/// </summary>
public class StyleSetting : Setting
{
    private readonly StyleDefaults _defaults;
    private GUIStyle _cachedGUIStyle;
    private int _builtVersion = -1;

    /// <summary>
    /// Gets the enabled feature flags for this style setting.
    /// </summary>
    public StyleFeatures Features { get; }

    /// <summary>
    /// Whether the style group foldout is currently expanded in the UI.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// The default expanded state restored on <see cref="Reset"/>.
    /// </summary>
    public bool DefaultExpanded { get; set; }

    /// <summary>
    /// Callback triggered whenever any sub-property within this style changes.
    /// </summary>
    public Action<StyleSetting> OnStyleChanged { get; set; }

    /// <summary>
    /// Monotonically increasing revision number incremented on every property mutation.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Collection of active child settings managed by this style instance.
    /// </summary>
    public List<Setting> ChildSettings { get; } = new();

    // Child Sub-Settings
    public ColorSetting TextColorSetting { get; }
    public FloatSetting FontSizeSetting { get; }
    public SelectSetting AlignmentSetting { get; }
    public SelectSetting FontStyleSetting { get; }
    public BoolSetting EnableOutlineSetting { get; }
    public ColorSetting OutlineColorSetting { get; }
    public FloatSetting OutlineWidthSetting { get; }
    public ColorSetting BackgroundColorSetting { get; }
    public BoolSetting WordWrapSetting { get; }

    /// <summary>
    /// Gets the current resolved <see cref="UnityEngine.GUIStyle"/>, lazily regenerated when dirty.
    /// </summary>
    public GUIStyle Style => GetGUIStyle();

    #region Direct Value Properties

    public Color TextColor
    {
        get => TextColorSetting != null ? TextColorSetting.Value : _defaults.TextColor;
        set { if (TextColorSetting != null) TextColorSetting.Value = value; }
    }

    public float FontSize
    {
        get => FontSizeSetting != null ? FontSizeSetting.Value : _defaults.FontSize;
        set { if (FontSizeSetting != null) FontSizeSetting.Value = value; }
    }

    public TextAnchor Alignment
    {
        get => AlignmentSetting != null ? (TextAnchor)AlignmentSetting.Value : _defaults.Alignment;
        set { if (AlignmentSetting != null) AlignmentSetting.Value = (int)value; }
    }

    public FontStyle FontStyle
    {
        get => FontStyleSetting != null ? (FontStyle)FontStyleSetting.Value : _defaults.FontStyle;
        set { if (FontStyleSetting != null) FontStyleSetting.Value = (int)value; }
    }

    public bool OutlineEnabled
    {
        get => EnableOutlineSetting != null ? EnableOutlineSetting.Value : _defaults.OutlineEnabled;
        set { if (EnableOutlineSetting != null) EnableOutlineSetting.Value = value; }
    }

    public Color OutlineColor
    {
        get => OutlineColorSetting != null ? OutlineColorSetting.Value : _defaults.OutlineColor;
        set { if (OutlineColorSetting != null) OutlineColorSetting.Value = value; }
    }

    public float OutlineWidth
    {
        get => OutlineWidthSetting != null ? OutlineWidthSetting.Value : _defaults.OutlineWidth;
        set { if (OutlineWidthSetting != null) OutlineWidthSetting.Value = value; }
    }

    public Color BackgroundColor
    {
        get => BackgroundColorSetting != null ? BackgroundColorSetting.Value : _defaults.BackgroundColor;
        set { if (BackgroundColorSetting != null) BackgroundColorSetting.Value = value; }
    }

    public bool WordWrap
    {
        get => WordWrapSetting != null ? WordWrapSetting.Value : _defaults.WordWrap;
        set { if (WordWrapSetting != null) WordWrapSetting.Value = value; }
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of <see cref="StyleSetting"/>.
    /// </summary>
    /// <param name="name">The display name of the style section.</param>
    /// <param name="features">Which style features to activate and display.</param>
    /// <param name="defaults">Optional baseline fallback values.</param>
    /// <param name="defaultExpanded">Whether the foldout begins expanded.</param>
    public StyleSetting(
        string name,
        StyleFeatures features = StyleFeatures.All,
        StyleDefaults defaults = null,
        bool defaultExpanded = true)
    {
        Name = name;
        Features = features;
        _defaults = defaults ?? new StyleDefaults();
        DefaultExpanded = defaultExpanded;
        IsExpanded = defaultExpanded;

        if (Features.HasFlag(StyleFeatures.TextColor))
        {
            TextColorSetting = new ColorSetting("Text Color", _defaults.TextColor)
            {
                OnValueChanged = _ => MarkDirty()
            };
            ChildSettings.Add(TextColorSetting);
        }

        if (Features.HasFlag(StyleFeatures.FontSize))
        {
            FontSizeSetting = new FloatSetting(
                "Font Size",
                _defaults.MinFontSize,
                _defaults.MaxFontSize,
                _defaults.FontSize,
                _defaults.FontSizePrecision,
                1f,
                120f,
                true,
                false)
            {
                OnValueChanged = _ => MarkDirty()
            };
            ChildSettings.Add(FontSizeSetting);
        }

        if (Features.HasFlag(StyleFeatures.Alignment))
        {
            AlignmentSetting = new SelectSetting("Alignment", typeof(TextAnchor), (int)_defaults.Alignment)
            {
                OnSelectionChanged = _ => MarkDirty()
            };
            ChildSettings.Add(AlignmentSetting);
        }

        if (Features.HasFlag(StyleFeatures.FontStyle))
        {
            FontStyleSetting = new SelectSetting("Font Style", typeof(FontStyle), (int)_defaults.FontStyle)
            {
                OnSelectionChanged = _ => MarkDirty()
            };
            ChildSettings.Add(FontStyleSetting);
        }

        if (Features.HasFlag(StyleFeatures.Outline))
        {
            EnableOutlineSetting = new BoolSetting("Text Outline", _defaults.OutlineEnabled)
            {
                OnValueChanged = _ => MarkDirty()
            };
            OutlineColorSetting = new ColorSetting("Outline Color", _defaults.OutlineColor)
            {
                OnValueChanged = _ => MarkDirty()
            };
            OutlineWidthSetting = new FloatSetting(
                "Outline Width",
                _defaults.MinOutlineWidth,
                _defaults.MaxOutlineWidth,
                _defaults.OutlineWidth,
                _defaults.OutlineWidthPrecision,
                0f,
                1f,
                true,
                false)
            {
                OnValueChanged = _ => MarkDirty()
            };

            ChildSettings.Add(EnableOutlineSetting);
            ChildSettings.Add(OutlineColorSetting);
            ChildSettings.Add(OutlineWidthSetting);
        }

        if (Features.HasFlag(StyleFeatures.BackgroundColor))
        {
            BackgroundColorSetting = new ColorSetting("Background Color", _defaults.BackgroundColor)
            {
                OnValueChanged = _ => MarkDirty()
            };
            ChildSettings.Add(BackgroundColorSetting);
        }

        if (Features.HasFlag(StyleFeatures.WordWrap))
        {
            WordWrapSetting = new BoolSetting("Word Wrap", _defaults.WordWrap)
            {
                OnValueChanged = _ => MarkDirty()
            };
            ChildSettings.Add(WordWrapSetting);
        }
    }

    /// <summary>
    /// Customizes the display label of a specific child setting.
    /// </summary>
    public StyleSetting SetChildName(StyleFeatures feature, string customName)
    {
        switch (feature)
        {
            case StyleFeatures.TextColor:
                if (TextColorSetting != null) TextColorSetting.CustomName = customName;
                break;
            case StyleFeatures.FontSize:
                if (FontSizeSetting != null) FontSizeSetting.CustomName = customName;
                break;
            case StyleFeatures.Alignment:
                if (AlignmentSetting != null) AlignmentSetting.CustomName = customName;
                break;
            case StyleFeatures.FontStyle:
                if (FontStyleSetting != null) FontStyleSetting.CustomName = customName;
                break;
            case StyleFeatures.BackgroundColor:
                if (BackgroundColorSetting != null) BackgroundColorSetting.CustomName = customName;
                break;
            case StyleFeatures.WordWrap:
                if (WordWrapSetting != null) WordWrapSetting.CustomName = customName;
                break;
        }
        return this;
    }

    /// <summary>
    /// Indicates whether this style setting or any of its child controls currently has input focus.
    /// </summary>
    public override bool IsFocused
    {
        get
        {
            if (base.IsFocused) return true;
            if (ChildSettings != null)
            {
                for (int i = 0; i < ChildSettings.Count; i++)
                {
                    if (ChildSettings[i]?.IsFocused == true) return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Marks the style dirty, incrementing <see cref="Version"/> and firing <see cref="OnStyleChanged"/>.
    /// </summary>
    public void MarkDirty()
    {
        Version++;
        OnStyleChanged?.Invoke(this);
    }

    /// <summary>
    /// Restores all child settings and expansion state to their defaults.
    /// </summary>
    public override void Reset()
    {
        if (IsDisabled) return;
        IsExpanded = DefaultExpanded;
        for (int i = 0; i < ChildSettings.Count; i++)
        {
            ChildSettings[i]?.Reset();
        }
        MarkDirty();
    }

    /// <summary>
    /// Returns a cached <see cref="GUIStyle"/> synchronized with the current setting values.
    /// </summary>
    public GUIStyle GetGUIStyle()
    {
        if (_cachedGUIStyle == null)
        {
            _cachedGUIStyle = new GUIStyle();
        }

        if (_builtVersion != Version)
        {
            UpdateGUIStyle(_cachedGUIStyle);
            _builtVersion = Version;
        }

        return _cachedGUIStyle;
    }

    /// <summary>
    /// Applies this style's current active features directly into an existing <see cref="GUIStyle"/>.
    /// </summary>
    public void UpdateGUIStyle(GUIStyle style)
    {
        if (style == null) return;

        if (Features.HasFlag(StyleFeatures.TextColor) && TextColorSetting != null)
        {
            style.normal.textColor = TextColor;
        }

        if (Features.HasFlag(StyleFeatures.FontSize) && FontSizeSetting != null)
        {
            style.fontSize = Mathf.Max(1, (int)Mathf.Round(FontSize));
        }

        if (Features.HasFlag(StyleFeatures.Alignment) && AlignmentSetting != null)
        {
            style.alignment = Alignment;
        }

        if (Features.HasFlag(StyleFeatures.FontStyle) && FontStyleSetting != null)
        {
            style.fontStyle = FontStyle;
        }

        if (Features.HasFlag(StyleFeatures.WordWrap) && WordWrapSetting != null)
        {
            style.wordWrap = WordWrap;
        }

        if (Features.HasFlag(StyleFeatures.BackgroundColor) && BackgroundColorSetting != null)
        {
            style.normal.background = ThemeData.GetTex(BackgroundColor);
        }
    }

    /// <summary>
    /// Applies active font size, color, alignment, and outline settings to a <see cref="TextMeshProUGUI"/> component.
    /// </summary>
    public void ApplyTo(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        if (Features.HasFlag(StyleFeatures.TextColor) && TextColorSetting != null)
        {
            tmp.color = TextColor;
        }

        if (Features.HasFlag(StyleFeatures.FontSize) && FontSizeSetting != null)
        {
            tmp.fontSize = FontSize;
        }

        if (Features.HasFlag(StyleFeatures.Alignment) && AlignmentSetting != null)
        {
            tmp.alignment = ConvertTextAnchorToTMP(Alignment);
        }

        if (Features.HasFlag(StyleFeatures.FontStyle) && FontStyleSetting != null)
        {
            tmp.fontStyle = ConvertFontStyleToTMP(FontStyle);
        }

        if (Features.HasFlag(StyleFeatures.Outline) && EnableOutlineSetting != null)
        {
            // Access fontMaterial so TMP clones an instance material rather than mutating shared game font materials.
            Material mat = tmp.fontMaterial;
            if (OutlineEnabled)
            {
                if (mat != null)
                {
                    mat.EnableKeyword("OUTLINE_ON");
                }
                if (OutlineWidthSetting != null)
                {
                    tmp.outlineWidth = OutlineWidth;
                }
                if (OutlineColorSetting != null)
                {
                    tmp.outlineColor = (Color32)OutlineColor;
                }
            }
            else
            {
                tmp.outlineWidth = 0f;
                if (mat != null)
                {
                    mat.DisableKeyword("OUTLINE_ON");
                }
            }
        }
    }

    /// <summary>
    /// Renders text with the configured outline and text style using <see cref="GUIHelper.DrawBoxWithOutlinedText"/>.
    /// </summary>
    public void DrawOutlinedText(Rect position, string text)
    {
        GUIStyle style = GetGUIStyle();
        Color textCol = Features.HasFlag(StyleFeatures.TextColor) ? TextColor : Color.white;
        Color outlineCol = Features.HasFlag(StyleFeatures.Outline) ? OutlineColor : Color.black;
        int radius = Features.HasFlag(StyleFeatures.Outline)
            ? Mathf.Max(1, (int)Mathf.Round(OutlineWidth))
            : 1;

        if (Features.HasFlag(StyleFeatures.Outline) && !OutlineEnabled)
        {
            GUI.Label(position, text, style);
        }
        else
        {
            GUIHelper.DrawBoxWithOutlinedText(position, text, style, textCol, outlineCol, radius);
        }
    }

    /// <summary>
    /// Converts a Unity <see cref="TextAnchor"/> into a TextMeshPro <see cref="TextAlignmentOptions"/>.
    /// </summary>
    public static TextAlignmentOptions ConvertTextAnchorToTMP(TextAnchor anchor) => anchor switch
    {
        TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
        TextAnchor.UpperCenter => TextAlignmentOptions.Top,
        TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
        TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
        TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
        TextAnchor.MiddleRight => TextAlignmentOptions.Right,
        TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
        TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
        TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
        _ => TextAlignmentOptions.Center
    };

    /// <summary>
    /// Converts a Unity <see cref="FontStyle"/> into a TextMeshPro <see cref="FontStyles"/>.
    /// </summary>
    public static FontStyles ConvertFontStyleToTMP(FontStyle style) => style switch
    {
        FontStyle.Bold => FontStyles.Bold,
        FontStyle.Italic => FontStyles.Italic,
        FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
        _ => FontStyles.Normal
    };

    /// <summary>
    /// Renders the style foldout header and child controls into the IMGUI layout.
    /// </summary>
    public override void Draw(ref float y, float width)
    {
        bool prevGUIEnabled = GUI.enabled;
        if (IsDisabled) GUI.enabled = false;
        try
        {
            float indent = ClientConfig.indent;
            float gap = ClientConfig.spacing;
            float elemH = GUIManager.S(24f);
            float actionBtnW = GUIManager.S(24f);

            Event e = Event.current;
            float headerTotalW = width - (indent * 2f);

            Rect headerRect;
            Rect resetRect;

            if (CanReset)
            {
                float resetStartX = indent + headerTotalW - actionBtnW;
                float headerW = headerTotalW - actionBtnW - gap;
                headerRect = new Rect(indent, y, headerW, elemH);
                resetRect = new Rect(resetStartX, y, actionBtnW, elemH);
            }
            else
            {
                headerRect = new Rect(indent, y, headerTotalW, elemH);
                resetRect = Rect.zero;
            }

            string foldArrow = IsExpanded ? "▼ " : "▶ ";
            string label = foldArrow + DisplayName;

            GUI.Box(headerRect, label, ThemeManager.SectionHeaderStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && headerRect.Contains(e.mousePosition))
            {
                e.Use();
                IsExpanded = !IsExpanded;
            }

            if (!IsDisabled && CanReset && DrawResetButton(resetRect))
            {
                Reset();
            }

            y += elemH + gap;

            if (IsExpanded)
            {
                for (int i = 0; i < ChildSettings.Count; i++)
                {
                    var child = ChildSettings[i];
                    if (child == null) continue;

                    // Conditionally hide outline sub-settings when text outline is off
                    if ((child == OutlineColorSetting || child == OutlineWidthSetting) &&
                        EnableOutlineSetting != null && !EnableOutlineSetting.Value)
                    {
                        continue;
                    }

                    bool childWasDisabled = child.IsDisabled;
                    if (IsDisabled) child.IsDisabled = true;
                    try
                    {
                        child.Draw(ref y, width);
                    }
                    finally
                    {
                        if (IsDisabled) child.IsDisabled = childWasDisabled;
                    }
                }
            }
        }
        finally
        {
            GUI.enabled = prevGUIEnabled;
        }
    }
}
