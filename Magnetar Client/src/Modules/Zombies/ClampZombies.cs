using HarmonyLib;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Magnetar_Client.Modules.Helpers;



#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class ClampZombies : Module
{
    public override string Name { get; set; } = "Clamp Zombies";
    public override string Description { get; set; } = "Merges excess zombies on the same row into a single stronger zombie when zombie count exceeds the limit.";
    public override string SearchHints { get; set; } = "clampzombies limitzombies maxzombies mergezombies";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

    public static ClampZombies instance;

    public IntSetting MaxZombiesSetting;
    public FloatSetting HpAdditionPercentageSetting;
    public FloatSetting SpeedReductionSetting;
    public FloatSetting VisualScaleSetting;

    public ClampZombies()
    {
        instance = this;

        CreateCategory("General");

        MaxZombiesSetting = new IntSetting("Max Zombies", 1, 100, 20);
        HpAdditionPercentageSetting = new FloatSetting("Health Transfer %", 0f, 100f, 100f, 1);
        SpeedReductionSetting = new FloatSetting("Speed Multiplier", 0.1f, 1f, 0.9f, 2);
        VisualScaleSetting = new FloatSetting("Visual Scale Increase", 0f, 0.5f, 0.05f, 2);

        AddSettings(MaxZombiesSetting, HpAdditionPercentageSetting, SpeedReductionSetting, VisualScaleSetting);
        EndCategory();
    }

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        int currentZombies = GameData.ZombieList.Count;
        int limit = MaxZombiesSetting.Value;

        if (currentZombies <= limit) return;

        int zombiesToMerge = currentZombies - limit;

        // 1. Group zombies by Lane and Type
        var groupedZombies = GameData.ZombieList
            .Where(z => z != null && z.Alive && z.gameObject != null)
            .GroupBy(z => new { z.theZombieRow, z.theZombieType })
            .ToList();

        foreach (var group in groupedZombies)
        {
            if (zombiesToMerge <= 0) break;

            // 2. Sort the group by their X position so adjacent zombies are next to each other
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

                float pct = HpAdditionPercentageSetting.Value / 100f;
                long addedMaxHp = (long)(leftZombie.theHealth * pct);
                int addedArmor1 = leftZombie.theFirstArmorHealth > 0 ? (int)(leftZombie.theFirstArmorHealth * pct) : 0;
                int addedArmor2 = leftZombie.theSecondArmorHealth > 0 ? (int)(leftZombie.theSecondArmorHealth * pct) : 0;

                // Update rightZombie base stats deterministically in ZombieStatManager
                ZombieStatManager.AddBase(rightZombie, StatType.MaxHealth, addedMaxHp);
                if (addedArmor1 > 0) ZombieStatManager.AddBase(rightZombie, StatType.Armor1Max, addedArmor1);
                if (addedArmor2 > 0) ZombieStatManager.AddBase(rightZombie, StatType.Armor2Max, addedArmor2);
                ZombieStatManager.AddBase(rightZombie, StatType.Scale, VisualScaleSetting.Value);
                ZombieStatManager.MultiplyBase(rightZombie, StatType.Speed, SpeedReductionSetting.Value);
                ZombieStatManager.Evaluate(rightZombie);

                // Move left zombie out and kill it
                var movePosition = leftZombie.transform.localPosition;
                movePosition.x = 20;
                leftZombie.SetPosition(movePosition);

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
