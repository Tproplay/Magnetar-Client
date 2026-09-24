using System;
using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;

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
        float resetBtnW = Config.S(22f);
        float gap = Config.S(4f);
        float subLabelW = Config.S(16f);

        float labelW = Mathf.Max(width * 0.35f, width - Config.indent * 2 - Config.SettingWidth - resetBtnW - (gap * 2));
        GUI.Label(new Rect(Config.indent, y, labelW, elemH), Translator.Translate(Name), Magnetar_Default.SettingLabelStyle);

        float totalControlW = Config.SettingWidth;
        float itemW = (totalControlW - (gap * 3f) - (subLabelW * 2f)) / 2f;

        float currX = width - Config.indent - resetBtnW - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", Magnetar_Default.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);
        string newX = DrawSetting.DrawManualTextField(xRect, Value.x.ToString("0.##"), "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", Magnetar_Default.TextStyle);
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
        Rect resetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, elemH);
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
        float resetBtnW = Config.S(22f);
        float gap = Config.S(4f);
        float subLabelW = Config.S(14f);

        float labelW = Mathf.Max(width * 0.32f, width - Config.indent * 2 - Config.SettingWidth - resetBtnW - (gap * 2));
        GUI.Label(new Rect(Config.indent, y, labelW, elemH), Translator.Translate(Name), Magnetar_Default.SettingLabelStyle);

        float totalControlW = Config.SettingWidth * 1.15f;
        float itemW = (totalControlW - (gap * 5f) - (subLabelW * 3f)) / 3f;
        float currX = width - Config.indent - resetBtnW - gap - totalControlW;

        // X component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "X", Magnetar_Default.TextStyle);
        currX += subLabelW + gap;
        Rect xRect = new(currX, y, itemW, elemH);
        string newX = DrawSetting.DrawManualTextField(xRect, Value.x.ToString("0.##"), "0");
        currX += itemW + gap;

        // Y component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Y", Magnetar_Default.TextStyle);
        currX += subLabelW + gap;
        Rect yRect = new(currX, y, itemW, elemH);
        string newY = DrawSetting.DrawManualTextField(yRect, Value.y.ToString("0.##"), "0");
        currX += itemW + gap;

        // Z component
        GUI.Label(new Rect(currX, y, subLabelW, elemH), "Z", Magnetar_Default.TextStyle);
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
        Rect resetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, elemH);
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}