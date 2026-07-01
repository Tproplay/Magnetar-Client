using HarmonyLib;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace Magnetar_Client.Game
{
    /// <summary>
    /// Contains Useful App Stats, mainly used for code optimization
    /// </summary>
    public static class AppData
    {
        public static Board board;
        public static bool BoardInstanceIsNull = true;

        [HarmonyPatch(typeof(Board))]
        private static class BoardPatch
        {
            [HarmonyPatch(nameof(Board.Awake))]
            [HarmonyPostfix]
            public static void AwakePostfix(Board __instance)
            {
                board = __instance;
                BoardInstanceIsNull = false;
            }

            [HarmonyPatch(nameof(Board.OnDestroy))]
            [HarmonyPostfix]
            public static void OnDestroyPostfix(Board __instance)
            {
                if (board != null && board.Pointer == __instance.Pointer)
                {
                    board = null;
                    BoardInstanceIsNull = true;
                }
            }
        }

        /*
        public static Wheel wheel;

        [HarmonyPatch(typeof(InGameTool))]
        public static class WheelPatch
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            public static void StartPostfix(InGameTool __instance)
            {
                if (__instance == null) return;

                // We safely cast it to Wheel inside the method instead
                var wheelInstance = __instance.TryCast<Wheel>();
                if (wheelInstance != null)
                {
                    wheel = wheelInstance;
                }
            }
        }
        */
    }



    /// <summary>
    /// Contains useful Game Stats like the plants place, zombies in the lawn, etc.
    /// </summary>
    public static class GameData
    {
        #region PlantList
        /// <summary>
        /// Sorted List of the current active plants on the board.
        /// </summary>
        public static List<Plant> plantList = new List<Plant>();


        [HarmonyPatch(typeof(Plant))]
        private class plantListPatch
        {
            [HarmonyPatch(nameof(Plant.Start))]
            [HarmonyPostfix]
            public static void StartPostFix(Plant __instance)
            {
                if (AppData.BoardInstanceIsNull) return;

                if (!plantList.Contains(__instance))
                    plantList.Add(__instance);
            }

            [HarmonyPatch(nameof(Plant.Die))]
            [HarmonyPostfix]
            public static void DiePostFix(Plant __instance)
            {
                if (AppData.BoardInstanceIsNull) return;

                if (plantList.Contains(__instance))
                    plantList.Remove(__instance);
                
            }
        }

        #endregion 

        #region ZombieList
        /// <summary>
        /// Sorted List of the current active (non-idle) zombies on the board.
        /// </summary>
        public static List<Zombie> zombieList => GetZombies();
        private static List<Zombie> _zombieList = new List<Zombie>();
        static int _currentFrame;
        static List<Zombie> GetZombies()
        {
            if (_currentFrame == UnityEngine.Time.frameCount) return _zombieList;
            else
            {
                _currentFrame = UnityEngine.Time.frameCount;

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
        private class ZombieListPatch
        {
            [HarmonyPatch(nameof(Zombie.Start))]
            [HarmonyPostfix]
            public static void StartPostFix(Zombie __instance)
            {
                if (AppData.BoardInstanceIsNull || __instance.gameObject==null|| __instance.isIdle ||
                    __instance.theZombieType==ZombieType.Nothing) return;
                
                if (!_zombieList.Contains(__instance))
                    _zombieList.Add(__instance);
            }

            [HarmonyPatch(nameof(Zombie.Die))]
            [HarmonyPrefix]
            public static void DiePreFix(Zombie __instance)
            {
                if (_zombieList.Contains(__instance))
                    _zombieList.Remove(__instance);
            }

            [HarmonyPatch(nameof(Zombie.DestoryZombie))]
            [HarmonyPrefix]
            public static void DestoryZombiePreFix(Zombie __instance)
            {
                if (_zombieList.Contains(__instance))
                    _zombieList.Remove(__instance);
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

            [HarmonyPatch(nameof(Zombie.Die))]
            [HarmonyPrefix]
            public static void DiePrefix(Zombie __instance)
            {
                if (__instance == null || !__instance.isMindControlled) return;
                Hypno_Zombies_Killed++;
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
                plantList.Clear();
                _zombieList.Clear();

                TotalNumberOfBulletsSpawned = 0;

                Hypno_Zombies_Spawned = 0; Hypno_Zombies_Killed = 0;
                
            }

            [HarmonyPatch(nameof(Board.Die))]
            [HarmonyPostfix]
            static void DiePostFix(Board __instance)
            {
                // Things to be reset at the end of a level
                plantList.Clear();
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


    public static class AlmanacData
    {
        public static PlantType plantTypeSelected = PlantType.Nothing;
        public static ZombieType zombieTypeSelected = ZombieType.Nothing;

        public static System.Type latestSelectedCardType = null;
    }


    #region AlmanacData

    [HarmonyPatch(typeof(AlmanacPlantMenu))]
    public static class _AlmanacPlantMenu_SelectCard_Patch
    {
        [HarmonyPatch(nameof(AlmanacPlantMenu.SelectCard))]
        [HarmonyPrefix]
        public static void Prefix(AlmanacCardUI card)
        {
            if (card == null) return;
            AlmanacData.plantTypeSelected = card.PlantType;
            AlmanacData.latestSelectedCardType = typeof(PlantType);
        }

    }

    [HarmonyPatch(typeof(AlmanacZombieMenu))]
    public static class _AlmanacZombieMenu_SelectCard_Patch
    {
        [HarmonyPatch(nameof(AlmanacZombieMenu.SelectCard))]
        [HarmonyPrefix]
        public static void Prefix(AlmanacCardUI card)
        {
            if (card == null) return;
            AlmanacData.zombieTypeSelected = card.ZombieType;
            AlmanacData.latestSelectedCardType = typeof(ZombieType);
        }

    }

    #endregion
}
