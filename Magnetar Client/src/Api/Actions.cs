// 
// This file contains Actions that will be invoked at specific
// moments during client initialization
//
// Note: Although these look like they are preserved for external
// mods/addons, but some of these are used by internal classes
//

using System;

namespace Magnetar_Client.Api;

public static class Actions
{
    // Initialization
    public static Action OnEarlyInitialize;

    public static Action OnEarlyInitializeCore;

    public static Action OnInitializePreferences;

    public static Action OnModuleCategoyInitialized;

    public static Action OnLateInitializeCore;

    // Update

    public static Action OnUpdate;
    public static Action OnWarmUp;
    public static Action OnGUI;
    public static Action OnGUIShow;

    // Quit
    public static Action OnApplicationQuit;

    // Misc
    public static Action OnLanguageChanged;
    public static Action OnProfileChanged;
    


}
