using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public partial class TranslationDomain
{
    #region Untranslated Management

    public void DumpEnglishTemplate()
    {
        string englishDir = GetEnglishDirectory();
        if (!Directory.Exists(englishDir)) Directory.CreateDirectory(englishDir);

        try
        {
            // Triggers hook with the direct english directory (Translations/English)
            OnDumpEnglishTemplate?.Invoke(englishDir);
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[{DomainName}] Error dumping English template: {ex.Message}");
        }
    }

    public void RecordUntranslated(string input)
    {
        if (!DumpUntranslated || !IsLoaded || string.IsNullOrEmpty(input)) return;

        string currentLang = Config.Language ?? "English";

        lock (_dumpLock)
        {
            if (_dumpedUntranslated.Add(input))
            {
                UntranslatedStrings.Add(input);
                AppendUntranslatedToDisk(input, currentLang);
            }
        }
    }

    private string GetUntranslatedFilePath(string targetDir)
    {
        string safeName = string.Join("_", DomainName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(targetDir, $"{safeName}_untranslated.json");
    }

    private void AppendUntranslatedToDisk(string input, string language)
    {
        string targetDir = GetLanguageDirectory(language);
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        string untranslatedFile = GetUntranslatedFilePath(targetDir);
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
                TranslatorLogger.Warning($"[{DomainName}] Dumped untranslated string: \"{input}\" ({language})");
            }
        }
        catch { }
    }

    private void CleanUntranslatedOnDisk(string targetDir)
    {
        string untranslatedFile = GetUntranslatedFilePath(targetDir);
        if (!File.Exists(untranslatedFile)) return;

        lock (_dumpLock)
        {
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(untranslatedFile));
                if (dict == null || dict.Count == 0) return;

                bool modified = false;
                var keysToRemove = new List<string>();

                foreach (var key in dict.Keys)
                {
                    if (_exactTranslations.ContainsKey(key))
                    {
                        keysToRemove.Add(key);
                        modified = true;
                    }
                    else
                    {
                        foreach (var scoped in _scopedExactTranslations.Values)
                        {
                            if (scoped.ContainsKey(key))
                            {
                                keysToRemove.Add(key);
                                modified = true;
                                break;
                            }
                        }
                    }
                }

                foreach (var k in keysToRemove) dict.Remove(k);

                if (modified)
                {
                    if (dict.Count == 0)
                        File.Delete(untranslatedFile);
                    else
                        File.WriteAllText(untranslatedFile, JsonConvert.SerializeObject(dict, Formatting.Indented));
                }
            }
            catch { }
        }
    }

    private void SyncDirectoryWithEnglish(string englishDir, string targetDir)
    {
        foreach (var engFile in Directory.GetFiles(englishDir, "*.json", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(engFile);
            if (fileName.EndsWith("untranslated.json", StringComparison.OrdinalIgnoreCase)) continue;

            string rel = engFile.Substring(englishDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedRel = NormalizePath(rel);

            // Domain only syncs the files it owns
            if (!IsAllowed(normalizedRel))
                continue;

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