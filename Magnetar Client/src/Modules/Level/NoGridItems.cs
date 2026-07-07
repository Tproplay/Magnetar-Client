using Magnetar_Client.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Utils.Translator;
using static Magnetar_Client.Utils.Magnetar_Logger;
using HarmonyLib;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class NoCraters : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Craters";
        public override string Description { get; set; } = "Removes all the craters while the module is active.";
        public override string SearchHints { get; set; } = "nocraters removecraters craterremoval deletecraters" +
            " nocrater craterless clearcraters craterclear anti-crater craterfix nocratermod craterblocker nocraeters " +
            "nocraetors nocratars nocreaters nocreators nocratres nocraterss cratereraser craterslayer cratereraser " +
            "craterterminator groundfix repairground smoothground cratergone craterhide hidecraters craterdisappear " +
            "cratercancel craterbypass craternull";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static NoCraters instance;

        public BoolSetting AutoTurnOff;

        public NoCraters()
        {
            instance = this;

            CreateCategory("Extra");

            AutoTurnOff = new BoolSetting("Auto Turn Off", false);
            Settings.Add(AutoTurnOff);

            EndCategory();
        }

        // Mod Logic

        public static float deltaTime = 0;
        public override void OnUpdateActive()
        {
            // Handle auto turn off
            if (AutoTurnOff.Value)
            {
                deltaTime += Time.deltaTime;
                if (deltaTime > 0.3f)
                {
                    Active = false;
                    deltaTime = 0;
                }
            }
        }
        public override void OnEnable()
        {
            if (BoardInstanceIsNull || board.griditemArray == null) return;

            for (int i = board.griditemArray.Count - 1; i >= 0; i--)
            {
                GridItem item = board.griditemArray[i];
                if (item == null) continue;
                if (item.theItemType == GridItemType.CraterDay || item.theItemType == GridItemType.CraterNight)
                    item.Die();
            }
            
        }

        [HarmonyPatch(typeof(GridItem))]
        public static class GridItemPatch
        {
            [HarmonyPatch(nameof(GridItem.SetGridItem))]
            [HarmonyPrefix]
            public static bool SetGridItemPrefix(GridItemType theType)
            {
                if (instance == null || !instance.Active) return true;
                if ((theType == GridItemType.CraterDay) || (theType == GridItemType.CraterNight))
                    return false;
                return true;
            }
        }
    }


    public class NoGridItem : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Grid Item";
        public override string Description { get; set; } = "Removes all the selected Grid Item(s) while the module is active.";
        public override string SearchHints { get; set; } = "nogriditem removegriditem griditemremover cleargriditems " +
            "deletegriditems nogriditems nogriditemmod nogriditemfix nograve noladder noiceblock noscarypot nograves" +
            " noladders noiceblocks noscarypots anti-grave anti-ladder anti-iceblock anti-pot removegraves removeladders" +
            " removeiceblocks removescarypots graveclearer ladderclearer iceblockclearer potclearer nogriditemm nogrditem " +
            "nogrititem nogriditme nogriditemz removegridobjects deletegridobjects";


        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static NoGridItem instance;

        public MultiSelectSetting selectedGridItems;

        public bool TurnOffAfterUse = true;
        public BoolSetting AutoTurnOff;

#if MELONLOADER || BEPINEX
        public BoolSetting DebugMode;
#endif

        public NoGridItem()
        {
            instance = this;

            CreateCategory("General");

            selectedGridItems = new MultiSelectSetting("Grid Items")
            {
                CustomNames = TranslatedNames(typeof(GridItemType))
            };
            Settings.Add(selectedGridItems);

            EndCategory();
            CreateCategory("Extra");

            AutoTurnOff = new BoolSetting("Auto Turn Off", TurnOffAfterUse);
            Settings.Add(AutoTurnOff);

            EndCategory();
#if MELONLOADER || BEPINEX
            DebugMode = new BoolSetting("Debug Mode", false);
            Settings.Add(DebugMode);
#endif
        }

        public override void OnLanguageChanged()
        {
            selectedGridItems.CustomNames = TranslatedNames(typeof(GridItemType));
        }

        // Mod Logic

        public static float deltaTime = 0;
        public override void OnUpdateActive()
        {
            // Handle auto turn off
            if (!AutoTurnOff.Value) return;
            deltaTime += Time.deltaTime;
            if (deltaTime > 0.3f)
            {
                Active = false;
                deltaTime = 0;
            }
        }
        public override void OnEnable()
        {
            if (BoardInstanceIsNull || board.griditemArray == null) return;

#if MELONLOADER || BEPINEX
            if (DebugMode.Value)
            {
                DebugLogger.Msg("Found " + board.griditemArray.Count + " grid items.");
            }
#endif

            for (int i = board.griditemArray.Count - 1; i >= 0; i--)
            {
#if MELONLOADER || BEPINEX
                if (DebugMode.Value) DebugLogger.Msg($"Checking Grid Item at index {i}");
#endif
                GridItem item = board.griditemArray[i];
                if (item == null) continue;
                if (selectedGridItems.IsSelected((int)item.theItemType))
                { 
                    item.Die();
#if MELONLOADER || BEPINEX
                    if (DebugMode.Value) DebugLogger.Msg($"Removed Grid Item of type {item.theItemType} at index {i}");
#endif
                }
            }

            
        }

        [HarmonyPatch(typeof(GridItem))]
        public static class GridItemPatch
        {
            [HarmonyPatch(nameof(GridItem.SetGridItem))]
            [HarmonyPrefix]
            public static bool SetGridItemPrefix(GridItemType theType)
            {
                if (instance == null || !instance.Active) return true;
                if (instance.selectedGridItems.IsSelected((int)theType))
                    return false;
                return true;
            }
        }
    }
}

