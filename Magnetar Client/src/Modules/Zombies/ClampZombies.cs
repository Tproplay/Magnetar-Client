using Il2Cpp;
using Magnetar_Client.Game;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class ClampZombies : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Clamp Zombies";
        public override string Description { get; set; } = "Maintains stable FPS by merging the HP of nearby identical zombies when the spawn limit is exceeded." +
            "\nMay Break the Damage Counter.";
        public override string SearchHints { get; set; } = "clampzombies clampzombie limitfps maxfps fpsoptimize fixlag mergezombies" +
            " combinezombies zombiehp healthmerge mergespawn performanceboost memoryfix stackzombies zombieclamp hpstack zombielimit" +
            " fpsboost lagfix clusterspawns clumpzombies mergehp optimizefps lowfps hpfusion zombiemerge lagprevent clampzomb";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

        // Mod Data
        public static ClampZombies instance;

        public IntSetting MaxZombiesSetting;
        private float lastCheckTime = 0f;

        public FloatSetting SpeedReductionSetting;
        public FloatSetting HpAdditionPercentageSetting;
        public FloatSetting VisualScaleSetting;

        public ClampZombies()
        {
            instance = this;

            CreateCategory("General");

            MaxZombiesSetting = new IntSetting("Max Zombies", 10, 1000, 60);
            AddSettings(MaxZombiesSetting);

            SpeedReductionSetting = new FloatSetting("Apply Speed Reduction", 0.01f, 1, 0.9f, 2);
            AddSettings(SpeedReductionSetting);

            HpAdditionPercentageSetting = new FloatSetting("Add % of Hp", 0.1f, 100, 100, 1);
            AddSettings(HpAdditionPercentageSetting);

            EndCategory();
            CreateCategory("Extra");

            VisualScaleSetting = new FloatSetting("Visual Scale", 0.01f, 1, 0.03f, 2);
            AddSettings(VisualScaleSetting);

            EndCategory();
        }

        // Mod Logic
        public override void OnUpdateActive()
        {
            if (Time.time - lastCheckTime < 1f) return;
            lastCheckTime = Time.time;

            if (BoardInstanceIsNull) return;

            int currentZombies = GameData.zombieList.Count;
            int limit = MaxZombiesSetting.Value;

            if (currentZombies <= limit) return;

            int zombiesToMerge = currentZombies - limit;

            // 1. Group zombies by Lane and Type
            var groupedZombies = GameData.zombieList
                .Where(z => z != null && z.Alive && !z.isIdle && z.gameObject != null)
                .GroupBy(z => new { z.theZombieRow, z.theZombieType })
                .ToList();

            foreach (var group in groupedZombies)
            {
                if (zombiesToMerge <= 0) break;

                // 2. Sort the group by their X position so adjacent zombies are next to each other in the list
                var sortedGroup = group.OrderBy(z => z.transform.position.x).ToList();

                // 3. Iterate and merge neighbors
                for (int i = 0; i < sortedGroup.Count - 1; i++)
                {
                    if (zombiesToMerge <= 0) break;

                    Zombie rightZombie = sortedGroup[i + 1];
                    Zombie leftZombie = sortedGroup[i];

                    if (rightZombie == null || leftZombie == null || !rightZombie.Alive || !leftZombie.Alive) continue;

                    // Don't merge if they are far away
                    if (rightZombie.Column - leftZombie.Column > 1) continue;


                    // Sum Healths
                    rightZombie.theMaxHealth += (int)(leftZombie.theHealth * HpAdditionPercentageSetting.Value/100);
                    rightZombie.theHealth += (int)(leftZombie.theHealth * HpAdditionPercentageSetting.Value / 100);

                    if (leftZombie.theFirstArmorHealth > 0)
                    {
                        rightZombie.theFirstArmorMaxHealth += (int)(leftZombie.theFirstArmorHealth * HpAdditionPercentageSetting.Value / 100);
                        rightZombie.theFirstArmorHealth += (int)(leftZombie.theFirstArmorHealth * HpAdditionPercentageSetting.Value / 100);
                    }
                    if (leftZombie.theSecondArmorHealth > 0)
                    {
                        rightZombie.theSecondArmorMaxHealth += (int)(leftZombie.theSecondArmorHealth * HpAdditionPercentageSetting.Value / 100);
                        rightZombie.theSecondArmorHealth += (int)(leftZombie.theSecondArmorHealth * HpAdditionPercentageSetting.Value / 100);
                    }

                    // Merged zombie is slightly larger so the player knows it is buffed
                    rightZombie.transform.localScale += new Vector3(VisualScaleSetting.Value, VisualScaleSetting.Value, 0f);

                    // Apply Speed Reduction

                    rightZombie.uniqueSpeed *= SpeedReductionSetting.Value;

                    // Left Zombie GET OUT

                    var movePosition = leftZombie.transform.localPosition;
                    movePosition.x = 20;

                    leftZombie.SetPosition(movePosition);

                    // Kill the zombie
                    leftZombie.theHealth = 0;
                    leftZombie.theFirstArmorHealth = 0;
                    leftZombie.theSecondArmorHealth = 0;

                    leftZombie.Die();

                    zombiesToMerge--;

                    i++;
                }
            }
        }
    }
}