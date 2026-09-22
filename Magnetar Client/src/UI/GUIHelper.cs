using System.Text.RegularExpressions;
using UnityEngine;

namespace Magnetar_Client.UI;

public static class GUIHelper
{
    public static readonly Regex RichTextTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);

    /// <summary>
    /// Regex to match opening color tags like <color=red>, <color=#FF0000> and closing </color> tags
    /// </summary>
    public static readonly Regex ColorTagRegex = new(@"<\/?color(=[^>]+)?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static GUIStyle _cachedOutlineStyle;

    public static void DrawBoxWithOutlinedText(
        Rect position, string text, GUIStyle style, Color textColor, Color outlineColor, int borderRadius = 1)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_cachedOutlineStyle == null)
        {
            _cachedOutlineStyle = new GUIStyle();
        }

        // Copy relevant typography and layout properties without invoking stripped copy-constructors
        _cachedOutlineStyle.alignment = style.alignment;
        _cachedOutlineStyle.fontSize = style.fontSize;
        _cachedOutlineStyle.fontStyle = style.fontStyle;
        _cachedOutlineStyle.font = style.font;
        _cachedOutlineStyle.wordWrap = style.wordWrap;
        _cachedOutlineStyle.clipping = style.clipping;
        _cachedOutlineStyle.richText = true;

        // Strip rich-text color tags for the outline passes
        string outlineText = ColorTagRegex != null ? ColorTagRegex.Replace(text, "") : text;
        _cachedOutlineStyle.normal.textColor = outlineColor;

        // Draw the 8-directional outline
        GUI.Label(new Rect(position.x - borderRadius, position.y - borderRadius, position.width, position.height), outlineText, _cachedOutlineStyle);
        GUI.Label(new Rect(position.x + borderRadius, position.y - borderRadius, position.width, position.height), outlineText, _cachedOutlineStyle);
        GUI.Label(new Rect(position.x - borderRadius, position.y + borderRadius, position.width, position.height), outlineText, _cachedOutlineStyle);
        GUI.Label(new Rect(position.x + borderRadius, position.y + borderRadius, position.width, position.height), outlineText, _cachedOutlineStyle);

        GUI.Label(new Rect(position.x - borderRadius, position.y, position.width, position.height), outlineText, _cachedOutlineStyle);
        GUI.Label(new Rect(position.x + borderRadius, position.y, position.width, position.height), outlineText, _cachedOutlineStyle);
        GUI.Label(new Rect(position.x, position.y - borderRadius, position.width, position.height), outlineText, _cachedOutlineStyle);
        GUI.Label(new Rect(position.x, position.y + borderRadius, position.width, position.height), outlineText, _cachedOutlineStyle);

        // Draw the primary center text
        _cachedOutlineStyle.normal.textColor = textColor;
        GUI.Label(position, text, _cachedOutlineStyle);
    }
    /// <summary>
    /// Get a Color which automatically changes color in a rainbow pattern
    /// </summary>
    public static Color RainbowColor { get; private set; }

    public static void _UpdateRainbowColor()
    {
        float hue = (Time.time * Config.RainbowSpeed) % 1.0f;

        RainbowColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
    }

}
