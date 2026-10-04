using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Api.PathsManager;

namespace Magnetar_Client.Utils;

public static class Translator
{
    public static void LoadTranslations()
    {
        try
        {
            string targetLanguage = TranslatorData.CurrentLanguage;

            TranslatorData.InvalidateBlacklist();
            TranslatorData.DumpEnglishTemplate();

            TranslatorData.ExactTranslations.Clear();
            TranslatorData.RegexTranslations.Clear();
            TranslatorData.NameCache.Clear();
            TranslatorData.DumpedUntranslated.Clear();
            TranslatorData.ModulesLinked = false;
            TranslatorData.HudLinked = false;

            string baseDir = Path.Combine(TranslationRootDir, targetLanguage);
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
                TranslatorLogger.Msg($"Created Magnetar Translation directory for: {targetLanguage}");
            }

            TranslatorData.SyncWithEnglishTemplate(targetLanguage);

            // 1. Load exact translation strings
            string stringsPath = Path.Combine(baseDir, "translation_strings.json");
            if (File.Exists(stringsPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(stringsPath);
                    TranslatorData.ExactTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new(StringComparer.Ordinal);
                    TranslatorLogger.Msg($"Loaded {TranslatorData.ExactTranslations.Count} exact strings for {targetLanguage}.");

                    foreach (var key in TranslatorData.ExactTranslations.Keys)
                    {
                        TranslatorData.DumpedUntranslated.Add(key);
                    }
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load exact strings: {ex.Message}");
                }
            }

            string untranslatedPath = Path.Combine(baseDir, "untranslated_strings.json");
            if (File.Exists(untranslatedPath))
            {
                try
                {
                    var existingUntranslated = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(untranslatedPath));
                    if (existingUntranslated != null)
                    {
                        foreach (var key in existingUntranslated.Keys)
                        {
                            TranslatorData.DumpedUntranslated.Add(key);
                        }
                    }
                }
                catch { }
            }

            // 2. Load regex rules
            string regexPath = Path.Combine(baseDir, "translation_regexs.json");
            if (File.Exists(regexPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(regexPath);
                    var rawData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new();

                    foreach (var entry in rawData)
                    {
                        TranslatorData.RegexTranslations.Add(new Regex(entry.Key, RegexOptions.Compiled), entry.Value);
                    }
                    TranslatorLogger.Msg($"Loaded {TranslatorData.RegexTranslations.Count} regex rules for {targetLanguage}.");
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load regex strings: {ex.Message}");
                }
            }

            if (Core.HUDRenderer.Elements != null && Core.HUDRenderer.Elements.Count > 0)
            {
                TranslatorData.LinkHudTranslations();
                TranslatorData.HudLinked = true;
            }

