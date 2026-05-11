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
            string baseDir = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Translation");

            // Ensure new directory exists
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
#if DEBUG
                TranslatorLogger.Msg("Created Magnetar Translation directory.");
#endif
            }

            // 1. Load Exact Strings
            string stringsPath = Path.Combine(baseDir, "translation_strings.json");
            if (File.Exists(stringsPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(stringsPath);
                    _exactTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
#if DEBUG
                    TranslatorLogger.Msg($"Loaded {_exactTranslations.Count} exact strings.");
#endif
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load exact strings: {ex.Message}");
                }
            }
            else
            {
#if DEBUG
                TranslatorLogger.Warning($"Translation file not found: {stringsPath}");
#endif
            }

            // 2. Load Regex Strings
            string regexPath = Path.Combine(baseDir, "translation_regexs.json");
            if (File.Exists(regexPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(regexPath);
                    var rawData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();

                    _regexTranslations.Clear();
                    foreach (var entry in rawData)
                    {
                        _regexTranslations.Add(new Regex(entry.Key, RegexOptions.Compiled), entry.Value);
                    }
#if DEBUG
                    TranslatorLogger.Msg($"Loaded {_regexTranslations.Count} regex rules.");
#endif
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load regex strings: {ex.Message}");
                }
            }
            else
            {
#if DEBUG
                TranslatorLogger.Warning($"Translation file not found: {regexPath}");
#endif
            }

            _isLoaded = true;
        }

        public static string Translate(string input)
        {
            if (!_isLoaded) LoadTranslations();
            if (string.IsNullOrEmpty(input)) return input;

            // 1. Check for an exact match
            if (_exactTranslations.TryGetValue(input, out string exactMatch))
            {
                return exactMatch;
            }

            // 2. Regex processing
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
                            if (string.IsNullOrWhiteSpace(val) || val == "：" || val == ":")
                                continue;
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

            return input;
        }

        public static Dictionary<int, string> TranslateEnum(Type enumType)
        {
            if (_nameCache.TryGetValue(enumType, out var cachedDict))
            {
                return new Dictionary<int, string>(cachedDict);
            }

            Dictionary<int, string> parsedNames = new Dictionary<int, string>();

            string magnetarDir = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Translation");
            string translatorAlmanacDir = Path.Combine(MelonEnvironment.ModsDirectory,
                "PvZ_Fusion_Translator", "Localization", "English", "Almanac");

            // The file we want to load OR create
            string targetFile = Path.Combine(magnetarDir, $"{enumType.Name}.json");

            string CleanText(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                return Il2Cpp.InGameText.RemoveRichTextTags(input);
            }

            // --- 1. LOAD FROM MAGNETAR FOLDER IF IT EXISTS ---
            if (File.Exists(targetFile))
            {
                try
                {
                    string json = File.ReadAllText(targetFile);
                    var rawData = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);

                    if (rawData != null)
                    {
                        foreach (var kvp in rawData)
                        {
                            parsedNames[kvp.Key] = CleanText(kvp.Value);
                        }
#if DEBUG
                        TranslatorLogger.Msg($"Loaded {parsedNames.Count} entries from {enumType.Name}.json");
#endif
                    }
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to parse Magnetar file {enumType.Name}.json: {ex.Message}");
                }
            }

            // --- 2. GENERATE NEW FILE (IMPORTING OLD DATA IF POSSIBLE) ---
            else
            {
                TranslatorLogger.Warning($"{enumType.Name}.json not found in Magnetar Translation. Generating new file...");
                try
                {
                    // A. Try to import PlantType data
                    if (enumType.Name == "PlantType")
                    {
                        string translatorPath = Path.Combine(translatorAlmanacDir, "LawnStringsTranslate.json");
                        if (File.Exists(translatorPath))
                        {
                            var root = JObject.Parse(File.ReadAllText(translatorPath));
                            if (root["plants"] != null)
                            {
                                foreach (var p in root["plants"])
                                {
                                    parsedNames[(int)p["seedType"]] = CleanText((string)p["name"]);
                                }
#if DEBUG
                                TranslatorLogger.Msg($"Imported {parsedNames.Count} Plant names from Pvz Fusion Translator.");
#endif
                            }
                        }
                    }
                    // B. Try to import ZombieType data
                    else if (enumType.Name == "ZombieType")
                    {
                        string translatorPath = Path.Combine(translatorAlmanacDir, "ZombieStringsTranslate.json");
                        if (File.Exists(translatorPath))
                        {
                            var root = JObject.Parse(File.ReadAllText(translatorPath));
                            if (root["zombies"] != null)
                            {
                                foreach (var z in root["zombies"])
                                {
                                    parsedNames[(int)z["theZombieType"]] = CleanText((string)z["name"]);
                                }
#if DEBUG
                                TranslatorLogger.Msg($"Imported {parsedNames.Count} Zombie names from Pvz Fusion Translator.");
#endif
                            }
                        }
                    }

                    // C. Fill in missing gaps
                    Array values = Enum.GetValues(enumType);
                    foreach (object val in values)
                    {
                        int intVal = (int)val;
                        
                        // Dump not found strings
                        if (!parsedNames.ContainsKey(intVal))
                        {
                            parsedNames[intVal] = val.ToString();
                        }
                    }

                    // D. Dump everything into the new Magnetar JSON file
                    string dumpJson = JsonConvert.SerializeObject(parsedNames, Formatting.Indented);
                    File.WriteAllText(targetFile, dumpJson);

#if DEBUG
                    TranslatorLogger.Warning($"Successfully generated {enumType.Name}.json with {parsedNames.Count} total entries.");
#endif
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
