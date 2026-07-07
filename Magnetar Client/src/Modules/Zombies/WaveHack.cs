using static Magnetar_Client.Game.AppData;
using System;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using MelonLoader;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx.Unity.IL2CPP.Utils;
#endif

namespace Magnetar_Client.Modules
{
    public class WaveHack : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Wave Hack";
        public override string Description { get; set; } = "Allows you to modify wave spawn logic.";
        public override string SearchHints { get; set; } = "wavehack wavemod wavespawn wavemodify wavespawnlogic " +
            "spawnlogic waveeditor wavemanager customwaves waveconfig wavespawnrate wavecontrol wavespawnsettings" +
            " modifyspawn wavespawncontrol wavecheat wavespawner wavechanger wavesize wavemanager wavebypass " +
            "waveflow dontsendzombies pause zombiespause wavepause pausewave removewave";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

        // Mod Data

        public static WaveHack instance;

        // Cooldown
        public BoolSetting Active_WaveCooldownSetting;
        public FloatSetting WaveCooldownSetting;
        public BoolSetting FreezeWaveSetting;

        // Spawn Rate
        public IntSetting ZombiesCountMultiplier;
        public FloatSetting SpawnDelay;

        public WaveHack()
        {
            instance = this;

            CreateCategory("Cooldown");

            Active_WaveCooldownSetting = new BoolSetting("Custom Wave Cooldown", false);
            WaveCooldownSetting = new FloatSetting("Wave Cooldown", 0, 60, 30, 3, 0);
            FreezeWaveSetting = new BoolSetting("Freeze Wave Timer", false);

            AddSettings(Active_WaveCooldownSetting, WaveCooldownSetting,FreezeWaveSetting);
            EndCategory();

            CreateCategory("Spawn Rate");

            ZombiesCountMultiplier = new IntSetting("Zombies count multiplier", 1, 10, 1, 0);
            SpawnDelay = new FloatSetting("Spawn Delay", 0, 3, 0.5f, 3);
            AddSettings(ZombiesCountMultiplier,SpawnDelay);
            EndCategory();

        }

        public static float last_val;

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            if (instance.FreezeWaveSetting.Value)
            {
                board.timeUntilNextWave = last_val;
            }

            if (instance.Active_WaveCooldownSetting.Value)
            {
                if (board.timeUntilNextWave > Math.Min(last_val, instance.WaveCooldownSetting.Value))
                    board.timeUntilNextWave = instance.WaveCooldownSetting.Value;
            }

            last_val = board.timeUntilNextWave;
        }

        [HarmonyPatch(typeof(BoardSpawner))]
        public static class BoardSpawnerPatch
        {
            static bool spawnedByMod = false;

            [HarmonyPatch(nameof(BoardSpawner.SummonZombies))]
            [HarmonyPrefix]
            public static bool SummonZombiesPrefix(int wave, BoardSpawner __instance)
            {
                if (spawnedByMod) return true;

                if (instance == null || !instance.Active || instance.ZombiesCountMultiplier.Value == 1) return true;
                if (instance.ZombiesCountMultiplier.Value == 0) return false;

#if MELONLOADER || RELEASE_MELON
                MelonCoroutines.Start(SpawnZombies(__instance, wave));
#elif BEPINEX || RELEASE_BEPINEX
                MonoBehaviourExtensions.StartCoroutine(__instance.Cast<MonoBehaviour>(), SpawnZombies(__instance, wave));
#endif
                return false;
            }

            public static IEnumerator SpawnZombies(BoardSpawner __instance, int wave)
            {

                for (int currentSpawnCount = 0; currentSpawnCount < instance.ZombiesCountMultiplier.Value; currentSpawnCount++)
                {
                    if (__instance == null) yield break;
                    yield return new WaitForSeconds(instance.SpawnDelay.Value);
                    if (__instance == null) yield break;

                    spawnedByMod = true;

                    try
                    {
                        __instance.SummonZombies(wave);
                    }
                    finally
                    {
                        spawnedByMod = false;
                    }
                }
            }


        }



    }
}