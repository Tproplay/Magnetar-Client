using System;
using System.Reflection;
using HarmonyLib;
using Magnetar_Client.Api;
using Magnetar_Client.Utils;

namespace AddonTemplate;

/// <summary>
/// Sample Harmony patch template demonstrating method hooking.
/// To activate: uncomment [HarmonyPatch] attributes and specify your target class and method.
/// </summary>
// [HarmonyPatch(typeof(TargetGameClass), nameof(TargetGameClass.TargetMethod))]
public static class SamplePatch
{
    // Prefix: Runs BEFORE the original game method.
    // Return true to continue with original execution, or false to skip original method.
    // [HarmonyPrefix]
    // public static bool Prefix()
    // {
    //     Magnetar_Logger.DebugLogger.Msg("[Addon Template] Prefix hook executed.");
    //     return true;
    // }

    // Postfix: Runs AFTER the original game method completes.
    // [HarmonyPostfix]
    // public static void Postfix()
    // {
    //     Magnetar_Logger.DebugLogger.Msg("[Addon Template] Postfix hook executed.");
    // }
}
