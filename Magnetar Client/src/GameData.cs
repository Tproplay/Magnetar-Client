using HarmonyLib;
using System.Collections.Generic;
using System;
using Magnetar_Client.Api;
using Magnetar_Client.Core;

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
    private static HashSet<Plant> _plantSet = new();
    static int _currentPlantCheckFrame;
    static List<Plant> GetPlants()
    {
        int frame = UnityEngine.Time.frameCount;
        if (frame - _currentPlantCheckFrame < 30) return _plantList;
        _currentPlantCheckFrame = frame;

        for (int i = _plantList.Count - 1; i >= 0; i--)
        {
            Plant plant = _plantList[i];
            if (plant == null || plant.gameObject == null)
            {
                if (plant != null) _plantSet.Remove(plant);
                _plantList.RemoveAt(i);
            }
        }
        return _plantList;
    }

    [HarmonyPatch(typeof(Plant))]
    static class PlantListPatch
    {
        [HarmonyPatch(nameof(Plant.Start))]
        [HarmonyPostfix]
        public static void StartPostFix(Plant __instance)
        {
            if (_plantSet.Add(__instance))
            {
                _plantList.Add(__instance);
                OnPlantSpawned?.Invoke(__instance);
            }
        }

        [HarmonyPatch(nameof(Plant.Die))]
        [HarmonyPrefix]
        public static void DiePreFix(Plant __instance)
        {
            if (_plantSet.Remove(__instance))
            {
                _plantList.Remove(__instance);
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
            if (_plantSet.Add(__result))
            {
                _plantList.Add(__result);
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
    private static HashSet<Zombie> _zombieSet = new();
    static int _currentZombieCheckFrame;
    static List<Zombie> GetZombies()
    {
        int frame = UnityEngine.Time.frameCount;
        if (frame - _currentZombieCheckFrame < 30) return _zombieList;
        _currentZombieCheckFrame = frame;

        for (int i = _zombieList.Count - 1; i >= 0; i--)
        {
            Zombie zombie = _zombieList[i];
            if (zombie == null || zombie.gameObject == null)
            {
                if (zombie != null) _zombieSet.Remove(zombie);
                _zombieList.RemoveAt(i);
            }
        }
        return _zombieList;
    }

    [HarmonyPatch(typeof(Zombie))]
    private static class ZombieListPatch
    {
        [HarmonyPatch(nameof(Zombie.Start))]
        [HarmonyPostfix]
        public static void StartPostFix(Zombie __instance)
        {
            if (_zombieSet.Add(__instance))
            {
                _zombieList.Add(__instance);
                OnZombieSpawned?.Invoke(__instance);
            }
        }

        [HarmonyPatch(nameof(Zombie.Die))]
        [HarmonyPrefix]
        public static void DiePreFix(Zombie __instance)
        {
            if (_zombieSet.Remove(__instance))
            { 
                OnZombieDie?.Invoke(__instance);
            }
        }

        [HarmonyPatch(nameof(Zombie.DestoryZombie))]
        [HarmonyPrefix]
        public static void DestoryZombiePreFix(Zombie __instance)
        {
            if (_zombieSet.Remove(__instance))
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
            if (_zombieSet.Add(__result))
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
            _plantList.Clear();
            _plantSet.Clear();
            _zombieList.Clear();
            _zombieSet.Clear();
            _currentPlantCheckFrame = 0;
            _currentZombieCheckFrame = 0;
        }

        [HarmonyPatch(nameof(Board.Die))]
        [HarmonyPostfix]
        static void DiePostFix(Board __instance)
        {
            _plantList.Clear();
            _plantSet.Clear();
            _zombieList.Clear();
            _zombieSet.Clear();
            _currentPlantCheckFrame = 0;
            _currentZombieCheckFrame = 0;
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

    #region Status

    /// <summary>
    /// Represents the high-level game status detected by Magnetar Client.
    /// </summary>
    public enum Status
    {
        // Menu
        /// <summary>Navigating the main menu.</summary>
        Menu,
        /// <summary>Magnetar GUI is open on top of the main menu.</summary>
        Magnetar_GUI,

        // Game
        /// <summary>Interlude / transitioning between states or starting a level.</summary>
        Transition,
        /// <summary>Selecting seeds for the level.</summary>
        Selecting,
        /// <summary>Currently playing in a level.</summary>
        InGame,
        /// <summary>Roaming in the Zen / Big Garden.</summary>
        
        // Extra
        Big_Garden,
        /// <summary>Browsing the Almanac.</summary>
        InAlmanac,
    }

    private static Status _status = Status.Menu;

    /// <summary>
    /// Current high-level game status.
    /// </summary>
    public static Status CurrentStatus
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                Status previous = _status;
                _status = value;
                OnStatusChanged?.Invoke(previous, value);
            }
        }
    }

    /// <summary>
    /// Invoked when <see cref="CurrentStatus"/> changes (previousStatus, newStatus).
    /// </summary>
    public static Action<Status, Status> OnStatusChanged;

    /// <summary>
    /// Updates <see cref="CurrentStatus"/> based on the current game state and UI visibility.
    /// </summary>
    public static void UpdateStatus()
    {
        try
        {
            GameStatus Gamestatus = GameAPP.theGameStatus;

            if (Gamestatus == GameStatus.InGame && !BoardInstanceIsNull)
            {
                CurrentStatus = Status.InGame;
            }
            else if (InMainMenu && (ClientConfig.InGUI))
            {
                CurrentStatus = Status.Magnetar_GUI;
            }
            else if (InMainMenu)
            {
                CurrentStatus = Status.Menu;
            }
            else if (InAlmanac)
            {
                CurrentStatus = Status.InAlmanac;
            }
            else if (Gamestatus == GameStatus.InInterlude)
            {
                CurrentStatus = Status.Transition;
            }
            else if (Gamestatus == GameStatus.Selecting)
            {
                CurrentStatus = Status.Selecting;
            }
            else if (Gamestatus == GameStatus.BigGarden)
            {
                CurrentStatus = Status.Big_Garden;
            }
        }
        catch
        {
            // Ignore during early initialization or if game objects are invalid
        }
    }

    #endregion

    #region Service Lifecycle

    internal static void Init()
    {
        ServiceRegistry.Register(new GameDataService());
    }

    private class GameDataService : IClientService, IUpdatable
    {
        public string Name => "GameData";
        public int Priority => ServicePriority.Highest;

        public void OnUpdate() => UpdateStatus();
    }

    #endregion
}