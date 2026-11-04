using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting representing a single-choice dropdown selection from a collection of options or an enum.
/// </summary>
public class SelectSetting : Setting
{
    private int _value;

    /// <summary>
    /// The default selected option value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public int DefaultValue;

    /// <summary>
    /// Dictionary of available options mapping integer keys to their default display names.
    /// </summary>
    public Dictionary<int, string> Options { get; set; } = new();

    /// <summary>
    /// The enum type used to populate options, or <see langword="null"/> if not created from an enum.
    /// </summary>
    public System.Type EnumType { get; private set; }

    /// <summary>
    /// Optional dictionary mapping integer keys to custom display name overrides.
    /// </summary>
    public Dictionary<int, string> CustomNames { get; set; } = new();

    /// <summary>
    /// Event invoked when the selected option value changes.
    /// </summary>
    public Action<int> OnSelectionChanged { get; set; }

    /// <summary>
    /// Gets or sets the currently selected option value.
    /// </summary>
    public int Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                _value = value;
                OnSelectionChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectSetting"/> with a display name and default value.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="defaultValue">The initial and default integer value.</param>
    /// <param name="customSaveKey">Optional custom key for persistence.</param>
    public SelectSetting(string name, int defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectSetting"/> populated from the values of an enum type.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="enumType">The enumeration type used to populate options.</param>
    /// <param name="defaultValue">The initial and default integer value.</param>
    public SelectSetting(string name, System.Type enumType, int defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
        EnumType = enumType;

        if (enumType != null && enumType.IsEnum)
        {
            foreach (var val in Enum.GetValues(enumType))
                Options[Convert.ToInt32(val)] = val.ToString();
        }
    }

    /// <summary>
    /// Adds or updates an available option with the specified ID and display name.
    /// </summary>
    /// <param name="id">The integer key identifying the option.</param>
    /// <param name="displayName">The display text shown for the option.</param>
    public void AddOption(int id, string displayName) => Options[id] = displayName;

    /// <summary>
    /// Restores the selected value back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset() => Value = DefaultValue;

    /// <summary>
    /// Sets the value directly without invoking events or checking <see cref="Setting.IsDisabled"/>.
    /// </summary>
    public void SetDirect(int val)
    {
        _value = val;
    }

    /// <summary>
    /// Renders the select dropdown setting control within the GUI window.
    /// </summary>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        int controlId = GetHashCode();

        bool prevGUIEnabled = GUI.enabled;
        if (IsDisabled) GUI.enabled = false;

        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float btnW = ClientConfig.SettingWidth;

        // Label on the left: fills remaining horizontal space
        float labelW = width - (indent * 2f) - btnW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, elemH);
        GUI.Label(labelRect, DisplayName, ThemeManager.SettingLabelStyle);

        // Right-to-left layout: [Dropdown Button] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float btnStartX = resetStartX - gap - btnW;

        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        Rect btnRect = new(btnStartX, y, btnW, elemH);

        string currentValName = "Unknown";
        if (Options.ContainsKey(Value))
        {
            currentValName = (CustomNames != null && CustomNames.ContainsKey(Value)) ? CustomNames[Value] : Options[Value];
        }

        if (!IsDisabled && btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (DrawSetting.ActiveDropdownId == controlId)
            {
                DrawSetting.ResetDropdownState();
            }
            else
            {
                DrawSetting.ActiveDropdownId = controlId;
                DrawSetting.dropdownScrollY = 0f;
                DrawSetting.dropdownScrollVelocity = 0f;
                DrawSetting.dropdownIsSwiping = false;
                DrawSetting.dropdownCandidateIndex = -1;
                DrawSetting.FocusedControlId = -1;
            }
            e.Use();
        }

        string arrow = (DrawSetting.ActiveDropdownId == controlId) ? " ▲" : " ▼";
        GUI.Box(btnRect, currentValName + arrow, ThemeManager.SettingOff);

        if (DrawSetting.ActiveDropdownId == controlId)
        {
            float rowHeight = SettingValues.DropdownRowHeight;
            int maxVisibleRows = SettingValues.DropdownMaxVisibleRows;
            int itemCount = Options.Count;
            float dropHeight = Mathf.Min(itemCount * rowHeight, maxVisibleRows * rowHeight);
            float maxScroll = Mathf.Max(0f, (itemCount * rowHeight) - dropHeight);

            Rect dropRect = new(btnRect.x, btnRect.y + btnRect.height, btnRect.width, dropHeight);
            DrawSetting.ActiveDropdownWindowRect = new Rect(dropRect.x, dropRect.y + DrawSetting.CurrentWindowGroupOffsetY, dropRect.width, dropRect.height);

            // Momentum / Inertia decay
            if (!DrawSetting.dropdownIsSwiping && Mathf.Abs(DrawSetting.dropdownScrollVelocity) > 0.5f)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                DrawSetting.dropdownScrollY = Mathf.Clamp(DrawSetting.dropdownScrollY + (DrawSetting.dropdownScrollVelocity * dt), 0f, maxScroll);
                DrawSetting.dropdownScrollVelocity = Mathf.Lerp(DrawSetting.dropdownScrollVelocity, 0f, 1f - Mathf.Exp(-4.5f * dt));

                if (DrawSetting.dropdownScrollY <= 0f || DrawSetting.dropdownScrollY >= maxScroll || Mathf.Abs(DrawSetting.dropdownScrollVelocity) < 1f)
                {
                    DrawSetting.dropdownScrollVelocity = 0f;
                }
            }

