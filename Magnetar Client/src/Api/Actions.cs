using Magnetar_Client;
using Magnetar_Client.Core;
using System;

namespace Magnetar_Client.Api;

public static class Actions
{
    public static class Core
    {
        public static Action OnEarlyInitialize;
        public static Action OnPreApplyHarmonyPatches;
        public static Action OnPostApplyHarmonyPatches;

        public static Action OnEarlyInitializeCore;
        public static Action OnLateInitializeCore;
    }

    public static class SaveLoad
    {
        public static Action OnLateInitializePreferences;
    }

    public static class ModuleManager
    {

    }
}
