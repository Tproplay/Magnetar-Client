using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.NEF;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core
{
    public static class NEFManager
    {
        // Base (unscaled, GUIScale == 1) margin around the window - actual
        // margin is derived via Config.S() so it scales with the rest of the UI.
        private const float BaseMargin = 60f;

        public static bool ShowMenu = false;
        public static float elementHeight => Config.S(25f);
        public static Rect windowRect = new Rect(60, 60, 1000, 700);

        public static void Init()
        {
            NEFData.Init();

            DebugLogger.Msg("Initialized Not Enough Fusions");
        }

        public static void Render()
        {
            float margin = Config.S(BaseMargin);
            windowRect.x = margin;
            windowRect.y = margin;
            windowRect.width = Config.WindowWidth - margin * 2f;
            windowRect.height = Config.WindowHeight - margin * 2f;

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