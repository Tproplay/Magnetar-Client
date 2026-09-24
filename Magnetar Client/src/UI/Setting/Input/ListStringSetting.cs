using System;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;

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
        float gap = Config.S(4f);
        float resetBtnW = Config.S(22f);
        float actionBtnW = Config.S(24f);

        float addBtnW = Config.SettingWidth;

        // 1. Label on top/left
        float labelW = Mathf.Max(width * 0.35f, Config.S(120f));
        GUI.Label(new Rect(Config.indent, y, labelW, elemH), Translator.Translate(Name), Magnetar_Default.SettingLabelStyle);

        float rightBoxW = width - Config.indent * 2f - labelW - Config.S(10f);
        float startX = width - Config.indent - rightBoxW;

        int removeIndex = -1;

        // 2. Render each textfield entry with minus '-' button
        for (int i = 0; i < Values.Count; i++)
        {
            Rect rowRect = new(startX, y, rightBoxW - actionBtnW - gap, elemH);
            Rect delRect = new(startX + rightBoxW - actionBtnW, y, actionBtnW, elemH);

            Values[i] = DrawSetting.DrawManualTextField(rowRect, Values[i] ?? "", "", AutocompleteVars);

            GUI.Box(delRect, "—", Magnetar_Default.ListRemoveButtonStyle);
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

        // 3. Bottom Row
        Rect resetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, elemH);
        Rect addBtnRect = new(resetRect.x - gap - addBtnW, y, addBtnW, elemH);

        bool canAdd = Values.Count < MaxCount;
        GUIStyle addStyle = canAdd ? Magnetar_Default.ListAddButtonStyle : Magnetar_Default.CategoryModuleOffStyle;

        GUI.Box(addBtnRect, canAdd ? Translator.Translate("Add") : Translator.Translate("Max Reached"), addStyle);

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