using Il2Cpp;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Magnetar_Client.Modules
{
    public class SmallerParticles : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Smaller Projectiles";
        public override string Description { get; set; } = "Reduces the visual size of selected Projectiles to reduce screen clutter.";
        public override string SearchHints { get; set; } = "smallerprojectiles smallprojectiles projectile size visual reduce screen clutter " +
            "tiny peas bullet bulletsize smallpea lessclutter screenclutter projectile size visualreduce projectileclutter shrinking " +
            "projectilesize projectil smallbullet clear screen projectilevisual smallammo ammoclutter pea size hitclutter tinyprojectiles " +
            "lagclutter projectils";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;

        public static SmallerParticles instance;

        // Mod Data
        public MultiSelectSetting BulletTypeSetting;
        public FloatSetting ScaleSetting;

        public static Dictionary<IntPtr, Vector3> originalScales = new Dictionary<IntPtr, Vector3>();

        public SmallerParticles()
        {
            instance = this;

            BulletTypeSetting = new MultiSelectSetting("Bullet Types", typeof(BulletType));
            var allBulletTypes = Enum.GetValues(typeof(BulletType)).Cast<int>();
            BulletTypeSetting.SelectedValues.UnionWith(allBulletTypes);
            Settings.Add(BulletTypeSetting);

            ScaleSetting = new FloatSetting("Scale Multiplier", 0.1f, 1f, 0.5f, 2);
            Settings.Add(ScaleSetting);
        }

        // Mod Logic

        public override void OnDisable()
        {
            var allBullets = UnityEngine.Object.FindObjectsOfType<Bullet>();
            foreach (var bullet in allBullets)
            {
                if (bullet != null && originalScales.TryGetValue(bullet.Pointer, out Vector3 origScale))
                {
                    bullet.transform.localScale = origScale;
                }
            }
        }

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
    }
}