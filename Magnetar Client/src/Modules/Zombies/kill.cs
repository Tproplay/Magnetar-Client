using Il2Cpp;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Game.AppData;

using static Magnetar_Client.Game.GameData;

namespace Magnetar_Client.Modules
{
    public class KillZombies : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Kill All Zombies";
        public override string Description { get; set; } = "Kills the selected zombie(s) while the module is active.";
        public override string SearchHints { get; set; } = "killallzombies killzombies zombiekiller removezombies " +
            "deletezombies zombieremoval zombieslayer destroyzombies exterminatezombies zombieexterminator " +
            "zombieclear clearzombies wipezombies zombiewipe zombiedeath deathzombies killallzombie killalzoombies " +
            "kilallzombies killallzombiees killallzombis zombiedestructor zombiedestroyer zombiedeleter zombiesmasher " +
            "zombiepurger zombieexecutioner zombieelimination zombieterminator zombieender zombieeraser zombievanisher";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

        // Mod Data

        public static KillZombies instance;

        public MultiSelectSetting ZombiesSelectedSetting;
        public MultiSelectSetting HypnoZombiesSelectedSetting;

        public bool TurnOffAfterUse = true;
        public BoolSetting AutoTurnOff;
        public override bool Active { get; set; } = false;
        public static float deltaTime = 0;


        private static Dictionary<int,string> zombieNameOverriden = new Dictionary<int, string>();

        public KillZombies()
        {
            instance = this;

            zombieNameOverriden = Translator.TranslateEnum(typeof(ZombieType));
            foreach (var name in zombieNameOverriden)
            {
                zombieNameOverriden[name.Key] = $"{zombieNameOverriden[name.Key]} ({name.Key})";
            }

            ZombiesSelectedSetting = new MultiSelectSetting("Entities", typeof(ZombieType))
            {
                MaxSelection = -1,
                CustomNames = zombieNameOverriden,
                Blacklist = new HashSet<int> {
                    (int)ZombieType.Nothing,

                },

            };
            ZombiesSelectedSetting.SelectedValues.UnionWith(ZombiesSelectedSetting.Options.Keys);
            Settings.Add(ZombiesSelectedSetting);

            HypnoZombiesSelectedSetting = new MultiSelectSetting("Hypnotized Entities", typeof(ZombieType))
            {
                MaxSelection = -1,
                CustomNames = zombieNameOverriden,
                Blacklist = new HashSet<int> {
                    (int)ZombieType.Nothing,

                },

            };
            HypnoZombiesSelectedSetting.SelectedValues.UnionWith(HypnoZombiesSelectedSetting.Options.Keys);
            Settings.Add(HypnoZombiesSelectedSetting);

            AutoTurnOff = new BoolSetting("Auto Turn Off", TurnOffAfterUse);
            Settings.Add(AutoTurnOff);

        }

        public override void OnLanguageChanged()
        {
            zombieNameOverriden = Translator.TranslateEnum(typeof(ZombieType));
            foreach (var name in zombieNameOverriden)
            {
                zombieNameOverriden[name.Key] = $"{zombieNameOverriden[name.Key]} ({name.Key})";
            }
            HypnoZombiesSelectedSetting.CustomNames = zombieNameOverriden;
            ZombiesSelectedSetting.CustomNames = zombieNameOverriden;
        }

        // Mod Logic
        public override void OnUpdateActive()
        {
            // Handle auto turn off
            if (AutoTurnOff.Value)
            {
                deltaTime += Time.deltaTime;
                if (deltaTime > 0.3f)
                {
                    Active = false;
                    deltaTime = 0;
                }
            }

            if (BoardInstanceIsNull) return; 

            foreach (var zombie in zombieList)
            {
   
                if (ZombiesSelectedSetting.IsSelected((int)zombie.theZombieType)
                    || HypnoZombiesSelectedSetting.IsSelected((int)zombie.theZombieType))
                {
                    zombie.theHealth = 0;
                }
            }
            
        }
    }
}
