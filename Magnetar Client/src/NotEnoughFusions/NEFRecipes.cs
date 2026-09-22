using System.Collections.Generic;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.NEF.Data;

public static class NEFRecipes
{
    // UNIFIED ENTITY WRAPPER
    public struct RecipeEntity : System.IEquatable<RecipeEntity>
    {
        public int Id;
        public bool IsZombie;

        public static RecipeEntity Plant(PlantType pt) => new() { Id = (int)pt, IsZombie = false };
        public static RecipeEntity Zombie(ZombieType zt) => new() { Id = (int)zt, IsZombie = true };
        public static RecipeEntity Custom(int customId) => new() { Id = customId, IsZombie = false };
        public static RecipeEntity Nothing() => new() { Id = -1, IsZombie = false };

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

    public static List<CustomRecipe> GetTitanRecipes()
    {
        List<CustomRecipe> recipies = new();

        foreach (var pair in MixBomb.Recipe)
        {
            recipies.Add(
                new CustomRecipe
                {
                    Result = RecipeEntity.Plant(pair.value[2]),
                    ParentA = RecipeEntity.Plant(pair.value[0]),
                    ParentB = RecipeEntity.Plant(pair.Key),
                    ParentC = RecipeEntity.Plant(pair.value[1]),
                }
                );
        }

        return recipies;

    }

    public static List<CustomRecipe> SpawnedPlants = new()
    {
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.BigSunNut),
            ParentA = RecipeEntity.Plant(PlantType.UltimateSunNut),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.UltimateFly),
            ParentA = RecipeEntity.Plant(PlantType.UltimatePumpkin),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.SuperPumpkin),
            ParentA = RecipeEntity.Plant(PlantType.CactusBlover),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.FireNut),
            ParentA = RecipeEntity.Plant(PlantType.TorchFireNut),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.Firekelp),
            ParentA = RecipeEntity.Plant(PlantType.KelpTorch),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.FireMine),
            ParentA = RecipeEntity.Plant(PlantType.TorchMine),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.FireCaltrop),
            ParentA = RecipeEntity.Plant(PlantType.TorchSpike),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.TorchFirePumpkin),
            ParentA = RecipeEntity.Plant(PlantType.TorchPumpkin),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.FireSeaShroom),
            ParentA = RecipeEntity.Plant(PlantType.TorchSeaShroom),
        },
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.FireSquash),
            ParentA = RecipeEntity.Plant(PlantType.SquashTorch),
        },
    };

    public static List<CustomRecipe> GetFurnanceRecipes()
    {
        List<CustomRecipe> recipies = new();

        foreach (var pair in PineFurnace.mixDic)
        {
            recipies.Add(
                new CustomRecipe
                {
                    Result = RecipeEntity.Plant(pair.Value),
                    ParentA = RecipeEntity.Plant(PlantType.PineFurnace),
                    ParentB = RecipeEntity.Plant(pair.Key),
                }
                );
        }

        return recipies;

    }

    public static List<CustomRecipe> MiscellaneousPlants = new()
    {
        new CustomRecipe
        {
            Result = RecipeEntity.Plant(PlantType.MagicSnowPea),
            ParentA = RecipeEntity.Plant(PlantType.SnowPeaShooter),
            ParentB = RecipeEntity.Plant(PlantType.IceBean)
        },
    };



    public static List<CustomRecipe> ZombieItemPlants = new()
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
        foreach (CustomRecipe plantRecipe in GetTitanRecipes())
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

        foreach (CustomRecipe plantRecipe in GetFurnanceRecipes())
        {
            AddToList(plantRecipe);
        }
    }
}