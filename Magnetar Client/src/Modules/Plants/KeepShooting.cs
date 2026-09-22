using HarmonyLib;
using System.Linq;
using static Magnetar_Client.Game.AppData;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class KeepShooting : Module
{
    // Mod Info
    public override string Name { get; set; } = "Keep Shooting";
    public override string Description { get; set; } = "Makes Plants fire continuously.";
    public override string SearchHints { get; set; } = "keepshooting continuousfire autofire rapidfire" +
        " nonstopshooting endlessfire alwaysshooting holdfire automaticshooting shootingloop firingloop " +
        "fireloop keepfire shootingbot autoshot keepshotting keepshoting continousfire continuousshoot " +
        "nonstopfire rapidshooting firespam shootspam infinitieshooting infiniteshoot burstfire firingrate" +
        " firespeed shootalways perpetualfire constantfire";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

    // Mod Data

    public static KeepShooting instance;

    public MultiSelectSetting PlantsSelectedSetting;

    public KeepShooting()
    {
        instance = this;

        CreateCategory("General");

        PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
        {
            Blacklist = Banned.PlantTypeBanned,
            CustomNames = TranslatedNames(typeof(PlantType))
        };

        PlantsSelectedSetting.Options.Keys.ToList().ForEach(PlantsSelectedSetting.Select);

        Settings.Add(PlantsSelectedSetting);

        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        PlantsSelectedSetting.CustomNames = TranslatedNames(typeof(PlantType));
    }

    // Mod Logic


    [HarmonyPatch(typeof(Plant))]
    public static class PlantShootablePatch
    {
        [HarmonyPatch(nameof(Plant.Shootable))]
        [HarmonyPostfix]
        public static void ShootablePostfix(Plant __instance, ref bool __result)
        {
            if (instance == null || BoardInstanceIsNull) return;
            if (!instance.Active) return;

            if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                __result = true;
        }
    }
}
