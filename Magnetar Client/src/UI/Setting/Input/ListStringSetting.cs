using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public class ListStringSetting : Setting
{
    public List<string> Values = new();
    public List<string> DefaultValues = new();
    public int MaxCount { get; set; } = 10;
    public List<string> AutocompleteVars;

    public Action<List<string>> OnValueChanged { get; set; }

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

    public override void Reset()
    {
        Values = new List<string>(DefaultValues);
        OnValueChanged?.Invoke(Values);
    }

    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        float elemH = Config.elementHeight;
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float actionBtnW = Config.SettingsInput.ResetButtonW;
        float addBtnW = Config.SettingWidth;

        // Label on the left
        float labelW = Mathf.Max(width * 0.35f, Config.S(120f));
        Rect labelRect = new(indent, y, labelW, elemH);
        GUI.Label(labelRect, Translator.Translate(Name), ThemeManager.SettingLabelStyle);

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

        GUI.Box(addBtnRect, Translator.Translate(canAdd ? "Add" : "Max Reached"), addStyle);

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

        y += elemH + Config.spacing;
    }
}