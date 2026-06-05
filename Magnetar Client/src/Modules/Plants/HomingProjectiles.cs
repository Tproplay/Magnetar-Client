using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Magnetar_Client.Modules
{
    public class HomingProjectiles : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Homing Bullets";
        public override string Description { get; set; } = "Makes Plants fire Homing rounds.";
        public override string SearchHints { get; set; } = "homingbullets homingrounds trackingbullets targetingsystem " +
            "trackingrounds smartbullets seekerbullets seekershells seekerprojectiles bullettracking homingmod " +
            "homingplugin homingattack hommingbullets hommingrounds hominbullets homingbulits homingbullits " +
            "targetbullets targettingbullets autobullets guidedbullets guidedrounds missilebullets curvebullets " +
            "magneticbullets stickybullets lockonbullets lockonrounds precisionbullets";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static HomingProjectiles instance;

        public List<int> preselected = Enum.GetValues(typeof(BulletType)).Cast<int>().ToList();
        public MultiSelectSetting selectedBulletsSetting;

        public HomingProjectiles()
        {
            instance = this;

            selectedBulletsSetting = new MultiSelectSetting("Projectiles", typeof(BulletType))
            {
                CustomNames = TranslatedNames(typeof(BulletType)),
            };

            selectedBulletsSetting.Options.Keys.ToList().ForEach(selectedBulletsSetting.Select);

            Settings.Add(selectedBulletsSetting);
        }

        public override void OnLanguageChanged()
        {
            selectedBulletsSetting.CustomNames = TranslatedNames(typeof(BulletType));
        }


        // Mod Logic

        [HarmonyPatch(typeof(CreateBullet))]
        public static class CreateBulletPatch
        {
            [HarmonyPatch(nameof(CreateBullet.SetBullet))]
            [HarmonyPrefix]
            public static void SetBulletPrefix(ref BulletType theBulletType,ref BulletMoveWay theMovingWay, bool fromEnermy = false)
            {
                if (instance == null || !instance.Active || fromEnermy) return;
                if (!instance.selectedBulletsSetting.IsSelected((int)theBulletType)) return;

                theMovingWay = BulletMoveWay.Track;
            }
        }
    }
}
