using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public static partial class Translator
{
    private static string TranslationRootDir => TranslationData.TranslationRootDir;

    private static string CurrentLanguage => TranslationData.CurrentLanguage;

    private static readonly Dictionary<string, TranslationDomain> _domains = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a translation domain
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static TranslationDomain RegisterDomain(TranslationDomain domain)
    {
        if (domain == null) throw new ArgumentNullException(nameof(domain));
        _domains[domain.DomainName] = domain;
        return domain;
    }

    /// <summary>
    /// Creates and registers a new translation domain
    /// </summary>
    public static TranslationDomain CreateDomain(string domainName, params string[] sources) =>
        RegisterDomain(new TranslationDomain(domainName, sources));

    /// <summary>
    /// Tries to find a domain from name
    /// </summary>
    public static TranslationDomain GetDomain(string domainName) =>
        _domains.TryGetValue(domainName, out var domain) ? domain : null;

    /// <summary>
    /// Get all the registered domains
    /// </summary>
    public static IReadOnlyCollection<TranslationDomain> AllDomains => _domains.Values;

    /// <summary>
    /// Ensures that translations are loaded and that module and HUD templates are dumped, synced, and linked.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (!TranslationData.IsLoaded)
        {
            LoadTranslations();
        }

        if (!TranslationData.ModulesLinked && Core.ModuleManager.Modules != null && Core.ModuleManager.Modules.Count > 0)
        {
            LinkModuleTranslations();
            TranslationData.ModulesLinked = true;
        }

        if (!TranslationData.HudLinked && Core.HUDManager_.HUDRenderer.Elements != null && Core.HUDManager_.HUDRenderer.Elements.Count > 0)
        {
            LinkHudTranslations();
            TranslationData.HudLinked = true;
        }
    }

    /// <summary>
    /// Applies a regex translation template to an input string, supporting named placeholders
    /// (e.g. {colorName}), numbered capture groups ({1}, {2}), and optional group lookup in local translations.
    /// </summary>
    /// <param name="input">The input string to match and replace.</param>
    /// <param name="pattern">The regex pattern or placeholder template.</param>
    /// <param name="replacement">The replacement template with placeholders or group numbers.</param>
    /// <param name="exactLookup">Optional local translation dictionary to translate captured non-numeric groups.</param>
    /// <param name="result">The resulting string after replacement if matched.</param>
    /// <returns>True if matched and replaced; otherwise false.</returns>
    public static bool TryApplyRegexReplacement(string input, string pattern, string replacement, Dictionary<string, string> exactLookup, out string result)
    {
        result = input;
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(replacement))
            return false;

        try
        {
            // A. Escape literal unescaped parentheses around simple words (e.g. "(Real)" -> "\(Real\)")
            string normalizedPattern = Regex.Replace(pattern, @"(?<!\\)\(([a-zA-Z0-9_\s]+)\)", @"\($1\)");

            // B. Convert named variable placeholders like "{colorName}" into named regex capture groups
            normalizedPattern = Regex.Replace(normalizedPattern, @"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", "(?<$1>[^>]+)");

            Match match = Regex.Match(input, normalizedPattern);
            if (!match.Success)
            {
                match = Regex.Match(input, pattern);
                if (!match.Success) return false;
            }

            string templated = replacement;

            // 1. Replace named placeholders first: {colorName} -> captured group value
            foreach (Group g in match.Groups)
            {
                if (!int.TryParse(g.Name, out _))
                {
                    templated = templated.Replace($"{{{g.Name}}}", g.Value);
                }
            }

            // 2. Replace numbered placeholders: {1}, {2}, etc.
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (match.Groups[i].Success)
                {
                    string val = match.Groups[i].Value;
                    string sub = val;

                    if (!long.TryParse(val, out _) && exactLookup != null && exactLookup.TryGetValue(val, out var transSub))
                    {
                        sub = transSub;
                    }

                    templated = templated.Replace($"{{{i}}}", sub);
                }
            }

            result = templated;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Translates a string
    /// </summary>
    public static string Translate(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        if (!TranslationData.IsLoaded) LoadTranslations();

        if (!TranslationData.ModulesLinked && Core.ModuleManager.Modules != null && Core.ModuleManager.Modules.Count > 0)
        {
            LinkModuleTranslations();
            TranslationData.ModulesLinked = true;
        }

        if (!TranslationData.HudLinked && Core.HUDManager_.HUDRenderer.Elements != null && Core.HUDManager_.HUDRenderer.Elements.Count > 0)
        {
            LinkHudTranslations();
            TranslationData.HudLinked = true;
        }

        if (TranslationData.ExactTranslations.TryGetValue(input, out string exactMatch))
            return exactMatch;

        string trimmedInput = input.Trim();
        if (string.IsNullOrWhiteSpace(trimmedInput)) return input;

        if (TranslationData.ExactTranslations.TryGetValue(trimmedInput, out string trimmedMatch))
            return input.StartsWith(" ") || input.EndsWith(" ") ? input.Replace(trimmedInput, trimmedMatch) : trimmedMatch;

        foreach (var rule in TranslationData.RegexTranslations)
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
                        if (!long.TryParse(val, out _) && TranslationData.ExactTranslations.TryGetValue(val, out string translatedCapture))
                        {
                            finalWord = translatedCapture;
                        }

                        template = template.Replace($"{{{i}}}", finalWord);
                    }
                }

                TranslationData.RegexMatchedInputs.Add(input);
                TranslationData.ExactTranslations[input] = template;
                return template;
            }
        }

        TranslationData.ExactTranslations[input] = input;

        HashSet<string> blacklist = GetCachedBlacklist();
        if (!blacklist.Contains(input) && !blacklist.Contains(trimmedInput))
        {
            TranslationData.KnownEnglishStrings.Add(trimmedInput);
            TranslationData.MissingStringsDirty = true;
            AppendUntranslatedStringToDisk(trimmedInput);
        }

        return input;
    }

    /// <summary>
    /// Translates an enum from the Enums folder in translation directory
    /// </summary>
    /// <param name="enumType">The enum to translate</param>
    public static Dictionary<int, string> TranslateEnum(Type enumType)
    {
        if (enumType == null)
        {
            TranslatorLogger.Error("TranslateEnum called with null enumType!");
            return new Dictionary<int, string>();
        }

        if (TranslationData.NameCache.TryGetValue(enumType, out var cachedDict))
            return new Dictionary<int, string>(cachedDict);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            Dictionary<int, string> parsedNames = LoadEnumTranslations(enumType);
            TranslationData.NameCache[enumType] = parsedNames;
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
}