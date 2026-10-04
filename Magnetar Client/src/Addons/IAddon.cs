using System.Collections.Generic;
using System.Reflection;

namespace Magnetar_Client.Api;

public class AddonInfo
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public Assembly LoadedAssembly { get; set; }
    public IAddon AddonInstance { get; set; }
    public List<Modules.Module> RegisteredModules { get; } = new();
}

public interface IAddon
{
    string Name { get; }
    string Version { get; }
    string Author { get; }

    /// <summary>
    /// Invoked immediately after the assembly is loaded into memory.
    /// </summary>
    void OnLoad();

    /// <summary>
    /// Invoked during mod unload or shutdown.
    /// </summary>
    void OnUnload();
}