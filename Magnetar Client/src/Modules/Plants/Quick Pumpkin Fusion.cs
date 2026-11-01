using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;

#endif

namespace Magnetar_Client.Modules
{
    public class QuickPumpkinFusion : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Quick Pumpkin Fusion";
        public override string Description { get; set; } = "Allows you to directly fuse the plant seed onto the pumpkin plant.";
        public override string SearchHints { get; set; } = "quickpumpkinfusion pumpkinfusion quickpumpkin instantfusion fastfusion" +
            " pumpkinembed pumpkinstack pumpkinupgrade fusionpumpkin quickfuse pumpkinplant seedfusion instantpumpkin fastfuse" +
            " pumpkinmerge seedmerge fusionshortcut pumpkincombiner";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static QuickPumpkinFusion instance;

        public QuickPumpkinFusion()
        {
            instance = this;
        }

        // Mod Logic

        public static PlantType GetFusion(PlantType containerType, PlantType seedType)
        {
            MixData.TryGetMix(containerType, seedType, out PlantType result);
            return result;
        }


        // Must also patch CheckBox so that when a pumpkin is on a fusable tile,
        // the game treats it as occupied (returns false) and routes to CheckMix
        // instead of calling SetPlant directly.
        [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckBox))]
        public static class CreatePlant_CheckBox_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.Normal)]
            public static bool Prefix(ref int theBoxColumn, ref int theBoxRow, ref PlantType theSeedType, ref bool __result)
            {
                if (instance == null || !instance.Active) return true;

                var gridPlants = Lawnf.Get1x1Plants(theBoxColumn, theBoxRow);
                if (gridPlants != null)
                {
                    foreach (Plant plant in gridPlants)
                    {
                        if (TypeMgr.IsPumpkin(plant.thePlantType))
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
            // Priority.Normal (400) — runs AFTER QuickPotFusion (Priority.High = 600)
            // so pot is always preferred when both modules are active.
            [HarmonyPrefix]
            [HarmonyPriority(Priority.Normal)]
            public static bool Prefix(CreatePlant __instance, ref int theColumn, ref int theRow, ref PlantType theUsedType, ref Plant __result)
            {
                if (instance == null || !instance.Active) return true;

                // If QuickPotFusion already handled this placement, don't double-fuse
                if (QuickPotFusion.instance != null && QuickPotFusion.instance.Active && QuickPotFusion.FiredThisPlacement)
                    return true;

                var gridPlants = Lawnf.Get1x1Plants(theColumn, theRow);
                if (gridPlants != null)
                {
                    foreach (Plant plant in gridPlants)
                    {
                        if (TypeMgr.IsPumpkin(plant.thePlantType))
                        {
                            PlantType fusionType = GetFusion(plant.thePlantType, theUsedType);
                            if (fusionType != PlantType.Peashooter)
                            {
                                if (!__instance.Lim(fusionType) && !__instance.LimTravel(fusionType))
                                {
                                    plant.Die(Plant.DieReason.ByMix);
                                    __result = __instance.SetPlant(theColumn, theRow, fusionType, withEffect: true);
                                    __instance.MixEvent(theUsedType, __result.GetComponent<Plant>(), theRow);
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
