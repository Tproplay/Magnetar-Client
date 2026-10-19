using HarmonyLib;
using System.Collections.Generic;
using System;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace Magnetar_Client;

/// <summary>
/// Contains useful Game Stats like the plants place, zombies in the lawn, etc.
/// </summary>
public static class GameData
{
    #region PlantList
    /// <summary>
    /// Sorted List of the current active plants on the BoardInstance.
    /// </summary>
    public static List<Plant> PlantList => GetPlants();
    /// <summary>
    /// Invoked when a <see cref="Plant"/> is spawned/created
    /// </summary>
    public static Action<Plant> OnPlantSpawned;
    /// <summary>
    /// Invoked when a <see cref="Plant"/> is about to die, after it has been removed from <see cref="PlantList"/>.
    /// </summary>
    public static Action<Plant> OnPlantDie;


    private static List<Plant> _plantList = new();
    static int _currentPlantCheckFrame;
    static List<Plant> GetPlants()
    {
        if (_currentPlantCheckFrame == UnityEngine.Time.frameCount) return _plantList;
        else
        {
            _currentPlantCheckFrame = UnityEngine.Time.frameCount;

            for (int i = _plantList.Count - 1; i >= 0; i--)
            {
                Plant plant = _plantList[i];

                if (plant == null || plant.gameObject == null)
                {
                    _plantList.RemoveAt(i);
                }
            }
            return _plantList;
        }
    }

    [HarmonyPatch(typeof(Plant))]
    static class PlantListPatch
    {
        [HarmonyPatch(nameof(Plant.Start))]
        [HarmonyPostfix]
        public static void StartPostFix(Plant __instance)
        {
            if (!PlantList.Contains(__instance))
            {
                PlantList.Add(__instance);
                OnPlantSpawned?.Invoke(__instance);
            }
        }

