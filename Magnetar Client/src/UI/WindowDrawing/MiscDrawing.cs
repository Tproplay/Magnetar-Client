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
        public static void SeperatorFull(ref float y, float width, float spacing)
        {
            float lineThickness = Mathf.Max(1f, Config.S(1f));

            GUI.Box(new Rect(0, y, width, lineThickness), "", Magnetar_Default.SeparatorStyle);

            y += (spacing * 2f);
        }

        /// <summary>
        /// Draws a scalable separator with indent margins. Optionally draws a labeled, clickable category foldout.
        /// </summary>
        public static bool Seperator(ref float y, float width, float indent, float spacing, string name = "", bool isCollapsible = false, bool isExpanded = true, Color? customTextColor = null)
        {
            y += spacing;
            float lineThickness = Mathf.Max(1f, Config.S(1f));

            if (string.IsNullOrEmpty(name))
            {
                GUI.Box(new Rect(indent, y, width - (indent * 2f), lineThickness), "", Magnetar_Default.SeparatorStyle);

                y += lineThickness + spacing;
                return isExpanded;
            }

            // --- Labeled & Collapsible Category Separator ---
            float elementH = Config.elementHeight;
            float lineY = Mathf.Round(y + (elementH / 2f) - (lineThickness / 2f));

            string displayName = isCollapsible ? (isExpanded ? $"▼ {name}" : $"▶ {name}") : name;

            float textPadding = Config.S(12f);
            float textWidth = Magnetar_Default.SeparatorTextStyle.CalcSize(new GUIContent(displayName)).x + textPadding;
            float lineW = Mathf.Max(0f, (width - (indent * 2f) - textWidth) / 2f);

            // 1. Draw Left & Right Horizontal Lines
            if (lineW > 0f)
            {
                GUI.Box(new Rect(indent, lineY, lineW, lineThickness), "", Magnetar_Default.SeparatorStyle);
                GUI.Box(new Rect(indent + lineW + textWidth, lineY, lineW, lineThickness), "", Magnetar_Default.SeparatorStyle);
            }

            Rect textRect = new Rect(indent + lineW, y, textWidth, elementH);

            // 2. Click-to-Fold Handling
            if (isCollapsible)
            {
                Rect clickRect = new Rect(indent, y, width - (indent * 2f), elementH);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && clickRect.Contains(Event.current.mousePosition))
                {
                    isExpanded = !isExpanded;
                    Event.current.Use();
                }
            }

            // 3. Draw Centered Title Text
            Color prevColor = GUI.contentColor;
            if (customTextColor.HasValue)
            {
                GUI.contentColor = customTextColor.Value;
            }

            GUI.Label(textRect, displayName, Magnetar_Default.SeparatorTextStyle);

            if (customTextColor.HasValue)
            {
                GUI.contentColor = prevColor;
            }

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