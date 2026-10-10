//
// IMagnetarAddon is used to create Addons for Magnetar Client.
//
// Simply place your file in Magnetar Data / Addons to get it loaded
// automatically into the client.
//

using Magnetar_Client.HUDElements;
using System.Collections.Generic;
using System.Reflection;

namespace Magnetar_Client.Api;

internal class AddonInfo
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