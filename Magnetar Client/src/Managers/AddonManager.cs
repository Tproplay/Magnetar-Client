using Magnetar_Client.Api;
using Magnetar_Client.HUDElements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;
using Magnetar_Client.Core.HUDManager_;

namespace Magnetar_Client.Core;

public static class AddonManager
{
    public static readonly List<AddonInfo> LoadedAddons = new();

    private static readonly Dictionary<string, (Assembly Assembly, Type[] Types)> DiscoveredAssemblies = new();
    private static bool _isResolverAttached;

    private static void EnsureAssemblyResolver()
    {
        if (_isResolverAttached) return;

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            try
            {
                string assemblySimpleName = new AssemblyName(args.Name).Name;
                string targetPath = Path.Combine(AddonsDir, assemblySimpleName + ".dll");

                if (File.Exists(targetPath))
                {
                    return Assembly.LoadFrom(targetPath);
                }

                // Deep search in subfolders within AddonsDir
                if (Directory.Exists(AddonsDir))
                {
                    string matchedFile = Directory.GetFiles(AddonsDir, assemblySimpleName + ".dll", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(matchedFile))
                    {
                        return Assembly.LoadFrom(matchedFile);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"Error resolving assembly '{args.Name}': {ex.Message}");
            }

            return null;
        };

        _isResolverAttached = true;
    }

    /// <summary>
    /// Scans the 'Mods/Magnetar Addon' directory, loads all assemblies into memory,
    /// and instantiates and initializes all IMagnetarAddon implementations first.
    /// </summary>
    public static void InitAddons()
    {
        try
        {
            EnsureAssemblyResolver();

            LoadedAddons.Clear();
            DiscoveredAssemblies.Clear();

            string[] dllFiles = Directory.GetFiles(AddonsDir, "*.dll", SearchOption.AllDirectories);
            if (dllFiles.Length == 0)
            {
                return;
            }

            DebugLogger.Msg($"Discovered {dllFiles.Length} candidate assembly file(s)");

            // Load assemblies
            foreach (string filePath in dllFiles)
            {
                try
                {
                    var info = LoadAssembly(filePath);
                    if (info!= null) LoadedAddons.Add(info);
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"Failed to load assembly '{Path.GetFileName(filePath)}': {ex}");
                }
            }

            // Execute their OnLoad
            LoadedAddons.ForEach(
                addon => addon.AddonInstances.
                    ForEach(
                        instance => instance.OnLoad()
                        )
                );

        }
        catch (Exception ex)
        {
            DebugLogger.Error($"Critical error during InitAddons: {ex}");
        }
    }

    private static AddonInfo LoadAssembly(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        Assembly assembly = Assembly.LoadFrom(filePath);

        Type[] exportedTypes;
        try
        {
            exportedTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            exportedTypes = ex.Types.Where(t => t != null).ToArray();
            DebugLogger.Warning($"Type resolution warning in '{fileName}': {ex.LoaderExceptions.FirstOrDefault()?.Message}");
        }

        DiscoveredAssemblies[filePath] = (assembly, exportedTypes);

        var addonInfo = new AddonInfo
        {
            FileName = fileName,
            FilePath = filePath,
            LoadedAssembly = assembly
        };

        // Discover concrete IMagnetarAddon implementations
        var addonEntryTypes = exportedTypes.Where(t =>
        typeof(IMagnetarAddon).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

        foreach (Type addonType in addonEntryTypes)
        {
            try
            {
                var addonInstance = (IMagnetarAddon)Activator.CreateInstance(addonType);
                addonInfo.AddonInstances.Add(addonInstance);

                DebugLogger.Msg($"Discovered addon '{addonInstance.Name}' v{addonInstance.Version} by {addonInstance.Author} in '{fileName}'");
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"Failed to instantiate IMagnetarAddon type '{addonType.FullName}' in '{fileName}': {ex}");
            }
        }

        return addonInfo;
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
                return;
            }

            DebugLogger.Msg("Scanning addon assemblies for Module subclasses...");
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
                        DebugLogger.Error($"Failed to register addon module '{modType.FullName}' from '{addonInfo.FileName}': {ex}");
                    }
                }

                if (addonInfo.RegisteredModules.Count > 0)
                {
                    DebugLogger.Msg($"Registered {addonInfo.RegisteredModules.Count} module(s) from '{addonInfo.FileName}'");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"Critical error during InitModules: {ex}");
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
                        DebugLogger.Error($"Failed to register addon HUD element '{hudType.FullName}' from '{addonInfo.FileName}': {ex}");
                    }
                }

                if (addonInfo.RegisteredHudElements.Count > 0)
                {
                    DebugLogger.Msg($"Registered {addonInfo.RegisteredHudElements.Count} HUD element(s) from '{addonInfo.FileName}'");
                }
            }

        }
        catch (Exception ex)
        {
            DebugLogger.Error($"Critical error during InitHUDElements: {ex}");
        }
    }

    /// <summary>
    /// Discovers and registers all IClientService implementations from addon assemblies into the ServiceRegistry.
    /// </summary>
    public static void InitServices()
    {
        try
        {
            if (DiscoveredAssemblies.Count == 0) return;

            int totalServices = 0;

            foreach (var addonInfo in LoadedAddons)
            {
                if (!DiscoveredAssemblies.TryGetValue(addonInfo.FilePath, out var cachedData))
                    continue;

                var serviceTypes = cachedData.Types
                    .Where(t => t.IsClass
                                && !t.IsAbstract
                                && typeof(IClientService).IsAssignableFrom(t))
                    .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var svcType in serviceTypes)
                {
                    try
                    {
                        var serviceInstance = (IClientService)Activator.CreateInstance(svcType);
                        ServiceRegistry.Register(serviceInstance);
                        totalServices++;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Error($"Failed to register addon service '{svcType.FullName}' from '{addonInfo.FileName}': {ex}");
                    }
                }
            }

            if (totalServices > 0)
            {
                DebugLogger.Msg($"Registered {totalServices} client service(s) from addons.");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"Critical error during InitServices: {ex}");
        }
    }
}