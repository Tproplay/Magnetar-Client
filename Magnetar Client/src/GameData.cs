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

    #region Status

    /// <summary>
    /// Helper that is used to check for the current game status.
    /// </summary>
    public static class StatusCheck
    {
        #region Menu
        /// <summary>
        /// Checks if the player is in the Normal MainMenu
        /// </summary>
        public static bool InMainMenuNormal
        {
            get
            {
                if (GameAPP.canvas == null) return true;
                return (GameAPP.canvas.Find("MainMenu(Clone)") != null);
            }
        }
        /// <summary>
        /// Checks if the player is specifically on the Normal MainMenu
        /// </summary>
        public static bool OnMainMenuNormal => InMainMenuNormal && !InAlmanac;

        /// <summary>
        /// Checks if the player is in the Odyssey MainMenu
        /// </summary>
        public static bool InMainMenuTravel
        {
            get
            {
                if (GameAPP.canvas == null) return true;
                return (GameAPP.canvas.Find("MainMenu_travel(Clone)") != null);
            }
        }
        /// <summary>
        /// Checks if the player is specifically on the Odyssey MainMenu
        /// </summary>
        public static bool OnMainMenuTravel => InMainMenuTravel && !InAlmanac;

        /// <summary>
        /// Checks if the player is in the MainMenu (both Normal and Odyssey)
        /// </summary>
        public static bool InMainMenu => InMainMenuNormal || InMainMenuTravel;

        /// <summary>
        /// Checks if the player is specifically on the MainMenu (both Normal and Odyssey)
        /// </summary>
        public static bool OnMainMenu => OnMainMenuNormal || OnMainMenuTravel;

        #region Menus Normal

        // MainMenu is destroyed if the player is in these menus

        /// <summary>
        /// Checks if the player is in the Challenges menu of the Normal MainMenu
        /// </summary>
        public static bool InChallenge
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("ChallengeMenu(Clone)") != null;
            }
        }

        /// <summary>
        /// Checks if the player is in the Super Level Editor menu of the Normal MainMenu
        /// </summary>
        public static bool InCustomLevelEditor
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return GameAPP.canvasUp.Find("CustomMenu(Clone)") != null;
            }
        }

        /// <summary>
        /// Checks if the player is in the I Zombie Challenges menu of the Normal MainMenu
        /// </summary>
        public static bool InIZMenu
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("IZMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player is in the Survival Challenges menu of the Normal MainMenu
        /// </summary>
        public static bool InSurvivalMenu
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("SurvivalMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player is in the Fusion Showcase menu of the Normal MainMenu
        /// </summary>
        public static bool InExploreMenu
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("ExploreMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player is in the Recommended Levels menu of the Normal MainMenu
        /// </summary>
        public static bool InRecommendedMenu
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return GameAPP.canvasUp.Find("HotLevelMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player is in the Harvest Mode menu of the Normal MainMenu
        /// </summary>
        public static bool InTreasureMenu
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("TreasureMainMenu(Clone)") != null;
            }
        }

        #endregion

        #region Menus Travel

        // MainMenu Travel is destroyed if the player is in these menus

        /// <summary>
        /// Checks if the player is in the Odyssey Mode Survival menu of the Odyssey MainMenu
        /// </summary>
        public static bool InTravelSurvival
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("TravelMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player is in the Adventure menu of the Odyssey MainMenu
        /// </summary>
        public static bool InTravelAdventure
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("TravelAdvantureMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player is in the Minigame menu of the Odyssey MainMenu
        /// </summary>
        public static bool InTravelMinigame
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("TravelGameMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player is in the Challenge menu of the Odyssey MainMenu
        /// </summary>
        public static bool InTravelChallenge
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("TravelChallengeMenu(Clone)") != null;
            }
        }

        #endregion

        #region Settings

        // Main Menu is not destroyed in these
        /// <summary>
        /// Checks if the player is in the Help section menu of the Normal MainMenu
        /// </summary>
        public static bool InHelpMenu
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return GameAPP.canvasUp.Find("Thanks(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player is in the MainMenu Settings window
        /// </summary>
        public static bool InMenuSettingMenu
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return GameAPP.canvasUp.Find("PauseMenu_mainMenu(Clone)") != null;
            }
        }

        /// <summary>
        /// Checks if the player is in the Advanced Settings window
        /// </summary>
        public static bool InAdvancedSettings
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return GameAPP.canvasUp.Find("OptionMenu(Clone)") != null;
            }
        }

        #endregion

        #endregion

        #region Alamanc
        /// <summary>
        /// Checks if the player is checking the Almanac
        /// </summary>
        public static bool InAlmanac
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                // Remains active if OnAlamacPlant/Zombie is active
                return GameAPP.canvasUp.Find("AlmanacMenu(Clone)") != null;
            }
        }

        /// <summary>
        /// Checks if the player on Almanac Plant
        /// </summary>
        public static bool OnAlmanacPlant
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return GameAPP.canvasUp.Find("AlmanacPlantMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player on Almanac Zombie
        /// </summary>
        public static bool OnAlmanacZombie
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return GameAPP.canvasUp.Find("AlmanacZombieMenu(Clone)") != null;
            }
        }
        /// <summary>
        /// Checks if the player on Almanac Shooting
        /// </summary>
        public static bool OnAlmanacShooting
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return GameAPP.canvasUp.Find("AlmanacShootingMenu(Clone)") != null;
            }
        }
        #endregion

        #region ZenGarden

        // MainMenu is destroyed if the player is in Zen Garden menus

        /// <summary>
        /// Checks if the player is in the ZenGarden
        /// </summary>
        public static bool InZenGarden
        {
            get
            {
                if (GameAPP.canvas == null) return false;
                return GameAPP.canvas.Find("GardenUI(Clone)") != null;
            }
        }

        /// <summary>
        /// Checks if the player is in the Big ZenGarden
        /// </summary>
        public static bool InBigZenGarden => Garden.Instance != null;

        #endregion

        #region Game

        /// <summary>
        /// Checks if the player is in the Level enter / Map switch in odyssey mode translation
        /// </summary>
        public static bool OnGameEnterAnimation => GameAPP.theGameStatus == GameStatus.InInterlude;

        /// <summary>
        /// Checks if the player is in a level
        /// </summary>
        public static bool InGameLevel => !BoardInstanceIsNull;

        /// <summary>
        /// Checks if the player is in a level's seed selection
        /// </summary>
        public static bool InGamePickingSeeds => InGameLevel && GameAPP.theGameStatus == GameStatus.Selecting;

        /// <summary>
        /// Checks if the player is in a level and has paused the game
        /// </summary>
        public static bool InGamePaused
        {
            get
            {
                if (GameAPP.canvasUp == null) return false;
                return InGameLevel && GameAPP.canvasUp.Find("PauseMenu(Clone)") != null;
            }
        }

        #endregion
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

        public void OnUpdate() { }
    }

    #endregion
}