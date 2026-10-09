using Magnetar_Client.Core.Lifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Api;

public static class ServiceRegistry
{
    private static readonly List<IClientService> _services = new();

    // Cached execution arrays sorted by Priority (lowest number executes first)
    private static IInitializable[] _initializables = Array.Empty<IInitializable>();
    private static IWarmUp[] _warmUps = Array.Empty<IWarmUp>();
    private static IUpdatable[] _updatables = Array.Empty<IUpdatable>();
    private static IRenderable[] _renderables = Array.Empty<IRenderable>();
    private static IMenuRenderable[] _menuRenderables = Array.Empty<IMenuRenderable>();
    private static ICloseHandler[] _closeHandlers = Array.Empty<ICloseHandler>();
    private static ILanguageAware[] _languageAwares = Array.Empty<ILanguageAware>();
    private static IPersistent[] _persistents = Array.Empty<IPersistent>();
    private static IQuittable[] _quittables = Array.Empty<IQuittable>();

    public static IReadOnlyList<IClientService> Services => _services;
    public static IReadOnlyList<ICloseHandler> CloseHandlers => _closeHandlers;

    /// <summary>
    /// Registers a client service and rebuilds execution pipelines.
    /// </summary>
    public static void Register(IClientService service)
    {
        if (service == null)
        {
            DebugLogger.Error("[ServiceRegistry] Cannot register a null service.");
            return;
        }

        if (_services.Contains(service))
        {
            DebugLogger.Warning($"[ServiceRegistry] Service '{service.Name}' is already registered.");
            return;
        }

        _services.Add(service);
        RebuildPipelines();
    }

    /// <summary>
    /// Unregisters an active service.
    /// </summary>
    public static void Unregister(IClientService service)
    {
        if (service == null || !_services.Remove(service)) return;

        RebuildPipelines();
    }

    private static void RebuildPipelines()
    {
        _initializables = _services.OfType<IInitializable>().OrderBy(s => s.Priority).ToArray();
        _warmUps = _services.OfType<IWarmUp>().OrderBy(s => s.Priority).ToArray();
        _updatables = _services.OfType<IUpdatable>().OrderBy(s => s.Priority).ToArray();
        _renderables = _services.OfType<IRenderable>().OrderBy(s => s.Priority).ToArray();
        _menuRenderables = _services.OfType<IMenuRenderable>().OrderBy(s => s.Priority).ToArray();
        _closeHandlers = _services.OfType<ICloseHandler>().OrderBy(s => s.Priority).ToArray();
        _languageAwares = _services.OfType<ILanguageAware>().OrderBy(s => s.Priority).ToArray();
        _persistents = _services.OfType<IPersistent>().OrderBy(s => s.Priority).ToArray();
        _quittables = _services.OfType<IQuittable>().OrderBy(s => s.Priority).ToArray();
    }

    public static void InitializeAll()
    {
        for (int i = 0; i < _initializables.Length; i++)
        {
            try
            {
                _initializables[i].Initialize();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error initializing '{_initializables[i].Name}': {ex}");
            }
        }
    }

    public static void WarmUpAll()
    {
        for (int i = 0; i < _warmUps.Length; i++)
        {
            try
            {
                _warmUps[i].OnWarmUp();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error warming up '{_warmUps[i].Name}': {ex}");
            }
        }
    }

    public static void UpdateAll()
    {
        for (int i = 0; i < _updatables.Length; i++)
        {
            try
            {
                _updatables[i].OnUpdate();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error updating '{_updatables[i].Name}': {ex}");
            }
        }
    }

    public static void RenderAll()
    {
        for (int i = 0; i < _renderables.Length; i++)
        {
            try
            {
                _renderables[i].OnGUI();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error rendering '{_renderables[i].Name}': {ex}");
            }
        }
    }

    public static void RenderMenuAll()
    {
        for (int i = 0; i < _menuRenderables.Length; i++)
        {
            try
            {
                _menuRenderables[i].OnMenuGUI();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error rendering menu for '{_menuRenderables[i].Name}': {ex}");
            }
        }
    }

    public static void NotifyLanguageChanged()
    {
        for (int i = 0; i < _languageAwares.Length; i++)
        {
            try
            {
                _languageAwares[i].OnLanguageChanged();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error in OnLanguageChanged for '{_languageAwares[i].Name}': {ex}");
            }
        }
    }

    public static void SaveAll()
    {
        for (int i = 0; i < _persistents.Length; i++)
        {
            try
            {
                _persistents[i].OnSave();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error saving '{_persistents[i].Name}': {ex}");
            }
        }
    }

    public static void LoadAll()
    {
        for (int i = 0; i < _persistents.Length; i++)
        {
            try
            {
                _persistents[i].OnLoad();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error loading '{_persistents[i].Name}': {ex}");
            }
        }
    }

    public static void QuitAll()
    {
        for (int i = 0; i < _quittables.Length; i++)
        {
            try
            {
                _quittables[i].OnApplicationQuit();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ServiceRegistry] Error quitting '{_quittables[i].Name}': {ex}");
            }
        }
    }
}