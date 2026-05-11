using Il2Cpp;

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

        private static string wasAlreadyEnabled;

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (Board.Instance == null) { wasAlreadyEnabled = null; return; }
            if (wasAlreadyEnabled != null) return;

            wasAlreadyEnabled = Board.Instance.boardTag.isColumn ? "Yes" : "No";

            Board.BoardTag boardTags = Board.Instance.boardTag;
            boardTags.isColumn = true;

            Board.Instance.boardTag = boardTags;

        }

        public override void OnDisable()
        {
            if (wasAlreadyEnabled == null || wasAlreadyEnabled == "Yes") { wasAlreadyEnabled = null; return; }

            Board.BoardTag boardTags = Board.Instance.boardTag;
            boardTags.isColumn = false;

            Board.Instance.boardTag = boardTags;
            wasAlreadyEnabled = null;

        }
    }

    public class TravelPlants : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Travel Plants";
        public override string Description { get; set; } = "Allows you to fuse Travel/Odyssey Plants";
        public override string SearchHints { get; set; } = "travelplants odysseyplants travelplantfusion " +
            "odysseyplantfusion fuseplants fusionplants travelplantodyssey travelodyssey odysseyfuse travelfuse " +
            "plantmerging plantcombine travelplantcombine odysseyplantcombine travelplantmix odysseyplantmix " +
            "travelplantmerger odysseyplantmerger travelplantsfused odysseyplantsfused travelplantsmix travelplantsmerger " +
            "travalplants travleplants odesseyplants odessyplants odyseyplants odyseeplants plantfusion travelodysseymod";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;
        
        // Mod Data

        private static string wasAlreadyEnabled;

        // Mod Logic
        public override void OnUpdateActive()
        {
            if (Board.Instance == null) { wasAlreadyEnabled = null; return; }
            if (wasAlreadyEnabled != null) return;

            wasAlreadyEnabled = Board.Instance.boardTag.enableAllTravelPlant ? "Yes" : "No";

            Board.BoardTag boardTags = Board.Instance.boardTag;
            boardTags.enableAllTravelPlant = true;

            Board.Instance.boardTag = boardTags;

        }

        public override void OnDisable()
        {
            if (wasAlreadyEnabled == null || wasAlreadyEnabled == "Yes") { wasAlreadyEnabled = null; return; }

            Board.BoardTag boardTags = Board.Instance.boardTag;
            boardTags.enableAllTravelPlant = false;

            Board.Instance.boardTag = boardTags;
            wasAlreadyEnabled = null;
        }
    }

}
