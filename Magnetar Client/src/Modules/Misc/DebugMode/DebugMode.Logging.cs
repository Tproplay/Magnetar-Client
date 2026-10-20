using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Magnetar_Client.Utils.Magnetar_Logger;
using System;
using System.Runtime.InteropServices;
using Magnetar_Client.NEF;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public partial class DebugMode
{
    public override void OnUpdateActive()
    {
        if (Time.realtimeSinceStartup < _time + speed.Value) return;
        _time = Time.realtimeSinceStartup;

        if (selected.IsSelected((int)Options.TheGameStatus))
        {
            DebugModeLogger.Msg("GameStatus: " + GameAPP.theGameStatus);
        }

        if (selected.IsSelected((int)Options.BoardTag))
        {
            if (!GameData.BoardInstanceIsNull)
            {
                StringBuilder sb = new();
                sb.AppendLine("Dumping boardTag Fields:");

                FieldInfo[] fields = GameData.BoardInstance.boardTag.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object value = field.GetValue(GameData.BoardInstance.boardTag);
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
                    GameData.PlantList.Select(kvp =>
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
                    GameData.ZombieList.Select(kvp =>
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
            selected.Deselect((int)Options.CheatKeys);
            foreach (var key in CheatKeys)
            {
                DebugModeLogger.Msg($"Found cheat code: {key}");
            }
        }

        if (selected.IsSelected((int)Options.AllRecipes))
        {
            selected.Deselect((int)Options.AllRecipes);

            foreach (var recipe in NEFData.AddedRecipes)
            {
                if (recipe.IsSingle || recipe.IsTriple) continue;
                DebugModeLogger.Msg(
                    $"Found recipe: " +
                    $"{(PlantType)recipe.ParentA.Id} ({recipe.ParentA.Id}) + " +
                    $"{(PlantType)recipe.ParentB.Id} ({recipe.ParentB.Id}) " +
                    $"-> {(PlantType)recipe.Result.Id} ({recipe.Result.Id})");
            }

            if (MixData._recipes != null)
            {
                foreach (var keypair in MixData._recipes)
                {
                    IntPtr rawPtr = keypair.Key.Pointer;
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

        if (selected.IsSelected((int)Options.AllNodes))
        {
            selected.Deselect((int)Options.AllNodes);
            DumpEventNodes();
        }
    }
}