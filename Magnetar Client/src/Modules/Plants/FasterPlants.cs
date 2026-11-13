using HarmonyLib;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using Magnetar_Client.Modules.Helpers;



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

    public EntitySelectSetting PlantsSelectedSetting;
    public FloatSetting AttackIntervalMultiplierSetting;
    public FloatSetting AnimationSpeedMultiplierSetting;
    public FloatSetting ProduceSpeedMultiplierSetting;

    public LabelSetting AdvancedLabel;
    public SectionSetting IndividualPlantsSectionSetting;

    public FasterPlants()
    {
        instance = this;

        CreateCategory("General");

        PlantsSelectedSetting = new EntitySelectSetting("Entities", EntitySelectSetting.Plant)
        {
            Blacklist = Banned.PlantTypeBanned,
            CustomNames = TranslateEnum(typeof(PlantType))
        };
        PlantsSelectedSetting.SelectAll(setDefault: true);

        AttackIntervalMultiplierSetting = new FloatSetting("Attack Interval", 0.01f, 50f, 50f, 3);
        AnimationSpeedMultiplierSetting = new FloatSetting("Animation Speed", 0.01f, 50f, 2f, 3);
        ProduceSpeedMultiplierSetting = new FloatSetting("Produce Speed", 0.01f, 50f, 50f, 3);

        AddSettings(PlantsSelectedSetting, AttackIntervalMultiplierSetting,
            AnimationSpeedMultiplierSetting, ProduceSpeedMultiplierSetting
        );
        EndCategory();

        CreateCategory("Advanced", false);

        AdvancedLabel = new("Modifications done here are preferred over Global modifications");

        IndividualPlantsSectionSetting = new SectionSetting("Individual Plant",
            (index) => new List<Setting>
            {
                new EntitySelectSetting("Entities", EntitySelectSetting.Plant)
                {
                    Blacklist = Banned.PlantTypeBanned,
                    CustomNames = TranslateEnum(typeof(PlantType))
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
        PlantsSelectedSetting.CustomNames = TranslateEnum(typeof(PlantType));

        foreach (var section in IndividualPlantsSectionSetting.Sections)
        {
            if (section.Find<MultiSelectSetting>("Entities", out var setting))
            {
                setting.CustomNames = TranslateEnum(typeof(PlantType));
            }
        }
    }

    private bool TryGetPlantMultipliers(int plantId, out float attackMult, out float animMult, out float produceMult)
    {
        var sections = IndividualPlantsSectionSetting.Sections;
        for (int i = sections.Count - 1; i >= 0; i--)
        {
            var sec = sections[i];
            if (sec.Find<MultiSelectSetting>("Entities", out var secMulti) && secMulti.IsSelected(plantId))
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

    public override void OnEnable()
    {
        UpdateModifiers();
    }

    public override void OnUpdateActive()
    {
        UpdateModifiers();
    }

    public override void OnDisable()
    {
        PlantStatManager.RemoveModifier(Name, PlantStatType.AttackInterval);
        PlantStatManager.RemoveModifier(Name, PlantStatType.AnimationSpeed);
        PlantStatManager.RemoveModifier(Name, PlantStatType.ProduceInterval);
    }

    private void UpdateModifiers()
    {
        if (!Active) return;

        PlantStatManager.SetModifier(Name, PlantStatType.AttackInterval, ModifierOp.Multiply, p =>
        {
            if (TryGetPlantMultipliers((int)p.thePlantType, out float atk, out _, out _))
            {
                return atk > 0f ? 1f / atk : 1f;
            }
            return 1f;
        });

        PlantStatManager.SetModifier(Name, PlantStatType.AnimationSpeed, ModifierOp.Multiply, p =>
        {
            if (TryGetPlantMultipliers((int)p.thePlantType, out _, out float anim, out _))
            {
                return anim;
            }
            return 1f;
        });

        PlantStatManager.SetModifier(Name, PlantStatType.ProduceInterval, ModifierOp.Multiply, p =>
        {
            if (TryGetPlantMultipliers((int)p.thePlantType, out _, out _, out float prod))
            {
                return prod > 0f ? 1f / prod : 1f;
            }
            return 1f;
        });
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
