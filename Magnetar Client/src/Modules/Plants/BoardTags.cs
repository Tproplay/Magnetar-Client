using Il2Cpp;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class MultiPlanting : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Multiplanting";
        public override string Description { get; set; } = "Columns' Like you See 'Em";
        public override string SearchHints { get; set; } = "multiplanting columnplanting multiplant " +
            "columnplant multiplants columnplants multiplantingmod columnplantingmod multiplantingplugin " +
            "multiplantingtool multiplantinghack multiplantingcheat multiplaning multiplantin multiplantting " +
            "multiplantting columplanting columnplaning columnplantting columnplanter multiplanter rowplanting " +
            "gridplanting massplanting batchplanting fastplanting autoplanter plantmultiplier plantcolumn plantrows " +
            "areaofeffectplanting";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static MultiPlanting instance;

        private static string wasAlreadyEnabled;

#if DEBUG
        public BoolSetting DebugMode;
#endif

        public MultiPlanting()
        {
            instance = this;
#if DEBUG
            DebugMode = new BoolSetting("Debug Mode", false);
            Settings.Add(DebugMode);
#endif
        }


        // Mod Logic

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull || !board.boardTag.isColumn) { wasAlreadyEnabled = null; return; }


            if (wasAlreadyEnabled != null) return;

#if DEBUG
            if (instance.DebugMode.Value) DebugLogger.Msg("Triggered Multiplant Enable");
#endif

            wasAlreadyEnabled = board.boardTag.isColumn ? "Yes" : "No";

#if DEBUG
            DebugLogger.Msg("Level was found to have boardtag.isColumn" + wasAlreadyEnabled);
#endif

            Board.BoardTag boardTags = board.boardTag;
            boardTags.isColumn = true;

            board.boardTag = boardTags;
#if DEBUG
            if (board.boardTag.isColumn) DebugLogger.Msg("Successfully Enabled Multiplanting");
#endif
        }

        public override void OnDisable()
        {
            if (BoardInstanceIsNull || wasAlreadyEnabled == null || wasAlreadyEnabled == "Yes") { wasAlreadyEnabled = null; return; }

            Board.BoardTag boardTags = board.boardTag;
            boardTags.isColumn = false;

            board.boardTag = boardTags;
            wasAlreadyEnabled = null;

        }


    }

    public class OdysseyPlants : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Odyssey Plants";
        public override string Description { get; set; } = "Allows you to fuse Travel/Odyssey Plants";
        public override string SearchHints { get; set; } = "travelplants odysseyplants travelplantfusion " +
            "odysseyplantfusion fuseplants fusionplants travelplantodyssey travelodyssey odysseyfuse travelfuse " +
            "plantmerging plantcombine travelplantcombine odysseyplantcombine travelplantmix odysseyplantmix " +
            "travelplantmerger odysseyplantmerger travelplantsfused odysseyplantsfused travelplantsmix travelplantsmerger " +
            "travalplants travleplants odesseyplants odessyplants odyseyplants odyseeplants plantfusion travelodysseymod";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static OdysseyPlants instance;

        private static string wasAlreadyEnabled;

        public OdysseyPlants() { instance = this; }


        // Mod Logic
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull || !board.boardTag.enableAllTravelPlant) { wasAlreadyEnabled = null; return; }
            if (wasAlreadyEnabled != null) return;

            wasAlreadyEnabled = board.boardTag.enableAllTravelPlant ? "Yes" : "No";

            Board.BoardTag boardTags = board.boardTag;
            boardTags.enableAllTravelPlant = true;

            board.boardTag = boardTags;

        }

        public override void OnDisable()
        {
            if (BoardInstanceIsNull || wasAlreadyEnabled == null || wasAlreadyEnabled == "Yes") { wasAlreadyEnabled = null; return; }

            Board.BoardTag boardTags = board.boardTag;
            boardTags.enableAllTravelPlant = false;

            board.boardTag = boardTags;
            wasAlreadyEnabled = null;
        }
    }

}
