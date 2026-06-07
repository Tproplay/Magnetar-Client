using Il2Cpp;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Utils.Translator;
using static Magnetar_Client.Utils.Magnetar_Logger;

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

        public bool TurnOffAfterUse = false;
        public BoolSetting AutoTurnOff;

        public NoCraters()
        {
            instance = this;

            AutoTurnOff = new BoolSetting("Auto Turn Off", TurnOffAfterUse);
            Settings.Add(AutoTurnOff);
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

            if (BoardInstanceIsNull || board.griditemArray == null) return;

            for (int i = board.griditemArray.Count - 1; i >= 0; i--)
            {
                GridItem item = board.griditemArray[i];
                if (item == null) continue;
                if (item.theItemType == GridItemType.CraterDay || item.theItemType == GridItemType.CraterNight)
                    item.Die();
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

#if DEBUG
        public BoolSetting DebugMode;
#endif

        public NoGridItem()
        {
            instance = this;


            selectedGridItems = new MultiSelectSetting("Grid Items")
            {
                CustomNames = TranslatedNames(typeof(GridItemType))
            };
            Settings.Add(selectedGridItems);

            AutoTurnOff = new BoolSetting("Auto Turn Off", TurnOffAfterUse);
            Settings.Add(AutoTurnOff);
#if DEBUG
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
            if (BoardInstanceIsNull || board.griditemArray == null) return;

#if DEBUG
            if (DebugMode.Value)
            {
                DebugLogger.Msg("Found " + board.griditemArray.Count + " grid items.");
            }
#endif

            for (int i = board.griditemArray.Count - 1; i >= 0; i--)
            {
#if DEBUG
                if (DebugMode.Value) DebugLogger.Msg($"Checking Grid Item at index {i}");
#endif
                GridItem item = board.griditemArray[i];
                if (item == null) continue;
                if (selectedGridItems.IsSelected((int)item.theItemType))
                { 
                    item.Die();
#if DEBUG
                    if (DebugMode.Value) DebugLogger.Msg($"Removed Grid Item of type {item.theItemType} at index {i}");
#endif
                }
            }

            // Handle auto turn off
            if (!AutoTurnOff.Value) return;
            deltaTime += Time.deltaTime;
            if (deltaTime > 0.3f)
            {
                Active = false;
                deltaTime = 0;
            }
        }
    }
}

