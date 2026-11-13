using HarmonyLib;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class ScratchOffCustomizer : Module
{
    // Mod Info
    public override string Name { get; set; } = "Scratch-Off Hack";
    public override string Description { get; set; } =
        "Manipulates the Scratch-Off ticket gacha system: guarantees Jackpots, removes zombie hazards, " +
        "enables free row re-rolls, forces max ticket tiers, and allows custom plant injections.";
    public override string SearchHints { get; set; } = "scratchoffhack scratchticket scratchgacha jackpotguarantee scratchcheat" +
        " scratchreroll scratchmod ticketgacha scratchoffmod scratchmanager scratchprize scratchrewards scratchfix scratcheditor " +
        "scratchconfig scratchprizehack scratchtier scratchremover hazardremover";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    // Mod Data
    public static ScratchOffCustomizer instance;

    // Settings
    public BoolSetting GuaranteedJackpot;
    public BoolSetting NoZombieCells;
    public BoolSetting FreeRefreshes;
    public BoolSetting ForceMaxTier;
    public BoolSetting ForceCustomPlant;
    public EntitySelectSetting CustomPlantSetting;
    public IntSetting RewardMultiplier;

    private static PlantType _jackpotPlant = PlantType.Pumpiner;
    public static bool IsScratchSettling;

    public ScratchOffCustomizer()
    {
        instance = this;

        CreateCategory("Gacha Manipulation");

        GuaranteedJackpot = new BoolSetting("Guaranteed Jackpot", true);
        NoZombieCells = new BoolSetting("No Zombie Cells", true);
        FreeRefreshes = new BoolSetting("Free Row Refreshes", true);
        ForceMaxTier = new BoolSetting("Force Max Tier (Epic)", false);
        RewardMultiplier = new IntSetting("Card Reward Multiplier", 1, 10, 1);

        AddSettings(GuaranteedJackpot, NoZombieCells, FreeRefreshes, ForceMaxTier, RewardMultiplier);
        EndCategory();

        CreateCategory("Custom Plants", false);

        ForceCustomPlant = new BoolSetting("Force Custom Plant", false);
        CustomPlantSetting = new EntitySelectSetting("Allowed Plants", EntitySelectSetting.Plant)
        {
            CustomNames = TranslateEnum(typeof(PlantType)),
            Blacklist = Banned.PlantTypeBanned,
        };

        AddSettings(ForceCustomPlant, CustomPlantSetting);
        EndCategory();
    }

    // Mod Logic

    private static PlantType GetSelectedPlant()
    {
        if (instance?.CustomPlantSetting?.SelectedValues != null && instance.CustomPlantSetting.SelectedValues.Count > 0)
        {
            var list = new List<int>(instance.CustomPlantSetting.SelectedValues);
            int chosen = list[Random.Range(0, list.Count)];
            return (PlantType)chosen;
        }
        return PlantType.Nothing;
    }

    #region Harmony Patches

    [HarmonyPatch(typeof(ScratchOffMenu))]
    public static class ScratchOffMenuGachaPatch
    {
        [HarmonyPatch(nameof(ScratchOffMenu.SetLevel))]
        [HarmonyPrefix]
        private static void SetLevelPrefix(ref int level)
        {
            if (instance != null && instance.Active && instance.ForceMaxTier.Value)
            {
                level = 4;
            }
        }

        [HarmonyPatch(nameof(ScratchOffMenu.Refresh))]
        [HarmonyPrefix]
        private static void RefreshPrefix(ScratchOffMenu __instance)
        {
            if (instance == null || !instance.Active) return;

            if (instance.FreeRefreshes.Value)
            {
                __instance.cost = 0;
                __instance.refreshCount = 0;
            }
            else if (__instance.cost <= 0)
            {
                // If cost was previously zeroed by FreeRefreshes, restore the legitimate base cost
                int firstCost = __instance.firstCost > 0 ? __instance.firstCost : 200;
                __instance.cost = Mathf.Max(25, firstCost / 4);
            }
        }

        [HarmonyPatch(nameof(ScratchOffMenu.UpdateCell))]
        [HarmonyPrefix]
        private static bool UpdateCellPrefix(ScratchOffMenu __instance, ScratchCell cell, ref PlantType plantType)
        {
            if (instance == null || !instance.Active) return true;

            // 1. Check custom plant injection
            if (instance.ForceCustomPlant.Value)
            {
                PlantType custom = GetSelectedPlant();
                if (custom != PlantType.Nothing)
                {
                    plantType = custom;
                }
            }
            else if (instance.GuaranteedJackpot.Value)
            {
                // Force at least 4 matching cells in the first row
                int cellIndex = __instance.cells != null ? __instance.cells.IndexOf(cell) : -1;
                if (cellIndex >= 0 && cellIndex < 4)
                {
                    if (cellIndex == 0)
                    {
                        _jackpotPlant = plantType != PlantType.Nothing ? plantType : PlantType.Pumpiner;
                    }
                    plantType = _jackpotPlant;
                }
            }

            // 2. Prevent zombie hazard roll
            if (instance.NoZombieCells.Value)
            {
                cell?.SetPlantType(plantType);
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(ScratchOffMenu.ScratchDataSettle))]
        [HarmonyPrefix]
        private static void SettlePrefix()
        {
            IsScratchSettling = true;
        }

        [HarmonyPatch(nameof(ScratchOffMenu.ScratchDataSettle))]
        [HarmonyPostfix]
        private static void SettlePostfix()
        {
            IsScratchSettling = false;
        }
    }

    [HarmonyPatch(typeof(Lawnf))]
    public static class LawnfDroppedCardMultiplierPatch
    {
        private static bool isDuplicating;

        [HarmonyPatch(nameof(Lawnf.SetDroppedCard), new System.Type[] { typeof(Vector2), typeof(PlantType), typeof(int) })]
        [HarmonyPostfix]
        private static void SetDroppedCardPostfix(Vector2 position, PlantType theSeedType, int theSeedCost)
        {
            if (isDuplicating || !IsScratchSettling || instance == null || !instance.Active) return;

            int multiplier = instance.RewardMultiplier.Value;
            if (multiplier <= 1) return;

            isDuplicating = true;
            try
            {
                for (int i = 1; i < multiplier; i++)
                {
                    Vector2 offset = new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f));
                    Lawnf.SetDroppedCard(position + offset, theSeedType, theSeedCost);
                }
            }
            finally
            {
                isDuplicating = false;
            }
        }
    }

    #endregion
}
