using Newtonsoft.Json.Linq;

namespace Magnetar_Client.Api;

public interface ISaveHandler
{
    /// <summary>
    /// Unique key name used as the section identifier in the profile JSON.
    /// </summary>
    string SectionKey { get; }

    /// <summary>
    /// Returns the data object to serialize into the save file.
    /// </summary>
    object ExportData();

    /// <summary>
    /// Imports and restores data from the profile JSON.
    /// </summary>
    void ImportData(JToken token);

    /// <summary>
    /// Resets all state managed by this handler to factory defaults.
    /// </summary>
    void ResetToDefault();
}