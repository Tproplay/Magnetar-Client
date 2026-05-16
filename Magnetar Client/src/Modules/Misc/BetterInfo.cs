
namespace Magnetar_Client.Modules
{
    public class BetterInfo : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Better Info";
        public override string Description { get; set; } = "Shows more plant/Zombie stats below HP.";
        public override string SearchHints { get; set; } = "bettershow showstats plantstats zombiestats statshow hpstats moreshow showmore" +
            " extrastats statdisplay plantinfo zombieinfo statsbelow statvisual showhp statui betterstats moreshowing statviewer infodisplay" +
            " healthstats statdetails stattext zombiestat plantstat showinfo betterinfo statlabel";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;
    }
}
