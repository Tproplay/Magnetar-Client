using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using MelonLoader.Utils;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
#endif

namespace Magnetar_Client.Utils
{
    // Defines the nested JSON structure for Modules_translations.json
    public class ModuleTranslationNode
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Search Hints", NullValueHandling = NullValueHandling.Ignore)]
        public string SearchHints { get; set; }

        [JsonProperty("settings")]
        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
    }

    // Defines the JSON structure for hud_translations.json
    public class HudTranslationNode
    {
        [JsonProperty("Name")]
        public string Name { get; set; }
    }

    public static class Translator
    {
        private static bool _isLoaded = false;
        private static bool _modulesLinked = false;
        private static bool _hudLinked = false;
        private static Dictionary<string, string> _exactTranslations = new Dictionary<string, string>();
        private static Dictionary<Regex, string> _regexTranslations = new Dictionary<Regex, string>();
        private static Dictionary<System.Type, Dictionary<int, string>> _nameCache = new Dictionary<System.Type, Dictionary<int, string>>();
        private static HashSet<string> _regexMatchedInputs = new HashSet<string>();

        private static string ModsDir =>
#if MELONLOADER || RELEASE_MELON
            MelonEnvironment.ModsDirectory;
#elif BEPINEX || RELEASE_BEPINEX
            Paths.PluginPath;
#endif

        public static void LoadTranslations()
        {
            string targetLanguage = Config.Language;

            SyncWithEnglishTemplate(targetLanguage);

            string baseDir = Path.Combine(ModsDir, "Magnetar Translation", targetLanguage);

            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
                TranslatorLogger.Msg($"Created Magnetar Translation directory for: {targetLanguage}");
            }

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
            _modulesLinked = false;
            _hudLinked = false;
            _isLoaded = true;
        }

        private static void SyncWithEnglishTemplate(string targetLanguage)
        {
            if (string.Equals(targetLanguage, "English", StringComparison.OrdinalIgnoreCase)) return;

            string englishDir = Path.Combine(ModsDir, "Magnetar Translation", "English");
            string targetDir = Path.Combine(ModsDir, "Magnetar Translation", targetLanguage);

            if (!Directory.Exists(englishDir)) return;

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string[] sourceFiles = Directory.GetFiles(englishDir, "*.json");

            foreach (string file in sourceFiles)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);

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

        private static void LinkModuleTranslations()
        {
            string modulesPath = Path.Combine(ModsDir, "Magnetar Translation", Config.Language, "Modules_translations.json");

            if (!File.Exists(modulesPath))
            {
                DumpMissingStrings();
            }

            if (!File.Exists(modulesPath)) return;

            try
            {
                var modulesDict = JsonConvert.DeserializeObject<Dictionary<string, ModuleTranslationNode>>(File.ReadAllText(modulesPath));
                if (modulesDict == null) return;

                foreach (var mod in Core.ModuleManager.Modules)
                {
                    if (modulesDict.TryGetValue(mod.Name, out var node))
                    {
                        if (!string.IsNullOrEmpty(node.Name)) _exactTranslations[mod.Name] = node.Name;
                        if (!string.IsNullOrEmpty(node.Description)) _exactTranslations[mod.Description] = node.Description;
                        if (!string.IsNullOrEmpty(node.SearchHints) && !string.IsNullOrEmpty(mod.SearchHints)) _exactTranslations[mod.SearchHints] = node.SearchHints;

                        if (mod.Settings != null && node.Settings != null)
                        {
                            foreach (var setting in mod.Settings)
                            {
                                if (string.IsNullOrWhiteSpace(setting.Name)) continue;

                                if (node.Settings.TryGetValue(setting.Name, out var setTrans))
                                {
                                    _exactTranslations[setting.Name] = setTrans;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"Failed to link module translations: {ex.Message}");
            }
        }

        private static void LinkHudTranslations()
        {
            string hudPath = Path.Combine(ModsDir, "Magnetar Translation", Config.Language, "hud_translations.json");

            if (!File.Exists(hudPath))
            {
                DumpMissingStrings();
            }

            if (!File.Exists(hudPath)) return;

            try
            {
                var hudDict = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(hudPath));
                if (hudDict == null) return;

                foreach (var element in Core.HUDRenderer.Elements)
                {
                    if (hudDict.TryGetValue(element.Name, out var node))
                    {
                        if (!string.IsNullOrEmpty(node.Name)) _exactTranslations[element.Name] = node.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"Failed to link HUD translations: {ex.Message}");
            }
        }

        public static void DumpMissingStrings()
        {
            string baseDir = Path.Combine(ModsDir, "Magnetar Translation", Config.Language);
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            string stringsPath = Path.Combine(baseDir, "translation_strings.json");
            string modulesPath = Path.Combine(baseDir, "Modules_translations.json");
            string hudPath = Path.Combine(baseDir, "hud_translations.json");

            // 1. Process Module Translations (Modules_translations.json)
            bool isDirtyModules = !File.Exists(modulesPath);
            Dictionary<string, ModuleTranslationNode> moduleTranslations = new Dictionary<string, ModuleTranslationNode>();

            if (File.Exists(modulesPath))
            {
                try { moduleTranslations = JsonConvert.DeserializeObject<Dictionary<string, ModuleTranslationNode>>(File.ReadAllText(modulesPath)) ?? new Dictionary<string, ModuleTranslationNode>(); } catch { }
            }

            HashSet<string> moduleManagedStrings = new HashSet<string>();

            if (Core.ModuleManager.Modules != null)
            {
                foreach (var mod in Core.ModuleManager.Modules)
                {
                    if (!moduleTranslations.ContainsKey(mod.Name))
                    {
                        moduleTranslations[mod.Name] = new ModuleTranslationNode
                        {
                            Name = _exactTranslations.ContainsKey(mod.Name) ? _exactTranslations[mod.Name] : mod.Name,
                            Description = _exactTranslations.ContainsKey(mod.Description) ? _exactTranslations[mod.Description] : mod.Description,
                            SearchHints = (!string.IsNullOrEmpty(mod.SearchHints) && _exactTranslations.ContainsKey(mod.SearchHints)) ? _exactTranslations[mod.SearchHints] : (mod.SearchHints ?? "")
                        };
                        isDirtyModules = true;
                    }

                    var node = moduleTranslations[mod.Name];

                    if (node.SearchHints == null && !string.IsNullOrEmpty(mod.SearchHints))
                    {
                        node.SearchHints = _exactTranslations.ContainsKey(mod.SearchHints) ? _exactTranslations[mod.SearchHints] : mod.SearchHints;
                        isDirtyModules = true;
                    }

                    moduleManagedStrings.Add(mod.Name);
                    moduleManagedStrings.Add(mod.Description);
                    if (!string.IsNullOrEmpty(mod.SearchHints)) moduleManagedStrings.Add(mod.SearchHints);

                    if (mod.Settings != null)
                    {
                        foreach (var setting in mod.Settings)
                        {
                            if (string.IsNullOrWhiteSpace(setting.Name)) continue;

                            moduleManagedStrings.Add(setting.Name);

                            if (node.Settings == null)
                            {
                                node.Settings = new Dictionary<string, string>();
                                isDirtyModules = true;
                            }

                            if (!node.Settings.ContainsKey(setting.Name))
                            {
                                node.Settings[setting.Name] = _exactTranslations.ContainsKey(setting.Name) ? _exactTranslations[setting.Name] : setting.Name;
                                isDirtyModules = true; 
                            }
                        }
                    }
                }
            }

            if (isDirtyModules && Core.ModuleManager.Modules != null && Core.ModuleManager.Modules.Count > 0)
            {
                try
                {
                    string jsonDump = JsonConvert.SerializeObject(moduleTranslations, Formatting.Indented);
                    File.WriteAllText(modulesPath, jsonDump);
                    TranslatorLogger.Warning($"Refreshed & Auto-dumped module strings into {Config.Language}/Modules_translations.json");
                }
                catch (Exception ex) { TranslatorLogger.Error($"Failed to dump module strings: {ex.Message}"); }
            }

            // 2. Process HUD Translations (hud_translations.json)
            bool isDirtyHud = !File.Exists(hudPath);
            Dictionary<string, HudTranslationNode> hudTranslations = new Dictionary<string, HudTranslationNode>();

            if (File.Exists(hudPath))
            {
                try { hudTranslations = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(hudPath)) ?? new Dictionary<string, HudTranslationNode>(); } catch { }
            }

            HashSet<string> hudManagedStrings = new HashSet<string>();

            if (Core.HUDRenderer.Elements != null)
            {
                foreach (var element in Core.HUDRenderer.Elements)
                {
                    if (!hudTranslations.ContainsKey(element.Name))
                    {
                        hudTranslations[element.Name] = new HudTranslationNode
                        {
                            Name = _exactTranslations.ContainsKey(element.Name) ? _exactTranslations[element.Name] : element.Name
                        };
                        isDirtyHud = true;
                    }
                    hudManagedStrings.Add(element.Name);
                }
            }

            if (isDirtyHud && Core.HUDRenderer.Elements != null && Core.HUDRenderer.Elements.Count > 0)
            {
                try
                {
                    string jsonDump = JsonConvert.SerializeObject(hudTranslations, Formatting.Indented);
                    File.WriteAllText(hudPath, jsonDump);
                    TranslatorLogger.Warning($"Auto-dumped HUD strings into {Config.Language}/hud_translations.json");
                }
                catch (Exception ex) { TranslatorLogger.Error($"Failed to dump HUD strings: {ex.Message}"); }
            }

            // 3. Process Standard Translations (Isolate standard keychains completely)
            var cleanDict = _exactTranslations
                .Where(kvp => !_regexMatchedInputs.Contains(kvp.Key) && !moduleManagedStrings.Contains(kvp.Key) && !hudManagedStrings.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            try
            {
                string jsonDump = JsonConvert.SerializeObject(cleanDict, Formatting.Indented);
                File.WriteAllText(stringsPath, jsonDump);
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"Failed to dump translation strings: {ex.Message}");
            }
        }

        public static string Translate(string input)
        {
            if (!_isLoaded) LoadTranslations();

            if (!_modulesLinked && Core.ModuleManager.Modules != null && Core.ModuleManager.Modules.Count > 0)
            {
                LinkModuleTranslations();
                _modulesLinked = true;
            }

            if (!_hudLinked && Core.HUDRenderer.Elements != null && Core.HUDRenderer.Elements.Count > 0)
            {
                LinkHudTranslations();
                _hudLinked = true;
            }

            if (string.IsNullOrWhiteSpace(input)) return input;

            if (_exactTranslations.TryGetValue(input, out string exactMatch))
            {
                if (exactMatch != input) return exactMatch;
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

                    _regexMatchedInputs.Add(input);
                    _exactTranslations[input] = template;

                    return template;
                }
            }

            if (!_exactTranslations.ContainsKey(input))
            {
                _exactTranslations[input] = input;
                SaveMissingStringToDisk();
            }

            return input;
        }

        private static void SaveMissingStringToDisk()
        {
            try
            {
                string baseDir = Path.Combine(ModsDir, "Magnetar Translation", Config.Language);
                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

                string stringsPath = Path.Combine(baseDir, "translation_strings.json");

                HashSet<string> moduleManagedStrings = new HashSet<string>();
                if (Core.ModuleManager.Modules != null)
                {
                    foreach (var mod in Core.ModuleManager.Modules)
                    {
                        moduleManagedStrings.Add(mod.Name);
                        moduleManagedStrings.Add(mod.Description);
                        if (!string.IsNullOrEmpty(mod.SearchHints)) moduleManagedStrings.Add(mod.SearchHints);

                        if (mod.Settings != null)
                        {
                            foreach (var setting in mod.Settings)
                            {
                                if (string.IsNullOrWhiteSpace(setting.Name)) continue;
                                moduleManagedStrings.Add(setting.Name);
                            }
                        }
                    }
                }

                HashSet<string> hudManagedStrings = new HashSet<string>();
                if (Core.HUDRenderer.Elements != null)
                {
                    foreach (var element in Core.HUDRenderer.Elements)
                    {
                        hudManagedStrings.Add(element.Name);
                    }
                }

                var cleanDict = _exactTranslations
                    .Where(kvp => !_regexMatchedInputs.Contains(kvp.Key) && !moduleManagedStrings.Contains(kvp.Key) && !hudManagedStrings.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                string jsonDump = JsonConvert.SerializeObject(cleanDict, Formatting.Indented);
                File.WriteAllText(stringsPath, jsonDump);
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

            Dictionary<int, string> parsedNames = LoadEnumTranslations(enumType);
            _nameCache[enumType] = parsedNames;
            return new Dictionary<int, string>(parsedNames);
        }

        public static Dictionary<int, string> LoadEnumTranslations(Type enumType)
        {
            Dictionary<int, string> parsedNames = new Dictionary<int, string>();

            string magnetarDir = Path.Combine(ModsDir, "Magnetar Translation", Config.Language);
            if (!Directory.Exists(magnetarDir)) Directory.CreateDirectory(magnetarDir);

            string translatorAlmanacDir = Path.Combine(ModsDir,
                "PvZ_Fusion_Translator", "Localization", Config.Language, "Almanac");

            string targetFile = Path.Combine(magnetarDir, $"{enumType.Name}.json");

            string CleanText(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                return Regex.Replace(input, "<.*?>", string.Empty);
            }

            bool requiresSave = false;

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
#if MELONLOADER
                TranslatorLogger.Warning($"{enumType.Name}.json not found. Generating new file...");
                requiresSave = true;

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
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to parse fallback for {enumType.Name}: {ex.Message}");
                }
#endif
            }

            Array values = Enum.GetValues(enumType);
            int missingCount = 0;

            foreach (object val in values)
            {
                int intVal = (int)val;
                if (!parsedNames.ContainsKey(intVal))
                {
                    parsedNames[intVal] = val.ToString();
                    missingCount++;
                    requiresSave = true;
                }
            }

            if (requiresSave)
            {
                try
                {
                    var sortedNames = parsedNames.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                    string dumpJson = JsonConvert.SerializeObject(sortedNames);

                    File.WriteAllText(targetFile, dumpJson);

                    if (missingCount > 0 && File.Exists(targetFile) && parsedNames.Count > missingCount)
                    {
                        TranslatorLogger.Warning($"Appended {missingCount} missing entries to existing {enumType.Name}.json");
                    }
                    else
                    {
                        TranslatorLogger.Warning($"Successfully generated {enumType.Name}.json with {parsedNames.Count} total entries.");
                    }
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to save {enumType.Name}.json: {ex.Message}");
                }
            }

            return parsedNames;
        }
    }
}