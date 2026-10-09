using Magnetar_Client.HUDElements;
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
    /// Runs after all addon assemblies are loaded.
    /// </summary>
    void OnLoad();
}