using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class SmallerProjectiles : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Smaller Projectiles";
        public override string Description { get; set; } = "Reduces the visual size of selected Projectiles to reduce screen clutter.";
        public override string SearchHints { get; set; } = "smallerprojectiles smallprojectiles projectile size visual reduce screen clutter " +
            "tiny peas bullet bulletsize smallpea lessclutter screenclutter projectile size visualreduce projectileclutter shrinking " +
            "projectilesize projectil smallbullet clear screen projectilevisual smallammo ammoclutter pea size hitclutter tinyprojectiles " +
            "lagclutter projectils";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;

        public static SmallerProjectiles instance;

        // Mod Data
        public MultiSelectSetting BulletTypeSetting;
        public FloatSetting ScaleSetting;

        public SmallerProjectiles()
        {
            instance = this;

            CreateCategory("General");

            BulletTypeSetting = new MultiSelectSetting("Bullet Types", typeof(BulletType))
            {
                CustomNames = TranslatedNames(typeof(BulletType)),
            };

            BulletTypeSetting.Options.Keys.ToList().ForEach(BulletTypeSetting.Select);
            Settings.Add(BulletTypeSetting);

            ScaleSetting = new FloatSetting("Scale Multiplier", 0.1f, 2f, 0.5f, 2);
            Settings.Add(ScaleSetting);

            EndCategory();

        }

        public override void OnLanguageChanged()
        {
            BulletTypeSetting.CustomNames = TranslatedNames(typeof(BulletType));
        }


        // Mod Logic

        public override void OnEnable()
        {
            var Bullets = UnityEngine.Object.FindObjectsOfType<Bullet>();

            foreach (var __instance in Bullets)
            {
                IntPtr ptr = __instance.Pointer;

                if (!originalScales.ContainsKey(ptr))
                {
                    originalScales[ptr] = __instance.transform.localScale;
                }

                if (instance != null && instance.Active && instance.BulletTypeSetting.IsSelected((int)__instance.theBulletType))
                {
                    Vector3 orig = originalScales[ptr];
                    float mult = instance.ScaleSetting.Value;

                    __instance.transform.localScale = new Vector3(orig.x * mult, orig.y * mult, orig.z);
                }
                else
                {
                    __instance.transform.localScale = originalScales[ptr];
                }
            }

        }

        public override void OnDisable()
        {
            var allBullets = Resources.FindObjectsOfTypeAll<Bullet>();

            foreach (var bullet in allBullets)
            {
                if (bullet != null && originalScales.TryGetValue(bullet.Pointer, out Vector3 origScale))
                {
                    bullet.transform.localScale = origScale;
                }
            }
            originalScales.Clear();
        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) originalScales.Clear();
        }

        public static Dictionary<IntPtr, Vector3> originalScales = new Dictionary<IntPtr, Vector3>();

        [HarmonyPatch(typeof(Bullet))]
        public static class BulletSpawnPatch
        {
            [HarmonyPatch(nameof(Bullet.InitData))]
            [HarmonyPostfix]
            public static void Postfix(Bullet __instance)
            {
                if (__instance == null) return;


                IntPtr ptr = __instance.Pointer;

                if (!originalScales.ContainsKey(ptr))
                {
                    originalScales[ptr] = __instance.transform.localScale;
                }

                if (instance != null && instance.Active && instance.BulletTypeSetting.IsSelected((int)__instance.theBulletType))
                {
                    Vector3 orig = originalScales[ptr];
                    float mult = instance.ScaleSetting.Value;

                    __instance.transform.localScale = new Vector3(orig.x * mult, orig.y * mult, orig.z);
                }
                else
                {
                    __instance.transform.localScale = originalScales[ptr];
                }
         
            }
        }

        [HarmonyPatch(typeof(Board))]
        public static class BoardAwakePatch
        {
            [HarmonyPatch(nameof(Board.Awake))]
            [HarmonyPostfix]
            public static void AwakePostfix()
            {
                originalScales.Clear();
            }

            [HarmonyPatch(nameof(Board.OnDestroy))]
            [HarmonyPostfix]
            public static void OnDestroyPostfix()
            {
                originalScales.Clear();
            }
        }
    }
}