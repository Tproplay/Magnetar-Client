#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;


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
        if (GameData.BoardInstanceIsNull) // Out of the game
        { wasAlreadyEnabled = null; return; }
        if
            (!GameData.BoardInstance.boardTag.isColumn) // Reset Level
        {
            wasAlreadyEnabled = null;
        }


        if (wasAlreadyEnabled != null) return;

        wasAlreadyEnabled = GameData.BoardInstance.boardTag.enableAllTravelPlant ? "Yes" : "No";

        Board.BoardTag boardTags = GameData.BoardInstance.boardTag;
        boardTags.enableAllTravelPlant = true;
        GameData.
                BoardInstance.boardTag = boardTags;

    }

    public override void OnDisable()
    {
        if (GameData.BoardInstanceIsNull || wasAlreadyEnabled == null || wasAlreadyEnabled == "Yes") { wasAlreadyEnabled = null; return; }

        Board.BoardTag boardTags = GameData.BoardInstance.boardTag;
        boardTags.enableAllTravelPlant = false;
        GameData.
                BoardInstance.boardTag = boardTags;
        wasAlreadyEnabled = null;
    }
}
