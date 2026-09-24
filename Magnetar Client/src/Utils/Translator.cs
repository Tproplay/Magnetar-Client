using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON

#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
#endif

namespace Magnetar_Client.Utils;

/// <summary>
/// Json format in which Module translations are saved
/// </summary>
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

/// <summary>
/// Json format in which HUD translations are saved
/// </summary>
public class HudTranslationNode
{
    [JsonProperty("Name")]
    public string Name { get; set; }
}

/// <summary>
/// Translator for magnetar client
/// </summary>
public static class Translator
{
    private static bool _isLoaded = false;
    private static bool _modulesLinked = false;
    private static bool _hudLinked = false;
    private static Dictionary<string, string> _exactTranslations = new();
    private static Dictionary<Regex, string> _regexTranslations = new();
    private static Dictionary<System.Type, Dictionary<int, string>> _nameCache = new();
    private static HashSet<string> _regexMatchedInputs = new();
    private static HashSet<string> _knownEnglishStrings = new();

    private static string ModsDir => SaveLoad.ModsDir;
    private static string TranslationRootDir => Path.Combine(ModsDir, "Magnetar Translation");

    private static string SanitizeFileName(string name)
    {
        string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"[{0}]", invalidChars);
        return Regex.Replace(name, invalidRegStr, "_");
    }

    /// <summary>
    /// Loads the {Config.Language} Language translation files from the disk.
    /// </summary>
    public static void LoadTranslations()
    {
        try
        {
            string targetLanguage = Config.Language;
            bool isEnglish = string.Equals(targetLanguage, "English", StringComparison.OrdinalIgnoreCase);

            DumpEnglishTemplate();

            // 1. Clear the mods translation data
            _exactTranslations.Clear();
            _regexTranslations.Clear();
            _nameCache.Clear();
            _modulesLinked = false;
            _hudLinked = false;

            // 2. English general UI strings/modules bypass disk (enum translations still load from disk via TranslateEnum)
            if (isEnglish)
            {
                _isLoaded = true;
                TranslatorLogger.Msg("Loaded English language.");
                return;
            }

            string baseDir = Path.Combine(TranslationRootDir, targetLanguage);
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
                TranslatorLogger.Msg($"Created Magnetar Translation directory for: {targetLanguage}");
            }

            // 3. Compare with English template and fill missing keys
            SyncWithEnglishTemplate(targetLanguage);

            // 4. Load exact strings
            string stringsPath = Path.Combine(baseDir, "translation_strings.json");
            if (File.Exists(stringsPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(stringsPath);
                    _exactTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new();
                    TranslatorLogger.Msg($"Loaded {_exactTranslations.Count} exact strings for {targetLanguage}.");
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load exact strings: {ex.Message}");
                }
            }

            // 5. Load regexes
            string regexPath = Path.Combine(baseDir, "translation_regexs.json");
            if (File.Exists(regexPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(regexPath);
                    var rawData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new();

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

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Load translations failed: {ex.Message}");
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Dumps the english localization strings.
    /// </summary>
    public static void DumpEnglishTemplate()
    {
        try
        {
            string englishDir = Path.Combine(TranslationRootDir, "English");
            if (!Directory.Exists(englishDir))
            {
                Directory.CreateDirectory(englishDir);
            }

            // 1. Modules Dump into individual JSON files inside English/Modules/
            if (Core.ModuleManager.Modules != null && Core.ModuleManager.Modules.Count > 0)
            {
                string englishModulesDir = Path.Combine(englishDir, "Modules");
                if (!Directory.Exists(englishModulesDir)) Directory.CreateDirectory(englishModulesDir);

                foreach (var mod in Core.ModuleManager.Modules)
                {
                    var node = new ModuleTranslationNode
                    {
                        Name = mod.Name,
                        Description = mod.Description,
                        SearchHints = mod.SearchHints ?? ""
                    };

                    if (mod.Settings != null)
                    {
                        foreach (var setting in mod.Settings)
                        {
                            if (!string.IsNullOrWhiteSpace(setting.Name))
                                node.Settings[setting.Name] = setting.Name;
                        }
                    }

                    string fileName = $"{SanitizeFileName(mod.Name)}.json";
                    string modFilePath = Path.Combine(englishModulesDir, fileName);
                    File.WriteAllText(modFilePath, JsonConvert.SerializeObject(node, Formatting.Indented));
                }
            }

            // 2. HUD Dump
            if (Core.HUDRenderer.Elements != null && Core.HUDRenderer.Elements.Count > 0)
            {
                var engHud = new Dictionary<string, HudTranslationNode>();
                foreach (var el in Core.HUDRenderer.Elements)
                {
                    engHud[el.Name] = new HudTranslationNode { Name = el.Name };
                }

                string hudPath = Path.Combine(englishDir, "hud_translations.json");
                File.WriteAllText(hudPath, JsonConvert.SerializeObject(engHud, Formatting.Indented));
            }

            // 3. General UI Strings Dump
            string stringsPath = Path.Combine(englishDir, "translation_strings.json");
            Dictionary<string, string> engStrings = new();

            if (File.Exists(stringsPath))
            {
                try { engStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(stringsPath)) ?? new(); } catch { }
            }

            bool dirtyStrings = false;
            foreach (var str in _knownEnglishStrings)
            {
                if (!engStrings.ContainsKey(str))
                {
                    engStrings[str] = str;
                    dirtyStrings = true;
                }
            }

            if (dirtyStrings || !File.Exists(stringsPath))
            {
                File.WriteAllText(stringsPath, JsonConvert.SerializeObject(engStrings, Formatting.Indented));
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to dump English template: {ex.Message}");
        }
    }

    /// <summary>
    /// Compares the target language folder files with english and fills in the missing entries.
    /// </summary>
    private static void SyncWithEnglishTemplate(string targetLanguage)
    {
        if (string.Equals(targetLanguage, "English", StringComparison.OrdinalIgnoreCase)) return;

        string englishDir = Path.Combine(TranslationRootDir, "English");
        string targetDir = Path.Combine(TranslationRootDir, targetLanguage);

        if (!Directory.Exists(englishDir)) return;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        try
        {
            // 1. Sync translation_strings.json
            SyncJsonDictionary(
                Path.Combine(englishDir, "translation_strings.json"),
                Path.Combine(targetDir, "translation_strings.json")
            );

            // 2. Sync Modules/ directory (file by file)
            string engModulesDir = Path.Combine(englishDir, "Modules");
            string targetModulesDir = Path.Combine(targetDir, "Modules");
            SyncModulesDirectory(engModulesDir, targetModulesDir);

            // 3. Sync hud_translations.json
            SyncHudJson(
                Path.Combine(englishDir, "hud_translations.json"),
                Path.Combine(targetDir, "hud_translations.json")
            );

            // 4. Sync translation_regexs.json
            string engRegex = Path.Combine(englishDir, "translation_regexs.json");
            string targetRegex = Path.Combine(targetDir, "translation_regexs.json");
            if (File.Exists(engRegex) && !File.Exists(targetRegex))
            {
                File.Copy(engRegex, targetRegex);
            }

            // 5. Sync Enums/ folder
            string englishEnumsDir = Path.Combine(englishDir, "Enums");
            string targetEnumsDir = Path.Combine(targetDir, "Enums");

            if (Directory.Exists(englishEnumsDir))
            {
                if (!Directory.Exists(targetEnumsDir)) Directory.CreateDirectory(targetEnumsDir);

                foreach (string engEnumFile in Directory.GetFiles(englishEnumsDir, "*.json"))
                {
                    string fileName = Path.GetFileName(engEnumFile);
                    string targetEnumFile = Path.Combine(targetEnumsDir, fileName);
                    SyncEnumJson(engEnumFile, targetEnumFile);
                }
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"SyncWithEnglishTemplate error: {ex.Message}");
        }
    }

    /// <summary>
    /// Dumps the missing entries in target file from the english file
    /// </summary>
    private static void SyncJsonDictionary(string engPath, string targetPath)
    {
        if (!File.Exists(engPath)) return;

        var engDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(engPath)) ?? new();
        var targetDict = new Dictionary<string, string>();

        if (File.Exists(targetPath))
        {
            try { targetDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(targetPath)) ?? new(); } catch { }
        }

        bool dirty = false;
        foreach (var kvp in engDict)
        {
            if (!targetDict.ContainsKey(kvp.Key) || string.IsNullOrEmpty(targetDict[kvp.Key]))
            {
                targetDict[kvp.Key] = kvp.Value;
                dirty = true;
            }
        }

        if (dirty || !File.Exists(targetPath))
        {
            File.WriteAllText(targetPath, JsonConvert.SerializeObject(targetDict, Formatting.Indented));
        }
    }

    /// <summary>
    /// Compares English Modules folder with target Modules folder and updates/creates each individual module json file
    /// </summary>
    private static void SyncModulesDirectory(string engDir, string targetDir)
    {
        if (!Directory.Exists(engDir)) return;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        foreach (string engFile in Directory.GetFiles(engDir, "*.json"))
        {
            string fileName = Path.GetFileName(engFile);
            string targetFile = Path.Combine(targetDir, fileName);

            var engNode = JsonConvert.DeserializeObject<ModuleTranslationNode>(File.ReadAllText(engFile));
            if (engNode == null) continue;

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
                    Settings = engNode.Settings != null ? new Dictionary<string, string>(engNode.Settings) : new()
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
            }

            if (dirty || !File.Exists(targetFile))
            {
                File.WriteAllText(targetFile, JsonConvert.SerializeObject(targetNode, Formatting.Indented));
            }
        }
    }

    /// <summary>
    /// Dumps the missing Hud translation entries in target file from the english file
    /// </summary>
    private static void SyncHudJson(string engPath, string targetPath)
    {
        if (!File.Exists(engPath)) return;

        var engHud = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(engPath)) ?? new();
        var targetHud = new Dictionary<string, HudTranslationNode>();

        if (File.Exists(targetPath))
        {
            try { targetHud = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(targetPath)) ?? new(); } catch { }
        }

        bool dirty = false;
        foreach (var kvp in engHud)
        {
            if (!targetHud.TryGetValue(kvp.Key, out var targetNode))
            {
                targetHud[kvp.Key] = new HudTranslationNode { Name = kvp.Value.Name };
                dirty = true;
            }
            else if (string.IsNullOrEmpty(targetNode.Name))
            {
                targetNode.Name = kvp.Value.Name;
                dirty = true;
            }
        }

        if (dirty || !File.Exists(targetPath))
        {
            File.WriteAllText(targetPath, JsonConvert.SerializeObject(targetHud, Formatting.Indented));
        }
    }

    /// <summary>
    /// Dumps the missing enum entries in target file from the english file
    /// </summary>
    private static void SyncEnumJson(string engPath, string targetPath)
    {
        if (!File.Exists(engPath)) return;

        var engEnum = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(engPath)) ?? new();
        var targetEnum = new Dictionary<int, string>();

        if (File.Exists(targetPath))
        {
            try { targetEnum = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(targetPath)) ?? new(); } catch { }
        }

        bool dirty = false;
        foreach (var kvp in engEnum)
        {
            if (!targetEnum.ContainsKey(kvp.Key) || string.IsNullOrEmpty(targetEnum[kvp.Key]))
            {
                targetEnum[kvp.Key] = kvp.Value;
                dirty = true;
            }
        }

        if (dirty || !File.Exists(targetPath))
        {
            var sorted = targetEnum.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText(targetPath, JsonConvert.SerializeObject(sorted, Formatting.Indented));
        }
    }

    /// <summary>
    /// Loads the {Config.Language} module translations from individual files in the Modules directory
    /// </summary>
    private static void LinkModuleTranslations()
    {
        if (string.Equals(Config.Language, "English", StringComparison.OrdinalIgnoreCase)) return;

        string modulesDir = Path.Combine(TranslationRootDir, Config.Language, "Modules");
        DumpMissingStrings();

        if (!Directory.Exists(modulesDir)) return;

        try
        {
            var moduleFiles = Directory.GetFiles(modulesDir, "*.json");
            var modulesDict = new Dictionary<string, ModuleTranslationNode>();

            foreach (var file in moduleFiles)
            {
                try
                {
                    var node = JsonConvert.DeserializeObject<ModuleTranslationNode>(File.ReadAllText(file));
                    if (node != null && !string.IsNullOrEmpty(node.Name))
                    {
                        string originalName = Path.GetFileNameWithoutExtension(file);
                        modulesDict[originalName] = node;
                    }
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to parse module translation file {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            if (Core.ModuleManager.Modules != null)
            {
                foreach (var mod in Core.ModuleManager.Modules)
                {
                    string sanitizedName = SanitizeFileName(mod.Name);
                    ModuleTranslationNode node = null;

                    if (!modulesDict.TryGetValue(sanitizedName, out node))
                    {
                        modulesDict.TryGetValue(mod.Name, out node);
                    }

                    if (node != null)
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
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to link module translations: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the {Config.Language} hud translations
    /// </summary>
    private static void LinkHudTranslations()
    {
        if (string.Equals(Config.Language, "English", StringComparison.OrdinalIgnoreCase)) return;

        string hudPath = Path.Combine(TranslationRootDir, Config.Language, "hud_translations.json");
        DumpMissingStrings();

        if (!File.Exists(hudPath)) return;

        try
        {
            var hudDict = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(hudPath));
            if (hudDict == null) return;

            if (Core.HUDRenderer.Elements != null)
            {
                foreach (var element in Core.HUDRenderer.Elements)
                {
                    if (hudDict.TryGetValue(element.Name, out var node))
                    {
                        if (!string.IsNullOrEmpty(node.Name)) _exactTranslations[element.Name] = node.Name;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to link HUD translations: {ex.Message}");
        }
    }

    /// <summary>
    /// Dumps the missing Module and hud translations in {Config.Language}
    /// </summary>
    public static void DumpMissingStrings()
    {
        // Update the english dump
        DumpEnglishTemplate();

        if (string.Equals(Config.Language, "English", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            SyncWithEnglishTemplate(Config.Language);

            string baseDir = Path.Combine(TranslationRootDir, Config.Language);
            string stringsPath = Path.Combine(baseDir, "translation_strings.json");

            HashSet<string> moduleManagedStrings = new();
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
                            if (!string.IsNullOrWhiteSpace(setting.Name))
                                moduleManagedStrings.Add(setting.Name);
                        }
                    }
                }
            }

            HashSet<string> hudManagedStrings = new();
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

            string generalDump = JsonConvert.SerializeObject(cleanDict, Formatting.Indented);
            File.WriteAllText(stringsPath, generalDump);
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to dump missing translation strings: {ex.Message}");
        }
    }

    /// <summary>
    /// Translates a english string to loaded language
    /// </summary>
    public static string Translate(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        _knownEnglishStrings.Add(input);

        // English returns direct base-mod string without querying disk
        if (string.Equals(Config.Language, "English", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

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
                List<string> captures = new();

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
        if (string.Equals(Config.Language, "English", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            string baseDir = Path.Combine(TranslationRootDir, Config.Language);
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            string stringsPath = Path.Combine(baseDir, "translation_strings.json");

            HashSet<string> moduleManagedStrings = new();
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
                            if (!string.IsNullOrWhiteSpace(setting.Name))
                                moduleManagedStrings.Add(setting.Name);
                        }
                    }
                }
            }

            HashSet<string> hudManagedStrings = new();
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

    /// <summary>
    /// Translates an enum and returns a dictionary
    /// </summary>
    public static Dictionary<int, string> TranslateEnum(Type enumType)
    {
        if (enumType == null)
        {
            TranslatorLogger.Error("TranslateEnum called with null enumType!");
            return new Dictionary<int, string>();
        }

        if (_nameCache.TryGetValue(enumType, out var cachedDict))
        {
            return new Dictionary<int, string>(cachedDict);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            Dictionary<int, string> parsedNames = LoadEnumTranslations(enumType);
            _nameCache[enumType] = parsedNames;
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

    /// <summary>
    /// Loads the translations for the enumType from Enums folder
    /// </summary>
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

        // Add any new variants defined in code/game that are missing from the English template
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

        string activeLang = Config.Language;
        string activeEnumsDir = Path.Combine(TranslationRootDir, activeLang, "Enums");
        if (!Directory.Exists(activeEnumsDir)) Directory.CreateDirectory(activeEnumsDir);

        string activeEnumFile = Path.Combine(activeEnumsDir, $"{enumType.Name}.json");

        // For non-English languages, sync with English template
        if (!string.Equals(activeLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            SyncEnumJson(englishEnumFile, activeEnumFile);
        }

        // Read the enum translations from disk
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