using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Magnetar_Client.Api;

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
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.Ordinal);
}

public class HudTranslationNode
{
    [JsonProperty("Name")]
    public string Name { get; set; }
}

public static class TranslationData
{
    public static bool IsLoaded { get; set; } = false;
    public static bool ModulesLinked { get; set; } = false;
    public static bool HudLinked { get; set; } = false;

    public static Dictionary<string, string> ExactTranslations { get; } = new(StringComparer.Ordinal);
    public static Dictionary<Regex, string> RegexTranslations { get; } = new();
    public static Dictionary<Type, Dictionary<int, string>> NameCache { get; } = new();

    public static HashSet<string> RegexMatchedInputs { get; } = new(StringComparer.Ordinal);
    public static HashSet<string> KnownEnglishStrings { get; } = new(StringComparer.Ordinal);
    public static HashSet<string> DumpedUntranslated { get; } = new(StringComparer.Ordinal);
    public static object UntranslatedFileLock { get; } = new();

    public static HashSet<string> CachedBlacklist { get; } = new(StringComparer.Ordinal);
    public static bool BlacklistDirty { get; set; } = true;
    public static bool MissingStringsDirty { get; set; } = false;

    public static string TranslationRootDir => PathsManager.TranslationRootDir;
    public static string CurrentLanguage => string.IsNullOrWhiteSpace(Config.Language) ? "English" : Config.Language;

    public static void ResetForReload()
    {
        ExactTranslations.Clear();
        RegexTranslations.Clear();
        NameCache.Clear();
        DumpedUntranslated.Clear();
        ModulesLinked = false;
        HudLinked = false;
    }
}