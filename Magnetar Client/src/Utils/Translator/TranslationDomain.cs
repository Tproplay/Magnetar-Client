using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public class TranslationDomain
{
    public string DomainName { get; }
    public string FolderName => DomainName;

    // Global domain translation lookup
    private Dictionary<string, string> _exactTranslations = new(StringComparer.Ordinal);
    private Dictionary<Regex, string> _regexTranslations = new();

    // Scoped translation lookup: RelativePath -> (Key -> TranslatedValue)
    // Relative paths are normalized with forward slashes (e.g. "Modules/Esp.json" or "Buttons/Actions.json")
    private Dictionary<string, Dictionary<string, string>> _scopedExactTranslations = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> UntranslatedStrings { get; } = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dumpedUntranslated = new(StringComparer.Ordinal);
    private readonly object _dumpLock = new();

    public event Action<string> OnDumpEnglishTemplate;

    public TranslationDomain(string domainName)
    {
        if (string.IsNullOrWhiteSpace(domainName))
            throw new ArgumentException("Domain name cannot be null or empty.", nameof(domainName));

        DomainName = domainName.Trim();
    }

    public string GetEnglishDirectory() => Path.Combine(TranslationRootDir, "English", DomainName);
    public string GetLanguageDirectory(string language) => Path.Combine(TranslationRootDir, language, DomainName);

    #region Scoped Translation APIs

    /// <summary>
    /// Translates an input string checking scoped relative file(s) or folder(s) first.
    /// If relativeFilesOrDir is null or empty, it queries the general domain cache.
    /// </summary>
    public string Translate(string input, string[] relativeFilesOrDir = null)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // 1. Scoped search inside relative file(s) or folder(s)
        if (relativeFilesOrDir != null && relativeFilesOrDir.Length > 0)
        {
            foreach (var scopePath in relativeFilesOrDir)
            {
                if (string.IsNullOrWhiteSpace(scopePath)) continue;
                string normalizedScope = NormalizePath(scopePath);

                // Exact file match: e.g. "Modules/Speed.json"
                if (_scopedExactTranslations.TryGetValue(normalizedScope, out var fileDict))
                {
                    if (fileDict.TryGetValue(input, out string val))
                        return val;
                }

                // Subdirectory scope match: e.g. "Modules" matches "Modules/Speed.json"
                foreach (var kvp in _scopedExactTranslations)
                {
                    string loadedPath = kvp.Key;
                    if (loadedPath.StartsWith(normalizedScope + "/", StringComparison.OrdinalIgnoreCase) ||
                        loadedPath.StartsWith(normalizedScope + "\\", StringComparison.OrdinalIgnoreCase))
                    {
                        if (kvp.Value.TryGetValue(input, out string subVal))
                            return subVal;
                    }
                }
            }
        }

        // 2. Global fallback across this domain
        if (_exactTranslations.TryGetValue(input, out string exact))
        {
            return exact;
        }

        // 3. Regex match fallback
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
                        string capture = match.Groups[i].Value;
                        if (string.IsNullOrWhiteSpace(capture) || capture == ":" || capture == "：") continue;

                        string trans = _exactTranslations.TryGetValue(capture, out string transVal) ? transVal : capture;
                        template = template.Replace($"{{{i}}}", trans);
                    }
                }

                lock (_exactTranslations)
                {
                    _exactTranslations[input] = template;
                }
                return template;
            }
        }

        // 4. Mark untranslated string if running in a non-English language
        string currentLang = Config.Language ?? "English";
        if (!string.Equals(currentLang, "English", StringComparison.OrdinalIgnoreCase))
        {
            lock (_dumpLock)
            {
                if (_dumpedUntranslated.Add(input))
                {
                    UntranslatedStrings.Add(input);
                    AppendUntranslatedToDisk(input, currentLang);
                }
            }
        }

        return input;
    }

    public string Translate(string input, string relativeFileOrDir)
    {
        return Translate(input, string.IsNullOrEmpty(relativeFileOrDir) ? null : new[] { relativeFileOrDir });
    }

    #endregion

    #region Loading & Synchronization

    public async Task LoadAsync(string language) => await Task.Run(() => Load(language));

    public void Load(string language)
    {
        string targetDir = GetLanguageDirectory(language);
        string englishDir = GetEnglishDirectory();

        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        if (!string.Equals(language, "English", StringComparison.OrdinalIgnoreCase) && Directory.Exists(englishDir))
        {
            SyncDirectoryWithEnglish(englishDir, targetDir);
        }

        if (!Directory.Exists(targetDir)) return;

        var tempExact = new Dictionary<string, string>(StringComparer.Ordinal);
        var tempRegex = new Dictionary<Regex, string>();
        var tempScoped = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string[] jsonFiles = Directory.GetFiles(targetDir, "*.json", SearchOption.AllDirectories);

            foreach (var file in jsonFiles)
            {
                string fileName = Path.GetFileName(file);
                if (string.Equals(fileName, "untranslated.json", StringComparison.OrdinalIgnoreCase)) continue;

                string relativePath = file.Substring(targetDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedRelPath = NormalizePath(relativePath);
                string engMatchingFile = Path.Combine(englishDir, relativePath);

                if (fileName.IndexOf("regex", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        var raw = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                        if (raw != null)
                        {
                            foreach (var kvp in raw) tempRegex[new Regex(kvp.Key, RegexOptions.Compiled)] = kvp.Value;
                        }
                    }
                    catch { }
                    continue;
                }

                if (!tempScoped.TryGetValue(normalizedRelPath, out var fileScopeDict))
                {
                    fileScopeDict = new Dictionary<string, string>(StringComparer.Ordinal);
                    tempScoped[normalizedRelPath] = fileScopeDict;
                }

                LoadTranslationJson(file, engMatchingFile, tempExact, fileScopeDict);
            }

            // Atomic reference updates
            _exactTranslations = tempExact;
            _regexTranslations = tempRegex;
            _scopedExactTranslations = tempScoped;
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[{DomainName}] Error loading translations: {ex.Message}");
        }
    }

    private void LoadTranslationJson(string targetFile, string engFile, Dictionary<string, string> globalExact, Dictionary<string, string> scopeExact)
    {
        try
        {
            JToken targetToken = JToken.Parse(File.ReadAllText(targetFile));
            JToken engToken = File.Exists(engFile) ? JToken.Parse(File.ReadAllText(engFile)) : null;

            ParseJsonToken(targetToken, engToken, globalExact, scopeExact);
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[{DomainName}] Failed parsing '{targetFile}': {ex.Message}");
        }
    }

    private void ParseJsonToken(JToken targetToken, JToken engToken, Dictionary<string, string> globalExact, Dictionary<string, string> scopeExact)
    {
        if (targetToken is JObject targetObj)
        {
            JObject engObj = engToken as JObject;

            foreach (var prop in targetObj.Properties())
            {
                JToken targetVal = prop.Value;
                JToken engVal = engObj?[prop.Name];

                if (targetVal.Type == JTokenType.String)
                {
                    string trans = targetVal.ToString();

                    if (engVal != null && engVal.Type == JTokenType.String)
                    {
                        string eng = engVal.ToString();
                        if (!string.IsNullOrEmpty(eng) && !string.IsNullOrEmpty(trans))
                        {
                            globalExact[eng] = trans;
                            scopeExact[eng] = trans;
                        }
                    }

                    if (!string.IsNullOrEmpty(prop.Name) && !string.IsNullOrEmpty(trans))
                    {
                        globalExact[prop.Name] = trans;
                        scopeExact[prop.Name] = trans;
                    }
                }
                else if (targetVal is JObject || targetVal is JArray)
                {
                    ParseJsonToken(targetVal, engVal, globalExact, scopeExact);
                }
            }
        }
        else if (targetToken is JArray targetArr && engToken is JArray engArr)
        {
            for (int i = 0; i < Math.Min(targetArr.Count, engArr.Count); i++)
            {
                ParseJsonToken(targetArr[i], engArr[i], globalExact, scopeExact);
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    #endregion

    #region Helpers & Disk Sync

    public void DumpEnglishTemplate()
    {
        string englishDir = GetEnglishDirectory();
        if (!Directory.Exists(englishDir)) Directory.CreateDirectory(englishDir);

        try
        {
            OnDumpEnglishTemplate?.Invoke(englishDir);
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[{DomainName}] Error dumping English template: {ex.Message}");
        }
    }

    public void DumpMissingStrings(string language)
    {
        if (UntranslatedStrings.Count == 0 || string.Equals(language, "English", StringComparison.OrdinalIgnoreCase)) return;

        string targetDir = GetLanguageDirectory(language);
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        string untranslatedFile = Path.Combine(targetDir, "untranslated.json");

        lock (_dumpLock)
        {
            try
            {
                var dict = new Dictionary<string, string>(StringComparer.Ordinal);
                if (File.Exists(untranslatedFile))
                {
                    try { dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(untranslatedFile)) ?? new(); } catch { }
                }

                foreach (var str in UntranslatedStrings)
                {
                    if (!dict.ContainsKey(str)) dict[str] = str;
                }

                File.WriteAllText(untranslatedFile, JsonConvert.SerializeObject(dict, Formatting.Indented));
            }
            catch (Exception ex)
            {
                TranslatorLogger.Error($"[{DomainName}] Failed dumping missing strings: {ex.Message}");
            }
        }
    }

    private void AppendUntranslatedToDisk(string input, string language)
    {
        string targetDir = GetLanguageDirectory(language);
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        string untranslatedFile = Path.Combine(targetDir, "untranslated.json");
        try
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            if (File.Exists(untranslatedFile))
            {
                try { dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(untranslatedFile)) ?? new(); } catch { }
            }

            if (!dict.ContainsKey(input))
            {
                dict[input] = input;
                File.WriteAllText(untranslatedFile, JsonConvert.SerializeObject(dict, Formatting.Indented));
                TranslatorLogger.Warning($"[{DomainName}] Dumped untranslated string: \"{input}\"");
            }
        }
        catch { }
    }

    private void SyncDirectoryWithEnglish(string englishDir, string targetDir)
    {
        foreach (var engFile in Directory.GetFiles(englishDir, "*.json", SearchOption.AllDirectories))
        {
            string rel = engFile.Substring(englishDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string targetFile = Path.Combine(targetDir, rel);
            string targetSubDir = Path.GetDirectoryName(targetFile);

            if (!Directory.Exists(targetSubDir)) Directory.CreateDirectory(targetSubDir);

            if (!File.Exists(targetFile))
            {
                try { File.Copy(engFile, targetFile); } catch { }
            }
        }
    }

    #endregion
}