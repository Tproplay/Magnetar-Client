using Magnetar_Client.Api;
using Magnetar_Client.Utils;
using Newtonsoft.Json;
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
        string currentLang = Translator.Language ?? "English";

        if (ModuleManager.Modules != null)
        {
            var modules = ModuleManager.GetModulesSnapshot();
            for (int i = 0; i < modules.Length; i++)
            {
                var mod = modules[i];
                if (mod == null) continue;

                ReloadModuleTranslations(mod, currentLang);
                mod.OnLanguageChanged();
            }
        }
    }

    public static void ReloadModuleTranslations(Modules.Module mod, string language)
    {
        if (mod == null) return;

        string sanitized = Translator.SanitizeFileName(mod.Name);
        string filePath = Path.Combine(PathsManager.TranslationRootDir, language, "Modules", $"{sanitized}.json");
        if (!File.Exists(filePath))
        {
            filePath = Path.Combine(PathsManager.TranslationRootDir, language, "Modules", $"{mod.Name}.json");
            if (!File.Exists(filePath)) return;
        }

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

    /// <summary>
    /// Saves the module's translation data (including SavedTranslationData and SavedRegexTranslations)
    /// to its JSON file in the specified language (defaulting to current language).
    /// </summary>
    public static void SaveModuleTranslations(Modules.Module mod, string language = null)
    {
        if (mod == null || string.IsNullOrWhiteSpace(mod.Name)) return;

        language ??= Translator.Language ?? "English";
        string sanitized = Translator.SanitizeFileName(mod.Name);
        string modulesDir = Path.Combine(PathsManager.TranslationRootDir, language, "Modules");
        if (!Directory.Exists(modulesDir)) Directory.CreateDirectory(modulesDir);

        string filePath = Path.Combine(modulesDir, $"{sanitized}.json");

        try
        {
            ModuleTranslationNode node = null;
            if (File.Exists(filePath))
            {
                try { node = JsonConvert.DeserializeObject<ModuleTranslationNode>(File.ReadAllText(filePath)); } catch { }
            }

            node ??= new ModuleTranslationNode();
            node.Name = string.IsNullOrEmpty(node.Name) ? mod.Name : node.Name;
            node.Description = string.IsNullOrEmpty(node.Description) ? mod.Description : node.Description;
            node.SearchHints = mod.SearchHints ?? node.SearchHints ?? "";

            node.Settings ??= new(StringComparer.Ordinal);
            if (mod.Settings != null)
            {
                foreach (var setting in mod.Settings)
                {
                    if (setting != null && !string.IsNullOrWhiteSpace(setting.Name) && !node.Settings.ContainsKey(setting.Name))
                    {
                        node.Settings[setting.Name] = setting.Name;
                    }
                }
            }

            if (mod.SavedTranslationData != null && mod.SavedTranslationData.Count > 0)
            {
                node.Translations ??= new(StringComparer.Ordinal);
                foreach (var kvp in mod.SavedTranslationData)
                {
                    node.Translations[kvp.Key] = kvp.Value;
                }
            }

            if (mod.SavedRegexTranslations != null && mod.SavedRegexTranslations.Count > 0)
            {
                node.RegexTranslations ??= new();
                foreach (var kvp in mod.SavedRegexTranslations)
                {
                    node.RegexTranslations[kvp.Key] = kvp.Value;
                }
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(node, Formatting.Indented));

            // If we modified the English template, sync this module's file to all target languages
            if (string.Equals(language, "English", StringComparison.OrdinalIgnoreCase))
            {
                SyncModuleFileToAllLanguages(sanitized);
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[ModuleTranslationHandler] Failed to save translations for module '{mod.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Syncs a single module translation file from English to all other language folders.
    /// </summary>
    public static void SyncModuleFileToAllLanguages(string sanitizedFileName)
    {
        string engFile = Path.Combine(PathsManager.TranslationRootDir, "English", "Modules", $"{sanitizedFileName}.json");
        if (!File.Exists(engFile)) return;

        try
        {
            var engNode = JsonConvert.DeserializeObject<ModuleTranslationNode>(File.ReadAllText(engFile));
            if (engNode == null) return;

            foreach (string langDir in Directory.GetDirectories(PathsManager.TranslationRootDir))
            {
                string lang = Path.GetFileName(langDir);
                if (string.Equals(lang, "English", StringComparison.OrdinalIgnoreCase)) continue;

                string targetModulesDir = Path.Combine(langDir, "Modules");
                if (!Directory.Exists(targetModulesDir)) Directory.CreateDirectory(targetModulesDir);

                string targetFile = Path.Combine(targetModulesDir, $"{sanitizedFileName}.json");
                ModuleTranslationNode targetNode = null;
                if (File.Exists(targetFile))
                {
                    try { targetNode = JsonConvert.DeserializeObject<ModuleTranslationNode>(File.ReadAllText(targetFile)); } catch { }
                }

                bool dirty = false;
                if (targetNode == null)
                {
                    targetNode = new ModuleTranslationNode
                    {
                        Name = engNode.Name,
                        Description = engNode.Description,
                        SearchHints = engNode.SearchHints,
                        Settings = engNode.Settings != null ? new Dictionary<string, string>(engNode.Settings) : new(),
                        Translations = engNode.Translations != null ? new Dictionary<string, string>(engNode.Translations) : null,
                        RegexTranslations = engNode.RegexTranslations != null ? new Dictionary<string, string>(engNode.RegexTranslations) : null
                    };
                    dirty = true;
                }
                else
                {
                    if (string.IsNullOrEmpty(targetNode.Name)) { targetNode.Name = engNode.Name; dirty = true; }
                    if (string.IsNullOrEmpty(targetNode.Description)) { targetNode.Description = engNode.Description; dirty = true; }
                    if (string.IsNullOrEmpty(targetNode.SearchHints) && !string.IsNullOrEmpty(engNode.SearchHints)) { targetNode.SearchHints = engNode.SearchHints; dirty = true; }

                    if (targetNode.Settings == null) { targetNode.Settings = new(); dirty = true; }
                    if (engNode.Settings != null)
                    {
                        foreach (var s in engNode.Settings)
                        {
                            if (!targetNode.Settings.ContainsKey(s.Key) || string.IsNullOrEmpty(targetNode.Settings[s.Key]))
                            {
                                targetNode.Settings[s.Key] = s.Value;
                                dirty = true;
                            }
                        }
                    }

                    if (engNode.Translations != null && engNode.Translations.Count > 0)
                    {
                        if (targetNode.Translations == null) { targetNode.Translations = new(StringComparer.Ordinal); dirty = true; }
                        foreach (var t in engNode.Translations)
                        {
                            if (!targetNode.Translations.ContainsKey(t.Key) || string.IsNullOrEmpty(targetNode.Translations[t.Key]))
                            {
                                targetNode.Translations[t.Key] = t.Value;
                                dirty = true;
                            }
                        }
                    }

                    if (engNode.RegexTranslations != null && engNode.RegexTranslations.Count > 0)
                    {
                        if (targetNode.RegexTranslations == null) { targetNode.RegexTranslations = new(); dirty = true; }
                        foreach (var r in engNode.RegexTranslations)
                        {
                            if (!targetNode.RegexTranslations.ContainsKey(r.Key) || string.IsNullOrEmpty(targetNode.RegexTranslations[r.Key]))
                            {
                                targetNode.RegexTranslations[r.Key] = r.Value;
                                dirty = true;
                            }
                        }
                    }
                }

                if (dirty || !File.Exists(targetFile))
                {
                    File.WriteAllText(targetFile, JsonConvert.SerializeObject(targetNode, Formatting.Indented));
                }
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[ModuleTranslationHandler] Failed syncing '{sanitizedFileName}.json' to languages: {ex.Message}");
        }
    }

    #endregion
}