using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Core;
using System.Collections.Generic;

namespace Magnetar_Client.NEF.Data
{
    public static class NEFRecipes
    {
        public class CustomRecipe
        {
            public PlantType Result;
            public PlantType ParentA;
            public PlantType ParentB;
            public PlantType ParentC = PlantType.Nothing; // Default to Nothing for standard 2-plant fusions
            public bool IsTriple => ParentC != PlantType.Nothing;
        }


        public static List<CustomRecipe> TitanPlants = new List<CustomRecipe>
            {
                new CustomRecipe
                {
                    Result = PlantType.BigGatling,
                    ParentA = PlantType.DoubleShooter,
                    ParentB = PlantType.ThreePeater,
                    ParentC = PlantType.DoubleShooter,
                },

                new CustomRecipe
                {
                    Result = PlantType.BigChomper,
                    ParentA = PlantType.Chomper,
                    ParentB= PlantType.Chomper,
                    ParentC= PlantType.Chomper,
                },

                new CustomRecipe
                {
                    Result = PlantType.BigPumpkin,
                    ParentA = PlantType.JackboxPumpkin,
                    ParentB = PlantType.Magnetshroom,
                    ParentC = PlantType.CherryPumpkin
                },

                new CustomRecipe
                {
                    Result = PlantType.BigWallNut,
                    ParentA = PlantType.WallNut,
                    ParentB = PlantType.TallNut,
                    ParentC = PlantType.WallNut
                },



            };



        public static void AddToList(CustomRecipe plantRecipe)
        {
            NEFData.AddedRecipes.Add(plantRecipe);
        }

        public static void RemoveFromList(CustomRecipe plantRecipe)
        {
            if (NEFData.AddedRecipes.Contains(plantRecipe))
                NEFData.AddedRecipes.Remove(plantRecipe);
        }

        public static void InitRecipes()
        {
            foreach (CustomRecipe plantRecipe in TitanPlants)
            {
                AddToList(plantRecipe);
            }
        }
    }
}
