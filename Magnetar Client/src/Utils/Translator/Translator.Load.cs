using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public static partial class Translator
{
    private static readonly object _loadLock = new();
    private static readonly object _queueLock = new();
    private static Task _runningWorker = null;
    private static bool _needsAnotherPass = false;
    private static readonly List<Action> _callbacks = new();
    private static readonly ConcurrentQueue<Action> _mainThreadQueue = new();

    /// <summary>
    /// Is new translation files being loaded
    /// </summary>
    public static bool IsLoading { get; private set; }

    internal static void Update()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"Error in main thread callback: {ex}");
            }
        }
    }

    private static Task LoadTranslationsAsync(Action onComplete = null)
    {
        lock (_queueLock)
        {
            if (onComplete != null)
            {
                _callbacks.Add(onComplete);
            }

            if (_runningWorker != null && !_runningWorker.IsCompleted)
            {
                _needsAnotherPass = true;
                return _runningWorker;
            }

            IsLoading = true;
            _runningWorker = Task.Run(RunTranslationWorker);
            return _runningWorker;
        }
    }

    private static void RunTranslationWorker()
    {
        IntPtr threadPtr = IntPtr.Zero;

#if MELONLOADER || RELEASE_MELON || BEPINEX || RELEASE_BEPINEX
        try
        {
            IntPtr domain = Il2CppInterop.Runtime.IL2CPP.il2cpp_domain_get();
            if (domain != IntPtr.Zero)
            {
                threadPtr = Il2CppInterop.Runtime.IL2CPP.il2cpp_thread_attach(domain);
            }
        }
        catch { }
#endif

        try
        {
            while (true)
            {
                lock (_queueLock)
                {
                    _needsAnotherPass = false;
                }

                lock (_loadLock)
                {
                    LoadTranslationsInternal();
                }

                lock (_queueLock)
                {
                    if (_needsAnotherPass)
                    {
                        continue;
                    }

                    IsLoading = false;
                    _runningWorker = null;
                    break;
                }
            }

            List<Action> callbacksToFire;
            lock (_queueLock)
            {
                callbacksToFire = new List<Action>(_callbacks);
                _callbacks.Clear();
            }

            _mainThreadQueue.Enqueue(() =>
            {
                foreach (var cb in callbacksToFire)
                {
                    try
                    {
                        cb?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        TranslatorLogger.Error($"Callback error: {ex}");
                    }
                }
            });
        }
        finally
        {
#if MELONLOADER || RELEASE_MELON || BEPINEX || RELEASE_BEPINEX
            if (threadPtr != IntPtr.Zero)
            {
                try
                {
                    Il2CppInterop.Runtime.IL2CPP.il2cpp_thread_detach(threadPtr);
                }
                catch { }
            }
#endif
        }
    }

    private static void LoadTranslations()
    {
        lock (_loadLock)
        {
            LoadTranslationsInternal();
        }
    }

    private static void LoadTranslationsInternal()
    {
        try
        {
            string targetLanguage = CurrentLanguage;

            InvalidateBlacklist();
            DumpEnglishTemplate();
            TranslationData.ResetForReload();

            string baseDir = Path.Combine(TranslationRootDir, targetLanguage);
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
                TranslatorLogger.Msg($"Created Magnetar Translation directory for: {targetLanguage}");
            }

            SyncWithEnglishTemplate(targetLanguage);

            string stringsPath = Path.Combine(baseDir, "translation_strings.json");
            if (File.Exists(stringsPath))
            {
                try
                {
                    var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(SafeReadAllText(stringsPath));
                    if (parsed != null)
                    {
                        foreach (var kvp in parsed)
                        {
                            TranslationData.ExactTranslations[kvp.Key] = kvp.Value;
                            TranslationData.DumpedUntranslated.Add(kvp.Key);
                        }
                        TranslatorLogger.Msg($"Loaded {parsed.Count} exact strings for {targetLanguage}.");
                    }
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load exact strings: {ex.Message}");
                }
            }

            LoadHudJsonFile(Path.Combine(baseDir, "hud_translations.json"));

            string untranslatedPath = Path.Combine(baseDir, "untranslated_strings.json");
            if (File.Exists(untranslatedPath))
            {
                try
                {
                    var existing = JsonConvert.DeserializeObject<Dictionary<string, string>>(SafeReadAllText(untranslatedPath));
                    if (existing != null)
                    {
                        foreach (var key in existing.Keys)
                            TranslationData.DumpedUntranslated.Add(key);
                    }
                }
                catch { }
            }

            string regexPath = Path.Combine(baseDir, "translation_regexs.json");
            if (File.Exists(regexPath))
            {
                try
                {
                    var rawData = JsonConvert.DeserializeObject<Dictionary<string, string>>(SafeReadAllText(regexPath));
                    if (rawData != null)
                    {
                        foreach (var entry in rawData)
                        {
                            TranslationData.RegexTranslations.Add(new Regex(entry.Key, RegexOptions.Compiled), entry.Value);
                        }
                        TranslatorLogger.Msg($"Loaded {TranslationData.RegexTranslations.Count} regex rules for {targetLanguage}.");
                    }
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"Failed to load regex strings: {ex.Message}");
                }
            }

            foreach (var domain in _domains.Values)
            {
                try { domain.Load(targetLanguage); } catch { }
            }

            if (Core.ModuleManager.IsInitialized)
            {
                LinkModuleTranslations();
            }

            if (Core.HUDManager.IsInitialized)
            {
                LinkHudTranslations();
            }

            TranslationData.IsLoaded = true;
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Load translations failed: {ex.Message}");
            TranslationData.IsLoaded = true;
        }
    }

    private static void LoadHudJsonFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            var token = JToken.Parse(SafeReadAllText(filePath));
            int count = 0;

            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    string key = prop.Name;
                    string value = null;

                    if (prop.Value.Type == JTokenType.String)
                    {
                        value = prop.Value.ToString();
                    }
                    else if (prop.Value is JObject childObj)
                    {
                        value = childObj["Name"]?.ToString() ?? childObj.Properties().FirstOrDefault()?.Value?.ToString();
                    }

                    if (!string.IsNullOrEmpty(value))
                    {
                        TranslationData.ExactTranslations[key] = value;
                        TranslationData.DumpedUntranslated.Add(key);
                        count++;
                    }
                }
            }

            TranslatorLogger.Msg($"Loaded {count} HUD entries from '{Path.GetFileName(filePath)}'.");
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed parsing HUD file '{Path.GetFileName(filePath)}': {ex.Message}");
        }
    }

    /// <summary>
    /// Links, syncs, and reloads HUD translations across all languages.
    /// </summary>
    public static void LinkHudTranslations()
    {
        if (!Core.HUDManager.IsInitialized || Core.HUDManager_.HUDRenderer.Elements == null || Core.HUDManager_.HUDRenderer.Elements.Count == 0) return;

        string englishDir = Path.Combine(TranslationRootDir, "English");
        string engHudPath = Path.Combine(englishDir, "hud_translations.json");

        // 1. Ensure English HUD templates are dumped
        DumpHudEnglishTemplate(englishDir);

        // 2. Sync to all target language directories
        if (Directory.Exists(TranslationRootDir))
        {
            foreach (string langDir in Directory.GetDirectories(TranslationRootDir))
            {
                string lang = Path.GetFileName(langDir);
                if (string.Equals(lang, "English", StringComparison.OrdinalIgnoreCase)) continue;
                SyncHudJson(engHudPath, Path.Combine(langDir, "hud_translations.json"));
            }
        }

        string currentLang = CurrentLanguage;
        if (!string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            string targetHudPath = Path.Combine(TranslationRootDir, currentLang, "hud_translations.json");
            LoadHudJsonFile(targetHudPath);
        }

        // 3. Reload element-specific translations and regexes for all HUD elements
        Core.HUDManager_.HudTranslationHandler.ReloadAllHudTranslations(currentLang);

        if (Core.HUDManager_.HUDRenderer.Elements != null)
        {
            var elements = Core.HUDManager_.HUDRenderer.GetElementsSnapshot();
            foreach (var element in elements)
            {
                if (element == null) continue;
                string typeName = element.GetType().Name;

                if (TranslationData.ExactTranslations.TryGetValue(element.Name, out var transName))
                {
                    TranslationData.ExactTranslations[typeName] = transName;
                    TranslationData.DumpedUntranslated.Add(typeName);
                }
                else if (TranslationData.ExactTranslations.TryGetValue(typeName, out var transType))
                {
                    TranslationData.ExactTranslations[element.Name] = transType;
                    TranslationData.DumpedUntranslated.Add(element.Name);
                }
            }
        }

        TranslationData.HudLinked = true;
        InvalidateBlacklist();
    }

    /// <summary>
    /// Links, syncs, and reloads module translations across all languages.
    /// </summary>
    public static void LinkModuleTranslations()
    {
        if (!Core.ModuleManager.IsInitialized || Core.ModuleManager.Modules == null || Core.ModuleManager.Modules.Count == 0) return;

        string englishDir = Path.Combine(TranslationRootDir, "English");
        string englishModulesDir = Path.Combine(englishDir, "Modules");

        // 1. Ensure English module templates are dumped
        DumpModulesEnglishTemplate(englishDir);

        // 2. Sync to all target language directories
        if (Directory.Exists(TranslationRootDir))
        {
            foreach (string langDir in Directory.GetDirectories(TranslationRootDir))
            {
                string lang = Path.GetFileName(langDir);
                if (string.Equals(lang, "English", StringComparison.OrdinalIgnoreCase)) continue;
                SyncModulesDirectory(englishModulesDir, Path.Combine(langDir, "Modules"));
            }
        }

        string currentLang = CurrentLanguage;

        // 3. If English, reload any user-customized translations from English JSON files
        if (string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            var modules = Core.ModuleManager.GetModulesSnapshot();
            foreach (var mod in modules)
            {
                if (mod == null) continue;
                Core.ModuleManager_.ModuleTranslationHandler.ReloadModuleTranslations(mod, "English");
            }
            TranslationData.ModulesLinked = true;
            return;
        }

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
                    var node = JsonConvert.DeserializeObject<ModuleTranslationNode>(SafeReadAllText(file));
                    if (node != null && !string.IsNullOrEmpty(node.Name))
                    {
                        modulesDict[Path.GetFileNameWithoutExtension(file)] = node;
                    }
                }
                catch { }
            }

            var modules = Core.ModuleManager.GetModulesSnapshot();
            foreach (var mod in modules)
            {
                if (mod == null) continue;

                string sanitizedName = SanitizeFileName(mod.Name);
                if (!modulesDict.TryGetValue(sanitizedName, out var node))
                    modulesDict.TryGetValue(mod.Name, out node);

                if (node != null)
                {
                    if (!string.IsNullOrEmpty(node.Name)) TranslationData.ExactTranslations[mod.Name] = node.Name;
                    if (!string.IsNullOrEmpty(node.Description)) TranslationData.ExactTranslations[mod.Description] = node.Description;
                    if (!string.IsNullOrEmpty(node.SearchHints) && !string.IsNullOrEmpty(mod.SearchHints)) TranslationData.ExactTranslations[mod.SearchHints] = node.SearchHints;

                    if (mod.Settings != null && node.Settings != null)
                    {
                        ExtractSettingNames(mod.Settings, name =>
                        {
                            if (node.Settings.TryGetValue(name, out var setTrans))
                                TranslationData.ExactTranslations[name] = setTrans;
                        });
                    }
                }

                // Reload translated custom phrases and regexes into the module
                Core.ModuleManager_.ModuleTranslationHandler.ReloadModuleTranslations(mod, currentLang);
            }

            TranslationData.ModulesLinked = true;
            InvalidateBlacklist();
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"Failed to link module translations: {ex.Message}");
        }
    }

    internal static Dictionary<int, string> LoadEnumTranslations(Type enumType)
    {
        string englishEnumsDir = Path.Combine(TranslationRootDir, "English", "Enums");
        if (!Directory.Exists(englishEnumsDir)) Directory.CreateDirectory(englishEnumsDir);

        string englishEnumFile = Path.Combine(englishEnumsDir, $"{enumType.Name}.json");

        Dictionary<int, string> englishEnumDict = new();
        if (File.Exists(englishEnumFile))
        {
            try
            {
                englishEnumDict = JsonConvert.DeserializeObject<Dictionary<int, string>>(SafeReadAllText(englishEnumFile)) ?? new();
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
            SafeWriteAllText(englishEnumFile, JsonConvert.SerializeObject(sortedEnglish, Formatting.Indented));
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
                var rawData = JsonConvert.DeserializeObject<Dictionary<int, string>>(SafeReadAllText(activeEnumFile));
                if (rawData != null)
                {
                    foreach (var kvp in rawData)
                        parsedNames[kvp.Key] = Regex.Replace(kvp.Value ?? "", "<.*?>", string.Empty);
                }
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"Failed parsing enum file {enumType.Name}.json: {ex.Message}");
            }
        }

        return parsedNames;
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

            SyncModulesDirectory(
                Path.Combine(englishDir, "Modules"),
                Path.Combine(targetDir, "Modules")
            );

            SyncHudJson(
                Path.Combine(englishDir, "hud_translations.json"),
                Path.Combine(targetDir, "hud_translations.json")
            );

            string engRegex = Path.Combine(englishDir, "translation_regexs.json");
            string targetRegex = Path.Combine(targetDir, "translation_regexs.json");
            if (File.Exists(engRegex) && !File.Exists(targetRegex))
                File.Copy(engRegex, targetRegex);

            string englishEnumsDir = Path.Combine(englishDir, "Enums");
            string targetEnumsDir = Path.Combine(targetDir, "Enums");

            if (Directory.Exists(englishEnumsDir))
            {
                if (!Directory.Exists(targetEnumsDir)) Directory.CreateDirectory(targetEnumsDir);

                foreach (string engEnumFile in Directory.GetFiles(englishEnumsDir, "*.json"))
                {
                    string fileName = Path.GetFileName(engEnumFile);
                    SyncEnumJson(engEnumFile, Path.Combine(targetEnumsDir, fileName));
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
        var engDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(SafeReadAllText(engPath)) ?? new();
        var targetDict = new Dictionary<string, string>(StringComparer.Ordinal);

        if (File.Exists(targetPath))
        {
            try { targetDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(SafeReadAllText(targetPath)) ?? new(StringComparer.Ordinal); } catch { }
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
            SafeWriteAllText(targetPath, JsonConvert.SerializeObject(targetDict, Formatting.Indented));
        }
    }

    /// <summary>
    /// Synchronizes module translation JSON files from the English directory to the target language directory,
    /// adding missing keys, settings, custom translations, and regex translations.
    /// </summary>
    /// <param name="engDir">Path to the English modules directory.</param>
    /// <param name="targetDir">Path to the target language modules directory.</param>
    public static void SyncModulesDirectory(string engDir, string targetDir)
    {
        if (!Directory.Exists(engDir)) return;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        foreach (string engFile in Directory.GetFiles(engDir, "*.json"))
        {
            string fileName = Path.GetFileName(engFile);
            string targetFile = Path.Combine(targetDir, fileName);

            var engNode = JsonConvert.DeserializeObject<ModuleTranslationNode>(SafeReadAllText(engFile));
            if (engNode == null) continue;

            ModuleTranslationNode targetNode = null;
            if (File.Exists(targetFile))
            {
                try { targetNode = JsonConvert.DeserializeObject<ModuleTranslationNode>(SafeReadAllText(targetFile)); } catch { }
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
                SafeWriteAllText(targetFile, JsonConvert.SerializeObject(targetNode, Formatting.Indented));
            }
        }
    }

    /// <summary>
    /// Synchronizes hud_translations.json from the English template to a target language file,
    /// syncing missing elements, translated element names, and custom translation/regex properties.
    /// </summary>
    /// <param name="engPath">Path to English hud_translations.json</param>
    /// <param name="targetPath">Path to target language hud_translations.json</param>
    public static void SyncHudJson(string engPath, string targetPath)
    {
        if (!File.Exists(engPath)) return;

        try
        {
            JToken engToken = JToken.Parse(SafeReadAllText(engPath));
            JToken targetToken = File.Exists(targetPath) ? JToken.Parse(SafeReadAllText(targetPath)) : null;

            bool dirty = false;

            if (engToken is JObject engObj)
            {
                JObject targetObj = targetToken as JObject ?? new JObject();

                foreach (var prop in engObj.Properties())
                {
                    if (targetObj[prop.Name] == null)
                    {
                        targetObj[prop.Name] = prop.Value.DeepClone();
                        dirty = true;
                    }
                    else if (prop.Value is JObject engSub && targetObj[prop.Name] is JObject targetSub)
                    {
                        foreach (var subProp in engSub.Properties())
                        {
                            if (targetSub[subProp.Name] == null || string.IsNullOrEmpty(targetSub[subProp.Name]?.ToString()))
                            {
                                targetSub[subProp.Name] = subProp.Value.DeepClone();
                                dirty = true;
                            }
                        }
                    }
                    else if (prop.Value.Type == JTokenType.String && string.IsNullOrEmpty(targetObj[prop.Name]?.ToString()))
                    {
                        targetObj[prop.Name] = prop.Value;
                        dirty = true;
                    }
                }

                if (dirty || !File.Exists(targetPath))
                {
                    SafeWriteAllText(targetPath, targetObj.ToString(Formatting.Indented));
                }
            }
            else if (engToken is JArray engArr)
            {
                if (!File.Exists(targetPath))
                {
                    File.Copy(engPath, targetPath);
                }
            }
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[SyncHudJson] Failed syncing '{Path.GetFileName(engPath)}': {ex.Message}");
        }
    }

    private static void SyncEnumJson(string engPath, string targetPath)
    {
        if (!File.Exists(engPath)) return;

        var engEnum = JsonConvert.DeserializeObject<Dictionary<int, string>>(SafeReadAllText(engPath)) ?? new();
        var targetEnum = new Dictionary<int, string>();

        if (File.Exists(targetPath))
        {
            try { targetEnum = JsonConvert.DeserializeObject<Dictionary<int, string>>(SafeReadAllText(targetPath)) ?? new(); } catch { }
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
            SafeWriteAllText(targetPath, JsonConvert.SerializeObject(sorted, Formatting.Indented));
        }
    }
}