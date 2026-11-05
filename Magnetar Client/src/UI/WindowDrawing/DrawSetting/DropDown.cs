using Magnetar_Client.Core;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    /// <summary>
    /// Gets or sets the vertical scroll position of the currently open dropdown list.
    /// </summary>
    public static float dropdownScrollY;

    /// <summary>
    /// Gets or sets the bounding rectangle of the currently open dropdown list in parent window coordinates.
    /// </summary>
    public static Rect ActiveDropdownWindowRect { get; set; } = Rect.zero;

    /// <summary>
    /// Vertical offset of the current scrollable group inside the active window.
    /// </summary>
    public static float CurrentWindowGroupOffsetY { get; set; } = 0f;

    /// <summary>
    /// Inertial scrolling velocity for dropdown menus.
    /// </summary>
    public static float dropdownScrollVelocity;

    /// <summary>
    /// Whether the user is currently touch-swiping the dropdown list.
    /// </summary>
    public static bool dropdownIsSwiping;

    /// <summary>
    /// The index of the dropdown item candidate clicked on touch down.
    /// </summary>
    public static int dropdownCandidateIndex = -1;

    /// <summary>
    /// The starting touch position when interaction begins on a dropdown.
    /// </summary>
    public static Vector2 dropdownTouchStart;

    /// <summary>
    /// The initial scroll offset when touch interaction begins.
    /// </summary>
    public static float dropdownTouchStartScroll;

    /// <summary>
    /// The previous touch Y coordinate for calculating swipe velocity.
    /// </summary>
    public static float dropdownLastTouchY;

    /// <summary>
    /// The unscaled time when the previous touch sample was recorded.
    /// </summary>
    public static float dropdownLastTouchTime;

    /// <summary>
    /// Resets all dropdown interaction and scroll tracking states.
    /// </summary>
    public static void ResetDropdownState()
    {
        ActiveDropdownId = -1;
        ActiveDropdownWindowRect = Rect.zero;
        dropdownScrollVelocity = 0f;
        dropdownIsSwiping = false;
        dropdownCandidateIndex = -1;
    }

    /// <summary>
    /// Renders a dropdown selection setting control and handles user interaction.
    /// </summary>
    /// <param name="selSet">The <see cref="BaseSelectSetting"/> to draw and handle.</param>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    public static void HandleSelectSetting(BaseSelectSetting selSet, ref float y, float width)
    {
        Event e = Event.current;
        int controlId = selSet.GetHashCode();

        string translatedName = selSet.DisplayName;
        GUI.Label(new Rect(ClientConfig.indent, y, width - ClientConfig.indent * 2 - ClientConfig.SettingWidth, ClientConfig.elementHeight), translatedName, ThemeManager.SettingLabelStyle);

        string currentValName = selSet.CurrentValueDisplayName;

        Rect btnRect = new(width - ClientConfig.indent - ClientConfig.SettingWidth, y, ClientConfig.SettingWidth, ClientConfig.elementHeight);
        bool isHovered = btnRect.Contains(e.mousePosition);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            if (ActiveDropdownId == controlId)
            {
                ResetDropdownState();
            }
            else
            {
                ActiveDropdownId = controlId;
                dropdownScrollY = 0f;
                dropdownScrollVelocity = 0f;
                dropdownIsSwiping = false;
                dropdownCandidateIndex = -1;
                FocusedControlId = -1;
            }
            e.Use();
        }

        string arrow = (ActiveDropdownId == controlId) ? " ▲" : " ▼";
        GUI.Box(btnRect, currentValName + arrow, ThemeManager.SettingOff);

        if (ActiveDropdownId == controlId)
        {
            float rowHeight = SettingValues.DropdownRowHeight;
            int maxVisibleRows = SettingValues.DropdownMaxVisibleRows;
            int itemCount = selSet.OptionsCount;
            float dropHeight = Mathf.Min(itemCount * rowHeight, maxVisibleRows * rowHeight);
            float maxScroll = Mathf.Max(0f, (itemCount * rowHeight) - dropHeight);

            Rect dropRect = new(btnRect.x, btnRect.y + btnRect.height, btnRect.width, dropHeight);
            ActiveDropdownWindowRect = new Rect(dropRect.x, dropRect.y + CurrentWindowGroupOffsetY, dropRect.width, dropRect.height);

            // Momentum / Inertia decay
            if (!dropdownIsSwiping && Mathf.Abs(dropdownScrollVelocity) > 0.5f)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                dropdownScrollY = Mathf.Clamp(dropdownScrollY + (dropdownScrollVelocity * dt), 0f, maxScroll);
                dropdownScrollVelocity = Mathf.Lerp(dropdownScrollVelocity, 0f, 1f - Mathf.Exp(-4.5f * dt));

                if (dropdownScrollY <= 0f || dropdownScrollY >= maxScroll || Mathf.Abs(dropdownScrollVelocity) < 1f)
                {
                    dropdownScrollVelocity = 0f;
                }
            }

            // Mouse ScrollWheel
            if (dropRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                dropdownScrollVelocity = 0f;
                dropdownScrollY = Mathf.Clamp(dropdownScrollY + e.delta.y * SettingValues.DropdownScrollSensitivity, 0f, maxScroll);
                e.Use();
            }

            // MouseDown / TouchDown inside dropdown
            if (dropRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                dropdownTouchStart = e.mousePosition;
                dropdownTouchStartScroll = dropdownScrollY;
                dropdownIsSwiping = false;
                dropdownScrollVelocity = 0f;
                dropdownLastTouchY = e.mousePosition.y;
                dropdownLastTouchTime = Time.realtimeSinceStartup;

                float localY = e.mousePosition.y - dropRect.y + dropdownScrollY;
                dropdownCandidateIndex = (int)(localY / rowHeight);
                e.Use();
            }

            // Drag / Swipe movement
            if (dropdownCandidateIndex != -1 || dropdownIsSwiping)
            {
                if ((e.type == EventType.MouseDrag || Input.GetMouseButton(0)) && !dropdownIsSwiping)
                {
                    if (Vector2.Distance(e.mousePosition, dropdownTouchStart) > GUIManager.S(6f))
                    {
                        dropdownIsSwiping = true;
                        dropdownCandidateIndex = -1;
                    }
                }

                if (dropdownIsSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove || Input.GetMouseButton(0)))
                {
                    float deltaY = dropdownTouchStart.y - e.mousePosition.y;
                    dropdownScrollY = Mathf.Clamp(dropdownTouchStartScroll + deltaY, 0f, maxScroll);

                    float dt = Time.realtimeSinceStartup - dropdownLastTouchTime;
                    if (dt > 0.001f)
                    {
                        float rawVel = (dropdownLastTouchY - e.mousePosition.y) / dt;
                        dropdownScrollVelocity = Mathf.Lerp(dropdownScrollVelocity, rawVel, 0.4f);
                        dropdownLastTouchY = e.mousePosition.y;
                        dropdownLastTouchTime = Time.realtimeSinceStartup;
                    }

                    if (e.type == EventType.MouseDrag) e.Use();
                }
            }

            // MouseUp / TouchUp
            if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
            {
                if (!dropdownIsSwiping && dropdownCandidateIndex >= 0)
                {
                    if (dropRect.Contains(e.mousePosition))
                    {
                        int i = 0;
                        foreach (var opt in selSet.GetRenderableOptions())
                        {
                            if (i == dropdownCandidateIndex)
                            {
                                selSet.SetValueRaw(opt.Key);
                                ResetDropdownState();
                                break;
                            }
                            i++;
                        }
                    }
                    dropdownCandidateIndex = -1;
                    e.Use();
                }
                else if (dropdownIsSwiping)
                {
                    dropdownIsSwiping = false;
                    dropdownCandidateIndex = -1;
                    float timeSinceSample = Time.realtimeSinceStartup - dropdownLastTouchTime;
                    if (timeSinceSample > 0.08f)
                    {
                        dropdownScrollVelocity = 0f;
                    }
                    e.Use();
                }
            }

            // Click outside closes dropdown
            if (e.type == EventType.MouseDown && !btnRect.Contains(e.mousePosition) && !dropRect.Contains(e.mousePosition))
            {
                ResetDropdownState();
            }

            float _dropdownScrollY = dropdownScrollY;

            OnPostDraw += () =>
            {
                GUI.Box(dropRect, "", ThemeManager.SettingOff);
                GUI.BeginGroup(dropRect);

                int i = 0;
                foreach (var opt in selSet.GetRenderableOptions())
                {
                    float drawY = (i * rowHeight) - _dropdownScrollY;

                    if (drawY + rowHeight > 0 && drawY < dropHeight)
                    {
                        Rect rowRect = new(0, drawY, dropRect.width, rowHeight);
                        bool isSelected = selSet.IsSelectedRaw(opt.Key);
                        GUIStyle style = isSelected ? ThemeManager.SettingOn : ThemeManager.SettingOff;

                        GUI.Box(rowRect, opt.DisplayName, style);
                    }
                    i++;
                }
                GUI.EndGroup();

                if (itemCount * rowHeight > dropHeight)
                {
                    float maxScrollVal = (itemCount * rowHeight) - dropHeight;
                    float scrollPct = _dropdownScrollY / maxScrollVal;
                    float handleHeight = Mathf.Max(GUIManager.S(12f), dropHeight * (dropHeight / (itemCount * rowHeight)));
                    float handleY = dropRect.y + (scrollPct * (dropHeight - handleHeight));
                    float scrollbarW = GUIManager.S(3f);

                    GUI.Box(new Rect(dropRect.x + dropRect.width - scrollbarW - GUIManager.S(1f), handleY, scrollbarW, handleHeight), "", ThemeManager.SettingOn);
                }
            };
        }
    }

    /// <summary>
    /// Overload for backwards compatibility with <see cref="SelectSetting"/>.
    /// </summary>
    public static void HandleSelectSetting(SelectSetting selSet, ref float y, float width) =>
        HandleSelectSetting((BaseSelectSetting)selSet, ref y, width);
}