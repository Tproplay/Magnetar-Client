using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public static partial class Translator
{
    public static event Action OnTranslationsLoaded;

    public static string TranslationRootDir => TranslationData.TranslationRootDir;
    public static string CurrentLanguage => TranslationData.CurrentLanguage;

    private static readonly Dictionary<string, TranslationDomain> _domains = new(StringComparer.OrdinalIgnoreCase);

    public static TranslationDomain RegisterDomain(TranslationDomain domain)
    {
        if (domain == null) throw new ArgumentNullException(nameof(domain));
        _domains[domain.DomainName] = domain;
        return domain;
    }

    public static TranslationDomain CreateDomain(string domainName, params string[] sources) =>
        RegisterDomain(new TranslationDomain(domainName, sources));

    public static TranslationDomain GetDomain(string domainName) =>
        _domains.TryGetValue(domainName, out var domain) ? domain : null;

    public static IReadOnlyCollection<TranslationDomain> AllDomains => _domains.Values;

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

        string trimmedInput = input.Trim();
        if (string.IsNullOrWhiteSpace(trimmedInput)) return input;

        if (TranslationData.ExactTranslations.TryGetValue(input, out string exactMatch))
            return exactMatch;

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

        if (string.Equals(CurrentLanguage, "English", StringComparison.OrdinalIgnoreCase))
            return input;

        HashSet<string> blacklist = GetCachedBlacklist();
        if (!blacklist.Contains(input) && !blacklist.Contains(trimmedInput))
        {
            TranslationData.KnownEnglishStrings.Add(trimmedInput);
            TranslationData.MissingStringsDirty = true;
            AppendUntranslatedStringToDisk(trimmedInput);
        }

        return input;
    }

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