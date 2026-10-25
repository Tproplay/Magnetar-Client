using HarmonyLib;
using System.Collections.Generic;
using Magnetar_Client.UI.Setting;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class SmallerZombies : Module
{
    public override string Name { get; set; } = "Smaller Zombies";
    public override string Description { get; set; } = "Changes the size of selected zombies.\n" +
        "Note: Changing size also affects zombie's speed.";
    public override string SearchHints { get; set; } = "";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Zombie;

    public static SmallerZombies instance;

    public FloatSetting sizeMultiplier;
    public MultiSelectSetting selectedZombies;

    public SmallerZombies()
    {
        instance = this;

        CreateCategory("General");

        sizeMultiplier = new FloatSetting("Scale multiplier", 0.5f, 2f, 0.75f, 3, 0f);

        selectedZombies = new MultiSelectSetting("Entities", typeof(ZombieType))
        {
            CustomNames = TranslateEnum(typeof(ZombieType)),
            Blacklist = Banned.ZombieTypeBanned,
        };
        selectedZombies.SelectAll(setDefault: true);

        AddSettings(sizeMultiplier, selectedZombies);

        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        selectedZombies.CustomNames = TranslateEnum(typeof(ZombieType));
    }

    public static Dictionary<Zombie, Vector3> originaltheZombieScale = new();

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        float currentMultiplier = sizeMultiplier.Value;

        foreach (var zombie in GameData.ZombieList)
        {
            if (zombie == null) continue;

            bool isSelected = selectedZombies.IsSelected((int)zombie.theZombieType);
            bool hasOriginal = originaltheZombieScale.TryGetValue(zombie, out Vector3 origScale);

            if (isSelected)
            {
                if (!hasOriginal)
                {
                    origScale = zombie.transform.localScale;
                    originaltheZombieScale[zombie] = origScale;
                }

                Vector3 targetScale = origScale * currentMultiplier;
                if (zombie.transform.localScale != targetScale)
                {
                    zombie.transform.localScale = targetScale;
                }
            }
            else if (hasOriginal)
            {
                zombie.transform.localScale = origScale;
                originaltheZombieScale.Remove(zombie);
            }
        }
    }

    public override void OnDisable()
    {
        foreach (var zombie in GameData.ZombieList)
        {
            if (zombie == null) continue;
            if (originaltheZombieScale.TryGetValue(zombie, out Vector3 origScale))
            {
                zombie.transform.localScale = origScale;
            }
        }

        originaltheZombieScale.Clear();
    }

    [HarmonyPatch(typeof(Zombie))]
    public static class ZombieCleanupPatch
    {
        [HarmonyPatch(nameof(Zombie.Die))]
        [HarmonyPostfix]
        public static void DiePostfix(Zombie __instance)
        {
            if (__instance != null) originaltheZombieScale.Remove(__instance);
        }

        [HarmonyPatch(nameof(Zombie.DestoryZombie))]
        [HarmonyPostfix]
        public static void DestroyPostfix(Zombie __instance)
        {
            if (__instance != null) originaltheZombieScale.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(Board))]
    public static class BoardCleanupPatch
    {
        [HarmonyPatch(nameof(Board.OnDestroy))]
        [HarmonyPostfix]
        public static void OnDestroyPostfix() => originaltheZombieScale.Clear();
    }
}
