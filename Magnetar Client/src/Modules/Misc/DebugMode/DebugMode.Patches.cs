using System;
using UnityEngine;
using HarmonyLib;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public partial class DebugMode
{
    [HarmonyPatch(typeof(GameAPP))]
    public static class GameAPPPatch
    {
        public static bool logged;

        [HarmonyPatch(nameof(GameAPP.PlaySound), new Type[] { typeof(int), typeof(float), typeof(float) })]
        [HarmonyPostfix]
        public static void PlaySoundIntPatch(int theSoundID, float theVolume, float pitch)
        {
            if (instance == null || !instance.Active || logged ||
                !instance.selected.IsSelected((int)Options.SoundPlayed)) return;

            SoundType soundType = (SoundType)theSoundID;
            DebugModeLogger.Msg($"Sound Played: SoundType={soundType} ({theSoundID}), Volume={theVolume}, Pitch={pitch}");
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

            DebugModeLogger.Msg($"Plant died: tile=({__instance.thePlantRow},{__instance.thePlantColumn}), Reason: {reason}");
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

            DebugModeLogger.Msg($"Zombie died: Type:{__instance.theZombieType}, tile=({__instance.theZombieRow},{__instance.Column}), Reason: {reason}");
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
            if (instance == null || !instance.Active || !instance.selected.IsSelected((int)Options.ParticleEmitted)) return;

            if (ParticleNameTranslated == null)
                DebugModeLogger.Msg($"Particle emitted: {particleType} ({(int)particleType})");
            else
                DebugModeLogger.Msg($"Particle emitted: {ParticleNameTranslated[(int)particleType]}");
        }
    }

    [HarmonyPatch(typeof(CreatePlant))]
    public static class CreatePlantPatch
    {
        [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.SetPlant))]
        [HarmonyPrefix]
        public static void SetPlantPrefix(int newColumn, int newRow, PlantType theSeedType,
            Plant targetPlant = null, Vector2 puffV = default, bool isFreeSet = false,
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