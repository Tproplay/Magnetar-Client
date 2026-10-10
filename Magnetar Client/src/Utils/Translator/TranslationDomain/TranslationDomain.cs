using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public partial class TranslationDomain
{
    public string DomainName { get; }

    public bool DumpUntranslated { get; set; } = true;
    public bool IsLoaded { get; private set; } = false;

    // Sources registered at or after initialization (relative to the language directory)
    public HashSet<string> Sources { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> BlacklistedPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> WhitelistedPaths { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Domain-wide translation dictionaries
    private Dictionary<string, string> _exactTranslations = new(StringComparer.Ordinal);
    private Dictionary<Regex, string> _regexTranslations = new();

    // Scoped translation dictionaries: RelativePath -> (Key -> Value)
    private Dictionary<string, Dictionary<string, string>> _scopedExactTranslations = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, List<KeyValuePair<Regex, string>>> _scopedRegexTranslations = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> UntranslatedStrings { get; } = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dumpedUntranslated = new(StringComparer.Ordinal);
    private readonly object _dumpLock = new();

    public event Action<string> OnDumpEnglishTemplate;

    public TranslationDomain(string domainName, params string[] sources)
    {
        if (string.IsNullOrWhiteSpace(domainName))
            throw new ArgumentException("Domain name cannot be null or empty.", nameof(domainName));

        DomainName = domainName.Trim();

        if (sources != null && sources.Length > 0)
        {
            AddSources(sources);
        }
    }

    public string GetEnglishDirectory() => Path.Combine(TranslationRootDir, "English");
    public string GetLanguageDirectory(string language) => Path.Combine(TranslationRootDir, language);

    #region Source Management

    public void AddSource(string source)
    {
        if (!string.IsNullOrWhiteSpace(source))
            Sources.Add(NormalizePath(source));
    }

    public void AddSources(params string[] sources)
    {
        if (sources == null) return;
        foreach (var s in sources) AddSource(s);
    }

    public void AddSources(IEnumerable<string> sources)
    {
        if (sources == null) return;
        foreach (var s in sources) AddSource(s);
    }

    public void SetSources(params string[] sources)
    {
        Sources.Clear();
        AddSources(sources);
    }

    public void SetSources(IEnumerable<string> sources)
    {
        Sources.Clear();
        AddSources(sources);
    }

    public string[] ResolveSources(string[] relativeFilesOrDir)
    {
        if (relativeFilesOrDir != null && relativeFilesOrDir.Length > 0)
            return relativeFilesOrDir;

        if (Sources.Count > 0)
            return Sources.ToArray();

        return null;
    }

    #endregion

    #region Translation Methods

    public string Translate(string input, string[] relativeFilesOrDir = null)
    {
        if (TryTranslate(input, out string translated, relativeFilesOrDir))
            return translated;

        RecordUntranslated(input);
        return input;
    }

    public string Translate(string input, string relativeFileOrDir) =>
        Translate(input, string.IsNullOrEmpty(relativeFileOrDir) ? null : new[] { relativeFileOrDir });

    public bool TryTranslate(string input, out string translated, string[] relativeFilesOrDir = null)
    {
        if (TryTranslateDirect(input, out translated, relativeFilesOrDir))
            return true;

        return TryTranslateRegex(input, out translated, relativeFilesOrDir);
    }

    public bool TryTranslate(string input, out string translated, string relativeFileOrDir) =>
        TryTranslate(input, out translated, string.IsNullOrEmpty(relativeFileOrDir) ? null : new[] { relativeFileOrDir });

    public string TranslateDirect(string input, string[] relativeFilesOrDir = null)
    {
        if (TryTranslateDirect(input, out string translated, relativeFilesOrDir))
            return translated;

        RecordUntranslated(input);
        return input;
    }

    public string TranslateDirect(string input, string relativeFileOrDir) =>
        TranslateDirect(input, string.IsNullOrEmpty(relativeFileOrDir) ? null : new[] { relativeFileOrDir });

    public bool TryTranslateDirect(string input, out string translated, string[] relativeFilesOrDir = null)
    {
        translated = input;
        if (string.IsNullOrEmpty(input)) return false;

        string[] effectiveSources = ResolveSources(relativeFilesOrDir);
        bool hasScope = effectiveSources != null && effectiveSources.Length > 0;

        if (hasScope)
        {
            foreach (var scopePath in effectiveSources)
            {
                if (string.IsNullOrWhiteSpace(scopePath)) continue;
                string normalizedScope = NormalizePath(scopePath);
                if (!IsAllowed(normalizedScope)) continue;

                if (_scopedExactTranslations.TryGetValue(normalizedScope, out var fileDict))
                {
                    if (fileDict.TryGetValue(input, out string val))
                    {
                        translated = val;
                        return true;
                    }
                }

                foreach (var kvp in _scopedExactTranslations)
                {
                    if (kvp.Key.StartsWith(normalizedScope + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        if (kvp.Value.TryGetValue(input, out string subVal))
                        {
                            translated = subVal;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        if (_exactTranslations.TryGetValue(input, out string exact))
        {
            translated = exact;
            return true;
        }

        return false;
    }

    public string TranslateRegex(string input, string[] relativeFilesOrDir = null)
    {
        if (TryTranslateRegex(input, out string translated, relativeFilesOrDir))
            return translated;

        RecordUntranslated(input);
        return input;
    }

    public string TranslateRegex(string input, string relativeFileOrDir) =>
        TranslateRegex(input, string.IsNullOrEmpty(relativeFileOrDir) ? null : new[] { relativeFileOrDir });

    public bool TryTranslateRegex(string input, out string translated, string[] relativeFilesOrDir = null)
    {
        translated = input;
        if (string.IsNullOrEmpty(input)) return false;

        string[] effectiveSources = ResolveSources(relativeFilesOrDir);
        bool hasScope = effectiveSources != null && effectiveSources.Length > 0;

        if (hasScope)
        {
            foreach (var scopePath in effectiveSources)
            {
                if (string.IsNullOrWhiteSpace(scopePath)) continue;
                string normalizedScope = NormalizePath(scopePath);
                if (!IsAllowed(normalizedScope)) continue;

                if (_scopedRegexTranslations.TryGetValue(normalizedScope, out var regexList))
                {
                    if (TryApplyRegexList(regexList, input, normalizedScope, out string regexVal))
                    {
                        translated = regexVal;
                        return true;
                    }
                }

                foreach (var kvp in _scopedRegexTranslations)
                {
                    if (kvp.Key.StartsWith(normalizedScope + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryApplyRegexList(kvp.Value, input, kvp.Key, out string subRegexVal))
                        {
                            translated = subRegexVal;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

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

                        string trans = capture;
                        if (!long.TryParse(capture, out _))
                        {
                            if (_exactTranslations.TryGetValue(capture, out string transVal))
                                trans = transVal;
                        }

                        template = template.Replace($"{{{i}}}", trans);
                    }
                }

                translated = template;
                return true;
            }
        }

        return false;
    }

    private bool TryApplyRegexList(List<KeyValuePair<Regex, string>> rules, string input, string scopePath, out string result)
    {
        result = null;
        if (rules == null || rules.Count == 0) return false;

        foreach (var rule in rules)
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

                        string trans = capture;
                        if (!long.TryParse(capture, out _))
                        {
                            if (_scopedExactTranslations.TryGetValue(scopePath, out var fileDict) &&
                                fileDict.TryGetValue(capture, out string transVal))
                            {
                                trans = transVal;
                            }
                            else
                            {
                                foreach (var dict in _scopedExactTranslations.Values)
                                {
                                    if (dict.TryGetValue(capture, out string fallbackVal))
                                    {
                                        trans = fallbackVal;
                                        break;
                                    }
                                }
                            }
                        }

                        template = template.Replace($"{{{i}}}", trans);
                    }
                }

                result = template;
                return true;
            }
        }

        return false;
    }

    #endregion
}