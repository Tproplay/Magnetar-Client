using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing
{
    public static class MiscDrawing
    {
        private static GUIStyle _cachedCategoryHeaderStyle;

        private static GUIStyle GetCategoryHeaderStyle(Color color)
        {
            if (_cachedCategoryHeaderStyle == null)
            {
                _cachedCategoryHeaderStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }

            _cachedCategoryHeaderStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(Config.S(12f)));
            _cachedCategoryHeaderStyle.normal.textColor = color;
            return _cachedCategoryHeaderStyle;
        }

        /// <summary>
        /// Draws a scalable horizontal separator spanning the full window width.
        /// </summary>
        public static void SeperatorFull(ref float y, float width, float spacing, Color color)
        {
            float lineThickness = Mathf.Max(1f, Config.S(1f));

            GUI.backgroundColor = color;
            GUI.Box(new Rect(0, y, width, lineThickness), "", Magnetar_Default.SeparatorStyle);
            GUI.backgroundColor = Color.white;

            y += (spacing * 2f);
        }

        /// <summary>
        /// Draws a scalable separator with indent margins. Optionally draws a labeled, clickable category foldout.
        /// </summary>
        public static bool Seperator(ref float y, float width, float indent, float spacing, Color color, string name = "", bool isCollapsible = false, bool isExpanded = true)
        {
            y += spacing;
            float lineThickness = Mathf.Max(1f, Config.S(1f));

            if (string.IsNullOrEmpty(name))
            {
                GUI.backgroundColor = color;
                GUI.Box(new Rect(indent, y, width - (indent * 2f), lineThickness), "", Magnetar_Default.SeparatorStyle);
                GUI.backgroundColor = Color.white;

                y += lineThickness + spacing;
                return isExpanded;
            }

            // --- Labeled & Collapsible Category Separator ---
            float elementH = Config.S(22f);
            float lineY = y + (elementH / 2f) - (lineThickness / 2f);

            GUIStyle style = GetCategoryHeaderStyle(color);

            float textPadding = Config.S(10f);
            float textWidth = style.CalcSize(new GUIContent(name)).x + textPadding;
            float lineW = Mathf.Max(0f, (width - (indent * 2f) - textWidth) / 2f);

            if (lineW > 0f)
            {
                GUI.backgroundColor = color;
                GUI.Box(new Rect(indent, lineY, lineW, lineThickness), "", Magnetar_Default.SeparatorStyle);
                GUI.Box(new Rect(indent + lineW + textWidth, lineY, lineW, lineThickness), "", Magnetar_Default.SeparatorStyle);
                GUI.backgroundColor = Color.white;
            }

            Rect textRect = new Rect(indent + lineW, y, textWidth, elementH);

            if (isCollapsible)
            {
                Rect clickRect = new Rect(indent, y, width - (indent * 2f), elementH);
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

        /// <summary>
        /// Draws an orthogonal link line between two points, scaling by both Canvas Zoom and GUIScale.
        /// </summary>
        public static void DrawOrthogonalLine(Vector2 pointA, Vector2 pointB, float Zoom = 1f)
        {
            Color oldColor = GUI.color;
            GUI.color = Color.white;

            float thickness = Mathf.Max(1f, Config.S(3f) * Zoom);
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