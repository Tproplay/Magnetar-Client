using Magnetar_Client.HUDElements;
using Magnetar_Client.Modules;
using System.Collections.Generic;
using System.Reflection;

namespace Magnetar_Client.Api;

public class AddonInfo
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public Assembly LoadedAssembly { get; set; }
    public List<IMagnetarAddon> AddonInstances { get; } = new();
    public List<Modules.Module> RegisteredModules { get; } = new();
    public List<HudElement> RegisteredHudElements { get; } = new();
}
public interface IMagnetarAddon
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