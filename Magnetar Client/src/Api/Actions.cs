using Magnetar_Client.Modules;
using System;

namespace Magnetar_Client.Api;

/// <summary>
/// Global event hooks invoked throughout the Magnetar Client lifecycle.
/// </summary>
public static class Actions
{
    // --- Initialization ---

    /// <summary>
    /// Invoked at the very beginning of client initialization, before any addons are loaded or Harmony patches are applied.
    /// </summary>
    public static event Action OnEarlyInitialize;

    /// <summary>
    /// Invoked after the Harmony patches are applied.
    /// </summary>
    public static event Action OnInitializeHarmony;

    /// <summary>
    /// Invoked at the start of core initialization, before preferences, menus, and managers are instantiated.
    /// </summary>
    public static event Action OnEarlyInitializeCore;

    /// <summary>
    /// Invoked after preference entries have been created.
    /// </summary>
    public static event Action OnInitializePreferences;

    /// <summary>
    /// Invoked after all the client's services are registered.
    /// </summary>
    public static event Action OnInitializeCore;

    /// <summary>
    /// Invoked at the end of core initialization after all services are registered and save profile is loaded.
    /// </summary>
    public static event Action OnLateInitializeCore;

    // --- Update & Rendering ---

    /// <summary>
    /// Invoked every frame during Unity's update loop.
    /// </summary>
    public static event Action OnUpdate;

    /// <summary>
    /// Invoked when opening the menu.
    /// </summary>
    public static event Action OnMenuOpen;

    /// <summary>
    /// Invoked after closing the menu.
    /// </summary>
    public static event Action OnMenuClosed;

    /// <summary>
    /// Invoked once on the first GUI frame when fonts, textures, themes, and UI services are warmed up.
    /// </summary>
    public static event Action OnWarmUp;

    /// <summary>
    /// Invoked during Unity's IMGUI loop before client services and menus are drawn.
    /// </summary>
    public static event Action OnEarlyGUI;

    /// <summary>
    /// Invoked during Unity's IMGUI loop after client services and menus are drawn.
    /// </summary>
    public static event Action OnLateGUI;

    /// <summary>
    /// Invoked at the main GUI phase, affected by transition color.
    /// </summary>
    public static event Action OnGUIShow;

    // --- Application State ---

    /// <summary>
    /// Invoked when the game is shutting down, before services clean up and profile data is saved to disk.
    /// </summary>
    public static event Action OnApplicationQuit;

    // --- Localization & Configuration ---

    /// <summary>
    /// Invoked when the client localization changes and translation dictionaries have completed reloading.
    /// </summary>
    public static event Action OnLanguageChanged;

    /// <summary>
    /// Invoked when the active configuration profile is switched.
    /// </summary>
    public static event Action OnProfileChanged;

    // --- Theming & Scaling ---

    /// <summary>
    /// Invoked when the active UI theme changes.
    /// </summary>
    public static event Action OnThemeChanged;

    /// <summary>
    /// Invoked when the GUI scale changes. Parameter is the new scale value.
    /// </summary>
    public static event Action<float> OnGUIScaleChanged;

    // --- Module Lifecycle ---

    /// <summary>
    /// Invoked when any module is enabled. Parameter is the module instance.
    /// </summary>
    public static event Action<Module> OnModuleEnabled;

    /// <summary>
    /// Invoked when any module is disabled. Parameter is the module instance.
    /// </summary>
    public static event Action<Module> OnModuleDisabled;

    /// <summary>
    /// Invoked when any module's settings window is opened. Parameter is the module instance.
    /// </summary>
    public static event Action<Module> OnModuleSettingOpened;

    /// <summary>
    /// Invoked when any module's settings window is closed. Parameter is the module instance.
    /// </summary>
    public static event Action<Module> OnModuleSettingClosed;

    // --- Internal Invoke Helpers (called by the client internals only) ---

    internal static void Invoke_OnEarlyInitialize() => OnEarlyInitialize?.Invoke();
    internal static void Invoke_OnInitializeHarmony() => OnInitializeHarmony?.Invoke();
    internal static void Invoke_OnEarlyInitializeCore() => OnEarlyInitializeCore?.Invoke();
    internal static void Invoke_OnInitializePreferences() => OnInitializePreferences?.Invoke();
    internal static void Invoke_OnInitializeCore() => OnInitializeCore?.Invoke();
    internal static void Invoke_OnLateInitializeCore() => OnLateInitializeCore?.Invoke();
    internal static void Invoke_OnUpdate() => OnUpdate?.Invoke();
    internal static void Invoke_OnMenuOpen() => OnMenuOpen?.Invoke();
    internal static void Invoke_OnMenuClosed() => OnMenuClosed?.Invoke();
    internal static void Invoke_OnWarmUp() => OnWarmUp?.Invoke();
    internal static void Invoke_OnEarlyGUI() => OnEarlyGUI?.Invoke();
    internal static void Invoke_OnLateGUI() => OnLateGUI?.Invoke();
    internal static void Invoke_OnGUIShow() => OnGUIShow?.Invoke();
    internal static void Invoke_OnApplicationQuit() => OnApplicationQuit?.Invoke();
    internal static void Invoke_OnLanguageChanged() => OnLanguageChanged?.Invoke();
    internal static void Invoke_OnProfileChanged() => OnProfileChanged?.Invoke();
    internal static void Invoke_OnThemeChanged() => OnThemeChanged?.Invoke();
    internal static void Invoke_OnGUIScaleChanged(float scale) => OnGUIScaleChanged?.Invoke(scale);
    internal static void Invoke_OnModuleEnabled(Module m) => OnModuleEnabled?.Invoke(m);
    internal static void Invoke_OnModuleDisabled(Module m) => OnModuleDisabled?.Invoke(m);
    internal static void Invoke_OnModuleSettingOpened(Module m) => OnModuleSettingOpened?.Invoke(m);
    internal static void Invoke_OnModuleSettingClosed(Module m) => OnModuleSettingClosed?.Invoke(m);
}
