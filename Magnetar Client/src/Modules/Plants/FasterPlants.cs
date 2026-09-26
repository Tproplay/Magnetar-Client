using HarmonyLib;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using static Magnetar_Client.Game.AppData;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

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

    public LabelSetting AdvancedLabel;
    public SectionSetting IndividualPlantsSectionSetting;

    public FasterPlants()
    {
        instance = this;

        CreateCategory("General");

        PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
        {
            Blacklist = Banned.PlantTypeBanned,
            CustomNames = TranslatedNames(typeof(PlantType))
        };
        PlantsSelectedSetting.SelectAll(setDefault: true);

        AttackIntervalMultiplierSetting = new FloatSetting("Attack Interval", 0.01f, 50f, 50f, 3);
        AnimationSpeedMultiplierSetting = new FloatSetting("Animation Speed", 0.01f, 50f, 2f, 3);
        ProduceSpeedMultiplierSetting = new FloatSetting("Produce Speed", 0.01f, 50f, 50f, 3);

        AddSettings(PlantsSelectedSetting,AttackIntervalMultiplierSetting,
            AnimationSpeedMultiplierSetting,ProduceSpeedMultiplierSetting
        );
        EndCategory();

        CreateCategory("Advanced",false);

        AdvancedLabel = new("Modifications done here are preferred over Global modifications");

        IndividualPlantsSectionSetting = new SectionSetting("Individual Plant",
            (index) => new List<Setting>
            {
                new MultiSelectSetting("Entities", typeof(PlantType))
                {
                    Blacklist = Banned.PlantTypeBanned,
                    CustomNames = TranslatedNames(typeof(PlantType))
                },
                new FloatSetting("Attack Interval", 0.01f, 50f, 1f, 3),
                new FloatSetting("Animation Speed", 0.01f, 50f, 1f, 3),
                new FloatSetting("Produce Speed", 0.01f, 50f, 1f, 3),
            }, 0);

        AddSettings(AdvancedLabel, IndividualPlantsSectionSetting);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        PlantsSelectedSetting.CustomNames = TranslatedNames(typeof(PlantType));

        foreach (var section in IndividualPlantsSectionSetting.Sections)
        {
            if (section.Find<MultiSelectSetting>("Entities", out var setting))
            {
                setting.CustomNames = TranslatedNames(typeof(PlantType));
            }
        }
    }

    private readonly Dictionary<Plant, float> originalthePlantAttackInterval = new();
    private readonly Dictionary<Plant, float> originalAnimationSpeeds = new();
    private readonly Dictionary<Plant, float> originalthePlantProduceInterval = new();

    /// <summary>
    /// Priority: Highest-index Section > Lowest-index Section > Global General Settings.
    /// </summary>
    private bool TryGetPlantMultipliers(int plantId, out float attackMult, out float animMult, out float produceMult)
    {
        var sections = IndividualPlantsSectionSetting.Sections;
        for (int i = sections.Count - 1; i >= 0; i--)
        {
            var sec = sections[i];
            if (sec.Find("Entities") is MultiSelectSetting secMulti && secMulti.IsSelected(plantId))
            {
                var atk = sec.Find<FloatSetting>("Attack Interval");
                var anim = sec.Find<FloatSetting>("Animation Speed");
                var prod = sec.Find<FloatSetting>("Produce Speed");

                attackMult = atk != null ? atk.Value : 1f;
                animMult = anim != null ? anim.Value : 1f;
                produceMult = prod != null ? prod.Value : 1f;
                return true;
            }
        }
            
        if (PlantsSelectedSetting.IsSelected(plantId))
        {
            attackMult = AttackIntervalMultiplierSetting.Value;
            animMult = AnimationSpeedMultiplierSetting.Value;
            produceMult = ProduceSpeedMultiplierSetting.Value;
            return true;
        }

        attackMult = 1f;
        animMult = 1f;
        produceMult = 1f;
        return false;
    }

    // Mod Logic
    public override void OnUpdateActive()
    {
        if (BoardInstanceIsNull) return;

        foreach (var plant in GameData.PlantList)
        {
            if (plant == null) continue;

            int plantId = (int)plant.thePlantType;
            bool isTargeted = TryGetPlantMultipliers(plantId, out float attackMult, out float animMult, out float produceMult);

            #region Attack Interval Modification
            if (isTargeted)
            {
                if (!originalthePlantAttackInterval.ContainsKey(plant))
                {
                    originalthePlantAttackInterval[plant] = plant.thePlantAttackInterval;
                }

                float targetAttack = originalthePlantAttackInterval[plant] / attackMult;
                if (plant.thePlantAttackInterval != targetAttack)
                {
                    plant.thePlantAttackInterval = targetAttack;
                }
            }
            else if (originalthePlantAttackInterval.TryGetValue(plant, out float origAtk))
            {
                plant.thePlantAttackInterval = origAtk;
                originalthePlantAttackInterval.Remove(plant);
            }
            #endregion

            #region Animation Speed Modification
            if (isTargeted)
            {
                if (!originalAnimationSpeeds.ContainsKey(plant))
                {
                    originalAnimationSpeeds[plant] = plant.thePlantSpeed;
                }

                float targetSpeed = originalAnimationSpeeds[plant] * animMult;
                if (plant.thePlantSpeed != targetSpeed)
                {
                    plant.thePlantSpeed = targetSpeed;
                }
            }
            else if (originalAnimationSpeeds.TryGetValue(plant, out float origSpeed))
            {
                plant.thePlantSpeed = origSpeed;
                originalAnimationSpeeds.Remove(plant);
            }
            #endregion

            #region Production Cooldown Modification
            if (isTargeted)
            {
                if (!originalthePlantProduceInterval.ContainsKey(plant))
                {
                    originalthePlantProduceInterval[plant] = plant.thePlantProduceInterval;
                }

                float targetProduce = originalthePlantProduceInterval[plant] / produceMult;
                if (plant.thePlantProduceInterval != targetProduce)
                {
                    plant.thePlantProduceInterval = targetProduce;
                }
            }
            else if (originalthePlantProduceInterval.TryGetValue(plant, out float origProd))
            {
                plant.thePlantProduceInterval = origProd;
                originalthePlantProduceInterval.Remove(plant);
            }
            #endregion
        }
    }

    public override void OnDisable()
    {
        foreach (var plant in GameData.PlantList)
        {
            if (plant == null) continue;

            if (originalthePlantAttackInterval.TryGetValue(plant, out float origAtk))
            {
                plant.thePlantAttackInterval = origAtk;
            }

            if (originalAnimationSpeeds.TryGetValue(plant, out float origSpeed))
            {
                plant.thePlantSpeed = origSpeed;
            }

            if (originalthePlantProduceInterval.TryGetValue(plant, out float origProd))
            {
                plant.thePlantProduceInterval = origProd;
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