using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using static Magnetar_Client.Game.AppData;
using Magnetar_Client.Game;
using UnityEngine;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class SmallerPlants : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Smaller Plants";
        public override string Description { get; set; } = "Changes the size of selected plants.";
        public override string SearchHints { get; set; } = "";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static SmallerPlants instance;

        public FloatSetting plantSize;
        public MultiSelectSetting selectedPlants;


        public SmallerPlants()
        {
            instance = this;

            CreateCategory("General");

            plantSize = new FloatSetting("Scale multiplier", 0.5f, 2f, 0.75f, 3, 0f);

            selectedPlants = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                Blacklist = Banned.PlantTypeBanned,
                CustomNames = TranslatedNames(typeof(PlantType))
            };
            selectedPlants.Options.Keys.ToList().ForEach(selectedPlants.Select);

            AddSettings(plantSize, selectedPlants);

            EndCategory();


        }

        public override void OnLanguageChanged()
        {
            selectedPlants.CustomNames = TranslatedNames(typeof(PlantType));
        }

        // Mod Logic
        Dictionary<Plant, Vector3> originalthePlantScale = new Dictionary<Plant, Vector3>();
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            foreach (var plant in GameData.plantList)
            {

                // Check if the plant is selected and if we haven't already stored its original scale
                if (selectedPlants.IsSelected((int)plant.thePlantType) &&
                    !originalthePlantScale.ContainsKey(plant))
                {
                    originalthePlantScale[plant] = plant.transform.localScale;
                }

                // Check if the plant is deselected while the module is running 
                if (!selectedPlants.IsSelected((int)plant.thePlantType) &&
                    originalthePlantScale.ContainsKey(plant))
                {
                    plant.transform.localScale = originalthePlantScale[plant];
                    originalthePlantScale.Remove(plant);
                }

                // Update the Scale
                if (originalthePlantScale.ContainsKey(plant))
                {
                    if (plant.transform.localScale != originalthePlantScale[plant] * plantSize.Value)
                    {
                        plant.transform.localScale = originalthePlantScale[plant] * plantSize.Value;
                    }
                }

            }

        }

        public override void OnDisable()
        {
            foreach (var plant in GameData.plantList)
            {
                if (originalthePlantScale.ContainsKey(plant))
                {
                    plant.transform.localScale = originalthePlantScale[plant];
                }
            }

            originalthePlantScale.Clear();
        }

    }
}
