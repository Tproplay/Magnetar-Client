namespace Magnetar_Client.Modules
{
    public class DimBackground : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Dim Background";
        public override string Description { get; set; } = "Dims the background while the modules window is open";
        public override string SearchHints { get; set; } = "dimbackground transparentbackground backgrounddim " +
            "backgroundtransparent dimmedbackground bgdim bgtransparent dimbg transparentbg darkerbackground " +
            "lowopacitybackground glassbackground clearbackground seethroughbackground dimbackround dimbackgound " +
            "transperent transparant transparents transperantbg transparantbg dimbackgorund opacitymultiplier hidebackground " +
            "nobackground backgroundopacity backgroundalpha backgroundshade backgrounddarken backgroundblur";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        public override bool Active { get; set; } = true; // On by default

        // Mod Data

        public static DimBackground instance;

        public  DimBackground()
        {
            instance = this;
            Config.dimBg = Active;
        }

        // Mod Logic
        public override void OnEnable()
        {
            Config.dimBg = true;
        }

        public override void OnDisable()
        {
            Config.dimBg = false;
        }
    }
}
