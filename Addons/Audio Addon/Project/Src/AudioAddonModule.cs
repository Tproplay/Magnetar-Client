using Magnetar_Client.Api;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;

namespace AudioAddon;

public class AudioAddonModule : Module
{
    // Mod Info
    public override string Name { get; set; } = "Audio Addon Feature";
    public override string Description { get; set; } = "Demonstrates settings and lifecycle callbacks.";
    public override string SearchHints { get; set; } = "audioaddon addon feature";
    public override string Author { get; set; } = "Tproplay";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Addon;

    public override bool EnableInVanillaMode { get; set; } = true;

    // Mod Data

    public static AudioAddonModule Instance { get; private set; }

    public AudioAddonModule()
    {
        Instance = this;

        SafePatchAll();

        CreateCategory("General");

        AddSettings();
        EndCategory();
    }

    private void SafePatchAll()
    {
        if (Magnetar_Client.Core.Main.HarmonyInstance == null)
        {
            Actions.OnInitializeHarmony += SafePatchAll;
            return;
        }

        var patchInfo = HarmonyManager.HarmonyPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), Magnetar_Client.Core.Main.HarmonyInstance);

        foreach (var failure in patchInfo.Failures)
        {
            MagnetarApi.LogWarning($"[{Name}] Failed to apply patch '{failure.ClassName}': {failure.Exception.Message}");
        }

        MagnetarApi.LogMsg($"[{Name}] Harmony initialized: {patchInfo.SuccessCount} class(es) patched.");
    }

    // Mod Logic

    public override void OnUpdateActive()
    {
        
    }

    public override void OnEnable()
    {
        
    }

    public override void OnDisable()
    {

    }
}
