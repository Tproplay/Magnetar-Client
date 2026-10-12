namespace Magnetar_Client.Api;

/// <summary>
/// Common priorities for deterministic execution order. Lower numbers execute earlier.
/// </summary>
public static class ServicePriority
{

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const int Highest = -1000;
    public const int CriticalCore = -500;
    public const int Core = 0;
    public const int Standard = 100;
    public const int Modules = 200;
    public const int HUD = 300;
    public const int Addons = 500;
    public const int UI = 800;
    public const int Lowest = 1000;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

/// <summary>
/// Base metadata interface for any client service.
/// </summary>
public interface IClientService
{
    /// <summary>
    /// Unique identifier or readable name of the service (used for logging and diagnostics).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution priority. Lower runs first.
    /// Refer to <see cref="ServicePriority"/> for standard tier offsets.
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Services that require initialization during startup.
/// </summary>
public interface IInitializable : IClientService
{
    /// <summary>
    /// Runs during Initialization of the client.
    /// </summary>
    void Initialize();
}

/// <summary>
/// Services that need to warm up fonts, textures, or styles at first frame rendering.
/// </summary>
public interface IWarmUp : IClientService
{
    /// <summary>
    /// Runs at the first frame of OnEarlyGUI
    /// </summary>
    void OnWarmUp();
}

/// <summary>
/// Services that run logic on Unity's Update loop.
/// </summary>
public interface IUpdatable : IClientService
{
    /// <summary>
    /// Runs Every Unity Engine Cycle
    /// </summary>
    void OnUpdate();
}

/// <summary>
/// Services that require rendering every OnEarlyGUI.
/// </summary>
public interface IRenderable : IClientService
{
    /// <summary>
    /// Renders regardless of whether Client Window is open or not.
    /// </summary>
    void OnGUI();

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
    /// Return true to permits closing the whole GUI.
    /// </summary>
    bool CanClose();

    /// <summary>
    /// Attempts to consume an Escape press.
    /// Return true if the event was consumed so the main GUI stays open.
    /// </summary>
    bool OnEscapePressed();
}

/// <summary>
/// Services that respond to language changes.
/// </summary>
public interface ILanguageAware : IClientService
{
    /// <summary>
    /// Runs after the language is changed.
    /// </summary>
    void OnLanguageChanged();
}

/// <summary>
/// Services that persist data to disk during regular saves or profile switches.
/// </summary>
public interface IPersistent : IClientService
{
    /// <summary>
    /// Runs just before the saving process starts.
    /// </summary>
    void OnSave();
    /// <summary>
    /// Runs after a new save file is loaded.
    /// </summary>
    void OnLoad();
}

/// <summary>
/// Services that perform cleanup when the game shuts down.
/// </summary>
public interface IQuittable : IClientService
{
    /// <summary>
    /// Runs on Application quit request.
    /// </summary>
    void OnApplicationQuit();
}
/// <summary>
/// Services that respond to theme changes.
/// </summary>
public interface IThemeAware : IClientService
{
    /// <summary>
    /// Runs after the Theme is changed.
    /// </summary>
    void OnThemeChanged();
}
/// <summary>
/// Services that respond to GUI Scale changes.
/// </summary>
public interface IScaleAware : IClientService
{
    /// <summary>
    /// Runs after the GUI Scale is changed.
    /// </summary>
    void OnGUIScaleChanged(float newScale);
}