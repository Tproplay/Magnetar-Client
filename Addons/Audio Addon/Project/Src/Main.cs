using Magnetar_Client.Api;
using Magnetar_Client.Utils;

namespace AudioAddon.Src;

public class Main : IMagnetarAddon
{
    public string Name => "Audio Addon";

    public string Version => "1.0.0";

    public string Author => "Tproplay";

    public void OnLoad()
    {
        Actions.OnInitializeHarmony += SafePatchAll;
        if (Magnetar_Client.Core.Main.HarmonyInstance != null)
        {
            SafePatchAll();
        }
    }

    public void OnUnload() { }

    private void SafePatchAll()
    {
        if (Magnetar_Client.Core.Main.HarmonyInstance == null) return;

        var patchInfo = HarmonyManager.HarmonyPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), Magnetar_Client.Core.Main.HarmonyInstance);

        foreach (var failure in patchInfo.Failures)
        {
            MagnetarApi.LogWarning($"[{Name}] Failed to apply patch '{failure.ClassName}': {failure.Exception.Message}");
        }

        MagnetarApi.LogMsg($"[{Name}] Harmony initialized: {patchInfo.SuccessCount} class(es) patched.");
    }
}