            // Mouse ScrollWheel
            if (dropRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                DrawSetting.dropdownScrollVelocity = 0f;
                DrawSetting.dropdownScrollY = Mathf.Clamp(DrawSetting.dropdownScrollY + e.delta.y * SettingValues.DropdownScrollSensitivity, 0f, maxScroll);
                e.Use();
            }

            // MouseDown / TouchDown inside dropdown
            if (dropRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                DrawSetting.dropdownTouchStart = e.mousePosition;
                DrawSetting.dropdownTouchStartScroll = DrawSetting.dropdownScrollY;
                DrawSetting.dropdownIsSwiping = false;
                DrawSetting.dropdownScrollVelocity = 0f;
                DrawSetting.dropdownLastTouchY = e.mousePosition.y;
                DrawSetting.dropdownLastTouchTime = Time.realtimeSinceStartup;

                float localY = e.mousePosition.y - dropRect.y + DrawSetting.dropdownScrollY;
                DrawSetting.dropdownCandidateIndex = (int)(localY / rowHeight);
                e.Use();
            }

            // Drag / Swipe movement
            if (DrawSetting.dropdownCandidateIndex != -1 || DrawSetting.dropdownIsSwiping)
            {
                if ((e.type == EventType.MouseDrag || Input.GetMouseButton(0)) && !DrawSetting.dropdownIsSwiping)
                {
                    if (Vector2.Distance(e.mousePosition, DrawSetting.dropdownTouchStart) > GUIManager.S(6f))
                    {
                        DrawSetting.dropdownIsSwiping = true;
                        DrawSetting.dropdownCandidateIndex = -1;
                    }
                }

                if (DrawSetting.dropdownIsSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove || Input.GetMouseButton(0)))
                {
                    float deltaY = DrawSetting.dropdownTouchStart.y - e.mousePosition.y;
                    DrawSetting.dropdownScrollY = Mathf.Clamp(DrawSetting.dropdownTouchStartScroll + deltaY, 0f, maxScroll);

                    float dt = Time.realtimeSinceStartup - DrawSetting.dropdownLastTouchTime;
                    if (dt > 0.001f)
                    {
                        float rawVel = (DrawSetting.dropdownLastTouchY - e.mousePosition.y) / dt;
                        DrawSetting.dropdownScrollVelocity = Mathf.Lerp(DrawSetting.dropdownScrollVelocity, rawVel, 0.4f);
                        DrawSetting.dropdownLastTouchY = e.mousePosition.y;
                        DrawSetting.dropdownLastTouchTime = Time.realtimeSinceStartup;
                    }

                    if (e.type == EventType.MouseDrag) e.Use();
                }
            }

            // MouseUp / TouchUp
            if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
            {
                if (!DrawSetting.dropdownIsSwiping && DrawSetting.dropdownCandidateIndex >= 0)
                {
                    if (dropRect.Contains(e.mousePosition))
                    {
                        int idx = 0;
                        foreach (var kvp in Options)
                        {
                            if (idx == DrawSetting.dropdownCandidateIndex)
                            {
                                Value = kvp.Key;
                                DrawSetting.ResetDropdownState();
                                break;
                            }
                            idx++;
                        }
                    }
                    DrawSetting.dropdownCandidateIndex = -1;
                    e.Use();
                }
                else if (DrawSetting.dropdownIsSwiping)
                {
                    DrawSetting.dropdownIsSwiping = false;
                    DrawSetting.dropdownCandidateIndex = -1;
                    float timeSinceSample = Time.realtimeSinceStartup - DrawSetting.dropdownLastTouchTime;
                    if (timeSinceSample > 0.08f)
                    {
                        DrawSetting.dropdownScrollVelocity = 0f;
                    }
                    e.Use();
                }
            }

            // Click outside closes dropdown
            if (e.type == EventType.MouseDown && !btnRect.Contains(e.mousePosition) && !dropRect.Contains(e.mousePosition))
            {
                DrawSetting.ResetDropdownState();
            }

            float scrollY = DrawSetting.dropdownScrollY;
            DrawSetting.OnPostDraw += () =>
            {
                GUI.Box(dropRect, "", ThemeManager.SettingOff);
                GUI.BeginGroup(dropRect);
                int i = 0;
                foreach (var kvp in Options)
                {
                    float drawY = (i * rowHeight) - scrollY;
                    if (drawY + rowHeight > 0 && drawY < dropHeight)
                    {
                        Rect row = new(0, drawY, dropRect.width, rowHeight);
                        string disp = (CustomNames != null && CustomNames.ContainsKey(kvp.Key)) ? CustomNames[kvp.Key] : kvp.Value;
                        GUI.Box(row, disp, (Value == kvp.Key) ? ThemeManager.SettingOn : ThemeManager.SettingOff);
                    }
                    i++;
                }
                GUI.EndGroup();

                if (maxScroll > 0)
                {
                    float scrollbarW = GUIManager.S(3f);
                    float scrollPct = scrollY / maxScroll;
                    float handleHeight = Mathf.Max(GUIManager.S(12f), dropHeight * (dropHeight / (itemCount * rowHeight)));
                    float handleY = dropRect.y + (scrollPct * (dropHeight - handleHeight));
                    Rect scrollbarRect = new(dropRect.x + dropRect.width - scrollbarW - GUIManager.S(1f), handleY, scrollbarW, handleHeight);
                    GUI.Box(scrollbarRect, "", ThemeManager.SettingOn);
                }
            };
        }

        if (!IsDisabled && DrawResetButton(resetRect))
        {
            Reset();
        }

        GUI.enabled = prevGUIEnabled;

        y += elemH + ClientConfig.spacing;
    }
}