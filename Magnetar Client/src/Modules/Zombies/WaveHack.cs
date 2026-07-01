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
        public BoolSetting Active_NumberOfZombiePerWave;
        public IntSetting NumberOfZombiePerWave;

        // Spawn Rate Multiplier
        public FloatSetting ZombiesCountMultiplier;

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

            Active_NumberOfZombiePerWave = new BoolSetting("Custom number of zombie spawn", false);
            NumberOfZombiePerWave = new IntSetting("Number of Zombie per wave", 5, 100, 15, 0);

            AddSettings(Active_NumberOfZombiePerWave,NumberOfZombiePerWave);
            EndCategory();

            CreateCategory("Spawn Rate Multiplier");

            ZombiesCountMultiplier = new FloatSetting("Zombies count multiplier", 0.5f, 10, 1f, 2, 0);

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



    }
}