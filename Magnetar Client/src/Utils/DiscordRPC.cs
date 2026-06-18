using HarmonyLib;
using Magnetar_Client.Core;
using System;
using System.IO;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using MelonLoader;
using MelonLoader.Utils;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
#endif

namespace Magnetar_Client.Utils
{
    [HarmonyPatch(typeof(main))]
    public static class MainPatch
    {
#if MELONLOADER || RELEASE_MELON
        [HarmonyPatch(nameof(main.OnInitializeMelon))]
#elif BEPINEX || RELEASE_BEPINEX
        [HarmonyPatch(nameof(main.Load))]
#endif
        [HarmonyPostfix]
        public static void DiscordRPC()
        {
#if MELONLOADER || RELEASE_MELON
            string userLibsPath = MelonEnvironment.UserLibsDirectory;
#elif BEPINEX || RELEASE_BEPINEX
            string userLibsPath = Path.Combine(Paths.BepInExRootPath, "UserLibs");
#endif

            if (!Directory.Exists(userLibsPath))
            {
                DebugLogger.Warning($"UserLibs directory not found at: {userLibsPath}");
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
                DebugLogger.Msg($"[Discord RPC] Successfully linked Discord library at: {userLibsPath}");
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[Discord RPC] Failed to set Environment PATH: {ex.Message}");
            }
#pragma warning restore CS0168 // Variable is declared but never used

        }
    }
}