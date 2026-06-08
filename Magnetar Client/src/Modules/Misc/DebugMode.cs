using Il2Cpp;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class DebugMode : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Debug Mode (for devs)";
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
            BoardTag = 2,
        }

        public DebugMode()
        {
            instance = this;

            CreateCategory("General");

            selected = new MultiSelectSetting("elements")
            {
                Options = new Dictionary<int, string>
                {
                    { (int)Options.TheGameStatus, "GameStatus" },
                    { (int)Options.BoardTag, "BoardTag" },
                }
            };
            Settings.Add(selected);

            speed = new FloatSetting("Time between Logs", 0.1f, 10, 1, 1);

            Settings.Add(speed);

            EndCategory();
        }




        // Mod Logic

        private static float _time = 0;
        public override void OnUpdateActive()
        {
            if (Time.realtimeSinceStartup < _time+speed.Value) return;

            if (selected.IsSelected((int)Options.TheGameStatus))
            {
                DebugLogger.Msg("[Debug Mode] GameStatus: " + GameAPP.theGameStatus);
            }

            if (selected.IsSelected((int)Options.BoardTag))
            {
                if (!BoardInstanceIsNull)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("[Debug Mode] Dumping boardTag Fields:");

                    // Grab all public, instance fields from the boardTag object
                    FieldInfo[] fields = board.boardTag.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                    foreach (FieldInfo field in fields)
                    {
                        object value = field.GetValue(board.boardTag);

                        sb.AppendLine($"   -> {field.Name}: {value}");
                    }
                    DebugLogger.Msg(sb.ToString());
                }
            }

            _time = Time.realtimeSinceStartup;
        }

        public override void OnEnable()
        {
            _time = Time.realtimeSinceStartup;
        }





    }
}
