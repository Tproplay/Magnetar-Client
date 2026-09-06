using UnityEngine;
using System;
using System.Collections.Generic;
using static Magnetar_Client.Utils.Magnetar_Logger;

using Magnetar_Client.UI.Themes;

namespace Magnetar_Client.TopBar
{
    public static class TopBar
    {
        // Base (unscaled, native-resolution) widths per tab, in the order they're drawn.
        private static List<TabType> Order = new List<TabType>();
        private static Dictionary<TabType, float> BaseWidth = new Dictionary<TabType, float>();

        // Cumulative base offsets, one extra entry at the end for the total width.
        // baseOffsets[i] = sum of BaseWidth for all tabs before Order[i].
        // baseOffsets[Order.Count] = total base width.
        private static float[] baseOffsets = Array.Empty<float>();

        private const float BaseHeight = 30;

        public static void Init()
        {
            Order.Clear();
            BaseWidth.Clear();

            foreach (TabType tab in (TabType[])Enum.GetValues(typeof(TabType)))
            {
                string name = tab.ToString();
                float btnWidth = name.Length * 12;
                btnWidth = Math.Max(btnWidth, 50);

                Order.Add(tab);
                BaseWidth[tab] = btnWidth;
            }

            baseOffsets = new float[Order.Count + 1];
            float running = 0;
            for (int i = 0; i < Order.Count; i++)
            {
                baseOffsets[i] = running;
                running += BaseWidth[Order[i]];
            }
            baseOffsets[Order.Count] = running;

            DebugLogger.Msg("Initialized the TopBar");
        }

        public static void Render()
        {
            // Everything here is authored in native (Config.WindowWidth-relative) space.
            // The outer GUI.matrix already handles converting that into real screen
            // pixels/letterboxing, so we must NOT mix in Screen.width/Input.mousePosition
            // (real screen space) - use Config.WindowWidth and Event.current.mousePosition
            // (already matrix-transformed) instead.

            // Scale each cumulative boundary and round to whole pixels, then derive each
            // button's width from the difference of consecutive rounded boundaries. This
            // guarantees adjacent buttons always share an exact edge - no 1px gaps or
            // overlaps regardless of GUIScale.
            int count = Order.Count;
            float[] scaledOffsets = new float[count + 1];
            for (int i = 0; i <= count; i++)
            {
                scaledOffsets[i] = Mathf.Round(Config.S(baseOffsets[i]));
            }

            float scaledTotalWidth = scaledOffsets[count];
            float scaledHeight = Mathf.Round(Config.S(BaseHeight));
            float startX = Mathf.Round(Config.WindowWidth / 2f - scaledTotalWidth / 2f);

            Rect barArea = new Rect(startX, 0, scaledTotalWidth, scaledHeight);

            if (barArea.Contains(Event.current.mousePosition))
            {
                if (Input.GetMouseButtonDown(0) | Input.GetMouseButtonDown(1) | Input.GetMouseButtonDown(2))
                {
                    Input.ResetInputAxes();
                }
            }

            for (int i = 0; i < count; i++)
            {
                TabType tab = Order[i];
                float scaledWidth = scaledOffsets[i + 1] - scaledOffsets[i];

                Rect rect = new Rect(0, 0, scaledWidth, scaledHeight);
                string name = tab.ToString();

                // Create a new group for each button
                Rect GroupRect = new Rect(startX + scaledOffsets[i], 0, scaledWidth, scaledHeight);

                GUI.BeginGroup(GroupRect);


                if (GUI.Button(rect, name, Config.CurrentTab == tab ? Magnetar_Default.TopBarActive : Magnetar_Default.TopBar))
                {
                    Config.CurrentTab = tab;
                    Event.current.Use();
                }

                GUI.EndGroup();
            }
        }
    }
}