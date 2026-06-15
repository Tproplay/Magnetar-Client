using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Utils;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;

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

        #region Zombie

        public static int TotalNumberOfZombiesSpawed = 0;
        public static int TotalNumberOfHypnotizedZombiesSpawed = 0;

        public static int TotalNumberOfZombiesKilled = 0;
        public static int TotalNumberOfHypnotizedZombiesKilled = 0;

        /// <summary>
        /// Total Damage Recieved By Zombies. Resets on starting a new level.
        /// </summary>
        public static long TotalDamagedRecievedByZombies = 0;
        public static long TotalDamagedRecievedByHypnotizedZombies = 0;

        [HarmonyPatch(typeof(Zombie))]
        private class ZombiePatch
        {
            [HarmonyPatch(nameof(Zombie.Start))]
            [HarmonyPostfix]
            public static void StartPostFix(Zombie __instance)
            {
                if (__instance == null || __instance.isIdle) return;

                if (__instance.isMindControlled)
                {
                    TotalNumberOfHypnotizedZombiesSpawed++;
                }
                else
                {
                    TotalNumberOfZombiesSpawed++;
                }
                
            }

            [HarmonyPatch(nameof(Zombie.Die))]
            [HarmonyPrefix]
            static void DiePatch(Zombie __instance)
            {
                if (__instance == null || !zombieList.Contains(__instance)) return;
                if (__instance.isMindControlled)
                    TotalNumberOfHypnotizedZombiesKilled++;
                else TotalNumberOfZombiesKilled++;
            }

            [HarmonyPatch(nameof(Zombie.TakeDamage))]
            [HarmonyPrefix]
            public static void TakeDamagePrefix(Zombie __instance, out float __state)
            {

                __state = __instance.CurrentAllHealth;
            }

            [HarmonyPatch(nameof(Zombie.TakeDamage))]
            [HarmonyPostfix]
            public static void TakeDamagePostfix(Zombie __instance, ref float __state)
            {
                float newHealth = __instance.CurrentAllHealth;

                float damageTaken = __state - newHealth;
                if (damageTaken > 0)
                {
                    if (__instance.isMindControlled)
                        TotalDamagedRecievedByZombies += (long)damageTaken;
                    else
                        TotalDamagedRecievedByHypnotizedZombies += (long)damageTaken;
                }
            }

            [HarmonyPatch(nameof(Zombie.SetMindControl))]
            [HarmonyPostfix]
            public static void SetMindControlPostfix(Zombie __instance)
            {
                if (__instance == null) return;

                TotalNumberOfHypnotizedZombiesSpawed++;
                TotalNumberOfZombiesKilled++;

            }

        }

        #endregion

        #region Plants
        public static int TotalNumberOfPlantsSpawned = 0;
        public static int TotalNumberOfPlantsKilled = 0;
        

        [HarmonyPatch(typeof(Plant))]
        private class PlantsKilledPatch
        {

            [HarmonyPatch(nameof(Plant.Start))]
            [HarmonyPostfix]
            public static void StartPostFix(Plant __instance)
            {
                if (__instance == null) return;

                TotalNumberOfPlantsSpawned += 1;
            }

            [HarmonyPatch(nameof(Plant.Die))]
            [HarmonyPrefix]
            static void DiePatch(Plant __instance)
            {
                if (__instance == null || !plantList.Contains(__instance)) return;
                TotalNumberOfPlantsKilled += 1;
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

        #region Sun

        public static int TotalAmountOfSunObtained = 0;
        public static int TotalAmountOfSunSpent = 0;

        private static int lastSunAmount = 0;
        [HarmonyPatch(typeof(Board))]
        private class SunUpdatePatch
        {
            [HarmonyPatch(nameof(Board.SunUpdate))]
            [HarmonyPostfix]
            public static void SunUpdatePostFix()
            {
                if (AppData.BoardInstanceIsNull) return;
                if (lastSunAmount == AppData.board.theSun) return;
                if (lastSunAmount < AppData.board.theSun)
                {
                    TotalAmountOfSunObtained += AppData.board.theSun - lastSunAmount;
                    lastSunAmount = AppData.board.theSun;
                }
                else
                {
                    TotalAmountOfSunSpent += lastSunAmount - AppData.board.theSun;
                    lastSunAmount = AppData.board.theSun;
                }
            }
        }

        #endregion

        #region Money

        public static int TotalAmountOfMoneyObtained = 0;
        public static int TotalAmountOfMoneySpent = 0;

        private static int lastMoneyAmount = 0;
        [HarmonyPatch(typeof(Board))]
        private class MoneyUpdatePatch
        {
            [HarmonyPatch(nameof(Board.Update))]
            [HarmonyPostfix]
            public static void UpdatePostFix()
            {
                if (AppData.BoardInstanceIsNull) return;
                if (lastMoneyAmount == AppData.board.theMoney) return;
                if (lastMoneyAmount < AppData.board.theMoney)
                {
                    TotalAmountOfMoneyObtained += AppData.board.theMoney - lastMoneyAmount;
                    lastMoneyAmount = AppData.board.theMoney;
                }
                else
                {
                    TotalAmountOfMoneySpent += lastMoneyAmount - AppData.board.theMoney;
                    lastMoneyAmount = AppData.board.theMoney;
                }
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

                TotalDamagedRecievedByZombies = 0;
                TotalNumberOfZombiesKilled = 0; TotalNumberOfHypnotizedZombiesKilled = 0; TotalNumberOfPlantsKilled = 0;
                TotalNumberOfZombiesSpawed = 0; TotalNumberOfHypnotizedZombiesSpawed = 0; TotalNumberOfPlantsSpawned = 0;

                TotalNumberOfBulletsSpawned = 0;

                TotalAmountOfSunObtained = 0; TotalAmountOfSunSpent = 0; lastSunAmount = AppData.board.theSun;
                TotalAmountOfMoneyObtained = 0; TotalAmountOfMoneySpent = 0; lastMoneyAmount = AppData.board.theMoney;
            }

            [HarmonyPatch(nameof(Board.Die))]
            [HarmonyPostfix]
            static void DiePostFix(Board __instance)
            {
                // Things to be reset at the end of a level
                plantList.Clear();
                zombieList.Clear();

            }
        }

        
        #endregion

        #region Get Level Name
        /// <summary>
        /// Get the Name of the level being played.
        /// </summary>
        public static string GetLevelName()
        {
            var ui = Il2Cpp.InGameUI.Instance;

            if (ui == null) return "Unknown";

            var tmpro = ui.LevelName1 != null ? ui.LevelName1 : (ui.LevelName2 != null ? ui.LevelName2 : ui.LevelName3);

            if (tmpro != null)
            {
                var textComponent = tmpro.GetComponent<Il2CppTMPro.TextMeshProUGUI>();

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
