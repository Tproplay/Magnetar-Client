using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public static class Translator
{
    private static readonly Dictionary<string, TranslationDomain> _domains = new(StringComparer.OrdinalIgnoreCase);

    public static string CurrentLanguage => string.IsNullOrWhiteSpace(Config.Language) ? "English" : Config.Language;

    public static bool IsLoading { get; private set; } = false;
    public static event Action OnTranslationsLoaded;

    #region Domain Registration

    public static TranslationDomain RegisterDomain(TranslationDomain domain)
    {
        if (domain == null) throw new ArgumentNullException(nameof(domain));
        _domains[domain.DomainName] = domain;
        return domain;
    }

    public static TranslationDomain CreateDomain(string domainName)
    {
        return RegisterDomain(new TranslationDomain(domainName));
    }

    public static TranslationDomain GetDomain(string domainName)
    {
        return _domains.TryGetValue(domainName, out var domain) ? domain : null;
    }

    public static IReadOnlyCollection<TranslationDomain> AllDomains => _domains.Values;

    #endregion

    #region Asynchronous Loading & Operations

    /// <summary>
    /// Loads translations for all domains asynchronously in a background thread to prevent GUI hitches.
    /// </summary>
    public static async Task LoadTranslationsAsync(Action onComplete = null)
    {
        if (IsLoading) return;
        IsLoading = true;

        string targetLang = CurrentLanguage;
        TranslatorLogger.Msg($"[Translator] Starting load for '{targetLang}' across {_domains.Count} domains...");

        var domainsSnapshot = _domains.Values.ToList();

        await Task.Run(() =>
        {
            foreach (var domain in domainsSnapshot)
            {
                try
                {
                    domain.DumpEnglishTemplate();
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"[Translator] Error dumping domain '{domain.DomainName}': {ex.Message}");
                }
            }

            foreach (var domain in domainsSnapshot)
            {
                try
                {
                    domain.Load(targetLang);
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"[Translator] Error loading domain '{domain.DomainName}': {ex.Message}");
                }
            }
        });

        IsLoading = false;
        TranslatorLogger.Msg("[Translator] Translation loading complete.");

        onComplete?.Invoke();
        OnTranslationsLoaded?.Invoke();
    }

    /// <summary>
    /// Fire-and-forget non-blocking load.
    /// </summary>
    public static void LoadTranslations()
    {
        _ = LoadTranslationsAsync();
    }

    public static void DumpMissingStrings()
    {
        string targetLang = CurrentLanguage;
        foreach (var domain in _domains.Values)
        {
            domain.DumpMissingStrings(targetLang);
        }
    }

    #endregion

    #region Helper functions
    /// <summary>
    /// Serializes an object to JSON and safely writes it to a file, 
    /// recursively creating any missing parent and nested subdirectories.
    /// </summary>
    /// <param name="directory">The base or nested directory path.</param>
    /// <param name="relativePath">The file name or nested relative path (e.g., "Modules/Combat/file.json").</param>
    /// <param name="data">The object to serialize.</param>
    /// <param name="formatting">JSON formatting style (defaults to Indented).</param>
    /// <returns>True if successfully written, false otherwise.</returns>
    public static bool SaveJson(string directory, string relativePath, object data, Formatting formatting = Formatting.Indented)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(relativePath) || data == null)
            return false;

        string combinedPath = Path.Combine(directory, relativePath);
        return SaveJson(combinedPath, data, formatting);
    }

    /// <summary>
    /// Serializes an object to JSON and safely writes it to a target path, 
    /// automatically ensuring all nested directories leading to the file are created.
    /// </summary>
    /// <param name="filePath">The full or relative file path.</param>
    /// <param name="data">The object to serialize.</param>
    /// <param name="formatting">JSON formatting style (defaults to Indented).</param>
    /// <returns>True if successfully written, false otherwise.</returns>
    public static bool SaveJson(string filePath, object data, Formatting formatting = Formatting.Indented)
    {
        if (string.IsNullOrWhiteSpace(filePath) || data == null)
            return false;

        try
        {
            // Resolve the actual target folder (including any nested folders in the path)
            string targetDir = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                // Recursively creates all nested parent directories
                Directory.CreateDirectory(targetDir);
            }

            string json = JsonConvert.SerializeObject(data, formatting);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            TranslatorLogger.Error($"[Translator] Failed to write JSON to '{filePath}': {ex.Message}");
            return false;
        }
    }

    public static Dictionary<string, string> CreateDictionary(string[] stringList)
    {
        var dictionary = new Dictionary<string, string>();
        foreach (string entry in stringList)
        {
            dictionary[entry] = entry;
        }
        return dictionary;
    }
    #endregion
}