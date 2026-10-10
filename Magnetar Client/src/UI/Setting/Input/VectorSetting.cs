using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public class Vector2Setting : Setting
{
    private Vector2 _value;
    public Vector2 DefaultValue;

    public Action<Vector2> OnValueChanged { get; set; }

    public Vector2 Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    public Vector2Setting(string name, Vector2 defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    public override void Reset() => Value = DefaultValue;

    public override void Draw(ref float y, float width)
    {
        float elemH = Config.elementHeight;
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float subLabelW = Config.S(16f);
        float totalControlW = Config.SettingWidth;

        // Label: Left space
        float labelW = width - (indent * 2f) - totalControlW - gap - resetBtnW;
        GUI.Label(new Rect(indent, y, labelW, elemH), Translate(Name), ThemeManager.SettingLabelStyle);

        // Component layout: [X] [box] [Y] [box]
        float itemW = (totalControlW - (gap * 3f) - (subLabelW * 2f)) / 2f;

        float resetStartX = width - indent - resetBtnW;
        float currX = resetStartX - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);
        string newX = DrawSetting.DrawManualTextField(xRect, Value.x.ToString("0.##"), "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);
        string newY = DrawSetting.DrawManualTextField(yRect, Value.y.ToString("0.##"), "0");

        if (float.TryParse(newX, out float px) && float.TryParse(newY, out float py))
        {
            if (!Mathf.Approximately(Value.x, px) || !Mathf.Approximately(Value.y, py))
            {
                Value = new Vector2(px, py);
            }
        }

        // Reset Button
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}

public class Vector2IntSetting : Setting
{
    private Vector2Int _value;
    public Vector2Int DefaultValue;

    public Action<Vector2Int> OnValueChanged { get; set; }

    public Vector2Int Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    public Vector2IntSetting(string name, Vector2Int defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    public override void Reset() => Value = DefaultValue;

    public override void Draw(ref float y, float width)
    {
        float elemH = Config.elementHeight;
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float subLabelW = Config.S(16f);
        float totalControlW = Config.SettingWidth;

        // Label: Left space
        float labelW = width - (indent * 2f) - totalControlW - gap - resetBtnW;
        GUI.Label(new Rect(indent, y, labelW, elemH), Translate(Name), ThemeManager.SettingLabelStyle);

        // Component layout: [X] [box] [Y] [box]
        float itemW = (totalControlW - (gap * 3f) - (subLabelW * 2f)) / 2f;

        float resetStartX = width - indent - resetBtnW;
        float currX = resetStartX - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);
        string newX = DrawSetting.DrawManualTextField(xRect, Value.x.ToString(), "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);
        string newY = DrawSetting.DrawManualTextField(yRect, Value.y.ToString(), "0");

        if (int.TryParse(newX, out int px) && int.TryParse(newY, out int py))
        {
            if (Value.x != px || Value.y != py)
            {
                Value = new Vector2Int(px, py);
            }
        }

        // Reset Button
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}

public class Vector3Setting : Setting
{
    private Vector3 _value;
    public Vector3 DefaultValue;

    public Action<Vector3> OnValueChanged { get; set; }

    public Vector3 Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    public Vector3Setting(string name, Vector3 defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    public override void Reset() => Value = DefaultValue;

    public override void Draw(ref float y, float width)
    {
        float elemH = Config.elementHeight;
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float subLabelW = Config.S(14f);
        float totalControlW = Config.SettingWidth * 1.15f;

        // Label: Left space
        float labelW = width - (indent * 2f) - totalControlW - gap - resetBtnW;
        GUI.Label(new Rect(indent, y, labelW, elemH), Translate(Name), ThemeManager.SettingLabelStyle);

        // Component layout: [X] [box] [Y] [box] [Z] [box]
        float itemW = (totalControlW - (gap * 5f) - (subLabelW * 3f)) / 3f;

        float resetStartX = width - indent - resetBtnW;
        float currX = resetStartX - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);
        string newX = DrawSetting.DrawManualTextField(xRect, Value.x.ToString("0.##"), "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);
        string newY = DrawSetting.DrawManualTextField(yRect, Value.y.ToString("0.##"), "0");
        currX += itemW + gap;

        // Z component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Z", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect zRect = new(currX, y, itemW, elemH);
        string newZ = DrawSetting.DrawManualTextField(zRect, Value.z.ToString("0.##"), "0");

        if (float.TryParse(newX, out float px) && float.TryParse(newY, out float py) && float.TryParse(newZ, out float pz))
        {
            if (!Mathf.Approximately(Value.x, px) || !Mathf.Approximately(Value.y, py) || !Mathf.Approximately(Value.z, pz))
            {
                Value = new Vector3(px, py, pz);
            }
        }

        // Reset Button
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}

public class Vector3IntSetting : Setting
{
    private Vector3Int _value;
    public Vector3Int DefaultValue;

    public Action<Vector3Int> OnValueChanged { get; set; }

    public Vector3Int Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    public Vector3IntSetting(string name, Vector3Int defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    public override void Reset() => Value = DefaultValue;

    public override void Draw(ref float y, float width)
    {
        float elemH = Config.elementHeight;
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float subLabelW = Config.S(14f);
        float totalControlW = Config.SettingWidth * 1.15f;

        // Label: Left space
        float labelW = width - (indent * 2f) - totalControlW - gap - resetBtnW;
        GUI.Label(new Rect(indent, y, labelW, elemH), Translate(Name), ThemeManager.SettingLabelStyle);

        // Component layout: [X] [box] [Y] [box] [Z] [box]
        float itemW = (totalControlW - (gap * 5f) - (subLabelW * 3f)) / 3f;

        float resetStartX = width - indent - resetBtnW;
        float currX = resetStartX - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);
        string newX = DrawSetting.DrawManualTextField(xRect, Value.x.ToString(), "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);
        string newY = DrawSetting.DrawManualTextField(yRect, Value.y.ToString(), "0");
        currX += itemW + gap;

        // Z component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Z", ThemeManager.TextStyle);
        currX += subLabelW + gap;
        Rect zRect = new(currX, y, itemW, elemH);
        string newZ = DrawSetting.DrawManualTextField(zRect, Value.z.ToString(), "0");

        if (int.TryParse(newX, out int px) && int.TryParse(newY, out int py) && int.TryParse(newZ, out int pz))
        {
            if (Value.x != px || Value.y != py || Value.z != pz)
            {
                Value = new Vector3Int(px, py, pz);
            }
        }

        // Reset Button
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}