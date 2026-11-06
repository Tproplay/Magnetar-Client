using System.Reflection;
using Magnetar_Client.Api;
using Magnetar_Client.Utils;

namespace VideoPlayer;

/// <summary>
/// Main addon lifecycle entrypoint discovered and loaded by Magnetar Client.
/// </summary>
public class Main : IMagnetarAddon
{
    public static Main Instance { get; private set; }

    public string Name => "Video Player";
    public string Version => "1.0.0";
    public string Author => "Tproplay";

    public Main()
    {
        Instance = this;
    }

    public void OnLoad()
    {
        Actions.OnInitializeHarmony += SafePatchAll;
        if (Magnetar_Client.Core.Main.HarmonyInstance != null)
        {
            SafePatchAll();
        }
        Magnetar_Logger.DebugLogger.Msg($"[{Name}] Loaded");
    }

    private void SafePatchAll()
    {
        if (Magnetar_Client.Core.Main.HarmonyInstance == null) return;

        var patchInfo = HarmonyManager.HarmonyPatchAll(Assembly.GetExecutingAssembly(), Magnetar_Client.Core.Main.HarmonyInstance);

        foreach (var failure in patchInfo.Failures)
        {
            MagnetarApi.LogWarning($"[{Name}] Failed to apply patch '{failure.ClassName}': {failure.Exception.Message}");
        }

        MagnetarApi.LogMsg($"[{Name}] Harmony initialized: {patchInfo.SuccessCount} class(es) patched.");
    }

    public void OnUnload()
    {
        Actions.OnInitializeHarmony -= SafePatchAll;
    }
}
