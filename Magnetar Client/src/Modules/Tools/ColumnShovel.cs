using HarmonyLib;
using Il2Cpp;
using static Il2Cpp.Plant;
using static Magnetar_Client.Game.GameData;

namespace Magnetar_Client.Modules
{
    public class ColumnShovel : Module
    {
        public override string Name { get; set; } = "Column Shovel";
        public override string Description { get; set; } = "Shoveling a plant, also shovel all identical plants in that column simultaneously.";
        public override string SearchHints { get; set; } = "columnshovel shovelcolumn columnremove massshovel shovelall " +
            "removecolumn identicalshovel multi-shovel shovelidentical columnclear clearsameplants removeallshovel" +
            " columncleanup shovelplants shovelmod columnshovelmod clearcolumn multi-remove shovelrow rowshovel clearsamecolumn " +
            "plantshovel column-shovel shovelidenticalplants massremove shovelgroup shovelmulti shovelsync colshovel";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        public static ColumnShovel instance;

        public ColumnShovel()
        {
            instance = this;
        }


        private static bool DieByMod = false;

        [HarmonyPatch(typeof(Plant))]
        public static class PlantPatch
        {
            [HarmonyPatch(nameof(Plant.Die))]
            [HarmonyPrefix]
            public static void Prefix(Plant __instance, DieReason reason)
            {
                if (DieByMod) return;
                if (instance == null || !instance.Active) return;

                if (reason != DieReason.ByShovel) return;

                ShovelMgr shovel = ShovelMgr.Instance;

                if (shovel == null) return;
                if (!shovel.isActiveAndEnabled || shovel.isPickUp) return;

                // Only works if the shovel is used by Player
                if (shovel.m.theMouseColumn != __instance.thePlantColumn ||
                    shovel.m.theMouseRow != __instance.thePlantRow) return;

                // BugFix: Disable Plant GodMode for smooth execution
                bool _disabledGodMode = false;
                if (GodMode.instance!=null && GodMode.instance.Active)
                {
                    GodMode.instance.Active = false;
                    _disabledGodMode = true;
                }

                // Kill All the Plants

                DieByMod = true;

                foreach (Plant plant in plantList)
                {
                    if (plant == __instance) continue;
                    if (plant.thePlantType == __instance.thePlantType &&
                        plant.thePlantColumn == __instance.thePlantColumn)
                    {
                        plant.Die(DieReason.ByShovel);
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