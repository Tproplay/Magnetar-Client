using HarmonyLib;
using Magnetar_Client.Core;
using System.Collections.Generic;
using UnityEngine;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.NEF.Data
{
    public static class NEFRecipes
    {
        // UNIFIED ENTITY WRAPPER
        public struct RecipeEntity : System.IEquatable<RecipeEntity>
        {
            public int Id;
            public bool IsZombie;

            public static RecipeEntity Plant(PlantType pt) => new RecipeEntity { Id = (int)pt, IsZombie = false };
            public static RecipeEntity Zombie(ZombieType zt) => new RecipeEntity { Id = (int)zt, IsZombie = true };
            public static RecipeEntity Custom(int customId) => new RecipeEntity { Id = customId, IsZombie = false };
            public static RecipeEntity Nothing() => new RecipeEntity { Id = -1, IsZombie = false };

            public bool IsNothing => Id == -1 && !IsZombie;

            public bool Equals(RecipeEntity other) => Id == other.Id && IsZombie == other.IsZombie;

            public override bool Equals(object obj)
            {
                return obj is RecipeEntity other && Equals(other);
            }
            public override int GetHashCode() => Id.GetHashCode() ^ IsZombie.GetHashCode();
            public static bool operator ==(RecipeEntity a, RecipeEntity b) => a.Equals(b);
            public static bool operator !=(RecipeEntity a, RecipeEntity b) => !a.Equals(b);
        }

        public class CustomRecipe
        {
            public RecipeEntity Result;
            public RecipeEntity ParentA;
            public RecipeEntity ParentB = RecipeEntity.Nothing(); // If Nothing, it's a single parent
            public RecipeEntity ParentC = RecipeEntity.Nothing();

            public bool IsTriple => !ParentC.IsNothing;
            public bool IsSingle => ParentB.IsNothing && ParentC.IsNothing;

            // Optional Edge Messages
            public string EdgeMessage = "";
            public Color EdgeMessageColor = Color.white;
        }

        public static List<CustomRecipe> TitanPlants = new List<CustomRecipe>
        {
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.BigGatling),
                ParentA = RecipeEntity.Plant(PlantType.DoubleShooter),
                ParentB = RecipeEntity.Plant(PlantType.ThreePeater),
                ParentC = RecipeEntity.Plant(PlantType.DoubleShooter),
            },
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.BigChomper),
                ParentA = RecipeEntity.Plant(PlantType.Chomper),
                ParentB = RecipeEntity.Plant(PlantType.Chomper),
                ParentC = RecipeEntity.Plant(PlantType.Chomper),
            },
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.BigPumpkin),
                ParentA = RecipeEntity.Plant(PlantType.MagnetPumpkin),
                ParentB = RecipeEntity.Plant(PlantType.Magnetshroom),
                ParentC = RecipeEntity.Plant(PlantType.CherryPumpkin),
            },
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.HugeWallNut),
                ParentA = RecipeEntity.Plant(PlantType.WallNut),
                ParentB = RecipeEntity.Plant(PlantType.TallNut),
                ParentC = RecipeEntity.Plant(PlantType.WallNut),
            },
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.BigGloom),
                ParentA = RecipeEntity.Plant(PlantType.SmallPuff),
                ParentB = RecipeEntity.Plant(PlantType.FumeShroom),
                ParentC = RecipeEntity.Plant(PlantType.SmallPuff),
            },
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.CabbageCannon),
                ParentA = RecipeEntity.Plant(PlantType.Cabbagepult),
                ParentB = RecipeEntity.Plant(PlantType.Cabbagepult),
                ParentC = RecipeEntity.Plant(PlantType.Cabbagepult),
            },
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.BigSeaShroom),
                ParentA = RecipeEntity.Plant(PlantType.Tanglekelp),
                ParentB = RecipeEntity.Plant(PlantType.SeaShroom),
                ParentC = RecipeEntity.Plant(PlantType.Tanglekelp),
            },
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.MelonCannon),
                ParentA = RecipeEntity.Plant(PlantType.Melonpult),
                ParentB = RecipeEntity.Plant(PlantType.Cornpult),
                ParentC = RecipeEntity.Plant(PlantType.Melonpult),
            }
        };

        public static List<CustomRecipe> SpawnedPlants = new List<CustomRecipe>
        {
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.BigSunNut),
                ParentA = RecipeEntity.Plant(PlantType.UltimateSunNut),
                EdgeMessage = "On Click",
                EdgeMessageColor = Color.white
            }
        };

        public static List<CustomRecipe> MiscellaneousPlants = new List<CustomRecipe>
        {
            new CustomRecipe
            {
                Result = RecipeEntity.Plant(PlantType.MagicSnowPea),
                ParentA = RecipeEntity.Plant(PlantType.SnowPeaShooter),
                ParentB = RecipeEntity.Plant(PlantType.IceBean)
            }
        };



        public static List<CustomRecipe> ZombieItemPlants = new List<CustomRecipe>
        {

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

            foreach (CustomRecipe plantRecipe in SpawnedPlants)
            {
                AddToList(plantRecipe);
            }

            foreach (CustomRecipe plantRecipe in MiscellaneousPlants)
            {
                AddToList(plantRecipe);
            }
        }
    }
}