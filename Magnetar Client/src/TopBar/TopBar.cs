using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;

using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.TopBar
{
    public static class TopBar
    {
        private static Dictionary<TabType, Rect> Buttons = new Dictionary<TabType, Rect>();
        private static float windowWidth = 0;

        public static void Init()
        {

            foreach (TabType tab in (TabType[])Enum.GetValues(typeof(TabType)))
            {
                string name = tab.ToString();
                float btnWidth = name.Length * 12;
                btnWidth = Math.Max(btnWidth, 50);

                Buttons[tab] = new Rect(0, 0, btnWidth, 30);
                windowWidth += btnWidth;
            }
        }

        public static void Render()
        {

            float cumuativeWidth = 0;

            foreach (TabType tab in Buttons.Keys)
            {
                Rect barArea = new Rect((Screen.width / 2) - (windowWidth / 2), 0, windowWidth, 30);

                if (barArea.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    if (Input.GetMouseButtonDown(0) | Input.GetMouseButtonDown(1) | Input.GetMouseButtonDown(2))
                    {
                        Input.ResetInputAxes();
                    }
                }

                Rect rect = Buttons[tab];
                string name = tab.ToString();

                // Create a new group for each button
                Rect GroupRect = new Rect(Config.WindowWidth/2-windowWidth/2+cumuativeWidth,0,rect.width,rect.height);

                GUI.BeginGroup(GroupRect);

                
                if (GUI.Button(rect, name,Config.CurrentTab == tab ? Magnetar_Default.TopBarActive : Magnetar_Default.TopBar))
                {
                    Config.CurrentTab = tab;
                    Event.current.Use();
                    UI.WindowDrawing.DrawSetting.isSearchFocused = false;
#if DEBUG
                    DebugLogger.Msg($"Changed Tab to : {name}");
#endif
                }

                GUI.EndGroup();

                cumuativeWidth += rect.width;
            }

        }
    }
}
