using HarmonyLib;
using Magnetar_Client.UI.Setting;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class CustomConveyor : Module
{
    // Mod Info
    public override string Name { get; set; } = "Custom Conveyor";
    public override string Description { get; set; } = "Forces specific plants into the conveyor belt.\nIf none are selected, injects every plant.";
    public override string SearchHints { get; set; } = "customconveyor conveyor belt customplants forceplants conveyorforce" +
        " plantconveyor selectconveyor editconveyor conveyorinjector injectplants conveyoroverride customconveyer" +
        " conveyerbelt plantinject injectevery plantspawn custombelt forceconveyer converyor conveyorcustom conveyerbelt" +
        " forceplant allplants conveyoritem spawnplant conveyorpool custompool conveyorlist";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    // Mod Data
    private bool _active;
    public override bool Active
    {
        get => _active;
        set
        {
            _active = value;
            if (ForceShowConveyer.Value)
            {
                EnableConveyer(value);
            }
        }
    }

    public static CustomConveyor instance;

    public MultiSelectSetting ConveyorPlantsSetting;
    public BoolSetting ForceShowConveyer;

    private bool wasSeedBankenable;
    private readonly Il2CppSystem.Collections.Generic.List<PlantType> plantList = new();

    public CustomConveyor()
    {
        instance = this;

        CreateCategory("General");

        ForceShowConveyer = new BoolSetting("Force enable conveyer", true)
        {
            OnValueChanged = ForceShowConveyerCall,
        };

        ConveyorPlantsSetting = new MultiSelectSetting("Allowed Plants", typeof(PlantType))
        {
            CustomNames = TranslatedNames(typeof(PlantType)),
            Blacklist = Banned.PlantTypeBanned,
            OnSelectionChanged = UpdatePlantList,
        };
        AddSettings(ForceShowConveyer, ConveyorPlantsSetting);

        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        ConveyorPlantsSetting.CustomNames = TranslatedNames(typeof(PlantType));
    }

    // Mod Logic
    private void UpdatePlantList(int id, bool selected)
    {
        var plant = (PlantType)id;
        if (selected)
        {
            if (!plantList.Contains(plant)) plantList.Add(plant);
        }
        else
        {
            plantList.Remove(plant);
        }
    }

    private void ForceShowConveyerCall(bool value)
    {
        if (!Active) return;
        EnableConveyer(value);
    }

    public void EnableConveyer(bool enable)
    {
        if (InGameUI.Instance == null) return;
        var ui = InGameUI.Instance.transform;
        if (ui == null) return;

        var conv = ui.Find("ConeryorBelt");
        var seedbank = ui.Find("SeedBank");

        if (conv == null || seedbank == null) return;

        if (enable)
        {
            if (seedbank.gameObject.activeSelf)
            {
                wasSeedBankenable = true;
            }
            conv.gameObject.SetActive(true);
            seedbank.gameObject.SetActive(false);
        }
        else
        {
            conv.gameObject.SetActive(false);
            if (wasSeedBankenable) seedbank.gameObject.SetActive(true);
            wasSeedBankenable = false;
        }
    }

    [HarmonyPatch(typeof(ConveyManager))]
    public static class ConveyBeltMgrPatch
    {
        [HarmonyPatch(nameof(ConveyManager.GetCardPool))]
        [HarmonyPostfix]
        public static void PostGetCardPool(ref Il2CppSystem.Collections.Generic.List<PlantType> __result)
        {
            if (instance == null || !instance.Active) return;

            if (instance.plantList.Count > 0)
            {
                __result = instance.plantList;
            }
            else if (GameAPP.resourcesManager?.allPlants != null)
            {
                __result = GameAPP.resourcesManager.allPlants;
            }
        }

        [HarmonyPatch(nameof(ConveyManager.Awake))]
        [HarmonyPostfix]
        public static void PostAwake(ConveyManager __instance)
        {
            if (instance == null || !instance.Active || __instance == null) return;

            if (instance.plantList.Count > 0)
            {
                __instance.plants = instance.plantList;
            }
            else if (GameAPP.resourcesManager?.allPlants != null)
            {
                __instance.plants = GameAPP.resourcesManager.allPlants;
            }
        }
    }

    [HarmonyPatch(typeof(Board))]
    public static class BoardPatch
    {
        [HarmonyPatch(nameof(Board.Start))]
        [HarmonyPostfix]
        public static void StartPostfix()
        {
            if (instance == null || !instance.Active) return;

            if (instance.ForceShowConveyer.Value)
            {
                instance.EnableConveyer(true);
            }
        }

        [HarmonyPatch(nameof(Board.Die))]
        [HarmonyPostfix]
        public static void DiePostfix()
        {
            if (instance == null) return;
            instance.wasSeedBankenable = false;
        }
    }
}
