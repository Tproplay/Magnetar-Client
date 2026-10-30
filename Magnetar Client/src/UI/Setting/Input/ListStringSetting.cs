using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.Core;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting module representing a dynamic list of editable strings, supporting addition, deletion,
/// count limits, and optional bracket autocomplete variables.
/// </summary>
public class ListStringSetting : Setting
{
    /// <summary>
    /// The collection of currently active string values.
    /// </summary>
    public List<string> Values = new();

    /// <summary>
    /// The default string list restored when <see cref="Reset"/> is executed.
    /// </summary>
    public List<string> DefaultValues = new();

    /// <summary>
    /// The maximum number of string entries allowed in this setting.
    /// </summary>
    public int MaxCount { get; set; } = 10;

    /// <summary>
    /// Optional list of variable names available for bracket autocomplete (e.g. "{player}").
    /// </summary>
    public List<string> AutocompleteVars;

    /// <summary>
    /// Event invoked whenever string items are added, edited, or removed.
    /// </summary>
    public Action<List<string>> OnValueChanged { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="ListStringSetting"/>.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="defaultValues">The initial and default string list.</param>
    /// <param name="maxCount">The maximum allowed entries.</param>
    /// <param name="autocompleteVars">Optional list of autocomplete placeholders.</param>
    public ListStringSetting(string name, List<string> defaultValues = null, int maxCount = 10, List<string> autocompleteVars = null)
    {
        Name = name;
        MaxCount = maxCount;
        AutocompleteVars = autocompleteVars;
        if (defaultValues != null)
        {
            DefaultValues = new List<string>(defaultValues);
            Values = new List<string>(defaultValues);
        }
    }

    /// <summary>
    /// Restores the string list back to <see cref="DefaultValues"/>.
    /// </summary>
    public override void Reset()
    {
        Values = new List<string>(DefaultValues);
        OnValueChanged?.Invoke(Values);
    }

    /// <summary>
    /// Renders the list items, individual delete buttons, add button, and reset button in IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float actionBtnW = SettingValues.ResetButtonW;
        float addBtnW = ClientConfig.SettingWidth;

        // Label on the left
        float labelW = Mathf.Max(width * 0.35f, GUIManager.S(120f));
        Rect labelRect = new(indent, y, labelW, elemH);
        GUI.Label(labelRect, DisplayName, ThemeManager.SettingLabelStyle);

        // Input fields right column area
        float rightBoxW = width - (indent * 2f) - labelW - gap;
        float startX = width - indent - rightBoxW;

        int removeIndex = -1;

        // Render each text field entry with delete '—' button
        for (int i = 0; i < Values.Count; i++)
        {
            float rowFieldW = rightBoxW - actionBtnW - gap;
            Rect rowRect = new(startX, y, rowFieldW, elemH);
            Rect delRect = new(startX + rowFieldW + gap, y, actionBtnW, elemH);

            Values[i] = DrawSetting.DrawManualTextField(rowRect, Values[i] ?? "", "", AutocompleteVars);

            GUI.Box(delRect, "—", ThemeManager.ListRemoveButtonStyle);
            if (delRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                removeIndex = i;
                e.Use();
            }

            y += elemH + gap;
        }

        if (removeIndex >= 0 && removeIndex < Values.Count)
        {
            Values.RemoveAt(removeIndex);
            OnValueChanged?.Invoke(Values);
        }

        // Bottom action row: [Add Button] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float addStartX = resetStartX - gap - addBtnW;

        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        Rect addBtnRect = new(addStartX, y, addBtnW, elemH);

        bool canAdd = Values.Count < MaxCount;
        GUIStyle addStyle = canAdd ? ThemeManager.ListAddButtonStyle : ThemeManager.CategoryModuleOffStyle;

        GUI.Box(addBtnRect, Translate(canAdd ? "Add" : "Max Reached"), addStyle);

        if (canAdd && addBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            Values.Add("");
            OnValueChanged?.Invoke(Values);
            e.Use();
        }

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + ClientConfig.spacing;
    }
}