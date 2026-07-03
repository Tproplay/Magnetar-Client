using static Magnetar_Client.Game.AppData;
using System;
using HarmonyLib;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
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

        public WaveHack()
        {
            instance = this;

            CreateCategory("Cooldown");

            Active_WaveCooldownSetting = new BoolSetting("Custom Wave Cooldown", false);
            WaveCooldownSetting = new FloatSetting("Wave Cooldown", 0, 60, 30, 1, 0);
            FreezeWaveSetting = new BoolSetting("Freeze Wave Timer", false);

            AddSettings(Active_WaveCooldownSetting, WaveCooldownSetting,FreezeWaveSetting);
            EndCategory();

            CreateCategory("Spawn Rate");

            ZombiesCountMultiplier = new IntSetting("Zombies count multiplier", 1, 10, 1, 0);

            AddSettings(ZombiesCountMultiplier);
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
                if (instance == null || !instance.Active || instance.ZombiesCountMultiplier.Value == 1 || 
                    spawnedByMod) return true;
                if (instance.ZombiesCountMultiplier.Value == 0) return false;

                spawnedByMod = true;

                for (int i = 0; i < instance.ZombiesCountMultiplier.Value; i++)
                {
                    __instance.SummonZombies(wave);
                }

                spawnedByMod = false;

                return false;

            }
        }



    }
}