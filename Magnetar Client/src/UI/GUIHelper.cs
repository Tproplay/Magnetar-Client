using System.Text.RegularExpressions;
using UnityEngine;

namespace Magnetar_Client.UI;

/// <summary>
/// Contains helper methods for GUI Creation.
/// </summary>
public static class GUIHelper
{
    /// <summary>
    /// Regex to match any rich text tag
    /// </summary>
    public static readonly Regex RichTextTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);

    /// <summary>
    /// Regex to match color tags
    /// </summary>
    public static readonly Regex ColorTagRegex = new(@"<\/?color(=[^>]+)?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static GUIStyle _cachedOutlineStyle;

    /// <summary>
    /// Renders text with an 8-directional drop outline around its perimeter.
    /// </summary>
    /// <param name="position">The bounding rectangle on the screen where the text should be rendered.</param>
    /// <param name="text">The raw or rich-text formatted string to draw.</param>
    /// <param name="style">The base <see cref="GUIStyle"/> to inherit typography, font settings, and alignment from.</param>
    /// <param name="textColor">The main color used for the foreground center text.</param>
    /// <param name="outlineColor">The color used to draw the background outline perimeter.</param>
    /// <param name="borderRadius">The pixel offset distance for the surrounding outline passes.</param>
    public static void DrawBoxWithOutlinedText(
        Rect position, string text, GUIStyle style, Color textColor, Color outlineColor, int borderRadius = 1)
    {
        if (string.IsNullOrEmpty(text)) return;

        _cachedOutlineStyle ??= new GUIStyle();

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

    internal static void OnUpdate()
    {
        UpdateRainbowColor();
    }

    private static void UpdateRainbowColor()
    {
        float hue = (Time.time * ClientConfig.RainbowSpeed) % 1.0f;

        RainbowColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
    }

}
