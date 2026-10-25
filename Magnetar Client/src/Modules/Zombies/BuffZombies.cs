using HarmonyLib;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using UnityEngine;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

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

    public override bool Active { get; set; }
    public BuffZombies()
    {
        instance = this;

        CreateCategory("General");

        ZombieSelectedSetting = new MultiSelectSetting("Entities", typeof(ZombieType))
        {
            CustomNames = TranslateEnum(typeof(ZombieType)),
            Blacklist = Banned.ZombieTypeBanned,

        };

        ZombieSelectedSetting.SelectAll(setDefault: true);

        HpMultiplierSettig = new FloatSetting("Hp Multiply", 0.1f, 100, 2, 3);

        AddSettings(ZombieSelectedSetting, HpMultiplierSettig);
        EndCategory();

    }

    public override void OnLanguageChanged()
    {
        ZombieSelectedSetting.CustomNames = TranslateEnum(typeof(ZombieType));
    }

    public readonly struct ZombieHpData
    {
        public readonly long BaseMax;
        public readonly int Armor1Max;
        public readonly int Armor2Max;

        public ZombieHpData(long baseMax, int armor1Max, int armor2Max)
        {
            BaseMax = baseMax;
            Armor1Max = armor1Max;
            Armor2Max = armor2Max;
        }
    }

    public static Dictionary<Zombie, ZombieHpData> originalHpData = new();

    // Mod Logic
    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        float multiplier = HpMultiplierSettig.Value;

        foreach (var zombie in GameData.ZombieList)
        {
            if (zombie == null) continue;

            // Only buff the zombie if it's selected in the UI
            if (ZombieSelectedSetting.IsSelected((int)zombie.theZombieType))
            {
                if (!originalHpData.ContainsKey(zombie))
                {
                    originalHpData[zombie] = new ZombieHpData(
                        zombie.theMaxHealth,
                        zombie.theFirstArmorMaxHealth,
                        zombie.theSecondArmorMaxHealth
                    );

                    zombie.theMaxHealth = (long)System.Math.Round(zombie.theMaxHealth * multiplier);
                    zombie.theFirstArmorMaxHealth = Mathf.RoundToInt(zombie.theFirstArmorMaxHealth * multiplier);
                    zombie.theSecondArmorMaxHealth = Mathf.RoundToInt(zombie.theSecondArmorMaxHealth * multiplier);

                    zombie.theHealth = (long)System.Math.Round(zombie.theHealth * multiplier);
                    zombie.theFirstArmorHealth = Mathf.RoundToInt(zombie.theFirstArmorHealth * multiplier);
                    zombie.theSecondArmorHealth = Mathf.RoundToInt(zombie.theSecondArmorHealth * multiplier);

                    zombie.UpdateHealthText();
                }
            }
        }
    }

    public override void OnDisable()
    {
        foreach (var zombie in GameData.ZombieList)
        {
            if (zombie == null) continue;

            if (originalHpData.TryGetValue(zombie, out ZombieHpData origData))
            {
                long origMaxHp = origData.BaseMax;
                long origFirstArmorMax = origData.Armor1Max;
                long origSecondArmorMax = origData.Armor2Max;

                // 1. Calculate current health ratios (safeguard against divide by zero for unarmored zombies)
                float hpRatio = zombie.theMaxHealth > 0 ? (float)zombie.theHealth / zombie.theMaxHealth : 0f;
                float armor1Ratio = zombie.theFirstArmorMaxHealth > 0 ? (float)zombie.theFirstArmorHealth / zombie.theFirstArmorMaxHealth : 0f;
                float armor2Ratio = zombie.theSecondArmorMaxHealth > 0 ? (float)zombie.theSecondArmorHealth / zombie.theSecondArmorMaxHealth : 0f;

                // 2. Restore Original Max Healths
                zombie.theMaxHealth = origMaxHp;
                zombie.theFirstArmorMaxHealth = (int)origFirstArmorMax;
                zombie.theSecondArmorMaxHealth = (int)origSecondArmorMax;

                // 3. Scale Current Healths down using the preserved ratios
                zombie.theHealth = Mathf.RoundToInt(origMaxHp * hpRatio);
                zombie.theFirstArmorHealth = Mathf.RoundToInt(origFirstArmorMax * armor1Ratio);
                zombie.theSecondArmorHealth = Mathf.RoundToInt(origSecondArmorMax * armor2Ratio);
            }
        }

        // Clean up memory
        originalHpData.Clear();
    }

    [HarmonyPatch(typeof(Zombie))]
    public static class ZombieCleanupPatch
    {
        [HarmonyPatch(nameof(Zombie.Die))]
        [HarmonyPostfix]
        public static void DiePostfix(Zombie __instance)
        {
            if (__instance != null) originalHpData.Remove(__instance);
        }

        [HarmonyPatch(nameof(Zombie.DestoryZombie))]
        [HarmonyPostfix]
        public static void DestroyPostfix(Zombie __instance)
        {
            if (__instance != null) originalHpData.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(Board))]
    public static class BoardCleanupPatch
    {
        [HarmonyPatch(nameof(Board.OnDestroy))]
        [HarmonyPostfix]
        public static void OnDestroyPostfix() => originalHpData.Clear();
    }
}
