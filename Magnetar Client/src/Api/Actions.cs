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
    public static Action OnEarlyInitialize;

    /// <summary>
    /// Invoked after the Harmony patches are applied.
    /// </summary>
    public static Action OnInitializeHarmony;

    /// <summary>
    /// Invoked at the start of core initialization, before preferences, menus, and managers are instantiated.
    /// </summary>
    public static Action OnEarlyInitializeCore;

    /// <summary>
    /// Invoked after preference entries have been created.
    /// </summary>
    public static Action OnInitializePreferences;

    /// <summary>
    /// Invoked after all the client's services are registed.
    /// </summary>
    public static Action OnInitializeCore;

    /// <summary>
    /// Invoked at the end of core initialization after all services are registed and save profile is loaded.
    /// </summary>
    public static Action OnLateInitializeCore;

    // --- Update & Rendering ---

    /// <summary>
    /// Invoked every frame during Unity's update loop.
    /// </summary>
    public static Action OnUpdate;

    /// <summary>
    /// Invoked once on the first GUI frame when fonts, textures, themes, and UI services are warmed up.
    /// </summary>
    public static Action OnWarmUp;

    /// <summary>
    /// Invoked during Unity's IMGUI loop before client services and menus are drawn.
    /// </summary>
    public static Action OnEarlyGUI;
    /// <summary>
    /// Invoked during Unity's IMGUI loop after client services and menus are drawn.
    /// </summary>
    public static Action OnLateGUI;

    /// <summary>
    /// Invoked at the main GUI phase, affected by transition color.
    /// </summary>
    public static Action OnGUIShow;

    // --- Application State ---

    /// <summary>
    /// Invoked when the game is shutting down, before services clean up and profile data is saved to disk.
    /// </summary>
    public static Action OnApplicationQuit;

    // --- Localization & Configuration ---

    /// <summary>
    /// Invoked when the client localization changes and translation dictionaries have completed reloading.
    /// </summary>
    public static Action OnLanguageChanged;

    /// <summary>
    /// Invoked when the active configuration profile is switched.
    /// </summary>
    public static Action OnProfileChanged;
}