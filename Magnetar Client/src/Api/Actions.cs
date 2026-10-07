using Magnetar_Client;
using Magnetar_Client.Core;
using System;

namespace Magnetar_Client.Api;

public static class Actions
{
    // Initialization
    public static Action OnEarlyInitialize;
    public static Action OnPreApplyHarmonyPatches;
    public static Action OnPostApplyHarmonyPatches;

    public static Action OnEarlyInitializeCore;

    public static Action OnEarlyInitializePreferences;
    public static Action OnLateInitializePreferences;

    public static Action OnLateInitializeCore;

    // Update

    public static Action OnUpdate;
    public static Action OnWarmUp;
    public static Action OnGUI;
    public static Action OnGUIShow;

    // Quit
    public static Action OnEarlyApplicationQuit;
    public static Action OnLateApplicationQuit;

    // Misc

}
