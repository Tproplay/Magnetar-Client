using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public partial class TranslationDomain
{
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
        var tempScopedExact = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var tempScopedRegex = new Dictionary<string, List<KeyValuePair<Regex, string>>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string[] jsonFiles = Directory.GetFiles(targetDir, "*.json", SearchOption.AllDirectories);

            foreach (var file in jsonFiles)
            {
                string fileName = Path.GetFileName(file);
                if (fileName.EndsWith("untranslated.json", StringComparison.OrdinalIgnoreCase)) continue;

                string relativePath = file.Substring(targetDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedRelPath = NormalizePath(relativePath);

                // Strictly load files that belong to this domain's Sources and aren't blacklisted
                if (!IsAllowed(normalizedRelPath))
                    continue;

                string engMatchingFile = Path.Combine(englishDir, relativePath);

                // Matches both old "translation_regexs.json" and new "regex" files
                if (IsRegexPath(normalizedRelPath))
                {
                    LoadRegexJson(file, tempRegex, tempScopedRegex, normalizedRelPath);
                    continue;
                }

                if (!tempScopedExact.TryGetValue(normalizedRelPath, out var fileScopeDict))
                {
                    fileScopeDict = new Dictionary<string, string>(StringComparer.Ordinal);
                    tempScopedExact[normalizedRelPath] = fileScopeDict;
                }

                LoadTranslationJson(file, engMatchingFile, tempExact, fileScopeDict, normalizedRelPath);
            }

            _exactTranslations = tempExact;
            _regexTranslations = tempRegex;
            _scopedExactTranslations = tempScopedExact;
            _scopedRegexTranslations = tempScopedRegex;

            IsLoaded = true;

            CleanUntranslatedOnDisk(targetDir);
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[{DomainName}] Error loading translations: {ex.Message}");
        }
    }

    private static bool IsRegexPath(string normalizedRelPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedRelPath)) return false;

        string[] segments = normalizedRelPath.Split('/');
        foreach (var segment in segments)
        {
            if (segment.IndexOf("regex", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private void LoadRegexJson(string file, Dictionary<Regex, string> globalRegex,
        Dictionary<string, List<KeyValuePair<Regex, string>>> scopedRegex, string normalizedRelPath)
    {
        try
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
            if (raw == null) return;

            var list = new List<KeyValuePair<Regex, string>>();
            foreach (var kvp in raw)
            {
                var compiled = new Regex(kvp.Key, RegexOptions.Compiled);
                globalRegex[compiled] = kvp.Value;
                list.Add(new KeyValuePair<Regex, string>(compiled, kvp.Value));
            }
            scopedRegex[normalizedRelPath] = list;
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[{DomainName}] Failed parsing regex file '{file}': {ex.Message}");
        }
    }

    private void LoadTranslationJson(string targetFile, string engFile, Dictionary<string, string> globalExact,
        Dictionary<string, string> scopeExact, string normalizedRelPath)
    {
        try
        {
            JToken targetToken = JToken.Parse(File.ReadAllText(targetFile));
            JToken engToken = File.Exists(engFile) ? JToken.Parse(File.ReadAllText(engFile)) : null;

            bool isEnumFile = normalizedRelPath.StartsWith("enums/", StringComparison.OrdinalIgnoreCase);
            ParseJsonToken(targetToken, engToken, globalExact, scopeExact, isEnumFile);
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[{DomainName}] Failed parsing '{targetFile}': {ex.Message}");
        }
    }

    private void ParseJsonToken(JToken targetToken, JToken engToken, Dictionary<string, string> globalExact,
        Dictionary<string, string> scopeExact, bool isEnumFile)
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
                            if (!isEnumFile) globalExact[eng] = trans;
                            scopeExact[eng] = trans;
                        }
                    }

                    if (!string.IsNullOrEmpty(prop.Name) && !string.IsNullOrEmpty(trans))
                    {
                        // Numeric enum keys remain strictly scoped to avoid dictionary pollution
                        if (!isEnumFile && !int.TryParse(prop.Name, out _))
                        {
                            globalExact[prop.Name] = trans;
                        }
                        scopeExact[prop.Name] = trans;
                    }
                }
                else if (targetVal is JObject || targetVal is JArray)
                {
                    ParseJsonToken(targetVal, engVal, globalExact, scopeExact, isEnumFile);
                }
            }
        }
        else if (targetToken is JArray targetArr && engToken is JArray engArr)
        {
            for (int i = 0; i < Math.Min(targetArr.Count, engArr.Count); i++)
            {
                ParseJsonToken(targetArr[i], engArr[i], globalExact, scopeExact, isEnumFile);
            }
        }
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').Trim('/');

    #endregion
}