        [HarmonyPatch(nameof(Plant.Die))]
        [HarmonyPrefix]
        public static void DiePreFix(Plant __instance)
        {
            if (PlantList.Remove(__instance))
            {
                OnPlantDie?.Invoke(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(CreatePlant))]
    private static class CreatePlantPatch
    {
        [HarmonyPatch(nameof(CreatePlant.SetPlant))]
        [HarmonyPostfix]
        public static void SetPlantPostfix(Plant __result)
        {
            if (!PlantList.Contains(__result))
            {
                PlantList.Add(__result);
                OnPlantSpawned?.Invoke(__result);
            }
        }
    }

    #endregion 

    #region ZombieList
    /// <summary>
    /// Sorted List of the current active (non-idle) zombies on the BoardInstance.
    /// </summary>
    public static List<Zombie> ZombieList => GetZombies();
    /// <summary>
    /// Invoked when a <see cref="Zombie"/> is spawned/created
    /// </summary>
    public static Action<Zombie> OnZombieSpawned;
    /// <summary>
    /// Invoked when a <see cref="Zombie"/> is about to die, after it has been removed from <see cref="ZombieList"/>.
    /// </summary>
    public static Action<Zombie> OnZombieDie;

    private static List<Zombie> _zombieList = new();
    static int _currentZombieCheckFrame;
    static List<Zombie> GetZombies()
    {
        if (_currentZombieCheckFrame == UnityEngine.Time.frameCount) return _zombieList;
        else
        {
            _currentZombieCheckFrame = UnityEngine.Time.frameCount;

            for (int i = _zombieList.Count - 1; i >= 0; i--)
            {
                Zombie zombie = _zombieList[i];

                if (zombie == null || zombie.gameObject == null)
                {
                    _zombieList.RemoveAt(i);
                }
            }
            return _zombieList;
        }
    }


    [HarmonyPatch(typeof(Zombie))]
    private static class ZombieListPatch
    {
        [HarmonyPatch(nameof(Zombie.Start))]
        [HarmonyPostfix]
        public static void StartPostFix(Zombie __instance)
        {
            if (!_zombieList.Contains(__instance))
            {
                _zombieList.Add(__instance);
                OnZombieSpawned?.Invoke(__instance);
            }
        }

        [HarmonyPatch(nameof(Zombie.Die))]
        [HarmonyPrefix]
        public static void DiePreFix(Zombie __instance)
        {
            if (_zombieList.Remove(__instance))
            { 
                OnZombieDie?.Invoke(__instance);
            }
        }

        [HarmonyPatch(nameof(Zombie.DestoryZombie))]
        [HarmonyPrefix]
        public static void DestoryZombiePreFix(Zombie __instance)
        {
            if (_zombieList.Remove(__instance))
            {
                OnZombieDie?.Invoke(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(CreateZombie))]
    private static class CreateZombiePatch
    {
        [HarmonyPatch(nameof(CreateZombie.SetZombie))]
        [HarmonyPostfix]
        public static void SetZombiePostfix(Zombie __result)
        {
            if (!_zombieList.Contains(__result))
            {
                _zombieList.Add(__result);
                OnZombieSpawned?.Invoke(__result);
            }
        }
    }

    #endregion

    [HarmonyPatch(typeof(Board))]
    static class BoardPatch
    {
        [HarmonyPatch(nameof(Board.Awake))]
        [HarmonyPostfix]
        static void AwakePostFix(Board __instance)
        {
            PlantList.Clear();
            _zombieList.Clear();
        }

        [HarmonyPatch(nameof(Board.Die))]
        [HarmonyPostfix]
        static void DiePostFix(Board __instance)
        {
            PlantList.Clear();
            _zombieList.Clear();

        }
    }

    #region Board Instance

    /// <summary>
    /// Instance of Board Active.
    /// </summary>
    public static Board BoardInstance;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static bool BoardInstanceIsNull = true;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    [HarmonyPatch(typeof(Board))]
    static class BoardInstancePatch
    {
        [HarmonyPatch(nameof(Board.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix(Board __instance)
        {
            GameData.BoardInstance = __instance;
            GameData.BoardInstanceIsNull = false;
        }

        [HarmonyPatch(nameof(Board.OnDestroy))]
        [HarmonyPrefix]
        static void OnDestroyPrefix(Board __instance)
        {
            if (GameData.BoardInstance != null && GameData.BoardInstance.Pointer == __instance.Pointer)
            {
                GameData.BoardInstance = null;
                GameData.BoardInstanceIsNull = true;
            }
        }
    }

    #endregion

    #region Wheel Barrow

    /// <summary>
    /// Instance of Wheel Barrow.
    /// </summary>
    public static Wheel WheelInstance;

    [HarmonyPatch(typeof(InGameTool))]
    static class WheelPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(InGameTool __instance)
        {
            if (__instance == null) return;

            var wheelInstance = __instance.TryCast<Wheel>();
            if (wheelInstance != null)
            {
                GameData.WheelInstance = wheelInstance;
            }
        }
    }

    #endregion

    /// <summary>
    /// Get the Name of the level being played. Returns Unknown if name was not found.
    /// </summary>
    public static string GetLevelName()
    {
        var ui = InGameUI.Instance;

        if (ui == null) return "Unknown";

        var tmpro = ui.LevelName1 != null ? ui.LevelName1 : (ui.LevelName2 != null ? ui.LevelName2 : ui.LevelName3);

        if (tmpro != null)
        {
            var textComponent = tmpro.GetComponent<TextMeshProUGUI>();

            return textComponent.GetParsedText();
        }

        return "Unknown";
    }

    /// <summary>
    /// Checks if the player is on the MainMenu
    /// </summary>
    public static bool InMainMenu
    {
        get
        {
            if (GameAPP.canvas == null) return true;
            return (GameAPP.canvas.Find("MainMenu(Clone)") != null ||
              GameAPP.canvas.Find("MainMenu_travel(Clone)") != null) && !InAlmanac;
        }
    }

    /// <summary>
    /// Checks if the player is checking the Almanac
    /// </summary>
    public static bool InAlmanac
    {
        get
        {
            if (GameAPP.canvasUp == null) return false;
            return GameAPP.canvasUp.Find("AlmanacMenu(Clone)") != null;
        }
    }
}