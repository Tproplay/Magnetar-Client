using Magnetar_Client.Api;
using Magnetar_Client.HUDElements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static Magnetar_Client.Api.MagnetarApi;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public static class AddonManager
{
    public static readonly List<AddonInfo> LoadedAddons = new();

    private static readonly Dictionary<string, (Assembly Assembly, Type[] Types)> DiscoveredAssemblies = new();

    /// <summary>
    /// Scans the 'Mods/Magnetar Addon' directory, loads all assemblies into memory,
    /// and instantiates and initializes all IAddon implementations first.
    /// </summary>
    public static void InitAddons()
    {
        try
        {
            LoadedAddons.Clear();
            DiscoveredAssemblies.Clear();

            if (!Directory.Exists(AddonsDir))
            {
                Directory.CreateDirectory(AddonsDir);
                DebugLogger.Msg($"[AddonManager] Created addon directory at: {AddonsDir}");
                return;
            }

            string[] dllFiles = Directory.GetFiles(AddonsDir, "*.dll", SearchOption.AllDirectories);
            if (dllFiles.Length == 0)
            {
                return;
            }

            DebugLogger.Msg($"[AddonManager] Discovered {dllFiles.Length} candidate assembly file(s)");

            // Pass 1: Load assemblies and execute IAddon lifecycle
            foreach (string filePath in dllFiles)
            {
                string fileName = Path.GetFileName(filePath);
                try
                {
                    Assembly assembly = Assembly.LoadFrom(filePath);

                    Type[] exportedTypes;
                    try
                    {
                        exportedTypes = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        exportedTypes = ex.Types.Where(t => t != null).ToArray();
                        DebugLogger.Warning($"[AddonManager] Type resolution warning in '{fileName}': {ex.LoaderExceptions.FirstOrDefault()?.Message}");
                    }

                    DiscoveredAssemblies[filePath] = (assembly, exportedTypes);

                    var addonInfo = new AddonInfo
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        LoadedAssembly = assembly
                    };

                    // Discover concrete IAddon implementations
                    Type addonEntryType = exportedTypes.FirstOrDefault(t =>
                        typeof(IAddon).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

                    if (addonEntryType != null)
                    {
                        try
                        {
                            var addonInstance = (IAddon)Activator.CreateInstance(addonEntryType);
                            addonInfo.AddonInstance = addonInstance;
                            addonInstance.OnLoad();
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.Error($"[AddonManager] Failed to instantiate IAddon in '{fileName}': {ex}");
                        }
                    }

                    LoadedAddons.Add(addonInfo);
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[AddonManager] Failed to load assembly '{fileName}': {ex}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[AddonManager] Critical error during InitAddons: {ex}");
        }
    }

    /// <summary>
    /// Discovers and registers all Module subclasses from the previously loaded addon assemblies.
    /// </summary>
    public static void InitModules()
    {
        try
        {
            if (DiscoveredAssemblies.Count == 0)
            {
                DebugLogger.Msg("[AddonManager] No assemblies found to load modules from.");
                return;
            }

            DebugLogger.Msg("[AddonManager] Scanning addon assemblies for Module subclasses...");
            int totalNewModules = 0;

            // Pass 2: Discover and instantiate Module implementations
            foreach (var addonInfo in LoadedAddons)
            {
                if (!DiscoveredAssemblies.TryGetValue(addonInfo.FilePath, out var cachedData))
                    continue;

                var moduleTypes = cachedData.Types
                    .Where(t => t.IsClass
                                && !t.IsAbstract
                                && typeof(Modules.Module).IsAssignableFrom(t))
                    .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var modType in moduleTypes)
                {
                    try
                    {
                        MagnetarApi.RegisterModule(modType);

                        Modules.Module registeredInstance = ModuleManager.Modules.FirstOrDefault(m => m.GetType() == modType);
                        if (registeredInstance != null && !addonInfo.RegisteredModules.Contains(registeredInstance))
                        {
                            addonInfo.RegisteredModules.Add(registeredInstance);
                            totalNewModules++;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Error($"[AddonManager] Failed to register addon module '{modType.FullName}' from '{addonInfo.FileName}': {ex}");
                    }
                }

                if (addonInfo.RegisteredModules.Count > 0)
                {
                    DebugLogger.Msg($"[AddonManager] Registered {addonInfo.RegisteredModules.Count} module(s) from '{addonInfo.FileName}'");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[AddonManager] Critical error during InitModules: {ex}");
        }
    }

    /// <summary>
    /// Discovers and registers all HudElement subclasses from the loaded addon assemblies.
    /// </summary>
    public static void InitHUDElements()
    {
        try
        {
            if (DiscoveredAssemblies.Count == 0)
            {
                DebugLogger.Msg("[AddonManager] No assemblies found to load HUD elements from.");
                return;
            }

            int totalNewElements = 0;

            foreach (var addonInfo in LoadedAddons)
            {
                if (!DiscoveredAssemblies.TryGetValue(addonInfo.FilePath, out var cachedData))
                    continue;

                var hudTypes = cachedData.Types
                    .Where(t => t.IsClass
                                && !t.IsAbstract
                                && t.IsSubclassOf(typeof(HudElement)))
                    .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var hudType in hudTypes)
                {
                    try
                    {
                        int beforeCount = HUDRenderer.Elements.Count;
                        HUDRenderer.RegisterElement(hudType);

                        if (HUDRenderer.Elements.Count > beforeCount)
                        {
                            var instance = HUDRenderer.Elements[HUDRenderer.Elements.Count - 1];
                            addonInfo.RegisteredHudElements.Add(instance);
                            totalNewElements++;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Error($"[AddonManager] Failed to register addon HUD element '{hudType.FullName}' from '{addonInfo.FileName}': {ex}");
                    }
                }

                if (addonInfo.RegisteredHudElements.Count > 0)
                {
                    DebugLogger.Msg($"[AddonManager] Registered {addonInfo.RegisteredHudElements.Count} HUD element(s) from '{addonInfo.FileName}'");
                }
            }

        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[AddonManager] Critical error during InitHUDElements: {ex}");
        }
    }
}