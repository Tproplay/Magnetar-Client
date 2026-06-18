using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Magnetar_Client.Utils;
using static Magnetar_Client.Game.AppData;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

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

        public MultiSelectSetting selectBulletsSetting;


        public ChangeBullet()
        {
            instance = this;

            CreateCategory("General");

            selectBulletsSetting = new MultiSelectSetting("Allowed bullets", typeof(BulletType))
            {
                CustomNames = TranslatedNames(typeof(BulletType)),
                Blacklist = new HashSet<int>
                {
                    162,220
                }
            };

            selectBulletsSetting.Options.Keys.ToList().ForEach(selectBulletsSetting.Select);

            Settings.Add(selectBulletsSetting);

            EndCategory();


        }

        public override void OnLanguageChanged()
        {
            selectBulletsSetting.CustomNames = TranslatedNames(typeof(BulletType));
        }

        // Mod Logic

        [HarmonyPatch(typeof(CreateBullet))]
        public static class CreateBulletPatch
        {

            [HarmonyPatch(nameof(CreateBullet.SetBullet))]
            [HarmonyPrefix]
            public static void SetBulletPrefix(ref BulletType theBulletType, ref bool fromEnermy)
            {
                if (instance == null || BoardInstanceIsNull) return;
                if (!instance.Active || fromEnermy) return;

                BulletType newType = theBulletType;

                if (instance.selectBulletsSetting.SelectedValues.Count == 1)
                    newType = (BulletType)instance.selectBulletsSetting.SelectedValues.First();

                if (instance.selectBulletsSetting.SelectedValues.Count > 1)
                {
                    newType = (BulletType)instance.selectBulletsSetting.SelectedValues.ElementAt(
                        UnityEngine.Random.RandomRangeInt(0, instance.selectBulletsSetting.SelectedValues.Count));
                }
                if (instance.selectBulletsSetting.IsSelected((int)newType))
                    theBulletType = newType;
            }
        }

    }
}
