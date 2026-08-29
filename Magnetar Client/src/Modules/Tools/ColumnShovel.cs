using HarmonyLib;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using static Magnetar_Client.Game.GameData;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class ColumnShovel : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Column Shovel";
        public override string Description { get; set; } = "Shoveling a plant, also shovel all identical plants in that column simultaneously.";
        public override string SearchHints { get; set; } = "columnshovel shovelcolumn columnremove massshovel shovelall " +
            "removecolumn identicalshovel multi-shovel shovelidentical columnclear clearsameplants removeallshovel" +
            " columncleanup shovelplants shovelmod columnshovelmod clearcolumn multi-remove shovelrow rowshovel clearsamecolumn " +
            "plantshovel column-shovel shovelidenticalplants massremove shovelgroup shovelmulti shovelsync colshovel";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data
        public static ColumnShovel instance;

        public SelectSetting Mode;
        public BoolSetting OnlySame;

        public ColumnShovel()
        {
            instance = this;

            CreateCategory("General");

            Mode = new SelectSetting("Mode", 0)
            {
                Options = new Dictionary<int, string>
                {
                    { 0 , "Column" },
                    { 1 , "Row" },
                    { 2 , "Rook" },
                    { 3 , "3x3" },
                    { 4 , "Full Lawn" },
                }
            };

            OnlySame = new BoolSetting("Only Similar Plants", true);

            AddSettings(Mode,OnlySame);

            EndCategory();
        }

        public override void OnLanguageChanged()
        {
            Mode.CustomNames = Mode.Options
                .ToDictionary(kvp => kvp.Key, kvp => Translator.Translate(kvp.Value));
        }

        // Mod Logic

        private static bool DieByMod = false;

        [HarmonyPatch(typeof(Plant))]
        public static class PlantPatch
        {
            [HarmonyPatch(nameof(Plant.Die))]
            [HarmonyPrefix]
            public static void Prefix(Plant __instance, Plant.DieReason reason)
            {
                if (DieByMod) return;
                if (instance == null || !instance.Active) return;

                if (reason != Plant.DieReason.ByShovel) return;

                Shovel shovel = Shovel.Instance;

                if (shovel == null) return;
                if (!shovel.isActiveAndEnabled || shovel.isPickUp) return;

                // Only works if the shovel is used by Player
                if (shovel.mouse.theMouseColumn != __instance.thePlantColumn) return;

                if (TypeMgr.FlyingPlants(__instance.thePlantType))
                {
                    if ((shovel.mouse.theMouseRow != __instance.thePlantRow - 1) &&
                        (shovel.mouse.theMouseRow != __instance.thePlantRow)) return;
                }
                else
                {
                    if (shovel.mouse.theMouseRow != __instance.thePlantRow) return;
                }

                // BugFix: Disable Plant GodMode for smooth execution
                bool _disabledGodMode = false;
                if (GodMode.instance!=null && GodMode.instance.Active)
                {
                    GodMode.instance.Active = false;
                    _disabledGodMode = true;
                }

                // Kill All the Plants

                DieByMod = true;

                
                for (int i = plantList.Count - 1; i >= 0; i--)
                {
                    Plant plant = plantList[i];
                    if (plant == __instance || (plant.thePlantType != __instance.thePlantType && 
                        instance.OnlySame.Value)) continue;

                    // Column
                    if (instance.Mode.Value == 0)
                    {
                        if (plant.thePlantColumn == __instance.thePlantColumn)
                        {
                            plant.Die(Plant.DieReason.ByShovel);
                        }
                    }

                    // Row
                    else if (instance.Mode.Value == 1)
                    {
                        if (plant.thePlantRow == __instance.thePlantRow)
                        {
                            plant.Die(Plant.DieReason.ByShovel);
                        }
                    }

                    // Rook
                    else if (instance.Mode.Value == 2)
                    {
                        if (plant.thePlantColumn == __instance.thePlantColumn
                            || plant.thePlantRow == __instance.thePlantRow)
                        {
                            plant.Die(Plant.DieReason.ByShovel);
                        }
                    }

                    // 3x3
                    else if (instance.Mode.Value == 3)
                    {
                        int colDiff = Math.Abs(plant.thePlantColumn - __instance.thePlantColumn);
                        int rowDiff = Math.Abs(plant.thePlantRow - __instance.thePlantRow);

                        if (colDiff <= 1 && rowDiff <= 1)
                        {
                            plant.Die(Plant.DieReason.ByShovel);
                        }
                    }

                    // Full Lawn
                    else if (instance.Mode.Value == 4)
                    {
                        plant.Die(Plant.DieReason.ByShovel);
                    }
                }

                

                DieByMod = false;

                // Re enable Plant GodMode
                if (_disabledGodMode)
                {
                    GodMode.instance.Active = true;
                }
                


            }
        }

        
        
    }
}