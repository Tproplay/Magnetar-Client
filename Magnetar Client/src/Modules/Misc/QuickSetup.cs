namespace Magnetar_Client.Modules
{
    public class QuickSetup : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Quick setup";
        public override string Description { get; set; } = "Save/load a custom setup loadout.";
        public override string SearchHints { get; set; } = "quicksetup loadout setupmanager savebuild loadbuild " +
            "setup presetmanager quickload quicksave plantsetup configuration buildmanager setuploader loadouts " +
            "quick-setup customsetup setupsaver presetload";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data

        public static QuickSetup instance;

        public QuickSetup()
        {
        }

        // Mod Logic
    }
}
