using HarmonyLib;
using System;
using System.Linq;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class Volley : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Volley";
        public override string Description { get; set; } = "Makes Plants fire multiple rounds of bullets.";
        public override string SearchHints { get; set; } = "homingbullets homingrounds trackingbullets targetingsystem " +
            "trackingrounds smartbullets seekerbullets seekershells seekerprojectiles bullettracking homingmod " +
            "homingplugin homingattack hommingbullets hommingrounds hominbullets homingbulits homingbullits " +
            "targetbullets targettingbullets autobullets guidedbullets guidedrounds missilebullets curvebullets " +
            "magneticbullets stickybullets lockonbullets lockonrounds precisionbullets";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static Volley instance;
        public IntSetting BulletMultiplier;
        public MultiSelectSetting selectedBulletsSetting;

        public BoolSetting AlsoAffectZombies;

        public FloatSetting VerticalSpread;

        public Volley()
        {
            instance = this;

            CreateCategory("General");

            BulletMultiplier = new IntSetting("Bullet multiplier", 1, 10, 2, 1);
            AlsoAffectZombies = new BoolSetting("Affect Zombie projectiles", false);

            selectedBulletsSetting = new MultiSelectSetting("Projectiles", typeof(BulletType))
            {
                CustomNames = TranslatedNames(typeof(BulletType)),
            };

            selectedBulletsSetting.Options.Keys.ToList().ForEach(selectedBulletsSetting.Select);

            AddSettings(BulletMultiplier, selectedBulletsSetting);

            EndCategory();
            CreateCategory("Bullet Spawn");

            VerticalSpread = new FloatSetting("Vertical Spread (unit)", 0, 1, 0.1f, 3);

            AddSettings(VerticalSpread);
            EndCategory();

        }

        public override void OnLanguageChanged()
        {
            selectedBulletsSetting.CustomNames = TranslatedNames(typeof(BulletType));
        }


        // Mod Logic

        [HarmonyPatch(typeof(CreateBullet))]
        public static class CreateBulletPatch
        {
            [ThreadStatic]
            public static bool SpawnedByMod = false;

            [HarmonyPatch(nameof(CreateBullet.SetBullet))]
            [HarmonyPrefix]
            public static bool SetBulletPrefix(CreateBullet __instance, float x, float y, int theRow, BulletType theBulletType, BulletMoveWay theMovingWay, bool fromEnermy = false)
            {
                if (SpawnedByMod) return true;
                if (instance == null || !instance.Active) return true;
                if (fromEnermy && !instance.AlsoAffectZombies.Value) return true;
                if (!instance.selectedBulletsSetting.IsSelected((int)theBulletType)) return true;

                int n = instance.BulletMultiplier.Value;
                if (n == 1) return true;

                float verticalSpread = instance.VerticalSpread.Value;

                SpawnedByMod = true;
                try
                {
                    float centerOffset = (n - 1) / 2f;

                    for (int i = 0; i < n; i++)
                    {
                        float step = i - centerOffset;
                        float deltay = step * verticalSpread;

                        __instance.SetBullet(x, y + deltay, theRow, theBulletType, theMovingWay, fromEnermy);
                    }
                }
                finally
                {
                    SpawnedByMod = false;
                }

                return false;
            }
        }

    }
}
