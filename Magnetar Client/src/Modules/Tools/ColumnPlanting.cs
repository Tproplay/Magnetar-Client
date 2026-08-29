using System.Collections.Generic;
using HarmonyLib;
using static Magnetar_Client.Game.AppData;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class MultiPlanting : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Column Planting";
        public override string Description { get; set; } = "Columns' Like you See 'Em.";
        public override string SearchHints { get; set; } = "multiplanting columnplanting multiplant " +
            "columnplant multiplants columnplants multiplantingmod columnplantingmod multiplantingplugin " +
            "multiplantingtool multiplantinghack multiplantingcheat multiplaning multiplantin multiplantting " +
            "multiplantting columplanting columnplaning columnplantting columnplanter multiplanter rowplanting " +
            "gridplanting massplanting batchplanting fastplanting autoplanter plantmultiplier plantcolumn plantrows " +
            "areaofeffectplanting";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static MultiPlanting instance;

        public SelectSetting Mode;

        public MultiPlanting()
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
            AddSettings( Mode );
            EndCategory();
        }


        // Mod Logic

        [HarmonyPatch(typeof(CreatePlant))]
        public static class CreatePlantPatch
        {
            static bool spawnByMod = false;

            [HarmonyPatch(nameof(CreatePlant.SetPlant))]
            [HarmonyPostfix]
            public static void SetPlantPrefix(CreatePlant __instance, Plant __result, int newColumn, int newRow, PlantType theSeedType)
            {
                if (spawnByMod) return;
                if (__result == null || instance == null || !instance.Active) return;
                if (theSeedType != Mouse.Instance.thePlantTypeOnMouse) return;

                spawnByMod = true;

                switch (instance.Mode.Value)
                {
                    case 0: // Column
                        for (int i = 0; i < board.rowNum; i++)
                        {
                            if (i == newRow) continue;
                            __instance.SetPlant(newColumn, i, theSeedType);
                        }
                        break;
                    case 1: // Row
                        for (int i = 0; i < board.columnNum; i++)
                        {
                            if (i == newColumn) continue;
                            __instance.SetPlant(i, newRow, theSeedType);
                        }
                        break;
                    case 2: // Rook
                        for (int i = 0; i < board.rowNum; i++)
                        {
                            if (i == newRow) continue;
                            __instance.SetPlant(newColumn, i, theSeedType);
                        }
                        for (int i = 0; i < board.columnNum; i++)
                        {
                            if (i == newColumn) continue;
                            __instance.SetPlant(i, newRow, theSeedType);
                        }
                        break;
                    case 3: // 3x3
                        for (int i = 0; i<9; i++)
                        {
                            int col = newColumn - 1 + i % 3;
                            int row = newRow - 1 + Mathf.FloorToInt(i / 3);
                            if (col == newColumn && row == newRow) continue;
                            __instance.SetPlant(col, row, theSeedType);
                        }
                        break;
                    case 4: // Full Lawn

                        int r_num = board.rowNum;
                        int c_num = board.columnNum;

                        int n = r_num * c_num;

                        for (int i = 0; i<n; i++)
                        {
                            int col = i % c_num;
                            int row = Mathf.FloorToInt(i / c_num);
                            if (col == newColumn && row == newRow) continue;
                            __instance.SetPlant(col, row, theSeedType);
                        }

                        break;
                }


                spawnByMod = false;


            }
        }


    }
}
