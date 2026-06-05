using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Game;
using Magnetar_Client.Utils;
using System;
using System.Linq;
using System.Collections.Generic;
using static Magnetar_Client.Game.AppData;

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

        public static FasterZombies instance;

        public MultiSelectSetting ZombieSelectedSetting;
        public FloatSetting theSpeedSettig;
        public override bool Active { get; set; } = false;

        public static Dictionary<IntPtr, float> originalSpeedData = new Dictionary<IntPtr, float>();

        public FasterZombies()
        {
            instance = this;

            ZombieSelectedSetting = new MultiSelectSetting("Entities", typeof(ZombieType))
            {
                MaxSelection = -1,
                CustomNames = TranslatedNames(typeof(ZombieType)),
                Blacklist = new HashSet<int> { (int)ZombieType.Nothing }
            };

            ZombieSelectedSetting.Options.Keys.ToList().ForEach(ZombieSelectedSetting.Select);
            Settings.Add(ZombieSelectedSetting);

            theSpeedSettig = new FloatSetting("Speed", 0.1f, 10f, 2f);
            Settings.Add(theSpeedSettig);
        }

        public override void OnLanguageChanged()
        {
            ZombieSelectedSetting.CustomNames = TranslatedNames(typeof(ZombieType));
        }

        // Mod Logic
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            float currentMultiplier = theSpeedSettig.Value;
            var selectedZombies = ZombieSelectedSetting.SelectedValues;

            foreach (var zombie in GameData.zombieList)
            {
                if (zombie == null || zombie.gameObject == null) continue;

                IntPtr ptr = zombie.Pointer;
                bool isSelected = selectedZombies.Contains((int)zombie.theZombieType);

                bool hasStoredSpeed = originalSpeedData.TryGetValue(ptr, out float origSpeed);

                if (isSelected)
                {
                    if (!hasStoredSpeed)
                    {
                        origSpeed = zombie.uniqueSpeed;
                        originalSpeedData[ptr] = origSpeed;
                    }

                    float targetSpeed = origSpeed * currentMultiplier;

                    // Only assign if different to prevent redundant memory writing
                    if (zombie.uniqueSpeed != targetSpeed)
                    {
                        zombie.uniqueSpeed = targetSpeed;
                    }
                }
                else if (hasStoredSpeed)
                {
                    // Zombie was deselected in the UI while the module is still active
                    zombie.uniqueSpeed = origSpeed;
                    originalSpeedData.Remove(ptr);
                }
            }
        }

        public override void OnDisable()
        {
            foreach (var zombie in GameData.zombieList)
            {
                if (zombie == null) continue;

                IntPtr ptr = zombie.Pointer;
                if (originalSpeedData.TryGetValue(ptr, out float origSpeed))
                {
                    zombie.uniqueSpeed = origSpeed;
                }
            }
            originalSpeedData.Clear();
        }

        [HarmonyPatch(typeof(Zombie))]
        public static class ZombieCleanupPatch
        {
            [HarmonyPatch(nameof(Zombie.Die))]
            [HarmonyPostfix]
            public static void DiePostfix(Zombie __instance)
            {
                if (__instance != null)
                {
                    originalSpeedData.Remove(__instance.Pointer);
                }
            }
        }
    }
}