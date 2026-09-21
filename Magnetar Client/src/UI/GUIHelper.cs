using System.Text.RegularExpressions;
using UnityEngine;

namespace Magnetar_Client.UI;

public static class GUIHelper
{
    public static readonly Regex RichTextTagRegex = new Regex(@"<[^>]*>", RegexOptions.Compiled);

    /// <summary>
    /// Regex to match opening color tags like <color=red>, <color=#FF0000> and closing </color> tags
    /// </summary>
    public static readonly Regex ColorTagRegex = new Regex(@"<\/?color(=[^>]+)?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void DrawBoxWithOutlinedText(
        Rect position, string text, GUIStyle style, Color textColor, Color outlineColor, int borderRadius = 1
        )
    {
        GUIStyle outlineStyle = new GUIStyle(style);
        outlineStyle.richText = true;

        string outlineText = ColorTagRegex.Replace(text, "");
        outlineStyle.normal.textColor = outlineColor;

        // 3. Draw the outline 8 times using the cleaned text
        GUI.Label(new Rect(position.x - borderRadius, position.y - borderRadius, position.width, position.height), outlineText, outlineStyle);
        GUI.Label(new Rect(position.x + borderRadius, position.y - borderRadius, position.width, position.height), outlineText, outlineStyle);
        GUI.Label(new Rect(position.x - borderRadius, position.y + borderRadius, position.width, position.height), outlineText, outlineStyle);
        GUI.Label(new Rect(position.x + borderRadius, position.y + borderRadius, position.width, position.height), outlineText, outlineStyle);

        GUI.Label(new Rect(position.x - borderRadius, position.y, position.width, position.height), outlineText, outlineStyle);
        GUI.Label(new Rect(position.x + borderRadius, position.y, position.width, position.height), outlineText, outlineStyle);
        GUI.Label(new Rect(position.x, position.y - borderRadius, position.width, position.height), outlineText, outlineStyle);
        GUI.Label(new Rect(position.x, position.y + borderRadius, position.width, position.height), outlineText, outlineStyle);

        outlineStyle.normal.textColor = textColor;
        GUI.Label(position, text, outlineStyle);
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
