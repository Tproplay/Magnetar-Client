using System.Collections.Generic;
using Magnetar_Client.Utils;

namespace Magnetar_Client.UI.WindowDrawing;

public static class SettingsDrawerTranslation
{
    // Dedicated domain for UI Settings Drawers
    public static readonly TranslationDomain Domain = Translator.CreateDomain("SettingsDrawer");

    /// <summary>
    /// Translates common UI text scoped to the settings drawer translation file.
    /// </summary>
    public static string T(string text) => Domain.Translate(text, "common.json");

    public static void Init()
    {
        Domain.OnDumpEnglishTemplate += DumpEnglishTemplates;
    }

    private static void DumpEnglishTemplates(string englishDir)
    {
        string[] commonStrings = new[]
        {
            "ON",
            "OFF",
            "Enabled",
            "Disabled",
            "Reset",
            "Press any key...",
            "None",
            "Select",
            "Deselect",
            "Select All",
            "Deselect All",
            "Search...",
            "Clear",
            "Close",
            "Back",
            "Add",
            "Remove",
            "Edit",
            "Done",
            "Default"
        };

        Dictionary<string, string> dict = Translator.CreateDictionary(commonStrings);
        Translator.SaveJson(englishDir, "common.json", dict);
    }
}