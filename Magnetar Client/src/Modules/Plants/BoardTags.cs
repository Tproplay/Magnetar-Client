using Il2Cpp;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    
    public class OdysseyPlants : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Odyssey Plants";
        public override string Description { get; set; } = "Allows you to fuse Travel/Odyssey Plants.";
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
            if (BoardInstanceIsNull) // Out of the game
            { wasAlreadyEnabled = null; return; }
            if
                (!board.boardTag.isColumn) // Reset Level
            {
                wasAlreadyEnabled = null;
            }


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
