using System;
using System.Collections.Generic;
using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using UnityEngine;

namespace Magnetar_Client.Api
{
    public static class Api
    {
        #region Lifecycle Events
        /// <summary>Invoked before core systems, modules, and configurations initialize.</summary>
        public static Action EarlyInitializeCore;

        /// <summary>Invoked after all modules, profiles, and configurations are loaded and active.</summary>
        public static Action LateInitializeCore;

        /// <summary>Invoked every frame on Unity's Update loop after modules update.</summary>
        public static Action OnUpdate;

        /// <summary>Invoked every frame inside Unity's OnGUI within Magnetar's scaled matrix.</summary>
        public static Action OnGUI;

        /// <summary>Invoked when the game application is shutting down before preferences save.</summary>
        public static Action OnApplicationQuit;

        /// <summary>Invoked once GUI shaders, themes, and font caches warm up.</summary>
        public static Action OnGUIWarmUp;
        #endregion

        #region Config & Profile Events
        /// <summary>Fired whenever client configuration or preferences are saved to disk.</summary>
        public static Action OnConfigSaved;

        /// <summary>Fired whenever an active profile finishes loading from disk.</summary>
        public static Action<string> OnProfileLoaded;
        #endregion

        #region State Queries
        /// <summary>True if the Magnetar UI is currently open on screen.</summary>
        public static bool IsMenuOpen => Config.showgui;

        /// <summary>Returns true if the client core has fully initialized.</summary>
        public static bool IsInitialized => ModuleManager.IsInitialized;
        #endregion

        #region Module Helpers
        /// <summary>Registers an addon module into the client registry.</summary>
        public static void RegisterModule(Type moduleType)
        {
            if (moduleType == null) return;
            ModuleManager.RegisterModule(moduleType);
        }

        /// <summary>Registers an addon module into the client registry.</summary>
        public static void RegisterModule<T>() where T : Module
        {
            ModuleManager.RegisterModule(typeof(T));
        }

        /// <summary>Finds a registered module by its generic type.</summary>
        public static T GetModule<T>() where T : Module
        {
            foreach (var mod in ModuleManager.Modules)
            {
                if (mod is T target) return target;
            }
            return null;
        }

        /// <summary>Finds a registered module by its display name.</summary>
        public static Module GetModule(string moduleName)
        {
            foreach (var mod in ModuleManager.Modules)
            {
                if (string.Equals(mod.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                    return mod;
            }
            return null;
        }
        #endregion

        #region Notifications & Logging
        /// <summary>Dispatches a debug log message through Magnetar's formatted logger.</summary>
        public static void Log(string message) => Utils.Magnetar_Logger.DebugLogger.Msg(message);

        /// <summary>Dispatches an error log message through Magnetar's formatted logger.</summary>
        public static void LogError(string message) => Utils.Magnetar_Logger.DebugLogger.Error(message);
        #endregion
    }
}