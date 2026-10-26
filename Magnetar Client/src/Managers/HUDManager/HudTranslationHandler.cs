using Magnetar_Client.Api;
using Magnetar_Client.HUDElements;
using Magnetar_Client.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core.HUDManager_;

/// <summary>
/// Handles reloading, saving, and cross-language syncing of HUD element translations and regex rules.
/// </summary>
public static class HudTranslationHandler
{
    #region Lifecycle & Translation Reloading

    /// <summary>
    /// Invoked when the client's language changes to reload translations for all HUD elements.
    /// </summary>
    public static void OnLanguageChanged()
    {
        string currentLang = Translator.Language ?? "English";
        ReloadAllHudTranslations(currentLang);
    }

    /// <summary>
    /// Reloads translations and regexes for all registered HUD elements from the specified language file.
    /// </summary>
    /// <param name="language">Target language code or name (defaults to current language).</param>
    public static void ReloadAllHudTranslations(string language = null)
    {
        language ??= Translator.Language ?? "English";

        if (HUDRenderer.Elements != null)
        {
            for (int i = 0; i < HUDRenderer.Elements.Count; i++)
            {
                var el = HUDRenderer.Elements[i];
                if (el == null) continue;

                ReloadHudElementTranslations(el, language);
                el.OnLanguageChanged();
            }
        }
    }

    /// <summary>
    /// Reloads translations and regex rules for a single HUD element from hud_translations.json.
    /// </summary>
    /// <param name="element">The HUD element to reload.</param>
    /// <param name="language">Target language code or name (defaults to current language).</param>
    public static void ReloadHudElementTranslations(HudElement element, string language = null)
    {
        if (element == null) return;
        language ??= Translator.Language ?? "English";

        string filePath = Path.Combine(PathsManager.TranslationRootDir, language, "hud_translations.json");
        if (!File.Exists(filePath)) return;

        try
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(filePath));
            if (dict == null) return;

            HudTranslationNode node = null;
            if (!dict.TryGetValue(element.Name, out node))
            {
                string sanitized = Translator.SanitizeFileName(element.Name);
                dict.TryGetValue(sanitized, out node);
            }

            if (node == null) return;

            if (!string.IsNullOrEmpty(node.Name))
            {
                TranslationData.ExactTranslations[element.Name] = node.Name;
            }

            if (node.ExtraData != null && node.ExtraData.Count > 0)
            {
                foreach (var kvp in node.ExtraData)
                {
                    if (string.Equals(kvp.Key, "Name", StringComparison.OrdinalIgnoreCase)) continue;

                    if (kvp.Value is JValue jVal)
                    {
                        string val = jVal.ToString();
                        if (element.SavedRegexTranslations != null && element.SavedRegexTranslations.ContainsKey(kvp.Key))
                        {
                            element.SavedRegexTranslations[kvp.Key] = val;
                        }
                        else if (element.SavedTranslationData != null && element.SavedTranslationData.ContainsKey(kvp.Key))
                        {
                            element.SavedTranslationData[kvp.Key] = val;
                        }
                        else
                        {
                            bool isRegex = kvp.Key.Contains("(") || kvp.Key.Contains("\\d") || kvp.Key.Contains(".*") ||
                                          kvp.Key.Contains("^") || kvp.Key.Contains("$") || val.Contains("{1}") ||
                                          kvp.Key.Contains("{");
                            if (isRegex)
                            {
                                element.SavedRegexTranslations ??= new();
                                element.SavedRegexTranslations[kvp.Key] = val;
                            }
                            else
                            {
                                element.SavedTranslationData ??= new(StringComparer.Ordinal);
                                element.SavedTranslationData[kvp.Key] = val;
                            }
                        }
                    }
                    else if (kvp.Value is JObject jObj)
                    {
                        if (string.Equals(kvp.Key, "Translations", StringComparison.OrdinalIgnoreCase))
                        {
                            element.SavedTranslationData ??= new(StringComparer.Ordinal);
                            foreach (var prop in jObj.Properties())
                            {
                                element.SavedTranslationData[prop.Name] = prop.Value?.ToString() ?? "";
                            }
                        }
                        else if (string.Equals(kvp.Key, "RegexTranslations", StringComparison.OrdinalIgnoreCase))
                        {
                            element.SavedRegexTranslations ??= new();
                            foreach (var prop in jObj.Properties())
                            {
                                element.SavedRegexTranslations[prop.Name] = prop.Value?.ToString() ?? "";
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[HudTranslationHandler] Failed reloading translations for '{element.Name}': {ex.Message}");
        }
    }

    #endregion

    #region Save & Sync Translations

    /// <summary>
    /// Saves a HUD element's translations and regexes to hud_translations.json for the specified language.
    /// If saved for English, synchronizes changes to all other language folders.
    /// </summary>
    /// <param name="element">The HUD element whose translations are being saved.</param>
    /// <param name="language">Target language code or name (defaults to current language).</param>
    public static void SaveHudElementTranslations(HudElement element, string language = null)
    {
        if (element == null || string.IsNullOrWhiteSpace(element.Name)) return;

        language ??= Translator.Language ?? "English";
        string filePath = Path.Combine(PathsManager.TranslationRootDir, language, "hud_translations.json");

        try
        {
            Dictionary<string, HudTranslationNode> dict = null;
            if (File.Exists(filePath))
            {
                try { dict = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(filePath)); } catch { }
            }
            dict ??= new Dictionary<string, HudTranslationNode>(StringComparer.Ordinal);

            if (!dict.TryGetValue(element.Name, out var node) || node == null)
            {
                node = new HudTranslationNode { Name = element.Name };
                dict[element.Name] = node;
            }

            if (string.IsNullOrEmpty(node.Name))
            {
                node.Name = element.Name;
            }

            node.ExtraData ??= new Dictionary<string, JToken>(StringComparer.Ordinal);

            if (element.SavedTranslationData != null && element.SavedTranslationData.Count > 0)
            {
                foreach (var kvp in element.SavedTranslationData)
                {
                    node.ExtraData[kvp.Key] = JToken.FromObject(kvp.Value);
                }
            }

            if (element.SavedRegexTranslations != null && element.SavedRegexTranslations.Count > 0)
            {
                foreach (var kvp in element.SavedRegexTranslations)
                {
                    node.ExtraData[kvp.Key] = JToken.FromObject(kvp.Value);
                }
            }

            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(dict, Formatting.Indented));

            if (string.Equals(language, "English", StringComparison.OrdinalIgnoreCase))
            {
                SyncHudToAllLanguages();
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[HudTranslationHandler] Failed saving translations for '{element.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes hud_translations.json from English to all target language directories.
    /// </summary>
    public static void SyncHudToAllLanguages()
    {
        string engPath = Path.Combine(PathsManager.TranslationRootDir, "English", "hud_translations.json");
        if (!File.Exists(engPath)) return;

        try
        {
            foreach (string langDir in Directory.GetDirectories(PathsManager.TranslationRootDir))
            {
                string lang = Path.GetFileName(langDir);
                if (string.Equals(lang, "English", StringComparison.OrdinalIgnoreCase)) continue;

                string targetPath = Path.Combine(langDir, "hud_translations.json");
                Translator.SyncHudJson(engPath, targetPath);
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[HudTranslationHandler] Failed syncing HUD translations to all languages: {ex.Message}");
        }
    }

    #endregion
}
