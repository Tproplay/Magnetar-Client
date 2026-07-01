using HarmonyLib;
using Magnetar_Client.Game;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Linq;
using static Magnetar_Client.Game.AppData;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class FasterPlants : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Faster Plants";
        public override string Description { get; set; } = "Makes the selected plant(s) faster while the module is active.";
        public override string SearchHints { get; set; } = "fasterplants speedplants plantattack speedup plantspeed " +
            "fastplant plantspeedup plantcooldown plantrate quickplants plantfast plantaccelerator plantboost " +
            "attackspeed fire-rate plantspeeder fasterplantsmod plantvelocity fasterplantsup fasterplantspeed " +
            "faster-plants plantrapid plantswift plantspeedy plantturbo plantoverdrive plantbuff plantagility " +
            "plantquickness plantalacrity fastattack";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static FasterPlants instance;

        public MultiSelectSetting PlantsSelectedSetting;

        public FloatSetting AttackIntervalMultiplierSetting;

        public FloatSetting AnimationSpeedMultiplierSetting;

        public FloatSetting ProduceSpeedMultiplierSetting;
        public override bool Active { get; set; } = false;

        public FasterPlants()
        {
            instance = this;

            CreateCategory("General");

            PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                MaxSelection = -1,
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,3000,
                },
                CustomNames = TranslatedNames(typeof(PlantType))
            };

            PlantsSelectedSetting.Options.Keys.ToList().ForEach(PlantsSelectedSetting.Select);

            Settings.Add(PlantsSelectedSetting);
            

            AttackIntervalMultiplierSetting = new FloatSetting("Attack Interval", 0.01f, 50, 50);
            Settings.Add(AttackIntervalMultiplierSetting);

            AnimationSpeedMultiplierSetting = new FloatSetting("Animation Speed", 0.01f, 50, 2);
            Settings.Add(AnimationSpeedMultiplierSetting);

            ProduceSpeedMultiplierSetting = new FloatSetting("Produce Speed", 0.01f, 50, 50);
            Settings.Add(ProduceSpeedMultiplierSetting);

            EndCategory();
        }


        public override void OnLanguageChanged()
        {
            PlantsSelectedSetting.CustomNames = TranslatedNames(typeof(PlantType));
        }
        
        Dictionary<Plant, float> originalthePlantAttackInterval = new Dictionary<Plant, float>();
        Dictionary<Plant, float> originalAnimationSpeeds = new Dictionary<Plant, float>();
        Dictionary<Plant, float> originalthePlantProduceInterval = new Dictionary<Plant, float>();



        // Mod Logic
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            foreach (var plant in GameData.plantList)
            {
                #region Attack Interval Modification

                // Check if the plant is selected and if we haven't already stored its original attack cooldown
                if (PlantsSelectedSetting.IsSelected((int)plant.thePlantType) &&
                    !originalthePlantAttackInterval.ContainsKey(plant))
                {
                    originalthePlantAttackInterval[plant] = plant.thePlantAttackInterval;
                }

                // Check if the plant is deselected while the module is running 
                if (!PlantsSelectedSetting.IsSelected((int)plant.thePlantType) &&
                    originalthePlantAttackInterval.ContainsKey(plant))
                {
                    plant.thePlantAttackInterval = originalthePlantAttackInterval[plant];
                    originalthePlantAttackInterval.Remove(plant);
                }

                // Update the Attack Interval
                if (originalthePlantAttackInterval.ContainsKey(plant))
                {
                    if (plant.thePlantAttackInterval != originalthePlantAttackInterval[plant] / AttackIntervalMultiplierSetting.Value)
                    {
                        plant.thePlantAttackInterval = originalthePlantAttackInterval[plant] / AttackIntervalMultiplierSetting.Value;
                    }
                }
                #endregion

                #region Animation Speed Modification

                // Check if the plant is selected and if we haven't already stored its original animation speed
                if (PlantsSelectedSetting.IsSelected((int)plant.thePlantType) &&
                    !originalAnimationSpeeds.ContainsKey(plant))
                {
                    originalAnimationSpeeds[plant] = plant.thePlantSpeed;
                }

                // Check if the plant is deselected while the module is running 
                if (!PlantsSelectedSetting.IsSelected((int)plant.thePlantType) &&
                    originalAnimationSpeeds.ContainsKey(plant))
                {
                    plant.thePlantSpeed = originalAnimationSpeeds[plant];
                    originalAnimationSpeeds.Remove(plant);
                }

                // Update the Animation Speed
                if (originalAnimationSpeeds.ContainsKey(plant))
                {
                    if (plant.thePlantSpeed != originalAnimationSpeeds[plant] * AnimationSpeedMultiplierSetting.Value)
                    {
                        plant.thePlantSpeed = originalAnimationSpeeds[plant] * AnimationSpeedMultiplierSetting.Value;
                    }
                }
                #endregion

                #region Production Cooldown Modification

                // Check if the plant is selected and if we haven't already stored its original production cooldown
                if (PlantsSelectedSetting.IsSelected((int)plant.thePlantType) &&
                    !originalthePlantProduceInterval.ContainsKey(plant))
                {
                    originalthePlantProduceInterval[plant] = plant.thePlantProduceInterval;
                }

                // Check if the plant is deselected while the module is running 
                if (!PlantsSelectedSetting.IsSelected((int)plant.thePlantType) &&
                    originalthePlantProduceInterval.ContainsKey(plant))
                {
                    plant.thePlantProduceInterval = originalthePlantProduceInterval[plant];
                    originalthePlantProduceInterval.Remove(plant);
                }

                // Update Production Speed
                if (originalAnimationSpeeds.ContainsKey(plant))
                {
                    if (plant.thePlantProduceInterval != originalthePlantProduceInterval[plant]/ProduceSpeedMultiplierSetting.Value)
                    {
                        plant.thePlantProduceInterval = originalthePlantProduceInterval[plant]/ProduceSpeedMultiplierSetting.Value;
                    }
                }
                #endregion
            }

        }

        public override void OnDisable()
        {
            // Reset the attack cooldowns of all modified plants to their original values
            foreach (var plant in GameData.plantList)
            {
                if (originalthePlantAttackInterval.ContainsKey(plant))
                {
                    plant.thePlantAttackInterval = originalthePlantAttackInterval[plant];
                }
                
                if (originalAnimationSpeeds.ContainsKey(plant))
                {
                    plant.thePlantSpeed = originalAnimationSpeeds[plant];
                }

                if (originalthePlantProduceInterval.ContainsKey(plant))
                {
                    plant.thePlantProduceInterval = originalthePlantProduceInterval[plant];
                }
            }

            originalthePlantAttackInterval.Clear();
            originalAnimationSpeeds.Clear();
            originalthePlantProduceInterval.Clear();
        }

        [HarmonyPatch(typeof(Plant))]
        public class PlantPatch
        {
            [HarmonyPatch(nameof(Plant.Update))]
            [HarmonyPrefix]
            public static bool UpdatePrefix(Plant __instance)
            {
                if (__instance.thePlantProduceCountDown > __instance.thePlantProduceInterval)
                {
                    __instance.thePlantProduceCountDown = __instance.thePlantProduceInterval;
                }

                if (__instance.thePlantAttackCountDown > __instance.thePlantAttackInterval)
                {
                    __instance.thePlantAttackCountDown = __instance.thePlantAttackInterval;
                }
                return true;
            }
        }
    }
}
