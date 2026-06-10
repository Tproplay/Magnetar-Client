using HarmonyLib;
using Magnetar_Client.Core;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.IO;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils
{
    [HarmonyPatch(typeof(main))]
    public static class MainPatch
    {
        [HarmonyPatch(nameof(main.OnInitializeMelon))]
        [HarmonyPostfix]
        public static void DiscordRPC()
        {
            string userLibsPath = MelonEnvironment.UserLibsDirectory;

            if (!Directory.Exists(userLibsPath)) 
            { 
                MelonLogger.Warning($"UserLibs directory not found at: {userLibsPath}"); 
                return;
            }

#pragma warning disable CS0168 // Variable is declared but never used
            try
            {
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

                // Only append if it's not already there to prevent bloat
                if (!currentPath.Contains(userLibsPath))
                {
                    Environment.SetEnvironmentVariable("PATH", $"{currentPath};{userLibsPath}",
                        EnvironmentVariableTarget.Process);
                }
#if DEBUG
                DebugLogger.Msg($"[Discord RPC] Successfully linked Discord library at: {userLibsPath}");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                DebugLogger.Error($"[Discord RPC] Failed to set Environment PATH: {ex.Message}");
#endif
            }
#pragma warning restore CS0168 // Variable is declared but never used

        }
    }
}