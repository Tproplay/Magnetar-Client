using Il2Cpp;
using Magnetar_Client.Utils;
using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static Magnetar_Client.Game.GameData;

namespace Magnetar_Client.Modules
{
    public class HypnotizeZombies : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Hypnotize All Zombies";
        public override string Description { get; set; } = "Hypnorizes the selected zombie(s) while the module is active.";
        public override string SearchHints { get; set; } = "hypnotizeallzombies hypnozombies zombieshypnotize hypnotisezombies" +
            " charmzombies zombiecharm mindcontrolzombies zombiesmindcontrol allyzombies zombiealliance brainwashzombies " +
            "turnzombies hypnotizezombie hypnozombie hipnotizezombies hipnozombies hypnatizezombies hypnotiseallzombies " +
            "hypnotisedzombies hypnoallzombies zombiehypnotizer zombiehypnosis zombiespell zombieconvert convertzombies " +
            "friendlyzombies zombiebrainwash zombiemind hypnotiseallzombie";


        public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

        // Mod Data
        public MultiSelectSetting ZombiesSelectedSetting;

        public bool TurnOffAfterUse = true;
        public BoolSetting AutoTurnOff;
        public override bool Active { get; set; } = false;
        public static float deltaTime = 0;



        private static Dictionary<int, string> zombieNameOverriden = new Dictionary<int, string>();

        public HypnotizeZombies()
        {

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

            AutoTurnOff = new BoolSetting("Auto Turn Off", TurnOffAfterUse);
            Settings.Add(AutoTurnOff);

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

            if (Board.Instance == null) return;

            foreach (var zombie in zombieList)
            {
                if (ZombiesSelectedSetting.IsSelected((int)zombie.theZombieType))
                {
                    zombie.isMindControlled = true;
                }
            }
            
        }
    }
}
