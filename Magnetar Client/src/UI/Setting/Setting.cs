using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public abstract class Setting
{
    public string Name;
    public bool IsDisabled { get; set; } = false;
    public virtual bool CanReset => true;

    public const string ResetSymbol = "R";



    /// <summary>
    /// The TranslationDomain used by this setting.
    /// </summary>
    public TranslationDomain Domain;
    /// <summary>
    /// Relative file(s) or folder(s) to search for translations (configured during setting creation).
    /// </summary>
    public string[] TranslationSources { get; set; }

    /// <summary>
    /// Convenience helper to set or get a single relative path without creating an array manually.
    /// </summary>
    public string TranslationSource
    {
        get => (TranslationSources != null && TranslationSources.Length > 0) ? TranslationSources[0] : null;
        set => TranslationSources = string.IsNullOrEmpty(value) ? null : new[] { value };
    }

    /// <summary>
    /// Translates text using the domain and sources configured on this setting.
    /// </summary>
    public string Translate(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        if (Domain != null)
        {
            string translated = Domain.Translate(text, TranslationSources);
            if (!string.Equals(translated, text, System.StringComparison.Ordinal))
                return translated;
        }

        return SettingsDrawerTranslation.Domain.Translate(text, "common.json");
    }

    public virtual void Reset() { }
   
    public abstract void Draw(ref float y, float width);

    public static bool DrawResetButton(Rect rect)
    {
        Event e = Event.current;
        bool isHovered = rect.Contains(e.mousePosition);

        GUI.Box(rect, ResetSymbol, ThemeManager.ResetButtonStyle);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            e.Use();
            return true;
        }

        return false;
    }
}