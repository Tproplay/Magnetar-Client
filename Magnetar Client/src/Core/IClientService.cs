namespace Magnetar_Client.Core.Lifecycle;

/// <summary>
/// Common priorities for deterministic execution order. Lower numbers execute earlier.
/// </summary>
public static class ServicePriority
{
    public const int Highest = -1000;
    public const int CriticalCore = -500;
    public const int Core = 0;
    public const int Standard = 100;
    public const int Modules = 200;
    public const int HUD = 300;
    public const int Addons = 500;
    public const int UI = 800;
    public const int Lowest = 1000;
}

/// <summary>
/// Base metadata interface for any client subsystem, manager, or addon service.
/// </summary>
public interface IClientService
{
    /// <summary>
    /// Unique identifier or readable name of the service (used for logging & diagnostics).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution priority. Lower runs first.
    /// Refer to <see cref="ServicePriority"/> for standard tier offsets.
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Services that require initialization during client startup (e.g. InitializeCore).
/// </summary>
public interface IInitializable : IClientService
{
    void Initialize();
}

/// <summary>
/// Services that need to warm up fonts, textures, or styles before first frame rendering.
/// </summary>
public interface IWarmUp : IClientService
{
    void OnWarmUp();
}

/// <summary>
/// Services that run logic on Unity's Update loop.
/// </summary>
public interface IUpdatable : IClientService
{
    void OnUpdate();
}

/// <summary>
/// Services that render screen overlays or HUD components (runs every OnGUI regardless of menu state).
/// </summary>
public interface IRenderable : IClientService
{
    void OnGUI();
}

/// <summary>
/// Services that render within the transformed matrix GUI window when Config.showgui is true.
/// </summary>
public interface IMenuRenderable : IClientService
{
    /// <summary>
    /// Renders menu components when the client window is open.
    /// </summary>
    void OnMenuGUI();
}

/// <summary>
/// Services that need to intercept, guard, or handle Escape key presses and navigation state.
/// </summary>
public interface ICloseHandler : IClientService
{
    /// <summary>
    /// Returns true if this service is in a clean state and permits closing the whole GUI.
    /// Returns false if sub-menus, search buffers, or binding states are active.
    /// </summary>
    bool CanClose();

    /// <summary>
    /// Attempts to consume an Escape press (e.g., backing out of a sub-window or dismissing a modal).
    /// Return true if the event was consumed so the main GUI stays open.
    /// </summary>
    bool OnEscapePressed();
}

/// <summary>
/// Services that respond to language changes to reload display string caches and format buffers.
/// </summary>
public interface ILanguageAware : IClientService
{
    void OnLanguageChanged();
}

/// <summary>
/// Services that persist data to disk during regular saves or profile switches.
/// </summary>
public interface IPersistent : IClientService
{
    void OnSave();
    void OnLoad();
}

/// <summary>
/// Services that perform cleanup when the game shuts down.
/// </summary>
public interface IQuittable : IClientService
{
    void OnApplicationQuit();
}