using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;

#endif

namespace Magnetar_Client.Modules
{
    public class QuickPotFusion : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Quick Pot Fusion";
        public override string Description { get; set; } = "Allows you to directly fuse the plant seed onto the pot plant.";
        public override string SearchHints { get; set; } = "quickpotfusion potfusion quickpot instantfusion fastfusion" +
            " potembed potstack potupgrade fusionpot quickfuse potplant seedfusion instantpot fastfuse" +
            " potmerge seedmerge fusionshortcut potcombiner flowerpot flowerpotfusion";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static QuickPotFusion instance;
        public static bool FiredThisPlacement;

        public QuickPotFusion()
        {
            instance = this;
        }

        // Mod Logic

        public static PlantType GetFusion(PlantType containerType, PlantType seedType)
        {
            MixData.TryGetMix(containerType, seedType, out PlantType result);
            return result;
        }

        // Must also patch CheckBox so that when a pot is on a fusable tile,
        // the game treats it as occupied (returns false) and routes to CheckMix
        // instead of calling SetPlant directly.
        [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckBox))]
        public static class CreatePlant_CheckBox_Patch
        {
            // Priority.High (600) — runs before QuickPumpkinFusion's CheckBox patch
            // so pot is always preferred when both modules are active on the same tile.
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            public static bool Prefix(ref int theBoxColumn, ref int theBoxRow, ref PlantType theSeedType, ref bool __result)
            {
                if (instance == null || !instance.Active) return true;

                FiredThisPlacement = false;

                var gridPlants = Lawnf.Get1x1Plants(theBoxColumn, theBoxRow);
                if (gridPlants != null)
                {
                    foreach (Plant plant in gridPlants)
                    {
                        if (TypeMgr.IsPot(plant.thePlantType))
                        {
                            PlantType fusionType = GetFusion(plant.thePlantType, theSeedType);
                            if (fusionType != PlantType.Peashooter)
                            {
                                __result = false;
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckMix))]
        public static class CreatePlant_CheckMix_Patch
        {
            // Priority.High (600) — runs BEFORE QuickPumpkinFusion (Priority.Normal = 400)
            // so pot is always preferred when both modules are active on the same tile.
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            public static bool Prefix(CreatePlant __instance, ref int theColumn, ref int theRow, ref PlantType theUsedType, ref Plant __result)
            {
                if (instance == null || !instance.Active) return true;

                var gridPlants = Lawnf.Get1x1Plants(theColumn, theRow);
                if (gridPlants != null)
                {
                    foreach (Plant plant in gridPlants)
                    {
                        if (TypeMgr.IsPot(plant.thePlantType))
                        {
                            PlantType fusionType = GetFusion(plant.thePlantType, theUsedType);
                            if (fusionType != PlantType.Peashooter)
                            {
                                if (!__instance.Lim(fusionType) && !__instance.LimTravel(fusionType))
                                {
                                    plant.Die(Plant.DieReason.ByMix);
                                    __result = __instance.SetPlant(theColumn, theRow, fusionType, withEffect: true);
                                    __instance.MixEvent(theUsedType, __result.GetComponent<Plant>(), theRow);
                                    FiredThisPlacement = true;
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }
        }
    }
}
