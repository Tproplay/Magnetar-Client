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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class AddonInfo
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public Assembly LoadedAssembly { get; set; }
    public List<IMagnetarAddon> AddonInstances { get; } = new();
    public List<Modules.Module> RegisteredModules { get; } = new();
    public List<HudElement> RegisteredHudElements { get; } = new();
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Interface to create a addon for Magnetar Client, Addon Manager automatically 
/// scans and loads classes with this interface from the Scans the 
/// 'Mods/Magnetar Addon' directory.
/// </summary>
public interface IMagnetarAddon
{
    /// <summary>
    /// The name of the Addon.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// The version of the Addon.
    /// </summary>
    string Version { get; }
    /// <summary>
    /// The author of this Addon.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Runs after all addon assemblies are loaded.
    /// </summary>
    void OnLoad();
}