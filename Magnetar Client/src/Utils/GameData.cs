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
        /// <summary>
        /// Sorted List of the current active plants on the board.
        /// </summary>
        public static List<Plant> plantList = new List<Plant>();
        /// <summary>
        /// Sorted List of the current active (non-idle) zombies on the board.
        /// </summary>
        public static List<Zombie> zombieList = new List<Zombie>();

        /// <summary>
        /// Total Damage Recieved By Zombies. Resets on starting a new level.
        /// </summary>
        public static long ZombieDamage = 0;
        /// <summary>
        /// Get the Name of the level being played.
        /// </summary>
        public static string GetLevelName()
        {
            var ui = Il2Cpp.InGameUI.Instance;

            if (ui == null) return "Unknown";

            var tmpro  = ui.LevelName1 != null ? ui.LevelName1 : (ui.LevelName2 != null ? ui.LevelName2 :ui.LevelName3); 

            if (tmpro != null)
            {
                string levelName = tmpro.GetComponent<Il2CppTMPro.TextMeshProUGUI>().text;

                string cleanName = Il2Cpp.InGameText.RemoveRichTextTags(levelName);

                return cleanName;
            }

            return "Unknown";
        }
    }

    #region GameData

    [HarmonyPatch(typeof(Plant))]
    public class _PlantPatch
    {
        [HarmonyPatch(nameof(Plant.Start))]
        [HarmonyPostfix]
        public static void StartPostFix(Plant __instance)
        {
            if (Board.Instance != null && __instance != null)
            {
                GameData.plantList.Add(__instance);
            }
        }

        [HarmonyPatch(nameof(Plant.OnDestroy))]
        [HarmonyPostfix]
        public static void OnDestroyPostFix(Plant __instance)
        {
            if (Board.Instance != null && __instance != null)
            {
                GameData.plantList.Remove(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Zombie))]
    public class _ZombiePatch
    {
        [HarmonyPatch(nameof(Zombie.Start))]
        [HarmonyPostfix]
        public static void StartPostFix(Zombie __instance)
        {
            if (Board.Instance != null && __instance != null && !__instance.isIdle)
            {
                GameData.zombieList.Add(__instance);
            }
        }
        [HarmonyPatch(nameof(Zombie.Die))]
        [HarmonyPostfix]
        public static void DiePostFix(Zombie __instance)
        {
            if (Board.Instance != null && __instance != null)
            {
                GameData.zombieList.Remove(__instance);
            }
        }


        private static float _current_theHealth = 0;
        private static float _current_firstArmorHealth = 0;
        private static float _current_secondArmorHealth = 0;

        [HarmonyPatch(nameof(Zombie.TakeDamage))]
        [HarmonyPrefix]
        public static void TakeDamagePrefix(Zombie __instance, DmgType theDamageType, int theDamage, PlantType reportType, bool fix)
        {
            _current_theHealth = __instance.theHealth;
            _current_firstArmorHealth = __instance.theFirstArmorHealth;
            _current_secondArmorHealth = __instance.theSecondArmorHealth;
        }

        [HarmonyPatch(nameof(Zombie.TakeDamage))]
        [HarmonyPostfix]
        public static void TakeDamagePostfix(Zombie __instance, DmgType theDamageType, int theDamage, PlantType reportType, bool fix)
        {
            GameData.ZombieDamage += (long) (
                _current_theHealth          - __instance.theHealth              +
                _current_firstArmorHealth   - __instance.theFirstArmorHealth    +
                _current_secondArmorHealth  - __instance.theSecondArmorHealth
                );
        }
    }

    [HarmonyPatch(typeof(Board))]
    public class _BoardPatch
    {
        [HarmonyPatch(nameof(Board.Awake))]
        [HarmonyPostfix]
        public static void AwakePostFix(Board __instance)
        {
            GameData.plantList.Clear();
            GameData.zombieList.Clear();

            GameData.ZombieDamage = 0; 
        }

        [HarmonyPatch(nameof(Board.Die))]
        [HarmonyPostfix]
        public static void DiePostFix(Board __instance)
        {
            GameData.plantList.Clear();
            GameData.zombieList.Clear();

        }
    }

    #endregion


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
