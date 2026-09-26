using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Magnetar_Client.Utils.Magnetar_Logger;
using Magnetar_Client.UI.Setting;

#if MELONLOADER || RELEASE_MELON

#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
#endif

namespace Magnetar_Client.Utils;

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

    private static Dictionary<string, string> _exactTranslations = new(StringComparer.Ordinal);
    private static Dictionary<Regex, string> _regexTranslations = new();
    private static Dictionary<System.Type, Dictionary<int, string>> _nameCache = new();
    private static HashSet<string> _regexMatchedInputs = new(StringComparer.Ordinal);
    private static HashSet<string> _knownEnglishStrings = new(StringComparer.Ordinal);
    private static readonly HashSet<string> _dumpedUntranslated = new(StringComparer.Ordinal);
    private static readonly object _untranslatedFileLock = new();

    private static HashSet<string> _cachedBlacklist = new(StringComparer.Ordinal);
    private static bool _blacklistDirty = true;
    private static bool _missingStringsDirty = false;

    private static string ModsDir => SaveLoad.ModsDir;
    private static string TranslationRootDir => Path.Combine(ModsDir, "Magnetar Translation");

    private static string CurrentLanguage => string.IsNullOrWhiteSpace(Config.Language) ? "English" : Config.Language;

    private static string SanitizeFileName(string name)
    {
        string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"[{0}]", invalidChars);
        return Regex.Replace(name, invalidRegStr, "_");
    }

    public static void InvalidateBlacklist()
    {
        _blacklistDirty = true;
    }

    private static void ExtractSettingNames(IEnumerable<Setting> settings, Action<string> onFoundName)
    {
        if (settings == null) return;

        foreach (var setting in settings)
        {
            if (setting == null || string.IsNullOrWhiteSpace(setting.Name)) continue;

            onFoundName(setting.Name);

            // Traverse child settings inside SectionSetting instances
            if (setting is UI.Setting.SectionSetting sec)
            {
                if (sec.Sections != null && sec.Sections.Count > 0)
                {
                    foreach (var section in sec.Sections)
                    {
                        if (section.ChildSettings != null)
                        {
                            ExtractSettingNames(section.ChildSettings, onFoundName);
                        }
                    }
                }
                else if (sec.TemplateFactory != null)
                {
                    // Fallback to sample template if no sections are spawned yet
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
        if (!_blacklistDirty && _cachedBlacklist.Count > 0)
            return _cachedBlacklist;

        _cachedBlacklist.Clear();

        // 1. Live modules in memory (including SectionSetting children)
        bool modulesReady = Core.ModuleManager.Modules != null && Core.ModuleManager.Modules.Count > 0;
        if (modulesReady)
        {
            foreach (var mod in Core.ModuleManager.Modules)
            {
                if (!string.IsNullOrEmpty(mod.Name)) _cachedBlacklist.Add(mod.Name);
                if (!string.IsNullOrEmpty(mod.Description)) _cachedBlacklist.Add(mod.Description);
                if (!string.IsNullOrEmpty(mod.SearchHints)) _cachedBlacklist.Add(mod.SearchHints);

                if (mod.Settings != null)
                {
                    ExtractSettingNames(mod.Settings, name => _cachedBlacklist.Add(name));
                }
            }
        }

        // 2. Live HUD elements in memory
        bool hudReady = Core.HUDRenderer.Elements != null && Core.HUDRenderer.Elements.Count > 0;
        if (hudReady)
        {
            foreach (var element in Core.HUDRenderer.Elements)
            {
                if (!string.IsNullOrEmpty(element.Name))
                    _cachedBlacklist.Add(element.Name);
            }
        }

        // 3. Disk English/Modules/
        string engModulesDir = Path.Combine(TranslationRootDir, "English", "Modules");
        if (Directory.Exists(engModulesDir))
        {
            foreach (string file in Directory.GetFiles(engModulesDir, "*.json"))
            {
                try
                {
                    var node = JsonConvert.DeserializeObject<ModuleTranslationNode>(File.ReadAllText(file));
                    if (node != null)
                    {
                        if (!string.IsNullOrEmpty(node.Name)) _cachedBlacklist.Add(node.Name);
                        if (!string.IsNullOrEmpty(node.Description)) _cachedBlacklist.Add(node.Description);
                        if (!string.IsNullOrEmpty(node.SearchHints)) _cachedBlacklist.Add(node.SearchHints);
                        if (node.Settings != null)
                        {
                            foreach (var s in node.Settings) _cachedBlacklist.Add(s.Key);
                        }
                    }
                }
                catch { }
            }
        }

        // 4. Disk English/hud_translations.json
        string engHudFile = Path.Combine(TranslationRootDir, "English", "hud_translations.json");
        if (File.Exists(engHudFile))
        {
            try
            {
                var hudDict = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(engHudFile));
                if (hudDict != null)
                {
                    foreach (var kvp in hudDict)
                    {
                        _cachedBlacklist.Add(kvp.Key);
                        if (kvp.Value != null && !string.IsNullOrEmpty(kvp.Value.Name))
                            _cachedBlacklist.Add(kvp.Value.Name);
                    }
                }
            }
            catch { }
        }

        if (modulesReady || hudReady)
        {
            _blacklistDirty = false;
        }

        return _cachedBlacklist;
    }

    public static void DumpEnglishTemplate()
    {
        try
        {
            string englishDir = Path.Combine(TranslationRootDir, "English");
            if (!Directory.Exists(englishDir))
            {
                Directory.CreateDirectory(englishDir);
            }

            // Modules Dump
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
                        // Recursively extract all names including SectionSetting children
                        ExtractSettingNames(mod.Settings, name =>
                        {
                            node.Settings[name] = name;
                        });
                    }

                    string fileName = $"{SanitizeFileName(mod.Name)}.json";
                    string modFilePath = Path.Combine(englishModulesDir, fileName);
                    File.WriteAllText(modFilePath, JsonConvert.SerializeObject(node, Formatting.Indented));
                }
            }

            // HUD Dump
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

            // General UI Strings Dump (purged against module and HUD blacklist)
            HashSet<string> blacklist = GetCachedBlacklist();

            string stringsPath = Path.Combine(englishDir, "translation_strings.json");
            Dictionary<string, string> engStrings = new(StringComparer.Ordinal);

            if (File.Exists(stringsPath))
            {
                try
                {
                    engStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(stringsPath)) ?? new(StringComparer.Ordinal);
                }
                catch { }
            }

            var cleanedEngStrings = engStrings
                .Where(kvp => !blacklist.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

            bool dirtyStrings = cleanedEngStrings.Count != engStrings.Count;

            foreach (var str in _knownEnglishStrings)
            {
                if (!blacklist.Contains(str) && !cleanedEngStrings.ContainsKey(str))
                {
                    cleanedEngStrings[str] = str;
                    dirtyStrings = true;
                }
            }

            if (dirtyStrings || !File.Exists(stringsPath))
            {
                File.WriteAllText(stringsPath, JsonConvert.SerializeObject(cleanedEngStrings, Formatting.Indented));
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to dump English template: {ex.Message}");
        }
    }

    public static void LoadTranslations()
    {
        try
        {
            string targetLanguage = CurrentLanguage;

            InvalidateBlacklist();
            DumpEnglishTemplate();

            _exactTranslations.Clear();
            _regexTranslations.Clear();
            _nameCache.Clear();
            _dumpedUntranslated.Clear();
            _modulesLinked = false;
            _hudLinked = false;

            string baseDir = Path.Combine(TranslationRootDir, targetLanguage);
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
                TranslatorLogger.Msg($"Created Magnetar Translation directory for: {targetLanguage}");
            }

            SyncWithEnglishTemplate(targetLanguage);

            // 1. Load exact translation strings
            string stringsPath = Path.Combine(baseDir, "translation_strings.json");
            if (File.Exists(stringsPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(stringsPath);
                    _exactTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new(StringComparer.Ordinal);
                    TranslatorLogger.Msg($"Loaded {_exactTranslations.Count} exact strings for {targetLanguage}.");

                    foreach (var key in _exactTranslations.Keys)
                    {
                        _dumpedUntranslated.Add(key);
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
                            _dumpedUntranslated.Add(key);
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

    private static void SyncWithEnglishTemplate(string targetLanguage)
    {
        if (string.Equals(targetLanguage, "English", StringComparison.OrdinalIgnoreCase)) return;

        string englishDir = Path.Combine(TranslationRootDir, "English");
        string targetDir = Path.Combine(TranslationRootDir, targetLanguage);

        if (!Directory.Exists(englishDir)) return;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        try
        {
            SyncJsonDictionary(
                Path.Combine(englishDir, "translation_strings.json"),
                Path.Combine(targetDir, "translation_strings.json")
            );

            string engModulesDir = Path.Combine(englishDir, "Modules");
            string targetModulesDir = Path.Combine(targetDir, "Modules");
            SyncModulesDirectory(engModulesDir, targetModulesDir);

            SyncHudJson(
                Path.Combine(englishDir, "hud_translations.json"),
                Path.Combine(targetDir, "hud_translations.json")
            );

            string engRegex = Path.Combine(englishDir, "translation_regexs.json");
            string targetRegex = Path.Combine(targetDir, "translation_regexs.json");
            if (File.Exists(engRegex) && !File.Exists(targetRegex))
            {
                File.Copy(engRegex, targetRegex);
            }

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

    private static void SyncJsonDictionary(string engPath, string targetPath)
    {
        if (!File.Exists(engPath)) return;

        HashSet<string> blacklist = GetCachedBlacklist();

        var engDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(engPath)) ?? new();
        var targetDict = new Dictionary<string, string>(StringComparer.Ordinal);

        if (File.Exists(targetPath))
        {
            try { targetDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(targetPath)) ?? new(StringComparer.Ordinal); } catch { }
        }

        int initialCount = targetDict.Count;
        targetDict = targetDict
            .Where(kvp => !blacklist.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

        bool dirty = targetDict.Count != initialCount;

        foreach (var kvp in engDict)
        {
            if (blacklist.Contains(kvp.Key)) continue;

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

    private static void SyncHudJson(string engPath, string targetPath)
    {
        if (!File.Exists(engPath)) return;

        var engHud = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(engPath)) ?? new();
        var targetHud = new Dictionary<string, HudTranslationNode>(StringComparer.Ordinal);

        if (File.Exists(targetPath))
        {
            try { targetHud = JsonConvert.DeserializeObject<Dictionary<string, HudTranslationNode>>(File.ReadAllText(targetPath)) ?? new(StringComparer.Ordinal); } catch { }
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
    /// Translates an input string. Only dumps to untranslated_strings.json if the string
    /// is completely missing from translation_strings.json and regex translations.
    /// </summary>
    public static string Translate(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

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

        // 1. Exact translation
        if (_exactTranslations.TryGetValue(input, out string exactMatch))
        {
            return exactMatch;
        }

        // 2. Regex
        foreach (var rule in _regexTranslations)
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
                        if (_exactTranslations.TryGetValue(val, out string translatedCapture))
                        {
                            finalWord = translatedCapture;
                        }

                        template = template.Replace($"{{{i}}}", finalWord);
                    }
                }

                _regexMatchedInputs.Add(input);
                _exactTranslations[input] = template;
                return template;
            }
        }

        _exactTranslations[input] = input;

        if (string.Equals(CurrentLanguage, "English", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        HashSet<string> blacklist = GetCachedBlacklist();
        if (!blacklist.Contains(input))
        {
            _knownEnglishStrings.Add(input);
            _missingStringsDirty = true;
            AppendUntranslatedStringToDisk(input);
        }

        return input;
    }

    private static void LinkModuleTranslations()
    {
        string currentLang = CurrentLanguage;
        if (string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase)) return;

        string modulesDir = Path.Combine(TranslationRootDir, currentLang, "Modules");
        if (!Directory.Exists(modulesDir)) return;

        try
        {
            var moduleFiles = Directory.GetFiles(modulesDir, "*.json");
            var modulesDict = new Dictionary<string, ModuleTranslationNode>(StringComparer.OrdinalIgnoreCase);

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

            InvalidateBlacklist();
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to link module translations: {ex.Message}");
        }
    }

    private static void LinkHudTranslations()
    {
        string currentLang = CurrentLanguage;
        if (string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase)) return;

        string hudPath = Path.Combine(TranslationRootDir, currentLang, "hud_translations.json");
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

            InvalidateBlacklist();
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to link HUD translations: {ex.Message}");
        }
    }
    

    /// <summary>
    /// Writes the missing untranslated string directly to untranslated_strings.json.
    /// </summary>
    private static void AppendUntranslatedStringToDisk(string input)
    {
        if (!_dumpedUntranslated.Add(input)) return;

        try
        {
            string targetLang = CurrentLanguage;
            if (string.Equals(targetLang, "English", StringComparison.OrdinalIgnoreCase)) return;

            string baseDir = Path.Combine(TranslationRootDir, targetLang);
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            string untranslatedPath = Path.Combine(baseDir, "untranslated_strings.json");
            Dictionary<string, string> dict = new(StringComparer.Ordinal);

            lock (_untranslatedFileLock)
            {
                if (File.Exists(untranslatedPath))
                {
                    try
                    {
                        string json = File.ReadAllText(untranslatedPath);
                        dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new(StringComparer.Ordinal);
                    }
                    catch { }
                }

                if (!dict.ContainsKey(input))
                {
                    dict[input] = input;
                    File.WriteAllText(untranslatedPath, JsonConvert.SerializeObject(dict, Formatting.Indented));
                    TranslatorLogger.Warning($"Dumped untranslated string: \"{input}\"");
                }
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to dump untranslated string '{input}': {ex.Message}");
        }
    }

    public static void DumpMissingStrings()
    {
        if (!_missingStringsDirty) return;

        DumpEnglishTemplate();

        string currentLang = CurrentLanguage;
        if (string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            _missingStringsDirty = false;
            return;
        }

        try
        {
            SyncWithEnglishTemplate(currentLang);

            string baseDir = Path.Combine(TranslationRootDir, currentLang);
            string stringsPath = Path.Combine(baseDir, "translation_strings.json");

            HashSet<string> blacklist = GetCachedBlacklist();

            var cleanDict = _exactTranslations
                .Where(kvp => !_regexMatchedInputs.Contains(kvp.Key) && !blacklist.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

            string generalDump = JsonConvert.SerializeObject(cleanDict, Formatting.Indented);
            File.WriteAllText(stringsPath, generalDump);
            _missingStringsDirty = false;
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

        string activeLang = CurrentLanguage;
        string activeEnumsDir = Path.Combine(TranslationRootDir, activeLang, "Enums");
        if (!Directory.Exists(activeEnumsDir)) Directory.CreateDirectory(activeEnumsDir);

        string activeEnumFile = Path.Combine(activeEnumsDir, $"{enumType.Name}.json");

        if (!string.Equals(activeLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            SyncEnumJson(englishEnumFile, activeEnumFile);
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