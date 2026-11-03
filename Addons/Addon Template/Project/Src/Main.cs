using System.Reflection;
using Magnetar_Client.Api;
using Magnetar_Client.Utils;

namespace AddonTemplate;

/// <summary>
/// Main addon lifecycle entrypoint discovered and loaded by Magnetar Client.
/// </summary>
public class Main : IMagnetarAddon
{
    public static Main Instance { get; private set; }

    public string Name => "Addon Template";
    public string Version => "1.0.0";
    public string Author => "Your Name";

    public Main()
    {
        Instance = this;
    }

    public void OnLoad()
    {
        Actions.OnInitializeHarmony += SafePatchAll;
        Magnetar_Logger.DebugLogger.Msg($"[{Name}] Loaded");
    }

    private void SafePatchAll()
    {
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
