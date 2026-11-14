using HarmonyLib;
using Magnetar_Client.UI.Setting;
using static Magnetar_Client.GameData;
using System.Linq;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class NoChestStorageLimit : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Chest Storage Limit";
        public override string Description { get; set; } = "Removes the 54-slot capacity limit and stack size limits from Pumpkin Chests and Ender Chests.";
        public override string SearchHints { get; set; } = "nocheststoragelimit chestlimit nostoragelimit pumpinerlimit pumpkinchestlimit " +
            "infinitestorage unlimitedchest bypassstack bypassslot chestcapacity cheststorage uncapchest";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data
        public static NoChestStorageLimit instance;

        public BoolSetting BypassSlotLimit;
        public BoolSetting BypassStackLimit;
        public IntSetting CustomMaxStack;


        public NoChestStorageLimit()
        {
            instance = this;

            CreateCategory("General");

            BypassSlotLimit = new BoolSetting("Bypass 54 Slot Limit", true);
            BypassStackLimit = new BoolSetting("Bypass 64 Stack Limit", true);
            CustomMaxStack = new IntSetting("Max Stack Size", 999, 64, 99999);

            AddSettings(BypassSlotLimit, BypassStackLimit, CustomMaxStack);
            EndCategory();
        }

        // Mod Logic

        public override void OnEnable()
        {
            UpdateAllChestsCapacity(9999);
        }

        public override void OnDisable()
        {
            UpdateAllChestsCapacity(54);
        }

        private static void UpdateAllChestsCapacity(int capacity)
        {
            var chests = PlantList.OfType<Pumpiner>().ToList();
            if (chests != null)
            {
                foreach (var chest in chests)
                {
                    chest.MaxGridCount = capacity;
                }
            }
        }

        #region Harmony Patches

        /// <summary>
        /// Expands the initial capacity when a Pumpkin Chest initializes.
        /// </summary>
        [HarmonyPatch(typeof(Pumpiner), nameof(Pumpiner.Start))]
        public static class Pumpiner_Start_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Pumpiner __instance)
            {
                if (instance == null || !instance.Active || __instance == null) return;

                if (instance.BypassSlotLimit?.Value == true)
                {
                    __instance.MaxGridCount = 9999;
                }
            }
        }

        /// <summary>
        /// Bypasses both the stack size limit and the slot count limit in Pumpiner.Add.
        /// </summary>
        [HarmonyPatch(typeof(Pumpiner), nameof(Pumpiner.Add))]
        public static class Pumpiner_Add_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(
                Pumpiner __instance,
                PlantType thePlantType,
                ref bool ignoreStack,
                bool fromPumpiner,
                ref bool __result)
            {
                if (instance == null || !instance.Active || __instance == null) return true;

                // 1. Bypass stack limit
                if (instance.BypassStackLimit?.Value == true)
                {
                    ignoreStack = true;
                }

                // 2. Bypass slot limit
                if (instance.BypassSlotLimit?.Value == true)
                {
                    __instance.MaxGridCount = 9999;

                    var dict = __instance.PumpinerPlants;
                    if (dict != null)
                    {
                        int currentCount = 0;
                        dict.TryGetValue(thePlantType, out currentCount);

                        int maxStack = instance.BypassStackLimit?.Value == true
                            ? (instance.CustomMaxStack?.Value ?? 999)
                            : 64;

                        if (currentCount >= maxStack && !ignoreStack)
                        {
                            __result = false;
                            return false;
                        }

                        dict[thePlantType] = currentCount + 1;

                        if (__instance.contains != null && !__instance.contains.Contains(thePlantType))
                        {
                            __instance.contains.Add(thePlantType);
                        }

                        __result = true;
                        return false; // Skip original method to avoid MaxGridCount rejection
                    }
                }

                return true;
            }
        }

        #endregion
    }
}
