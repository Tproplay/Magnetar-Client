using Il2Cpp;
using Magnetar_Client.Game;
using Magnetar_Client.Utils;
using System.Collections.Generic;

namespace Magnetar_Client.Modules
{
    public class FasterZombies : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Faster Zombies";
        public override string Description { get; set; } = "Makes the selected zombie(s) faster while the module is active.";
        public override string SearchHints { get; set; } = "fasterzombies speedzombies zombieboost zombiespeed fastzombies " +
            "zombiespeedup quickzombies zombievelocity zombierun runzombies fastzombie zombiespeedmod zombiespeeder " +
            "speedupzombies rapidzombies swiftzombies turbozombies zombieagility zombiequickness zombiealacrity " +
            "zombiefast zombiesprint zombiesprinting zombieoverdrive zombiemovement fasterzombie speedyzombies " +
            "fastmovezombies zombiehurry zombierapid";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

        // Mod Data
        public MultiSelectSetting ZombieSelectedSetting;

        public FloatSetting theSpeedSettig;

        public override bool Active { get; set; } = false;
        private Dictionary<int, string> zombieNameOverriden = new Dictionary<int, string>();
        public FasterZombies()
        {
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

            theSpeedSettig = new FloatSetting("Speed", 0.1f, 10, 2);
            Settings.Add(theSpeedSettig);
        }

        public static Dictionary<Zombie,float> originalSpeedData = new Dictionary<Zombie,float>();

        // Mod Logic
        public override void OnUpdateActive()
        {
            if (Board.Instance == null) return;

            foreach (var zombie in GameData.zombieList)
            {
                #region Speed modification

                // Check if the zombie is selected and if we haven't already stored its original speed
                if (!originalSpeedData.ContainsKey(zombie) &&
                    ZombieSelectedSetting.SelectedValues.Contains((int)zombie.theZombieType))
                {
                    originalSpeedData[zombie] = zombie.uniqueSpeed;
                }

                // Check if the zombie is deselected while the module is running 
                if (originalSpeedData.ContainsKey(zombie) &&
                    !ZombieSelectedSetting.SelectedValues.Contains((int)zombie.theZombieType))
                {
                    zombie.uniqueSpeed = originalSpeedData[zombie];
                    originalSpeedData.Remove(zombie);
                }

                // Update the Unique Speed
                if (originalSpeedData.ContainsKey(zombie))
                {
                    if (zombie.uniqueSpeed != originalSpeedData[zombie] * theSpeedSettig.Value)
                        zombie.uniqueSpeed = originalSpeedData[zombie] * theSpeedSettig.Value;
                }

                #endregion

            }

        }

        public override void OnDisable()
        {
            foreach (var zombie in GameData.zombieList)
            {
                if (originalSpeedData.ContainsKey(zombie))
                {
                    zombie.uniqueSpeed = originalSpeedData[zombie];
                }
            }
            originalSpeedData.Clear();
        }
    }
}
