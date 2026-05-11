using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Magnetar_Client.Utils;

namespace Magnetar_Client.Modules
{
    public class ChangeBullet : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Change Bullets";
        public override string Description { get; set; } = "Changes the Bullet Plant(s) shoot.";
        public override string SearchHints { get; set; } = "changebullets bulletchanger projectilechanger " +
            "bulletswap projectileswap changebullet switchbullets bulletskins bullettype projectiletype bulletmodifier " +
            "changeammo ammoswitch bulleteditor bulletcustomizer projectilecustomizer changebulletts changebullits " +
            "changebullits changebulits projectilemod ammomod bulletvariant projectileskin bulletoverride ammovariant " +
            "shootchange bulletprojectile bullettransformation bulletreplace";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static ChangeBullet instance;

        public List<int> preselected = Enum.GetValues(typeof(BulletType)).Cast<int>().ToList();
        public MultiSelectSetting selectBulletsSetting;


        public ChangeBullet()
        {
            instance = this;

            var overridenNames = Translator.TranslateEnum(typeof(BulletType));

            foreach (var name in overridenNames)
            {
                overridenNames[name.Key] = $"{name.Value} ({name.Key})";
            }

            selectBulletsSetting = new MultiSelectSetting("Allowed bullets", typeof(BulletType))
            {
                CustomNames = overridenNames,
                Blacklist = new HashSet<int>
                {
                    162,220
                }
            };
            Settings.Add(selectBulletsSetting);
            selectBulletsSetting.SelectedValues.UnionWith(preselected);

        }


        // Mod Logic

        [HarmonyPatch(typeof(CreateBullet))]
        public static class CreateBulletPatch
        {

            [HarmonyPatch(nameof(CreateBullet.SetBullet))]
            [HarmonyPrefix]
            public static void SetBulletPrefix(ref BulletType theBulletType, ref bool fromEnermy)
            {
                if (instance == null || Board.Instance == null) return;
                if (!instance.Active || fromEnermy) return;

                BulletType newType = theBulletType;

                if (instance.selectBulletsSetting.SelectedValues.Count == 1)
                    newType = (BulletType)instance.selectBulletsSetting.SelectedValues.First();

                if (instance.selectBulletsSetting.SelectedValues.Count > 1)
                {
                    newType = (BulletType)instance.selectBulletsSetting.SelectedValues.ElementAt(
                        UnityEngine.Random.RandomRangeInt(0, instance.selectBulletsSetting.SelectedValues.Count));
                }
                if (instance.selectBulletsSetting.SelectedValues.Contains((int)newType))
                    theBulletType = newType;
            }
        }

    }
}
