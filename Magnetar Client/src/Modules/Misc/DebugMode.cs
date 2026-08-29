using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Game.AppData;
using Magnetar_Client.Game;
using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using HarmonyLib;
using static Magnetar_Client.Modules.NoRender;



#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class DebugMode : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Debug Mode (for devs)";
        public override string Description { get; set; } = "Logs debug info onto the console.";
        public override string SearchHints { get; set; } = "debugmode debuginfo consolelog loginfo debuglog " +
            "debugconsole logger debugshow debugdata logs debugtools debugconsolelog devlog devmode debugprint " +
            "systemlog debugdump printdebug debugmonitor showlogs debugmessages debugview debugactive loggingmode" +
            " debugtrace debugpanel infolog";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data

        public static DebugMode instance;

        public FloatSetting speed;

        public MultiSelectSetting selected;

        public enum Options
        {
            TheGameStatus = 1,
            BoardTag = 2,
            ZombieList,
            PlantList,
            SoundPlayed,
            ZombieAnimations,
            PlantDieReason,
            ZombieDieReason,
            CheatKeys,
            ParticleEmitted,
            AllRecipes,
            CreatePlant_SetPlant
        }

        public static Dictionary<int, string> ParticleNameTranslated;

        public DebugMode()
        {
            instance = this;

            ParticleNameTranslated = TranslatedNames(typeof(ParticleType));

            CreateCategory("General");

            selected = new MultiSelectSetting("elements")
            {
                Options = new Dictionary<int, string>
                {
                    { (int)Options.TheGameStatus, "GameStatus" },
                    { (int)Options.BoardTag, "BoardTag" },
                    { (int)Options.ZombieList, "ZombieList" },
                    { (int)Options.PlantList, "PlantList" },
                    { (int)Options.SoundPlayed, "Sound Played" },
                    { (int)Options.ZombieAnimations, "Zombie Animations (single use)" },
                    { (int)Options.PlantDieReason, "Plant die reason" },
                    { (int)Options.ZombieDieReason, "Zombie die reason" },
                    { (int)Options.CheatKeys, "Cheat keys (single use)" },
                    { (int)Options.ParticleEmitted, "Particle effect emitted" },
                    { (int)Options.AllRecipes, "All recipes (single use)" },
                    { (int)Options.CreatePlant_SetPlant, "CreatePlant.SetPlant logs" },
                }
            };
            Settings.Add(selected);

            speed = new FloatSetting("Time between Logs", 0.1f, 10, 1, 2);

            Settings.Add(speed);

            EndCategory();
        }

        // Acess Private methods
        private static List<string> CheatKeys = new List<string>();

        // Mod Logic

        private static float _time = 0;
        public override void OnUpdateActive()
        {
            if (Time.realtimeSinceStartup < _time+speed.Value) return;

            if (selected.IsSelected((int)Options.TheGameStatus))
            {
                DebugModeLogger.Msg("GameStatus: " + GameAPP.theGameStatus);
            }

            if (selected.IsSelected((int)Options.BoardTag))
            {
                if (!BoardInstanceIsNull)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Dumping boardTag Fields:");

                    // Grab all public, instance fields from the boardTag object
                    FieldInfo[] fields = board.boardTag.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                    foreach (FieldInfo field in fields)
                    {
                        object value = field.GetValue(board.boardTag);

                        sb.AppendLine($"   -> {field.Name}: {value}");
                    }
                    DebugModeLogger.Msg(sb.ToString());
                }
            }

            if (selected.IsSelected((int)Options.PlantList))
            {
                DebugModeLogger.Msg(
                    string.Join(
                        Environment.NewLine,
                        GameData.plantList.Select(kvp=>
                            $"PlantType: {kvp.thePlantType}  " +
                            $"Tile: ({kvp.thePlantColumn},{kvp.thePlantRow})  " +
                            $"Health: {kvp.thePlantHealth}"
                        )
                        )
                    );
            }

            if (selected.IsSelected((int)Options.ZombieList))
            {
                DebugModeLogger.Msg(
                    string.Join(
                        Environment.NewLine,
                        GameData.zombieList.Select(kvp =>
                            $"ZombieType: {kvp.theZombieType}  " +
                            $"Coordinate: ({kvp.theZombieRow},{kvp.transform.position.x})  " +
                            $"Health: {kvp.CurrentAllHealth}"
                        )
                        )
                    );
            }

            if (selected.IsSelected((int)Options.ZombieAnimations))
            {
                selected.Deselect((int)Options.ZombieAnimations);

                DebugModeLogger.Msg("Dumping All animations");

                var prefabs = GameAPP.resourcesManager?.zombiePrefabs;

                if (prefabs == null)
                {
                    DebugModeLogger.Error("GameAPP.resourcesManager.zombiePrefabs is null or not loaded yet!");
                    return;
                }

                foreach (var pair in prefabs)
                {
                    ZombieType typeKey = pair.Key;
                    GameObject prefab = pair.Value;

                    if (prefab == null) continue;

                    Animator anim = prefab.GetComponent<Animator>();
                    if (anim == null) continue;

                    RuntimeAnimatorController controller = anim.runtimeAnimatorController;
                    if (controller == null) continue;

                    DebugModeLogger.Msg($"\n[ZombieType: {typeKey}] Prefab: {prefab.name}");

                    HashSet<string> uniqueClips = new();

                    foreach (AnimationClip clip in controller.animationClips)
                    {
                        if (clip != null && uniqueClips.Add(clip.name))
                        {
                            DebugModeLogger.Msg($"  -> Clip: {clip.name}");
                        }
                    }
                }

                DebugModeLogger.Msg("\n=== Bulk Animation Dump Complete ===");


            }

            if (selected.IsSelected((int)Options.CheatKeys))
            {
                selected.Deselect( (int)Options.CheatKeys);

                foreach (var key in CheatKeys)
                {
                    DebugModeLogger.Msg($"Found keypair: {key.ToString()}");
                }

            }

            if (selected.IsSelected((int)Options.AllRecipes))
            {
                selected.Deselect((int)Options.AllRecipes);

                if (MixData._recipes != null)
                {
                    foreach (var keypair in MixData._recipes)
                    {
                        // Get the unmanaged memory pointer to the boxed ValueTuple object
                        IntPtr rawPtr = keypair.Key.Pointer;

                        // Offset 0x10 (16) = Item1, Offset 0x14 (20) = Item2
                        PlantType plant1 = (PlantType)Marshal.ReadInt32(rawPtr, 0x10);
                        PlantType plant2 = (PlantType)Marshal.ReadInt32(rawPtr, 0x14);

                        PlantType result = keypair.Value;

                        string name1 = Enum.GetName(typeof(PlantType), plant1) ?? plant1.ToString();
                        string name2 = Enum.GetName(typeof(PlantType), plant2) ?? plant2.ToString();
                        string nameResult = Enum.GetName(typeof(PlantType), result) ?? result.ToString();

                        DebugModeLogger.Msg($"Found recipe: {name1} ({(int)plant1}) + {name2} ({(int)plant2}) -> {nameResult} ({(int)result})");
                    }
                }

            }

            _time = Time.realtimeSinceStartup;
        }

        public override void OnEnable()
        {
            _time = Time.realtimeSinceStartup;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            GameAPPPatch.logged = false;
        }


        [HarmonyPatch(typeof(GameAPP))]
        public static class GameAPPPatch
        {
            public static bool logged = false;

            [HarmonyPatch(nameof(GameAPP.PlaySound), new Type[] { typeof(int), typeof(float), typeof(float) })]
            [HarmonyPostfix]
            public static void PlaySoundIntPatch(int theSoundID, float theVolume, float pitch)
            {
                if (instance == null || !instance.Active || logged ||
                    !instance.selected.IsSelected((int)Options.SoundPlayed)) return;

                SoundType soundType = (SoundType)theSoundID;

                DebugModeLogger.Msg($"Sound Played: SoundType={soundType} ({theSoundID})," +
                    $" Volume={theVolume}, Pitch={pitch}");
                logged = true;
            }
        }

        [HarmonyPatch(typeof(Plant))]
        public static class PlantPatch
        {
            [HarmonyPatch(nameof(Plant.Die))]
            [HarmonyPrefix]
            public static void DiePatch(Plant.DieReason reason, Plant __instance)
            {
                if (instance == null || !instance.Active || 
                    !instance.selected.IsSelected((int)Options.PlantDieReason)) return;

                DebugModeLogger.Msg($"Plant died: tile=({__instance.thePlantRow}," +
                    $"{__instance.thePlantColumn}), Reason: {reason.ToString()}");
            }
        }

        [HarmonyPatch(typeof(Zombie))]
        public static class ZombiePatch
        {
            [HarmonyPatch(nameof(Zombie.Die))]
            [HarmonyPrefix]
            public static void DiePatch(int reason, Zombie __instance)
            {
                if (instance == null || !instance.Active ||
                    !instance.selected.IsSelected((int)Options.ZombieDieReason)) return;

                DebugModeLogger.Msg($"Zombie died: Type:{__instance.theZombieType}, " +
                    $"tile=({__instance.theZombieRow},{__instance.Column}), Reason: {reason.ToString()}");
            }
        }

        [HarmonyPatch(typeof(CheatKey))]
        public static class CheatKeyPatch
        {
            [HarmonyPatch(nameof(CheatKey.Awake))]
            [HarmonyPostfix]
            public static void AwakePostfix(CheatKey __instance)
            {
                if (__instance == null || __instance.key == null) return;
                foreach (var key in __instance.CheatKeys)
                {
                    CheatKeys.Add(key.Key);
                }
            }
        }

        [HarmonyPatch(typeof(ParticleManager))]
        public static class ParticleManagerPatch
        {
            [HarmonyPatch(nameof(ParticleManager.SetParticle))]
            [HarmonyPostfix]
            public static void SetParticlePostfix(ParticleType particleType)
            {
                if (instance==null || !instance.Active || !instance.selected.IsSelected((int)Options.ParticleEmitted)) return;
                if (ParticleNameTranslated==null) DebugModeLogger.Msg($"Particle emitted: {particleType} ({(int)particleType})");
                else DebugModeLogger.Msg($"Particle emitted: {ParticleNameTranslated[(int)particleType]}");
            }
        }

        [HarmonyPatch(typeof(CreatePlant))]
        public static class CreatePlantPatch
        {
            [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.SetPlant))]
            [HarmonyPrefix]
            public static void SetPlantPrefix(int newColumn, int newRow, PlantType theSeedType,
                Plant targetPlant = null, Vector2 puffV = default(Vector2), bool isFreeSet = false,
                bool withEffect = true, Plant hidplant = null)
            {
                if (instance == null || !instance.Active || !instance.selected.IsSelected((int)Options.CreatePlant_SetPlant)) return;

                DebugModeLogger.Msg($"[CreatePlant.SetPlant] Triggered with parameters:\n" +
                    $"- newColumn: {newColumn}\n" +
                    $"- newRow: {newRow}\n" +
                    $"- theSeedType: {theSeedType} ({(int)theSeedType})\n" +

                    $"- targetPlant: {(targetPlant != null ? $"{targetPlant.thePlantType} ({targetPlant.thePlantRow},{targetPlant.thePlantColumn})" : "null")}\n" +

                    $"- puffV: X={puffV.x}, Y={puffV.y}\n" +
                    $"- isFreeSet: {isFreeSet}\n" +
                    $"- withEffect: {withEffect}\n" +
                    $"- hidplant: {(hidplant != null ? hidplant.name : "null")}");
            }

        }
    }
}
