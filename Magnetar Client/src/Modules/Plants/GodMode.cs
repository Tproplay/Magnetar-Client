using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Utils;
using MelonLoader;
using System.Collections.Generic;
using static Il2Cpp.Plant;

namespace Magnetar_Client.Modules
{
    public class GodMode : Module
    {
        // Mod Info
        public override string Name { get; set; } = "God Mode";
        public override string Description { get; set; } = "Makes Plants Invincible.";
        public override string SearchHints { get; set; } = "godmode godmod invincibilty invinsible invincabel unkillable " +
            "nodamage immortal immortel invincibillity goddmode godmdoe invuln invulnerable invunrable invun invinc plantgod " +
            "plantinvincibility invensible invonceable invinci godmodecheat no-die infinitelife god-mode invinciblity godmodde";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static GodMode instance;

        public MultiSelectSetting PlantsSelectedSetting;

        public BoolSetting ImmuneToDamage;
        public BoolSetting ImmuneToVehicle;
        public BoolSetting ImmuneToShovel;



        private Dictionary<int, string> plantNameOverriden = new Dictionary<int, string>();

        public GodMode()
        {
            instance = this;

            plantNameOverriden = Translator.TranslateEnum(typeof(PlantType));

            foreach (var plant in plantNameOverriden)
            {
                plantNameOverriden[plant.Key] = $"{plant.Value} ({plant.Key})";
            }

            PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                MaxSelection = -1,
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = plantNameOverriden
            };


            ImmuneToDamage = new BoolSetting("Immune To Damage", true);
            ImmuneToVehicle = new BoolSetting("Immune To Vehicle", true);
            ImmuneToShovel = new BoolSetting("Immune To Shovel", true);

            Settings.Add(PlantsSelectedSetting);
            PlantsSelectedSetting.SelectedValues.UnionWith(plantNameOverriden.Keys);

            Settings.Add(ImmuneToDamage);
            Settings.Add(ImmuneToVehicle);
            Settings.Add(ImmuneToShovel);

        }

        // Mod Logic


        [HarmonyPatch(typeof(Plant))]
        public static class PlantGodModePatch
        {
            [HarmonyPatch(nameof(Plant.RealTakeDamage))]
            [HarmonyPrefix]
            public static bool RealTakeDamagePrefix(Plant __instance, int damage)
            {
                if (instance == null || Board.Instance == null) return true;
                if (!instance.Active || !instance.ImmuneToDamage.Value) return true;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                    return false;

                return true;
            }

            [HarmonyPatch(nameof(Plant.TakeDamage))]
            [HarmonyPrefix]
            public static bool TakeDamagePrefix(Plant __instance, int damage, int damageType)
            {
                if (instance == null || Board.Instance == null) return true;
                if (!instance.Active || !instance.ImmuneToDamage.Value) return true;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                    return false;

                return true;
            }
            
            [HarmonyPatch(nameof(Plant.Crashed))]
            [HarmonyPrefix]
            public static bool CrashedPatch(Plant __instance, int level, int soundID, Zombie zombie)
            {
                if (instance == null || Board.Instance == null) return true;
                if (!instance.Active || !instance.ImmuneToVehicle.Value) return true;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                    return false;

                return true;
            }

            [HarmonyPatch(nameof(Plant.Die))]
            [HarmonyPrefix]
            public static bool DiePatch(Plant __instance, DieReason reason)
            {
                if (instance == null || Board.Instance == null || !instance.Active) return true;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                {
                    if (reason == DieReason.ByShovel)
                    {
                        if (!instance.ImmuneToShovel.Value) return true;
                        ShovelMgr shovel = ShovelMgr.Instance;

                        if (shovel == null) return false;
                        if (!shovel.isActiveAndEnabled || shovel.isPickUp) return false;

                        // Only works if the shovel is used by Player
                        if (shovel.m.theMouseColumn == __instance.thePlantColumn &&
                            shovel.m.theMouseRow == __instance.thePlantRow) return true;

                        return false;
                    }
                    return true;
                }

                return true;
            }
        }
    }
}