            TranslatorData.IsLoaded = true;
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Load translations failed: {ex.Message}");
            TranslatorData.IsLoaded = true;
        }
    }

    /// <summary>
    /// Translates an input string. Only dumps to untranslated_strings.json if the string
    /// is completely missing from translation_strings.json and regex translations.
    /// </summary>
    public static string Translate(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        if (!TranslatorData.IsLoaded) LoadTranslations();

        if (!TranslatorData.ModulesLinked && Core.ModuleManager.Modules != null && Core.ModuleManager.Modules.Count > 0)
        {
            TranslatorData.LinkModuleTranslations();
            TranslatorData.ModulesLinked = true;
        }

        if (!TranslatorData.HudLinked && Core.HUDRenderer.Elements != null && Core.HUDRenderer.Elements.Count > 0)
        {
            TranslatorData.LinkHudTranslations();
            TranslatorData.HudLinked = true;
        }

        if (string.IsNullOrWhiteSpace(input)) return input;

        // 1. Exact translation
        if (TranslatorData.ExactTranslations.TryGetValue(input, out string exactMatch))
        {
            return exactMatch;
        }

        // 2. Regex match
        foreach (var rule in TranslatorData.RegexTranslations)
        {
            Match match = rule.Key.Match(input);
            if (match.Success)
            {
                string template = rule.Value;

                for (int i = 1; i < match.Groups.Count; i++)
                {
                    if (match.Groups[i].Success)
                    {
                        string val = match.Groups[i].Value;
                        if (string.IsNullOrWhiteSpace(val) || val == "：" || val == ":") continue;

                        string finalWord = val;
                        if (TranslatorData.ExactTranslations.TryGetValue(val, out string translatedCapture))
                        {
                            finalWord = translatedCapture;
                        }

                        template = template.Replace($"{{{i}}}", finalWord);
                    }
                }

                TranslatorData.RegexMatchedInputs.Add(input);
                TranslatorData.ExactTranslations[input] = template;
                return template;
            }
        }

        TranslatorData.ExactTranslations[input] = input;

        if (string.Equals(TranslatorData.CurrentLanguage, "English", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        HashSet<string> blacklist = TranslatorData.GetCachedBlacklist();
        if (!blacklist.Contains(input))
        {
            TranslatorData.KnownEnglishStrings.Add(input);
            TranslatorData.MissingStringsDirty = true;
            TranslatorData.AppendUntranslatedStringToDisk(input);
        }

        return input;
    }

    public static void DumpMissingStrings()
    {
        if (!TranslatorData.MissingStringsDirty) return;

        TranslatorData.DumpEnglishTemplate();

        string currentLang = TranslatorData.CurrentLanguage;
        if (string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            TranslatorData.MissingStringsDirty = false;
            return;
        }

        try
        {
            TranslatorData.SyncWithEnglishTemplate(currentLang);

            string baseDir = Path.Combine(TranslationRootDir, currentLang);
            string stringsPath = Path.Combine(baseDir, "translation_strings.json");

            HashSet<string> blacklist = TranslatorData.GetCachedBlacklist();

            var cleanDict = TranslatorData.ExactTranslations
                .Where(kvp => !TranslatorData.RegexMatchedInputs.Contains(kvp.Key) && !blacklist.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

            string generalDump = JsonConvert.SerializeObject(cleanDict, Formatting.Indented);
            File.WriteAllText(stringsPath, generalDump);
            TranslatorData.MissingStringsDirty = false;
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to dump missing translation strings: {ex.Message}");
        }
    }

    public static Dictionary<int, string> TranslateEnum(Type enumType)
    {
        if (enumType == null)
        {
            TranslatorLogger.Error("TranslateEnum called with null enumType!");
            return new Dictionary<int, string>();
        }

        if (TranslatorData.NameCache.TryGetValue(enumType, out var cachedDict))
        {
            return new Dictionary<int, string>(cachedDict);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            Dictionary<int, string> parsedNames = LoadEnumTranslations(enumType);
            TranslatorData.NameCache[enumType] = parsedNames;
            sw.Stop();

            TranslatorLogger.Msg($"Loaded and cached enum '{enumType.Name}' with {parsedNames.Count} entries ({sw.ElapsedMilliseconds} ms).");
            return new Dictionary<int, string>(parsedNames);
        }
        catch (Exception ex)
        {
            sw.Stop();
            TranslatorLogger.Error($"Exception occurred while translating enum '{enumType.Name}': {ex}");
            throw;
        }
    }

    public static Dictionary<int, string> LoadEnumTranslations(Type enumType)
    {
        string englishEnumsDir = Path.Combine(TranslationRootDir, "English", "Enums");
        if (!Directory.Exists(englishEnumsDir)) Directory.CreateDirectory(englishEnumsDir);

        string englishEnumFile = Path.Combine(englishEnumsDir, $"{enumType.Name}.json");

        Dictionary<int, string> englishEnumDict = new();
        if (File.Exists(englishEnumFile))
        {
            try
            {
                englishEnumDict = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(englishEnumFile)) ?? new();
            }
            catch { }
        }

        bool dirtyEnglish = false;
        Array values = Enum.GetValues(enumType);
        foreach (object val in values)
        {
            int intVal = (int)val;
            if (!englishEnumDict.ContainsKey(intVal))
            {
                englishEnumDict[intVal] = Enum.GetName(enumType, intVal) ?? intVal.ToString();
                dirtyEnglish = true;
            }
        }

        if (dirtyEnglish || !File.Exists(englishEnumFile))
        {
            var sortedEnglish = englishEnumDict.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText(englishEnumFile, JsonConvert.SerializeObject(sortedEnglish, Formatting.Indented));
        }

        string activeLang = TranslatorData.CurrentLanguage;
        string activeEnumsDir = Path.Combine(TranslationRootDir, activeLang, "Enums");
        if (!Directory.Exists(activeEnumsDir)) Directory.CreateDirectory(activeEnumsDir);

        string activeEnumFile = Path.Combine(activeEnumsDir, $"{enumType.Name}.json");

        if (!string.Equals(activeLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            TranslatorData.SyncEnumJson(englishEnumFile, activeEnumFile);
        }

        Dictionary<int, string> parsedNames = new();

        if (File.Exists(activeEnumFile))
        {
            try
            {
                string json = File.ReadAllText(activeEnumFile);
                var rawData = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);
                if (rawData != null)
                {
                    foreach (var kvp in rawData)
                    {
                        parsedNames[kvp.Key] = Regex.Replace(kvp.Value ?? "", "<.*?>", string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"Failed to parse enum file {enumType.Name}.json: {ex.Message}");
            }
        }

        return parsedNames;
    }
}