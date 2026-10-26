using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Magnetar_Client.UI.Setting;
using static Magnetar_Client.Utils.Magnetar_Logger;
using Magnetar_Client.Api;

namespace Magnetar_Client.Utils;

/// <summary>
/// Translator manages all the translations for the client.
/// </summary>
public static partial class Translator
{

    private static string _language = "English";
    /// <summary>
    /// The current language loaded, direct assignment will load the new translation file 
    /// </summary>
    public static string Language
    {
        get => _language;
        set
        {
            if (string.Equals(_language, value, StringComparison.OrdinalIgnoreCase))
                return;

            _language = value;

            _ = Translator.LoadTranslationsAsync(() =>
            {
                ServiceRegistry.NotifyLanguageChanged();
                Actions.Invoke_OnLanguageChanged();
            });
        }
    }
    private static void InvalidateBlacklist()
    {
        TranslationData.BlacklistDirty = true;
    }

    /// <summary>
    /// Sanitizes a file name by replacing invalid file system characters with underscores.
    /// </summary>
    public static string SanitizeFileName(string name)
    {
        string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        return Regex.Replace(name, $"[{invalidChars}]", "_");
    }

    private static void ExtractSettingNames(IEnumerable<Setting> settings, Action<string> onFoundName)
    {
        if (settings == null) return;

        foreach (var setting in settings)
        {
            if (setting == null || string.IsNullOrWhiteSpace(setting.Name)) continue;

            onFoundName(setting.Name);

            if (setting is SectionSetting sec)
            {
                if (sec.Sections != null && sec.Sections.Count > 0)
                {
                    foreach (var section in sec.Sections)
                    {
                        if (section.ChildSettings != null)
                            ExtractSettingNames(section.ChildSettings, onFoundName);
                    }
                }
                else if (sec.TemplateFactory != null)
                {
                    try
                    {
                        var sampleChildren = sec.TemplateFactory(0);
                        ExtractSettingNames(sampleChildren, onFoundName);
                    }
                    catch { }
                }
            }
        }
    }

    private static HashSet<string> GetCachedBlacklist()
    {
        if (!TranslationData.BlacklistDirty && TranslationData.CachedBlacklist.Count > 0)
            return TranslationData.CachedBlacklist;

        TranslationData.CachedBlacklist.Clear();

        if (Core.ModuleManager.Modules != null && Core.ModuleManager.Modules.Count > 0)
        {
            foreach (var mod in Core.ModuleManager.Modules)
            {
                if (!string.IsNullOrEmpty(mod.Name)) TranslationData.CachedBlacklist.Add(mod.Name);
                if (!string.IsNullOrEmpty(mod.Description)) TranslationData.CachedBlacklist.Add(mod.Description);
                if (!string.IsNullOrEmpty(mod.SearchHints)) TranslationData.CachedBlacklist.Add(mod.SearchHints);

                if (mod.Settings != null)
                    ExtractSettingNames(mod.Settings, name => TranslationData.CachedBlacklist.Add(name));
            }
        }

        if (Core.HUDManager_.HUDRenderer.Elements != null && Core.HUDManager_.HUDRenderer.Elements.Count > 0)
        {
            foreach (var element in Core.HUDManager_.HUDRenderer.Elements)
            {
                if (!string.IsNullOrEmpty(element.Name))
                    TranslationData.CachedBlacklist.Add(element.Name);

                TranslationData.CachedBlacklist.Add(element.GetType().Name);
            }
        }

        string engHudFile = Path.Combine(TranslationRootDir, "English", "hud_translations.json");
        AddJsonKeysToBlacklist(engHudFile);

        TranslationData.BlacklistDirty = false;
        return TranslationData.CachedBlacklist;
    }

