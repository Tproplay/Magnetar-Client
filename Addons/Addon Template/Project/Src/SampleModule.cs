using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;

namespace AddonTemplate;

/// <summary>
/// A sample Magnetar module showing settings, UI grouping, and lifecycle hooks.
/// Any class extending Module is automatically discovered and registered by Magnetar Client.
/// </summary>
public class SampleModule : Module
{
    // Mod Info
    public override string Name { get; set; } = "Sample Feature";
    public override string Description { get; set; } = "A template feature demonstrating settings and callbacks.";
    public override string SearchHints { get; set; } = "sample template example addon quickstart";
    public override string Author { get; set; } = "Your Name";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Addon;

    public override bool EnableInVanillaMode { get; set; } = true;

    // Mod Data
    public BoolSetting EnableFeature;
    public FloatSetting Multiplier;
    public StringSetting CustomText;

    public static SampleModule Instance { get; private set; }

    public SampleModule()
    {
        Instance = this;

        CreateCategory("General");

        EnableFeature = new BoolSetting("Enable Sub-feature", true);
        Multiplier = new FloatSetting("Multiplier", 1.0f, 10.0f, 2.0f, 1);
        CustomText = new StringSetting("Custom Message", "Hello from Addon Template!");

        AddSettings(EnableFeature, Multiplier, CustomText);
        EndCategory();
    }

    // Mod Logic

    public override void OnUpdateActive()
    {
        // Do frame-level logic here
    }

    public override void OnEnable()
    {
        Magnetar_Logger.DebugLogger.Msg($"[{Name}] Enabled.");
    }

    public override void OnDisable()
    {
        Magnetar_Logger.DebugLogger.Msg($"[{Name}] Disabled.");
    }
}
