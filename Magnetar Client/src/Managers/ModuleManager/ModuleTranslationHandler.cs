using Magnetar_Client.Api;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core.ModuleManager_;

public static class ModuleTranslationHandler
{

    #region Lifecycle & Translation Reloading

    public static void OnLanguageChanged()
    {
        string currentLang = Config.Language ?? "English";

        if (ModuleManager.Modules != null)
        {
            for (int i = 0; i < ModuleManager.Modules.Count; i++)
            {
                var mod = ModuleManager.Modules[i];
                if (mod == null) continue;

                ReloadModuleTranslations(mod, currentLang);
                mod.OnLanguageChanged();
            }
        }
    }

    public static void ReloadModuleTranslations(Modules.Module mod, string language)
    {
        if (mod == null) return;

        string filePath = Path.Combine(
            PathsManager.TranslationRootDir,
            language,
            "Modules",
            $"{mod.Name}.json"
        );

        if (!File.Exists(filePath)) return;

        try
        {
            var token = JToken.Parse(File.ReadAllText(filePath));

            // 1. Reload module custom translation dictionary
            var customTransToken = token["Translations"] ?? token["CustomTranslations"];
            if (customTransToken != null)
            {
                var dict = customTransToken.ToObject<Dictionary<string, string>>();
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        mod.SavedTranslationData[kvp.Key] = kvp.Value;
                    }
                }
            }

            // 2. Reload module custom regex dictionary
            var regexToken = token["RegexTranslations"];
            if (regexToken != null)
            {
                var regexDict = regexToken.ToObject<Dictionary<string, string>>();
                if (regexDict != null)
                {
                    foreach (var kvp in regexDict)
                    {
                        mod.SavedRegexTranslations[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[ModuleTranslationHandler] Failed to reload translations for module '{mod.Name}': {ex.Message}");
        }
    }

    #endregion

    #region Enum Translations

    public static Dictionary<int, string> TranslateEnum(Type enumType)
    {
        return Translator.TranslateEnum(enumType);
    }

    #endregion
}