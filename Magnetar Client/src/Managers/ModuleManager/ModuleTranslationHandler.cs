using Magnetar_Client.Api;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core.ModuleManager_;

public static class ModuleTranslationHandler
{
    public static readonly TranslationDomain Domain = Translator.CreateDomain("Module Manager");
    private static readonly Dictionary<Type, Dictionary<int, string>> _enumCache = new();

    public static void Init()
    {
        Domain.OnDumpEnglishTemplate += DumpModuleEnglishTemplates;
    }

    #region Domain Scoping

    public static void SetDomain(Modules.Module module)
    {
        if (module == null)
            throw new ArgumentNullException(nameof(module));

        if (module.Settings == null)
            return;

        string moduleSource = $"Modules/{module.Name}.json";
        string[] sources = new[] { moduleSource };

        ApplyDomainRecursive(module.Settings, sources);
    }

    private static void ApplyDomainRecursive(IEnumerable<Setting> settings, string[] sources)
    {
        if (settings == null) return;

        foreach (Setting setting in settings)
        {
            if (setting == null) continue;

            setting.Domain = Domain;
            setting.TranslationSources = sources;

            if (setting is SectionSetting sectionSetting && sectionSetting.Sections != null)
            {
                foreach (var sec in sectionSetting.Sections)
                {
                    if (sec?.ChildSettings != null)
                    {
                        ApplyDomainRecursive(sec.ChildSettings, sources);
                    }
                }
            }
        }
    }

    #endregion

    #region Lifecycle & Translation Reloading

    public static void OnLanguageChanged()
    {
        InvalidateEnumCache();

        string currentLang = Config.Language ?? "English";

        if (ModuleManager.Modules != null)
        {
            for (int i = 0; i < ModuleManager.Modules.Count; i++)
            {
                var mod = ModuleManager.Modules[i];
                if (mod == null) continue;

                ReloadModuleTranslations(mod, currentLang);
                mod.OnDomainLanguageChanged();
                mod.OnLanguageChanged();
            }
        }
    }

    public static void ReloadModuleTranslations(Modules.Module mod, string language)
    {
        if (mod == null) return;

        string filePath = Path.Combine(
            PathsManager.TranslationRootDir,
            language,
            Domain.DomainName,
            "Modules",
            $"{mod.Name}.json"
        );

        if (!File.Exists(filePath)) return;

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

    #endregion

    #region Template Generation

    private static void DumpModuleEnglishTemplates(string domainDir)
    {
        if (ModuleManager.Modules == null || ModuleManager.Modules.Count == 0) return;


        var templateDirect = new string[]
        {
            "ON", "OFF", "Enabled", "Hold Mode", "KeyBind", "Search..."
        };
        string directTranslationPath = Path.Combine(domainDir, "translation_strings.json");
        Translator.SaveJson(directTranslationPath, Translator.CreateDictionary(templateDirect));

        string targetDir = Path.Combine(domainDir, "Modules");
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        foreach (var mod in ModuleManager.Modules)
        {
            if (mod == null) continue;

            var node = new
            {
                Name = mod.Name,
                Description = mod.Description,
                SearchHints = mod.SearchHints ?? "",
                Settings = new Dictionary<string, string>(),
                Translations = mod.SavedTranslationData ?? new Dictionary<string, string>(),
                RegexTranslations = mod.SavedRegexTranslations ?? new Dictionary<string, string>()
            };

            if (mod.Settings != null)
            {
                ExtractSettingNames(mod.Settings, name => node.Settings[name] = name);
            }

            string filePath = Path.Combine(targetDir, $"{mod.Name}.json");
            Translator.SaveJson(filePath, node);
        }

        var templateRegex = new Dictionary<string,string>
        {
            {"(\\d+) selected", "{1} selected"},
        };
        string regexTranslationPath = Path.Combine(domainDir, "translation_regex.json");
        Translator.SaveJson(regexTranslationPath, templateRegex);
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
                        if (section?.ChildSettings != null)
                        {
                            ExtractSettingNames(section.ChildSettings, onFoundName);
                        }
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

    #endregion

    #region Enum Translations

    public static Dictionary<int, string> TranslateEnum(Type enumType)
    {
        if (enumType == null) return new Dictionary<int, string>();

        if (_enumCache.TryGetValue(enumType, out var cached))
            return new Dictionary<int, string>(cached);

        string currentLang = Config.Language ?? "English";
        string baseDir = Path.Combine(PathsManager.TranslationRootDir, currentLang, Domain.DomainName, "Enums");
        string englishDir = Path.Combine(PathsManager.TranslationRootDir, "English", Domain.DomainName, "Enums");

        if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
        if (!Directory.Exists(englishDir)) Directory.CreateDirectory(englishDir);

        string englishFile = Path.Combine(englishDir, $"{enumType.Name}.json");
        string targetFile = Path.Combine(baseDir, $"{enumType.Name}.json");

        // Sync English definition
        Dictionary<int, string> englishDict = new();
        if (File.Exists(englishFile))
        {
            try { englishDict = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(englishFile)) ?? new(); } catch { }
        }

        bool dirtyEnglish = false;
        Array values = Enum.GetValues(enumType);
        foreach (object val in values)
        {
            int intVal = (int)val;
            if (!englishDict.ContainsKey(intVal))
            {
                englishDict[intVal] = Enum.GetName(enumType, intVal) ?? intVal.ToString();
                dirtyEnglish = true;
            }
        }

        if (dirtyEnglish || !File.Exists(englishFile))
        {
            var sorted = englishDict.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value);
            File.WriteAllText(englishFile, JsonConvert.SerializeObject(sorted, Formatting.Indented));
        }

        if (!string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            SyncEnumJson(englishFile, targetFile);
        }

        Dictionary<int, string> result = new();
        string activeFile = File.Exists(targetFile) ? targetFile : englishFile;

        if (File.Exists(activeFile))
        {
            try
            {
                var raw = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(activeFile));
                if (raw != null)
                {
                    foreach (var kvp in raw)
                    {
                        result[kvp.Key] = Regex.Replace(kvp.Value ?? "", "<.*?>", string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"[ModuleTranslationHandler] Failed loading enum '{enumType.Name}': {ex.Message}");
            }
        }

        _enumCache[enumType] = result;
        return new Dictionary<int, string>(result);
    }

    public static void InvalidateEnumCache() => _enumCache.Clear();

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
            var sorted = targetEnum.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value);
            File.WriteAllText(targetPath, JsonConvert.SerializeObject(sorted, Formatting.Indented));
        }
    }

    #endregion
}