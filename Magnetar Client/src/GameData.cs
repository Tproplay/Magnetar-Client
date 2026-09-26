using HarmonyLib;
using System.Collections.Generic;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace Magnetar_Client.Game;

/// <summary>
/// Contains Useful App Stats, mainly used for code optimization
/// </summary>
public static class AppData
{
    public static Board BoardInstance;
    public static bool BoardInstanceIsNull = true;

    [HarmonyPatch(typeof(Board))]
    private static class BoardPatch
    {
        [HarmonyPatch(nameof(Board.Awake))]
        [HarmonyPostfix]
        public static void AwakePostfix(Board __instance)
        {
            BoardInstance = __instance;
            BoardInstanceIsNull = false;
        }

        [HarmonyPatch(nameof(Board.OnDestroy))]
        [HarmonyPrefix]
        public static void OnDestroyPrefix(Board __instance)
        {
            if (BoardInstance != null && BoardInstance.Pointer == __instance.Pointer)
            {
                BoardInstance = null;
                BoardInstanceIsNull = true;
            }
        }
    }

    public static Wheel WheelInstance;

    [HarmonyPatch(typeof(InGameTool))]
    public static class WheelPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPostfix(InGameTool __instance)
        {
            if (__instance == null) return;

            var wheelInstance = __instance.TryCast<Wheel>();
            if (wheelInstance != null)
            {
                WheelInstance = wheelInstance;
            }
        }
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
    private static class PlantListPatch
    {
        [HarmonyPatch(nameof(Plant.Start))]
        [HarmonyPostfix]
        public static void StartPostFix(Plant __instance)
        {
            if (AppData.BoardInstanceIsNull) return;

            if (!PlantList.Contains(__instance))
                PlantList.Add(__instance);
        }

        [HarmonyPatch(nameof(Plant.Die))]
        [HarmonyPrefix]
        public static void DiePreFix(Plant __instance)
        {
            if (AppData.BoardInstanceIsNull) return;

            PlantList.Remove(__instance);
            
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
                PlantList.Add(__result);
        }
    }

    #endregion 

    #region ZombieList
    /// <summary>
    /// Sorted List of the current active (non-idle) zombies on the BoardInstance.
    /// </summary>
    public static List<Zombie> ZombieList => GetZombies();
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
            if (AppData.BoardInstanceIsNull || __instance.gameObject==null ||
                __instance.theZombieType==ZombieType.Nothing) return;

            if (!_zombieList.Contains(__instance))
                _zombieList.Add(__instance);
        }

        [HarmonyPatch(nameof(Zombie.Die))]
        [HarmonyPrefix]
        public static void DiePreFix(Zombie __instance)
        {
            if (_zombieList.Contains(__instance))
            { 
                _zombieList.Remove(__instance);
                Hypno_Zombies_Killed++;
            }
        }

        [HarmonyPatch(nameof(Zombie.DestoryZombie))]
        [HarmonyPrefix]
        public static void DestoryZombiePreFix(Zombie __instance)
        {
            if (_zombieList.Contains(__instance))
            {
                _zombieList.Remove(__instance);
                Hypno_Zombies_Killed++;
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
            if (!ZombieList.Contains(__result))
                ZombieList.Add(__result);

        }
    }

    #endregion

    #region Zombies

    public static int Hypno_Zombies_Spawned = 0;
    public static int Hypno_Zombies_Killed = 0;

    [HarmonyPatch(typeof(Zombie))]
    private static class ZombieStatsPatch
    {
        [HarmonyPatch(nameof(Zombie.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(Zombie __instance)
        {
            if (__instance == null || !__instance.isMindControlled) return;
            Hypno_Zombies_Spawned++;
        }

        [HarmonyPatch(nameof(Zombie.SetMindControl))]
        [HarmonyPostfix]
        public static void SetMindControlPostfix(Zombie __instance)
        {
            if (__instance == null) return;
            Hypno_Zombies_Spawned++;
        }
    }

    #endregion

    #region Bullets

    public static long TotalNumberOfBulletsSpawned = 0;
    [HarmonyPatch(typeof(Bullet))]
    private class BulletPatch
    {
        [HarmonyPatch(nameof(Bullet.InitData))]
        [HarmonyPostfix]
        public static void InitDataPatch(Bullet __instance)
        {
            if (__instance == null) return;

            TotalNumberOfBulletsSpawned++;
        }
    }
    


    #endregion

    #region Reset Values
    [HarmonyPatch(typeof(Board))]
    public class BoardPatch
    {
        [HarmonyPatch(nameof(Board.Awake))]
        [HarmonyPostfix]
        static void AwakePostFix(Board __instance)
        {
            // Things to be reset at the start of a level
            PlantList.Clear();
            _zombieList.Clear();

            TotalNumberOfBulletsSpawned = 0;

            Hypno_Zombies_Spawned = 0; Hypno_Zombies_Killed = 0;
            
        }

        [HarmonyPatch(nameof(Board.Die))]
        [HarmonyPostfix]
        static void DiePostFix(Board __instance)
        {
            // Things to be reset at the end of a level
            PlantList.Clear();
            _zombieList.Clear();

        }
    }

    
    #endregion

    #region Get Level Name
    /// <summary>
    /// Get the Name of the level being played.
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
    #endregion
}
