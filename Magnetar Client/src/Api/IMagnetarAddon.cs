using Magnetar_Client.HUDElements;
using System.Collections.Generic;
using System.Reflection;

namespace Magnetar_Client.Api;

/// <summary>
/// Holds runtime metadata and registered objects for a single loaded addon assembly.
/// </summary>
public class AddonInfo
{
    /// <summary>The file name of the loaded addon DLL.</summary>
    public string FileName { get; set; }

    /// <summary>The full file path of the loaded addon DLL.</summary>
    public string FilePath { get; set; }

    /// <summary>The loaded assembly.</summary>
    public Assembly LoadedAssembly { get; set; }

    /// <summary>All <see cref="IMagnetarAddon"/> entry-point instances found in this assembly.</summary>
    public List<IMagnetarAddon> AddonInstances { get; } = new();

    /// <summary>All <see cref="Modules.Module"/> instances registered from this assembly.</summary>
    public List<Modules.Module> RegisteredModules { get; } = new();

    /// <summary>All <see cref="HudElement"/> instances registered from this assembly.</summary>
    public List<HudElement> RegisteredHudElements { get; } = new();

    /// <summary>All <see cref="IClientService"/> instances registered from this assembly.</summary>
    public List<IClientService> RegisteredServices { get; } = new();
}

/// <summary>
/// Interface to create an addon for Magnetar Client. AddonManager automatically
/// scans and loads classes implementing this interface from the 'Mods/Magnetar Addon' directory.
/// </summary>
public interface IMagnetarAddon
{
    /// <summary>The display name of the addon.</summary>
    string Name { get; }

    /// <summary>The version string of the addon.</summary>
    string Version { get; }

    /// <summary>The author of the addon.</summary>
    string Author { get; }

    /// <summary>
    /// Runs after all addon assemblies are loaded and registered.
    /// </summary>
    void OnLoad();

    /// <summary>
    /// Runs when the addon is unloaded or the client shuts down.
    /// Use this to clean up Harmony patches, unregister services, or free resources.
    /// </summary>
    void OnUnload();
}
