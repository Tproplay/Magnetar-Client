using Il2Cpp;
using UnityEngine;
using System.Collections.Generic;
using static Magnetar_Client.Utils.Translator;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class AntiLagSpawns : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Anti-Lag Spawns";
        public override string Description { get; set; } = "Staggers mass zombie spawns over time to prevent lag spikes.\nMay " +
            "break Game's logic and definitely break the Zombie Counter.";
        public override string SearchHints { get; set; } = "antilag antilagspawns lagspawn fixlag lagfix countersmasher stagger spawns " +
            "staggerzombies staggeredspawns massspawn lagspike nolag spawnerlag zombielag lagprevent performanceboost spawnfix breakcounter" +
            " counterbreak zombiecounter smoothspawn logicbreak massspawnfix antilagmod lagspawns antispawnlag laglessspawns";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

        public static AntiLagSpawns instance;

        // Mod Data
        public IntSetting FrameDelaySetting;

        public static Queue<Zombie> staggerQueue = new Queue<Zombie>();
        public static int framesSinceLastSpawn = 0;

        public static int lastFrameCount = 0;
        public static int spawnsThisFrame = 0;

        public MultiSelectSetting UnaffectedZombies;
        public AntiLagSpawns()
        {
            instance = this;
            FrameDelaySetting = new IntSetting("Frames Between Spawns", 1, 10, 1);
            AddSettings(FrameDelaySetting);

            var NamesOverriden = TranslateEnum(typeof(ZombieType));

            UnaffectedZombies = new MultiSelectSetting("Unaffected Zombies", typeof(ZombieType))
            {
                CustomNames = NamesOverriden,
                SelectedValues = new HashSet<int>
                {
                    (int)ZombieType.ImpKing,
                    (int)ZombieType.ImpZombie,
                    (int)ZombieType.ObsidianImpZombie,
                },
                Blacklist = new HashSet<int>
                {
                    (int)ZombieType.Nothing,
                }
            };

            AddSettings(UnaffectedZombies);
        }

        // Mod Logic
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            framesSinceLastSpawn++;

            if (staggerQueue.Count > 0 && framesSinceLastSpawn >= FrameDelaySetting.Value)
            {
                Zombie nextZombie = staggerQueue.Dequeue();

                if (nextZombie != null && nextZombie.gameObject != null)
                {
                    nextZombie.gameObject.SetActive(true);
                }

                framesSinceLastSpawn = 0;
            }
        }

        public override void OnDisable()
        {
            // If the user turns the module off, instantly release all queued zombies
            while (staggerQueue.Count > 0)
            {
                Zombie z = staggerQueue.Dequeue();
                if (z != null && z.gameObject != null)
                {
                    z.gameObject.SetActive(true);
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(CreateZombie), nameof(CreateZombie.SetZombie))]
        public static class CreateZombiePatch
        {
            [HarmonyLib.HarmonyPostfix]
            public static void Postfix(ref Zombie __result)
            {
                if (instance == null || !instance.Active || __result == null) return;

                if (__result.theZombieType == ZombieType.ImpKing)

                if (Time.frameCount != lastFrameCount)
                {
                    lastFrameCount = Time.frameCount;
                    spawnsThisFrame = 0;
                }

                spawnsThisFrame++;

                if (spawnsThisFrame > 2)
                {
                    __result.gameObject.SetActive(false);
                    staggerQueue.Enqueue(__result);
                }
            }
        }
    }
}