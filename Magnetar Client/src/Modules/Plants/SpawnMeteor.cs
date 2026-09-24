#if MELONLOADER || RELEASE_MELON
#endif

namespace Magnetar_Client.Modules;

public class SpawnMeteor : Module
{
    // Mod Info
    public override string Name { get; set; } = "Spawn Meteor";
    public override string Description { get; set; } = "Spawns a meteor on the lawn";
    public override string SearchHints { get; set; } = "spawnmeteor meteorfall meteorshower meteorstrike " +
        "meteorspawn lawnmeteor meteordrop spacehazard impactmeteor meteorcheat meteorshowermod meteorimpact " +
        "summonmeteor fallingstar meteorite meteorsmash skyhazard";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

    // Mod Data

    public static SpawnMeteor instance;


    public SpawnMeteor()
    {
        instance = this;

        CreateCategory("General");


        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        
    }

    // Mod Logic

}
