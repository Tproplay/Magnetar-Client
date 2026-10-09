using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    public static float dropdownScrollY = 0f;

    public static void HandleSelectSetting(SelectSetting selSet, ref float y, float width)
    {
        Event e = Event.current;
        int controlId = selSet.GetHashCode();

        string translatedName = selSet.Translate(selSet.Name);
        GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, ThemeManager.SettingLabelStyle);

        string currentValName = "Unknown";
        if (selSet.Options.ContainsKey(selSet.Value))
        {
            currentValName = selSet.Options[selSet.Value];
            if (selSet.CustomNames != null && selSet.CustomNames.ContainsKey(selSet.Value))
            {
                currentValName = selSet.CustomNames[selSet.Value];
            }
        }

        Rect btnRect = new(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
        bool isHovered = btnRect.Contains(e.mousePosition);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            if (activeDropdownId == controlId)
            {
                activeDropdownId = -1;
            }
            else
            {
                activeDropdownId = controlId;
                dropdownScrollY = 0f;
                focusedControlId = -1;
            }
            e.Use();
        }

        string arrow = (activeDropdownId == controlId) ? " ▲" : " ▼";
        GUI.Box(btnRect, currentValName + arrow, ThemeManager.SettingOff);

        if (activeDropdownId == controlId)
        {
            float rowHeight = Config.SettingsInput.DropdownRowHeight;
            int maxVisibleRows = Config.SettingsInput.DropdownMaxVisibleRows;
            int itemCount = selSet.Options.Count;
            float dropHeight = Mathf.Min(itemCount * rowHeight, maxVisibleRows * rowHeight);

            Rect dropRect = new(btnRect.x, btnRect.y + btnRect.height, btnRect.width, dropHeight);

            if (dropRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                dropdownScrollY = Mathf.Clamp(dropdownScrollY + e.delta.y * Config.SettingsInput.DropdownScrollSensitivity,
                    0, Mathf.Max(0, (itemCount * rowHeight) - dropHeight));
                e.Use();
            }

            if (dropRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                float localY = e.mousePosition.y - dropRect.y + dropdownScrollY;
                int clickedIndex = (int)(localY / rowHeight);

                int i = 0;
                foreach (var kvp in selSet.Options)
                {
                    if (i == clickedIndex)
                    {
                        selSet.Value = kvp.Key;
                        activeDropdownId = -1;
                        e.Use();
                        break;
                    }
                    i++;
                }
            }

            if (e.type == EventType.MouseDown && !btnRect.Contains(e.mousePosition) && !dropRect.Contains(e.mousePosition))
            {
                activeDropdownId = -1;
            }

            float _dropdownScrollY = dropdownScrollY;

            OnPostDraw += () =>
            {
                GUI.Box(dropRect, "", ThemeManager.SettingOff);
                GUI.BeginGroup(dropRect);

                int i = 0;
                foreach (var kvp in selSet.Options)
                {
                    float drawY = (i * rowHeight) - _dropdownScrollY;

                    if (drawY + rowHeight > 0 && drawY < dropHeight)
                    {
                        Rect rowRect = new(0, drawY, dropRect.width, rowHeight);
                        string displayName = kvp.Value;

                        if (selSet.CustomNames != null && selSet.CustomNames.ContainsKey(kvp.Key))
                        {
                            displayName = selSet.CustomNames[kvp.Key];
                        }

                        bool isSelected = (selSet.Value == kvp.Key);
                        GUIStyle style = isSelected ? ThemeManager.SettingOn : ThemeManager.SettingOff;

                        GUI.Box(rowRect, displayName, style);
                    }
                    i++;
                }
                GUI.EndGroup();

                if (itemCount * rowHeight > dropHeight)
                {
                    float maxScroll = (itemCount * rowHeight) - dropHeight;
                    float scrollPct = _dropdownScrollY / maxScroll;
                    float handleHeight = Mathf.Max(10f, dropHeight * (dropHeight / (itemCount * rowHeight)));
                    float handleY = dropRect.y + (scrollPct * (dropHeight - handleHeight));

                    GUI.Box(new Rect(dropRect.x + dropRect.width - 4, handleY, 4, handleHeight), "", ThemeManager.SettingOn);
                }
            };
        }
    }
}