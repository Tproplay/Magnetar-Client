using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Utils;
using System.Collections.Generic;
namespace Magnetar_Client.Game
{
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
        public class plantListPatch
        {
            [HarmonyPatch(nameof(Plant.Start))]
            [HarmonyPostfix]
            public static void StartPostFix(Plant __instance)
            {
                if (Board.Instance == null || __instance == null) return;

                if (!plantList.Contains(__instance))
                    plantList.Add(__instance);
            }

            [HarmonyPatch(nameof(Plant.OnDestroy))]
            [HarmonyPostfix]
            public static void OnDestroyPostFix(Plant __instance)
            {
                if (Board.Instance == null || __instance == null) return;

                if (plantList.Contains(__instance))
                    plantList.Remove(__instance);
                
            }
        }

        #endregion 

        #region ZombieList
        /// <summary>
        /// Sorted List of the current active (non-idle) zombies on the board.
        /// </summary>
        public static List<Zombie> zombieList = new List<Zombie>();

        [HarmonyPatch(typeof(Zombie))]
        public class ZombieListPatch
        {
            [HarmonyPatch(nameof(Zombie.Start))]
            [HarmonyPostfix]
            public static void StartPostFix(Zombie __instance)
            {
                if (Board.Instance == null || __instance == null || __instance.isIdle) return;
                
                if (!zombieList.Contains(__instance))
                    zombieList.Add(__instance);
            }

            [HarmonyPatch(nameof(Zombie.Die))]
            [HarmonyPostfix]
            public static void DiePostFix(Zombie __instance)
            {
                if (Board.Instance == null || __instance == null || __instance.isIdle) return;

                if (zombieList.Contains(__instance))
                    zombieList.Remove(__instance);
            }
        }

        #endregion

        #region Zombie Damage
        /// <summary>
        /// Total Damage Recieved By Zombies. Resets on starting a new level.
        /// </summary>
        public static long TotalDamagedRecievedByZombies = 0;

        [HarmonyPatch(typeof(Zombie))]
        public class ZombieDamagePatch
        {

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
                    TotalDamagedRecievedByZombies += (long)damageTaken;
                }
            }
        }

        #endregion

        #region Plants Killed
        public static int TotalNumberOfPlantsKilled = 0;
        
        [HarmonyPatch(typeof(Plant))]
        private class PlantsKilledPatch
        {
            [HarmonyPatch(nameof(Plant.Die))]
            [HarmonyPrefix]
            static void DiePatch(Plant __instance)
            {
                if (__instance == null || !plantList.Contains(__instance)) return;
                TotalNumberOfPlantsKilled += 1;
            }
        }
        #endregion

        #region Zombies Killed
        public static int TotalNumberOfZombiesKilled = 0;

        [HarmonyPatch(typeof(Zombie))]
        private class ZombiesKilledPatch
        {
            [HarmonyPatch(nameof(Zombie.Die))]
            [HarmonyPrefix]
            static void DiePatch(Zombie __instance)
            {
                if (__instance == null || !zombieList.Contains(__instance)) return;
                TotalNumberOfZombiesKilled += 1;
            }
        }
        #endregion

        #region Plants Spawned
        public static int TotalNumberOfPlantsSpawned = 0;

        [HarmonyPatch(typeof(Plant))]
        private class PlantsSpawnedPatch
        {
            [HarmonyPatch(nameof(Plant.Start))]
            [HarmonyPostfix]
            public static void StartPostFix(Plant __instance)
            {
                if (__instance == null) return;

                TotalNumberOfPlantsSpawned += 1;
            }
        }
        #endregion

        #region Zombies Spawned
        public static int TotalNumberOfZombiesSpawed = 0;

        [HarmonyPatch(typeof(Zombie))]
        private class ZombiesSpawedPatch
        {
            [HarmonyPatch(nameof(Zombie.Start))]
            [HarmonyPostfix]
            public static void StartPostFix(Zombie __instance)
            {
                if ( __instance == null || __instance.isIdle) return;
                
                TotalNumberOfZombiesSpawed += 1;
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
                plantList.Clear();
                zombieList.Clear();

                TotalDamagedRecievedByZombies = 0;
                TotalNumberOfZombiesKilled = 0; TotalNumberOfPlantsKilled = 0;
                TotalNumberOfZombiesSpawed = 0; TotalNumberOfPlantsSpawned = 0;
            }

            [HarmonyPatch(nameof(Board.Die))]
            [HarmonyPostfix]
            static void DiePostFix(Board __instance)
            {
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
                string levelName = tmpro.GetComponent<Il2CppTMPro.TextMeshProUGUI>().text;

                string cleanName = Il2Cpp.InGameText.RemoveRichTextTags(levelName);

                return cleanName;
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
