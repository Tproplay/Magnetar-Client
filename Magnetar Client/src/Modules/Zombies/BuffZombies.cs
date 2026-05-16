using Il2Cpp;
using Magnetar_Client.Game;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class BuffZombies : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Buff Zombies";
        public override string Description { get; set; } = "Buffs the selected zombie(s) while the module is active.";
        public override string SearchHints { get; set; } = "buffzombies buffzombie bufzombie buffingzombies hpbuff healthboost " +
            "strongzombies zombiehp morehealth tankyzombies zombiefy buffzombs biffzombies buffzombes hpup extrahealth " +
            "buffedzombs healthup zombieboost buffmod buffactive buff-zombies hpplus zombiefying zombiebiff buffzom biffzom " +
            "hpp boost buffzombiez";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

        // Mod Data

        public static BuffZombies instance;

        public MultiSelectSetting ZombieSelectedSetting;

        public FloatSetting HpMultiplierSettig;

        public override bool Active { get; set; } = false;
        private Dictionary<int, string> zombieNameOverriden = new Dictionary<int, string>();
        public BuffZombies()
        {
            instance = this;

            zombieNameOverriden = Translator.TranslateEnum(typeof(ZombieType));

            foreach (var name in zombieNameOverriden)
            {
                zombieNameOverriden[name.Key] = $"{zombieNameOverriden[name.Key]} ({name.Key})";
            }

            ZombieSelectedSetting = new MultiSelectSetting("Entities", typeof(ZombieType))
            {
                MaxSelection = -1,
                CustomNames = zombieNameOverriden,
                Blacklist = new HashSet<int> {
                (int)ZombieType.Nothing
                }

            };

            ZombieSelectedSetting.SelectedValues.UnionWith(ZombieSelectedSetting.Options.Keys);

            Settings.Add(ZombieSelectedSetting);

            HpMultiplierSettig = new FloatSetting("Hp Multiply", 0.1f, 100, 2);
            Settings.Add(HpMultiplierSettig);
        }

        // Tracks the original max healths: [0] = Base, [1] = Armor1, [2] = Armor2
        public static Dictionary<Zombie, List<int>> originalHpData = new Dictionary<Zombie, List<int>>();

        // Mod Logic
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            float multiplier = HpMultiplierSettig.Value;

            foreach (var zombie in GameData.zombieList)
            {
                if (zombie == null) continue;

                // Only buff the zombie if it's selected in the UI
                if (ZombieSelectedSetting.IsSelected((int)zombie.theZombieType))
                {
                    if (!originalHpData.ContainsKey(zombie))
                    {
                        // 1. Store the original maximums
                        originalHpData[zombie] = new List<int> {
                            zombie.theMaxHealth,
                            zombie.theFirstArmorMaxHealth,
                            zombie.theSecondArmorMaxHealth
                        };

                        // 2. Multiply Max Healths
                        zombie.theMaxHealth = Mathf.RoundToInt(zombie.theMaxHealth * multiplier);
                        zombie.theFirstArmorMaxHealth = Mathf.RoundToInt(zombie.theFirstArmorMaxHealth * multiplier);
                        zombie.theSecondArmorMaxHealth = Mathf.RoundToInt(zombie.theSecondArmorMaxHealth * multiplier);

                        // 3. Multiply Current Healths
                        zombie.theHealth = Mathf.RoundToInt(zombie.theHealth * multiplier);
                        zombie.theFirstArmorHealth = Mathf.RoundToInt(zombie.theFirstArmorHealth * multiplier);
                        zombie.theSecondArmorHealth = Mathf.RoundToInt(zombie.theSecondArmorHealth * multiplier);
                    }
                }
            }
        }

        public override void OnDisable()
        {
            foreach (var zombie in GameData.zombieList)
            {
                if (zombie == null) continue;

                if (originalHpData.ContainsKey(zombie))
                {
                    List<int> origData = originalHpData[zombie];
                    int origMaxHp = origData[0];
                    int origFirstArmorMax = origData[1];
                    int origSecondArmorMax = origData[2];

                    // 1. Calculate current health ratios (safeguard against divide by zero for unarmored zombies)
                    float hpRatio = zombie.theMaxHealth > 0 ? (float)zombie.theHealth / zombie.theMaxHealth : 0f;
                    float armor1Ratio = zombie.theFirstArmorMaxHealth > 0 ? (float)zombie.theFirstArmorHealth / zombie.theFirstArmorMaxHealth : 0f;
                    float armor2Ratio = zombie.theSecondArmorMaxHealth > 0 ? (float)zombie.theSecondArmorHealth / zombie.theSecondArmorMaxHealth : 0f;

                    // 2. Restore Original Max Healths
                    zombie.theMaxHealth = origMaxHp;
                    zombie.theFirstArmorMaxHealth = origFirstArmorMax;
                    zombie.theSecondArmorMaxHealth = origSecondArmorMax;

                    // 3. Scale Current Healths down using the preserved ratios
                    zombie.theHealth = Mathf.RoundToInt(origMaxHp * hpRatio);
                    zombie.theFirstArmorHealth = Mathf.RoundToInt(origFirstArmorMax * armor1Ratio);
                    zombie.theSecondArmorHealth = Mathf.RoundToInt(origSecondArmorMax * armor2Ratio);
                }
            }

            // Clean up memory
            originalHpData.Clear();
        }
    }
}
