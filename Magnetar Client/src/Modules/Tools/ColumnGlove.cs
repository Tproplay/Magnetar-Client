using Il2Cpp;
using HarmonyLib;
using System.Collections.Generic;
using static Magnetar_Client.Game.GameData;

namespace Magnetar_Client.Modules
{
    public class ColumnGlove : Module
    {
        public override string Name { get; set; } = "Column Glove";
        public override string Description { get; set; } = "Moving a plant with the glove moves all identical plants in that column simultaneously.";
        public override string SearchHints { get; set; } = "columnglove glovecol columnmove movecolumn identicalplants " +
            "moveplants columnglovemod simultaneousmove massmove multi-move gloveswap plantglove plantcolumn glovemove " +
            "multijmove plantmover glovecolumn moveall rowmove quickmove glovecopy columncopy columnsync gloveplant " +
            "columnshift movemulti plantshift glovesync syncmove identicalmove";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        public static ColumnGlove instance;

        public ColumnGlove()
        {
            instance = this;
            
        }

        [HarmonyPatch(typeof(Mouse), nameof(Mouse.TryToSetPlantByGlove))]
        public static class MouseGlovePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Mouse __instance)
            {
                if (instance == null || !instance.Active) return true;

                int newCol = __instance.theMouseColumn;
                List<Plant> plants = new List<Plant>();

                // Find all identical plants in the original column
                foreach (var plant in plantList)
                {
                    if (plant == null || plant.gameObject == null) continue;

                    if (plant.thePlantColumn == __instance.thePlantOnGlove.thePlantColumn)
                    {
                        if (plant != __instance.thePlantOnGlove && plant.thePlantType == __instance.thePlantOnGlove.thePlantType)
                        {
                            plants.Add(plant);
                        }
                    }
                }

                // Replicate movement across the board
                foreach (var plant in plants)
                {
                    Plant gameObject = CreatePlant.Instance.SetPlant(newCol, plant.thePlantRow, plant.thePlantType);

                    if (newCol == __instance.thePlantOnGlove.thePlantColumn)
                    {
                        CreatePlant.Instance.SetPlant(newCol, __instance.thePlantOnGlove.thePlantRow, plant.thePlantType);
                    }
                    else
                    {
                        if (gameObject != null && gameObject.TryGetComponent<Plant>(out var component) && component != null)
                        {
                            plant.Die(Plant.DieReason.ByMix);
                        }
                    }
                }
                return true;
            }
        }
    }
}