    private static void AddJsonKeysToBlacklist(string filePath)
    {
        if (!File.Exists(filePath)) return;
        try
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(filePath));
            if (dict != null)
            {
                foreach (var k in dict.Keys)
                    TranslationData.CachedBlacklist.Add(k);
            }
        }
        catch { }
    }

    private static void DumpEnglishTemplate()
    {
        try
        {
            string englishDir = Path.Combine(TranslationRootDir, "English");
            if (!Directory.Exists(englishDir)) Directory.CreateDirectory(englishDir);

            if (Core.TabType.AllTabs != null)
            {
                foreach (var tab in Core.TabType.AllTabs)
                {
                    if (!string.IsNullOrEmpty(tab.Name))
                        TranslationData.KnownEnglishStrings.Add(tab.Name);
                }
            }

            var defaultControls = new[] { "Select", "Language", "Search...", "Deselect All", "GUI Configuration", "Reset", "ON", "OFF" };
            foreach (var ctrl in defaultControls)
                TranslationData.KnownEnglishStrings.Add(ctrl);

            DumpModulesEnglishTemplate(englishDir);
            DumpHudEnglishTemplate(englishDir);

            HashSet<string> blacklist = GetCachedBlacklist();
            string stringsPath = Path.Combine(englishDir, "translation_strings.json");
            Dictionary<string, string> engStrings = new(StringComparer.Ordinal);

            if (File.Exists(stringsPath))
            {
                try { engStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(stringsPath)) ?? new(StringComparer.Ordinal); } catch { }
            }

            var cleanedEngStrings = engStrings
                .Where(kvp => !blacklist.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

            bool dirtyStrings = cleanedEngStrings.Count != engStrings.Count;

            foreach (var str in TranslationData.KnownEnglishStrings)
            {
                if (!blacklist.Contains(str) && !cleanedEngStrings.ContainsKey(str))
                {
                    cleanedEngStrings[str] = str;
                    dirtyStrings = true;
                }
            }

            if (dirtyStrings || !File.Exists(stringsPath))
            {
                SafeWriteAllText(stringsPath, JsonConvert.SerializeObject(cleanedEngStrings, Formatting.Indented));
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to dump English template: {ex.Message}");
        }
    }

    /// <summary>
    /// Dumps English module translation templates for all registered modules.
    /// Preserves existing entries and populates Settings, Translations, and RegexTranslations.
    /// </summary>
    public static void DumpModulesEnglishTemplate(string englishDir = null)
    {
        if (Core.ModuleManager.Modules == null || Core.ModuleManager.Modules.Count == 0)
            return;

        try
        {
            englishDir ??= Path.Combine(TranslationRootDir, "English");
            string englishModulesDir = Path.Combine(englishDir, "Modules");
            if (!Directory.Exists(englishModulesDir)) Directory.CreateDirectory(englishModulesDir);

            foreach (var mod in Core.ModuleManager.Modules)
            {
                if (mod == null || string.IsNullOrWhiteSpace(mod.Name)) continue;

                string modFilePath = Path.Combine(englishModulesDir, $"{SanitizeFileName(mod.Name)}.json");
                ModuleTranslationNode node = null;
                if (File.Exists(modFilePath))
                {
                    try { node = JsonConvert.DeserializeObject<ModuleTranslationNode>(SafeReadAllText(modFilePath)); } catch { }
                }

                node ??= new ModuleTranslationNode();
                node.Name = string.IsNullOrEmpty(node.Name) ? mod.Name : node.Name;
                node.Description = string.IsNullOrEmpty(node.Description) ? mod.Description : node.Description;
                node.SearchHints = mod.SearchHints ?? node.SearchHints ?? "";

                node.Settings ??= new(StringComparer.Ordinal);
                if (mod.Settings != null)
                {
                    ExtractSettingNames(mod.Settings, name =>
                    {
                        if (!node.Settings.ContainsKey(name))
                            node.Settings[name] = name;
                    });
                }

                if (mod.SavedTranslationData != null && mod.SavedTranslationData.Count > 0)
                {
                    node.Translations ??= new(StringComparer.Ordinal);
                    foreach (var kvp in mod.SavedTranslationData)
                    {
                        if (!node.Translations.ContainsKey(kvp.Key))
                            node.Translations[kvp.Key] = kvp.Value;
                    }

                    foreach (var kvp in node.Translations)
                    {
                        mod.SavedTranslationData[kvp.Key] = kvp.Value;
                    }
                }

                if (mod.SavedRegexTranslations != null && mod.SavedRegexTranslations.Count > 0)
                {
                    node.RegexTranslations ??= new();
                    foreach (var kvp in mod.SavedRegexTranslations)
                    {
                        if (!node.RegexTranslations.ContainsKey(kvp.Key))
                            node.RegexTranslations[kvp.Key] = kvp.Value;
                    }

                    foreach (var kvp in node.RegexTranslations)
                    {
                        mod.SavedRegexTranslations[kvp.Key] = kvp.Value;
                    }
                }

                SafeWriteAllText(modFilePath, JsonConvert.SerializeObject(node, Formatting.Indented));
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed dumping English modules template: {ex.Message}");
        }
    }

    /// <summary>
    /// Dumps English HUD translation templates for all registered HUD elements.
    /// </summary>
    public static void DumpHudEnglishTemplate(string englishDir = null)
    {
        if (Core.HUDManager_.HUDRenderer.Elements == null || Core.HUDManager_.HUDRenderer.Elements.Count == 0)
            return;

        try
        {
            englishDir ??= Path.Combine(TranslationRootDir, "English");
            if (!Directory.Exists(englishDir)) Directory.CreateDirectory(englishDir);

            string hudPath = Path.Combine(englishDir, "hud_translations.json");
            var engHud = new Dictionary<string, HudTranslationNode>();
            if (File.Exists(hudPath))
            {
                try { engHud = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(SafeReadAllText(hudPath)) ?? new(); } catch { }
            }

            bool dirty = false;
            foreach (var el in Core.HUDManager_.HUDRenderer.Elements)
            {
                if (el != null && !string.IsNullOrWhiteSpace(el.Name) && !engHud.ContainsKey(el.Name))
                {
                    engHud[el.Name] = new HudTranslationNode { Name = el.Name };
                    dirty = true;
                }
            }

            if (dirty || !File.Exists(hudPath))
            {
                SafeWriteAllText(hudPath, JsonConvert.SerializeObject(engHud, Formatting.Indented));
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed dumping English HUD template: {ex.Message}");
        }
    }

    private static void AppendUntranslatedStringToDisk(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !TranslationData.DumpedUntranslated.Add(input)) return;

        try
        {
            string targetLang = CurrentLanguage;
            if (string.Equals(targetLang, "English", StringComparison.OrdinalIgnoreCase)) return;

            string baseDir = Path.Combine(TranslationRootDir, targetLang);
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            string untranslatedPath = Path.Combine(baseDir, "untranslated_strings.json");
            Dictionary<string, string> dict = new(StringComparer.Ordinal);

            lock (TranslationData.UntranslatedFileLock)
            {
                if (File.Exists(untranslatedPath))
                {
                    try
                    {
                        string json = SafeReadAllText(untranslatedPath);
                        dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new(StringComparer.Ordinal);
                    }
                    catch { }
                }

                if (!dict.ContainsKey(input))
                {
                    dict[input] = input;
                    SafeWriteAllText(untranslatedPath, JsonConvert.SerializeObject(dict, Formatting.Indented));
                    TranslatorLogger.Warning($"Dumped untranslated string: \"{input}\"");
                }
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed dumping untranslated string '{input}': {ex.Message}");
        }
    }

    /// <summary>
    /// Helper to save a object to disk
    /// </summary>

    public static bool SaveJson(string filePath, object data, Formatting formatting = Formatting.Indented)
    {
        if (string.IsNullOrWhiteSpace(filePath) || data == null)
            return false;

        try
        {
            string targetDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            File.WriteAllText(filePath, JsonConvert.SerializeObject(data, formatting));
            return true;
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed writing JSON to '{filePath}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Helper to create a dictionary from a array with same keys and values
    /// </summary>
    public static Dictionary<string, string> CreateDictionary(string[] stringList)
    {
        var dictionary = new Dictionary<string, string>();
        foreach (string entry in stringList)
            dictionary[entry] = entry;
        return dictionary;
    }

    private static string SafeReadAllText(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void SafeWriteAllText(string filePath, string content)
    {
        string dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream);
        writer.Write(content);
    }
}