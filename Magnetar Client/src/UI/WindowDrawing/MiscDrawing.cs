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
        /// Draws a 1px separator. Optionally draws a labeled, clickable category foldout.
        /// </summary>
        public static bool Seperator(ref float y, float width, float indent, float spacing, Color color, string name = "", bool isCollapsible = false, bool isExpanded = true)
        {
            y += spacing;

            if (string.IsNullOrEmpty(name))
            {
                GUI.backgroundColor = color;
                GUI.Box(new Rect(indent, y, width - (indent * 2), 1), "", Magnetar_Default.SeparatorStyle);
                GUI.backgroundColor = Color.white;
                y += 1 + spacing;
                return isExpanded;
            }

            // --- Labeled & Collapsible Category Separator ---
            float elementH = 22f;
            float lineY = y + (elementH / 2f);

            GUIStyle style = new GUIStyle() { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            style.normal.textColor = color;

            float textWidth = style.CalcSize(new GUIContent(name)).x + 10f;
            float lineW = (width - (indent * 2) - textWidth) / 2f;

            if (lineW > 0)
            {
                GUI.backgroundColor = color;
                GUI.Box(new Rect(indent, lineY, lineW, 1), "", Magnetar_Default.SeparatorStyle);
                GUI.Box(new Rect(indent + lineW + textWidth, lineY, lineW, 1), "", Magnetar_Default.SeparatorStyle);
                GUI.backgroundColor = Color.white;
            }

            Rect textRect = new Rect(indent + lineW, y, textWidth, elementH);

            if (isCollapsible)
            {
                Rect clickRect = new Rect(indent, y, width - (indent * 2), elementH);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && clickRect.Contains(Event.current.mousePosition))
                {
                    isExpanded = !isExpanded;
                    Event.current.Use();
                }
            }

            GUI.Label(textRect, name, style);

            y += elementH + spacing;
            return isExpanded;
        }

        public static void DrawOrthogonalLine(Vector2 pointA, Vector2 pointB, float Zoom = 1)
        {
            Color oldColor = GUI.color;
            GUI.color = Color.white;

            float thickness = Mathf.Max(1f, 3f * Zoom);
            float halfThick = thickness / 2f;
            float midY = (pointA.y + pointB.y) / 2f;

            GUI.Box(new Rect(pointA.x - halfThick, pointA.y, thickness, midY - pointA.y + halfThick), "", Magnetar_Default.NEFLineStyle);

            float minX = Mathf.Min(pointA.x, pointB.x);
            float maxX = Mathf.Max(pointA.x, pointB.x);
            GUI.Box(new Rect(minX - halfThick, midY - halfThick, (maxX - minX) + thickness, thickness), "", Magnetar_Default.NEFLineStyle);

            GUI.Box(new Rect(pointB.x - halfThick, midY - halfThick, thickness, pointB.y - midY + halfThick), "", Magnetar_Default.NEFLineStyle);

            GUI.color = oldColor;
        }
    }
}
