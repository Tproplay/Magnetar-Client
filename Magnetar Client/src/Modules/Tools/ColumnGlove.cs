using HarmonyLib;
using System.Collections.Generic;
using static Magnetar_Client.Game.GameData;
using UnityEngine;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

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
        public static bool IsMovedByGlove;

        [HarmonyPrefix]
        public static void Prefix()
        {
            IsMovedByGlove = true;
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            IsMovedByGlove = false;
        }
    }

    [HarmonyPatch(typeof(CreatePlant))]
    public static class CreatePlantPatch
    {
        private static bool _isByMod;

        [HarmonyPatch(nameof(CreatePlant.SetPlant))]
        [HarmonyPrefix]
        public static bool SetPlantPrefix(
            CreatePlant __instance,
            ref Plant __result,
            int newColumn,
            int newRow,
            PlantType theSeedType,
            Plant targetPlant,
            Vector2 puffV,
            bool isFreeSet,
            bool withEffect,
            Plant hidplant)
        {
            if (_isByMod || !MouseGlovePatch.IsMovedByGlove) return true;
            if (instance == null || !instance.Active) return true;

            _isByMod = true;
            try
            {
                int sourceColumn = targetPlant != null ? targetPlant.thePlantColumn : -1;

                Plant newPlant = __instance.SetPlant(
                    newColumn,
                    newRow,
                    theSeedType,
                    targetPlant,
                    puffV,
                    isFreeSet,
                    withEffect,
                    hidplant
                );

                __result = newPlant;

                if (newPlant != null && sourceColumn != -1 && sourceColumn != newColumn)
                {
                    List<Plant> plantsToMove = new();
                    for (int i = 0; i < PlantList.Count; i++)
                    {
                        Plant plant = PlantList[i];
                        if (plant != null &&
                            plant != newPlant &&
                            plant != targetPlant &&
                            plant.thePlantColumn == sourceColumn)
                        {
                            plantsToMove.Add(plant);
                        }
                    }

                    foreach (Plant plant in plantsToMove)
                    {
                        if (plant == null) continue;

                        Plant shifted = __instance.SetPlant(
                            newColumn,
                            plant.thePlantRow,
                            plant.thePlantType,
                            plant
                        );

                    }
                }

                return false;
            }
            finally
            {
                _isByMod = false;
            }
        }
    }

}