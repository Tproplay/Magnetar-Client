using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static Magnetar_Client.Game.AppData;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
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


        public GodMode()
        {
            instance = this;

            CreateCategory("General");

            PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                MaxSelection = -1,
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,3000
                },
                CustomNames = TranslatedNames(typeof(PlantType))
            };

            PlantsSelectedSetting.Options.Keys.ToList().ForEach(PlantsSelectedSetting.Select);

            ImmuneToDamage = new BoolSetting("Immune To Damage", true);
            ImmuneToVehicle = new BoolSetting("Immune To Vehicle", true);
            ImmuneToShovel = new BoolSetting("Immune To Shovel", true);

            Settings.Add(PlantsSelectedSetting);
            Settings.Add(ImmuneToDamage);
            Settings.Add(ImmuneToVehicle);
            Settings.Add(ImmuneToShovel);

            EndCategory();

        }

        public override void OnLanguageChanged()
        {
            PlantsSelectedSetting.CustomNames = TranslatedNames(typeof(PlantType));
        }


        // Mod Logic


        [HarmonyPatch(typeof(Plant))]
        public static class PlantGodModePatch
        {
            [HarmonyPatch(nameof(Plant.RealTakeDamage))]
            [HarmonyPrefix]
            public static bool RealTakeDamagePrefix(Plant __instance, int damage)
            {
                if (instance == null || BoardInstanceIsNull) return true;
                if (!instance.Active || !instance.ImmuneToDamage.Value) return true;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                    return false;

                return true;
            }

            [HarmonyPatch(nameof(Plant.TakeDamage))]
            [HarmonyPrefix]
            public static bool TakeDamagePrefix(Plant __instance, int damage, int damageType)
            {
                if (instance == null || BoardInstanceIsNull) return true;
                if (!instance.Active || !instance.ImmuneToDamage.Value) return true;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                    return false;

                return true;
            }
            
            [HarmonyPatch(nameof(Plant.Crashed))]
            [HarmonyPrefix]
            public static bool CrashedPatch(Plant __instance, int level, int soundID, Zombie zombie)
            {
                if (instance == null || BoardInstanceIsNull) return true;
                if (!instance.Active || !instance.ImmuneToVehicle.Value) return true;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                    return false;

                return true;
            }

            [HarmonyPatch(nameof(Plant.Die))]
            [HarmonyPrefix]
            public static bool DiePatch(Plant __instance, Plant.DieReason reason)
            {
                if (instance == null || BoardInstanceIsNull || !instance.Active) return true;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                {
                    if (reason == Plant.DieReason.ByShovel)
                    {
                        if (!instance.ImmuneToShovel.Value) return true;
                        Shovel shovel = Shovel.Instance;

                        if (shovel == null) return false;
                        if (!shovel.isActiveAndEnabled || shovel.isPickUp) return false;

                        // Only works if the shovel is used by Player
                        if (shovel.mouse.theMouseColumn == __instance.thePlantColumn &&
                            shovel.mouse.theMouseRow == __instance.thePlantRow) return true;
                        if (TypeMgr.FlyingPlants(__instance.thePlantType))
                        {
                            if (shovel.mouse.theMouseColumn == __instance.thePlantColumn &&
                            shovel.mouse.theMouseRow == (__instance.thePlantRow - 1)) return true;
                        }
                        
                        return false;
                    }
                    return true;
                }

                return true;
            }
        }
        
    }
}
