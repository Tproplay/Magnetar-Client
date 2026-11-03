using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Magnetar_Client.Utils;
using Magnetar_Client.Api;

namespace MagnetarAIAddon.Core;

public class Main : IMagnetarAddon
{
    public static Main Instance { get; private set; }

    public string Name => "Assist Bot";

    public string Version => "4.0.3";

    public string Author => "Tproplay";

    public void OnLoad()
    {
        Actions.OnLateInitializeCore += OnLateInitializeCore;
        Actions.OnInitializeHarmony += SafePatchAll;
    }

    private static void OnLateInitializeCore()
    {
#if ONLINE
        if (AssistBot.instance == null)
            return;
        
        if (AssistBot.instance.UpdateOnStart != null && AssistBot.instance.UpdateOnStart.Value)
        {
            Magnetar_Logger.DebugLogger.Msg("[Assist Bot] 'Update on start' is enabled. Syncing model with server...");
            _ = Task.Run(async () =>
            {
                await AIService.SyncModelWithServerAsync();
            });
        }
        else
        {
            Magnetar_Logger.DebugLogger.Msg("[Assist Bot] 'Update on start' is disabled or module inactive. Skipping model sync.");
        }
#endif
    }

    private void SafePatchAll()
    {
        var patchInfo = HarmonyManager.HarmonyPatchAll(Assembly.GetExecutingAssembly(), Magnetar_Client.Core.Main.HarmonyInstance);

        foreach (var failure in patchInfo.Failures)
        {
            MagnetarApi.LogWarning($"[Assist Bot] Failed to apply patch '{failure.ClassName}': {failure.Exception.Message}");
        }

        MagnetarApi.LogMsg($"[Assist Bot] Harmony initialized: {patchInfo.SuccessCount} classes successfully patched.");
    }

    public void OnUnload() { }
}
