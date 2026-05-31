using Il2Cpp;
using UnityEngine;
using System.Collections.Generic;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Modules
{
    public class DebugMode : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Debug Mode";
        public override string Description { get; set; } = "Logs debug info onto the console.";
        public override string SearchHints { get; set; } = "debugmode debuginfo consolelog loginfo debuglog " +
            "debugconsole logger debugshow debugdata logs debugtools debugconsolelog devlog devmode debugprint " +
            "systemlog debugdump printdebug debugmonitor showlogs debugmessages debugview debugactive loggingmode" +
            " debugtrace debugpanel infolog";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data

        public static DebugMode instance;

        public FloatSetting speed;

        public MultiSelectSetting selected;

        public enum Options
        {
            TheGameStatus = 1,

        }

        public DebugMode()
        {
            instance = this;
            selected = new MultiSelectSetting("elements")
            {
                Options = new Dictionary<int, string>
                {
                    { (int)Options.TheGameStatus, "GameStatus" },

                }
            };
            Settings.Add(selected);

            speed = new FloatSetting("Time between Logs", 0.1f, 10, 1, 1);

            Settings.Add(speed);
        }




        // Mod Logic

        private static float _time = 0;
        public override void OnUpdateActive()
        {
            _time += Time.deltaTime;

            if (_time < speed.Value) return;

            if (selected.IsSelected((int)Options.TheGameStatus))
            {
                DebugLogger.Msg("[Debug Mode] GameStatus: " + GameAPP.theGameStatus);
            }

            _time = 0;
        }

        public override void OnEnable()
        {
            _time = speed.Value;
        }





    }
}
