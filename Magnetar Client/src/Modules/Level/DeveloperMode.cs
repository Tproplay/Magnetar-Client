using HarmonyLib;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
namespace Magnetar_Client.Modules
{
    public class DeveloperMode : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Developer Mode";
        public override string Description { get; set; } = "Developer Mode is used by game devs during game testing";
        public override string SearchHints { get; set; } = "developermode devmode devmod developer " +
            "devtools developmentmode debugmode debugdev devtesting gametesting dev-tools devfeatures" +
            " adminmode devconsole cheatsdev devsettings testingmode developeroptions devtoolsenabled" +
            " devaccess devmenu developeraccess devbuild testingtool";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        public override bool Active
        {
            get => GameAPP.developerMode;
            set => GameAPP.developerMode = value;
        }
        // Mod Data

        public static DeveloperMode instance;

        public DeveloperMode() { instance = this; }


        
    }
}
