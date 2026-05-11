using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing
{
    public static class MiscDrawing
    {
        /// <summary>
        /// Draws a 1px seperator
        /// </summary>
        public static void SeperatorFull(ref float y,float width,float spacing, Color color)
        {
            GUI.backgroundColor = color;
            GUI.Box(new Rect(0, y, width, 1), "", Magnetar_Default.SeparatorStyle);
            GUI.backgroundColor = Color.white;
            y += spacing * 2;
        }

        /// <summary>
        /// Draws a 1px seperator
        /// </summary>
        public static void Seperator(ref float y, float width,float indent,float spacing,Color color)
        {
            GUI.backgroundColor = color;
            GUI.Box(new Rect(indent, y, width - (indent * 2), 1), "", Magnetar_Default.SeparatorStyle);
            GUI.backgroundColor = Color.white;
            y += spacing * 2;
        }
    }
}
