using MelonLoader.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils
{
    public static class Translator
    {
        private static bool _isLoaded = false;
        private static Dictionary<string, string> _exactTranslations = new Dictionary<string, string>();
        private static Dictionary<Regex, string> _regexTranslations = new Dictionary<Regex, string>();
        private static Dictionary<System.Type, Dictionary<int, string>> _nameCache = new Dictionary<System.Type, Dictionary<int, string>>();

        public static void LoadTranslations()
        {
            string targetLanguage = Config.Language;

            // 1. Sync files from the English master template first
            SyncWithEnglishTemplate(targetLanguage);

            string baseDir = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Translation", targetLanguage);

            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
                TranslatorLogger.Msg($"Created Magnetar Translation directory for: {targetLanguage}");
            }

            // 2. Load Exact Strings
            string stringsPath = Path.Combine(baseDir, "translation_strings.json");
            _exactTranslations.Clear();

            if (File.Exists(stringsPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(stringsPath);
                    _exactTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                    TranslatorLogger.Msg($"Loaded {_exactTranslations.Count} exact strings for {targetLanguage}.");
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load exact strings: {ex.Message}");
                }
            }

            // 3. Load Regex Strings
            string regexPath = Path.Combine(baseDir, "translation_regexs.json");
            _regexTranslations.Clear();

            if (File.Exists(regexPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(regexPath);
                    var rawData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();

                    foreach (var entry in rawData)
                    {
                        _regexTranslations.Add(new Regex(entry.Key, RegexOptions.Compiled), entry.Value);
                    }
                    TranslatorLogger.Msg($"Loaded {_regexTranslations.Count} regex rules for {targetLanguage}.");
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load regex strings: {ex.Message}");
                }
            }

            _nameCache.Clear();
            _isLoaded = true;
        }

        /// <summary>
        /// Copies all JSON files from the English folder to the target language folder if they don't exist.
        /// </summary>
        private static void SyncWithEnglishTemplate(string targetLanguage)
        {
            // Do not try to sync English to English
            if (string.Equals(targetLanguage, "English", StringComparison.OrdinalIgnoreCase)) return;

            string englishDir = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Translation", "English");
            string targetDir = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Translation", targetLanguage);

            if (!Directory.Exists(englishDir)) return; // Cannot copy if English master is missing

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string[] sourceFiles = Directory.GetFiles(englishDir, "*.json");

            foreach (string file in sourceFiles)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);

                // Only copy if the file is missing in the new language folder
                if (!File.Exists(destFile))
                {
                    try
                    {
                        File.Copy(file, destFile);
                        TranslatorLogger.Msg($"Copied template file to {targetLanguage}: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        TranslatorLogger.Error($"Failed to copy {fileName}: {ex.Message}");
                    }
                }
            }
        }

        public static void DumpMissingStrings()
        {
            if (Core.ModuleManager.Modules == null || Core.ModuleManager.Modules.Count == 0) return;

            bool isDirty = false;
            string baseDir = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Translation", Config.Language);
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            string stringsPath = Path.Combine(baseDir, "translation_strings.json");

            void AddIfMissing(string text)
            {
                if (!string.IsNullOrWhiteSpace(text) && !_exactTranslations.ContainsKey(text))
                {
                    _exactTranslations[text] = text;
                    isDirty = true;
                }
            }

            foreach (var mod in Core.ModuleManager.Modules)
            {
                AddIfMissing(mod.Name);
                AddIfMissing(mod.Description);
                AddIfMissing(mod.SearchHints);

                if (mod.Settings != null)
                {
                    foreach (var setting in mod.Settings)
                    {
                        AddIfMissing(setting.Name);
                    }
                }
            }

            if (isDirty)
            {
                try
                {
                    string jsonDump = JsonConvert.SerializeObject(_exactTranslations, Formatting.Indented);
                    File.WriteAllText(stringsPath, jsonDump);
                    TranslatorLogger.Warning($"Auto-dumped missing translation strings into {Config.Language}/translation_strings.json");
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to dump translation strings: {ex.Message}");
                }
            }
        }

        public static string Translate(string input)
        {
            if (!_isLoaded) LoadTranslations();

            if (string.IsNullOrWhiteSpace(input)) return input;

            if (_exactTranslations.TryGetValue(input, out string exactMatch))
            {
                return exactMatch;
            }

            foreach (var rule in _regexTranslations)
            {
                Match match = rule.Key.Match(input);
                if (match.Success)
                {
                    string template = rule.Value;
                    List<string> captures = new List<string>();

                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (match.Groups[i].Success)
                        {
                            string val = match.Groups[i].Value;
                            if (string.IsNullOrWhiteSpace(val) || val == "：" || val == ":") continue;
                            captures.Add(val);
                        }
                    }

                    bool isZeroIndexed = template.Contains("{0}");

                    for (int i = 0; i < captures.Count; i++)
                    {
                        string placeholder = isZeroIndexed ? $"{{{i}}}" : $"{{{i + 1}}}";
                        string finalWord = captures[i];

                        if (_exactTranslations.TryGetValue(captures[i], out string translatedCapture))
                        {
                            finalWord = translatedCapture;
                        }
                        template = template.Replace(placeholder, finalWord);
                    }
                    return template;
                }
            }

            _exactTranslations[input] = input;

            SaveMissingStringToDisk();

            return input;
        }

        private static void SaveMissingStringToDisk()
        {
            try
            {
                string baseDir = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Translation", Config.Language);
                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

                string stringsPath = Path.Combine(baseDir, "translation_strings.json");

                string jsonDump = JsonConvert.SerializeObject(_exactTranslations, Formatting.Indented);
                File.WriteAllText(stringsPath, jsonDump);

                TranslatorLogger.Msg("Caught and auto-dumped a new missing string.");
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"Failed to auto-dump missing string: {ex.Message}");
            }
        }

        public static Dictionary<int, string> TranslateEnum(Type enumType)
        {
            if (_nameCache.TryGetValue(enumType, out var cachedDict))
            {
                return new Dictionary<int, string>(cachedDict);
            }

            Dictionary<int, string> parsedNames = new Dictionary<int, string>();

            string magnetarDir = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Translation", Config.Language);
            if (!Directory.Exists(magnetarDir)) Directory.CreateDirectory(magnetarDir);

            string translatorAlmanacDir = Path.Combine(MelonEnvironment.ModsDirectory,
                "PvZ_Fusion_Translator", "Localization", Config.Language, "Almanac");

            string targetFile = Path.Combine(magnetarDir, $"{enumType.Name}.json");

            string CleanText(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                return Regex.Replace(input, "<.*?>", string.Empty);
            }

            if (File.Exists(targetFile))
            {
                try
                {
                    string json = File.ReadAllText(targetFile);
                    var rawData = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);

                    if (rawData != null)
                    {
                        foreach (var kvp in rawData) parsedNames[kvp.Key] = CleanText(kvp.Value);
                        TranslatorLogger.Msg($"Loaded {parsedNames.Count} entries from {enumType.Name}.json");
                    }
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to parse Magnetar file {enumType.Name}.json: {ex.Message}");
                }
            }
            else
            {
                TranslatorLogger.Warning($"{enumType.Name}.json not found. Generating new file...");
                try
                {
                    if (enumType.Name == "PlantType")
                    {
                        string translatorPath = Path.Combine(translatorAlmanacDir, "LawnStringsTranslate.json");
                        if (File.Exists(translatorPath))
                        {
                            var root = JObject.Parse(File.ReadAllText(translatorPath));
                            if (root["plants"] != null)
                            {
                                foreach (var p in root["plants"]) parsedNames[(int)p["seedType"]] = CleanText((string)p["name"]);
                            }
                        }
                    }
                    else if (enumType.Name == "ZombieType")
                    {
                        string translatorPath = Path.Combine(translatorAlmanacDir, "ZombieStringsTranslate.json");
                        if (File.Exists(translatorPath))
                        {
                            var root = JObject.Parse(File.ReadAllText(translatorPath));
                            if (root["zombies"] != null)
                            {
                                foreach (var z in root["zombies"]) parsedNames[(int)z["theZombieType"]] = CleanText((string)z["name"]);
                            }
                        }
                    }

                    Array values = Enum.GetValues(enumType);
                    foreach (object val in values)
                    {
                        int intVal = (int)val;
                        if (!parsedNames.ContainsKey(intVal)) parsedNames[intVal] = val.ToString();
                    }

                    string dumpJson = JsonConvert.SerializeObject(parsedNames, Formatting.Indented);
                    File.WriteAllText(targetFile, dumpJson);

                    TranslatorLogger.Warning($"Successfully generated {enumType.Name}.json with {parsedNames.Count} total entries.");
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to generate template for {enumType.Name}: {ex.Message}");
                }
            }

            _nameCache[enumType] = parsedNames;
            return new Dictionary<int, string>(parsedNames);
        }
    }
}