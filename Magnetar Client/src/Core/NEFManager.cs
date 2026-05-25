using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.NEF;

namespace Magnetar_Client.Core
{
    public static class NEFManager
    {
        public static bool ShowMenu = false;
        public static float elementHeight = 25f;
        public static Rect windowRect = new Rect(60, 60, 1000, 700);

        public static void Init()
        {
            NEFData.Init();
        }

        public static void Render()
        {
            windowRect.x = 60f;
            windowRect.y = 60f;
            windowRect.width = Config.WindowWidth - 120f;
            windowRect.height = Config.WindowHeight - 120f;

            windowRect = GUI.Window(
                2002,
                windowRect,
                (GUI.WindowFunction)NEFGUI.DrawNEFWindow,
                "Not Enough Fusions",
                Magnetar_Default.ModuleWindow
            );
        }
    }
}