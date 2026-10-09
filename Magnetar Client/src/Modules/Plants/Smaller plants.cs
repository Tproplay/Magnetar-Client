using System.Collections.Generic;
using Magnetar_Client.UI.Setting;
using static Magnetar_Client.Game.AppData;
using Magnetar_Client.Game;
using UnityEngine;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class SmallerPlants : Module
{
    // Mod Info
    public override string Name { get; set; } = "Smaller Plants";
    public override string Description { get; set; } = "Changes the size of selected plants.";
    public override string SearchHints { get; set; } = "smallerplants miniplants tinyplants plantscale " +
        "plantresize plantscaler minatureplants scaleplants compactplants microplants plantsize plantshrinker " +
        "shrinkplants resizeplants miniplantmod plantzoom scaledplants tinyplantmod microplantmod sizechanger";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

    // Mod Data

    public static SmallerPlants instance;

    public FloatSetting plantSize;
    public MultiSelectSetting PlantsSelectedSetting;

    public LabelSetting AdvancedLabel;
    public SectionSetting IndividualPlantsSectionSetting;

    public SmallerPlants()
    {
        instance = this;

        CreateCategory("General");

        plantSize = new FloatSetting("Scale multiplier", 0.5f, 2f, 0.75f, 3, 0f);

        PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
        {
            Blacklist = Banned.PlantTypeBanned,
            CustomNames = TranslateEnum(typeof(PlantType))
        };
        PlantsSelectedSetting.SelectAll(setDefault: true);

        AddSettings(plantSize, PlantsSelectedSetting);

        EndCategory();

        CreateCategory("Advanced");

        AdvancedLabel = new("Modifications done here are preferred over Global modifications");

        IndividualPlantsSectionSetting = new SectionSetting("Individual Plant",
            (index) => new List<Setting>
            {
                new MultiSelectSetting("Entities", typeof(PlantType))
                {
                    Blacklist = Banned.PlantTypeBanned,
                    CustomNames = TranslateEnum(typeof(PlantType))
                },
                new FloatSetting("Scale multiplier", 0.5f, 2f, 1f, 3, 0f),
            }, 0);

        AddSettings(AdvancedLabel, IndividualPlantsSectionSetting);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        PlantsSelectedSetting.CustomNames = TranslateEnum(typeof(PlantType));
        foreach (var section in IndividualPlantsSectionSetting.Sections)
        {
            if (section.Find<MultiSelectSetting>("Entities", out var setting))
            {
                setting.CustomNames = TranslateEnum(typeof(PlantType));
            }
        }
    }

    // Mod Logic

    private bool TryGetPlantMultipliers(int plantId, out float scaleMult)
    {
        var sections = IndividualPlantsSectionSetting.Sections;
        for (int i = sections.Count - 1; i >= 0; i--)
        {
            var sec = sections[i];
            if (sec.Find<MultiSelectSetting>("Entities", out var secMulti) && secMulti.IsSelected(plantId))
            {
                var scale = sec.Find<FloatSetting>("Scale multiplier");

                scaleMult = scale != null ? scale.Value : 1f;
                return true;
            }
        }

        if (PlantsSelectedSetting.IsSelected(plantId))
        {
            scaleMult = plantSize.Value;
            return true;
        }

        scaleMult = 1f;
        return false;
    }

    Dictionary<Plant, Vector3> originalthePlantScale = new();
    public override void OnUpdateActive()
    {
        if (BoardInstanceIsNull) return;

        foreach (var plant in GameData.PlantList)
        {
            int plantId = (int)plant.thePlantType;
            bool isTargeted = TryGetPlantMultipliers(plantId, out float scaleMult);

            if (isTargeted)
            {
                if (!originalthePlantScale.ContainsKey(plant))
                {
                    originalthePlantScale[plant] = plant.transform.localScale;
                }

                Vector3 targetScale = originalthePlantScale[plant] * scaleMult;
                if (plant.transform.localScale != targetScale)
                {
                    plant.transform.localScale = targetScale;
                }
            }
            else if (originalthePlantScale.TryGetValue(plant, out Vector3 origScale))
            {
                plant.transform.localScale = origScale;
                originalthePlantScale.Remove(plant);
            }

        }

    }

    public override void OnDisable()
    {
        foreach (var plant in GameData.PlantList)
        {
            if (originalthePlantScale.TryGetValue(plant, out var origScale))
            {
                plant.transform.localScale = origScale;
            }
        }

        originalthePlantScale.Clear();
    }

}
