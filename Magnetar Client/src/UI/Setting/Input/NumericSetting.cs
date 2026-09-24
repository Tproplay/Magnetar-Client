using System;
using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;

namespace Magnetar_Client.UI.Setting;

public class IntSetting : Setting
{
    private int _value;
    public int DefaultValue;
    public bool InstantUpdate = false;
    public int? PendingValue = null;

    public int Min;
    public int Max;
    public int TrueMin;
    public int TrueMax;

    public Action<int> OnValueChanging { get; set; }
    public Action<int> OnValueChanged { get; set; }

    public int DisplayValue => PendingValue ?? _value;

    public int Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                OnValueChanging?.Invoke(value);
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    public void SetPending(int val)
    {
        if (InstantUpdate) { Value = val; PendingValue = null; }
        else PendingValue = val;
    }

    public void Commit()
    {
        if (PendingValue.HasValue) { Value = PendingValue.Value; PendingValue = null; }
    }

    public IntSetting(string name, int min, int max, int defaultValue, int trueMin = int.MinValue, int trueMax = int.MaxValue, bool instantUpdate = false)
    {
        Name = name;
        Min = min; Max = max; TrueMin = trueMin; TrueMax = trueMax;
        InstantUpdate = instantUpdate;
        _value = System.Math.Max(TrueMin, System.Math.Min(defaultValue, TrueMax));
        DefaultValue = _value;
    }

    public override void Reset()
    {
        Value = DefaultValue;
        PendingValue = null;
    }

    public override void Draw(ref float y, float width)
    {
        DrawNumeric(this, ref y, width, false);
    }

    internal static void DrawNumeric(object setting, ref float y, float width, bool isFloat)
    {
        float val, sliderMin, sliderMax, trueMin, trueMax;
        string name;
        int decPlaces = 0, intTrueMin = 0, intTrueMax = 0, intSliderMin = 0, intSliderMax = 0;

        if (isFloat)
        {
            var s = (FloatSetting)setting;
            val = s.DisplayValue; sliderMin = s.Min; sliderMax = s.Max;
            trueMin = s.TrueMin; trueMax = s.TrueMax; name = s.Name; decPlaces = s.DecimalPlaces;
        }
        else
        {
            var s = (IntSetting)setting;
            val = s.DisplayValue; sliderMin = s.Min; sliderMax = s.Max;
            trueMin = s.TrueMin; trueMax = s.TrueMax; name = s.Name;
            intSliderMin = s.Min; intSliderMax = s.Max; intTrueMin = s.TrueMin; intTrueMax = s.TrueMax;
        }

        string formatString = isFloat ? ("0." + new string('0', decPlaces)) : "0";
        string translatedName = Translator.Translate(name);

        float resetBtnW = Config.S(22f);
        float gap = Config.S(6f);
        float inputW = Config.SettingsInput.NumericInputWidth;
        float sliderW = Config.SettingWidth - inputW - 10f;
        float trackH = Config.SettingsInput.SliderHeight;
        float thumbSize = Config.S(16f);

        float rightOffset = width - Config.indent;
        Rect resetRect = new(rightOffset - resetBtnW, y, resetBtnW, Config.elementHeight);
        Rect inputRect = new(resetRect.x - gap - inputW, y, inputW, Config.elementHeight);
        Rect sliderRect = new(inputRect.x - 10f - sliderW, y + ((Config.elementHeight - trackH) / 2f), sliderW, trackH);
        Rect sliderHitBox = new(sliderRect.x, y, sliderRect.width, Config.elementHeight);

        GUI.Label(new Rect(Config.indent, y, sliderRect.x - Config.indent - gap, Config.elementHeight), translatedName, Magnetar_Default.SettingLabelStyle);

        float LogConvert(float v) => Mathf.Sign(v) * Mathf.Log10(Mathf.Abs(v) + 1.0f);
        float ExpConvert(float l) => Mathf.Sign(l) * (Mathf.Pow(10.0f, Mathf.Abs(l)) - 1.0f);

        float logMin = LogConvert(sliderMin);
        float logMax = LogConvert(sliderMax);
        float visualVal = Mathf.Clamp(val, sliderMin, sliderMax);
        float logVal = LogConvert(visualVal);
        float percentage = Mathf.Clamp01((logVal - logMin) / (logMax - logMin));

        float fillWidth = sliderRect.width * percentage;
        float thumbX = sliderRect.x + fillWidth - (thumbSize / 2f);
        float thumbY = sliderRect.y + (trackH / 2f) - (thumbSize / 2f);
        Rect thumbRect = new(thumbX, thumbY, thumbSize, thumbSize);

        GUI.Box(sliderRect, "", Magnetar_Default.SliderTrackOffStyle);
        if (fillWidth > 0f) GUI.Box(new Rect(sliderRect.x, sliderRect.y, fillWidth, sliderRect.height), "", Magnetar_Default.SliderTrackOnStyle);
        GUI.Box(thumbRect, "", Magnetar_Default.SliderThumbStyle);

        Event e = Event.current;
        void CommitSettingValue() { if (isFloat) ((FloatSetting)setting).Commit(); else ((IntSetting)setting).Commit(); }

        void ApplyFromMouseX(float mouseX)
        {
            float mousePct = Mathf.Clamp01((mouseX - sliderRect.x) / sliderRect.width);
            float newVal = ExpConvert(logMin + (mousePct * (logMax - logMin)));
            if (isFloat) ((FloatSetting)setting).SetPending((float)Math.Round(Mathf.Clamp(newVal, sliderMin, sliderMax), decPlaces));
            else ((IntSetting)setting).SetPending((int)Math.Max(intSliderMin, Math.Min((long)newVal, intSliderMax)));
        }

        int sliderControlId = GUIUtility.GetControlID(name.GetHashCode(), FocusType.Passive);
        bool inHitbox = sliderHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition);

        if (e.type == EventType.MouseDown && e.button == 0 && inHitbox)
        {
            GUIUtility.hotControl = sliderControlId;
            DrawSetting.activeSliderId = sliderControlId;
            DrawSetting.activeNumericSetting = setting;
            DrawSetting.focusedControlId = -1;
            DrawSetting.activeTextFieldId = -1;
            ApplyFromMouseX(e.mousePosition.x);
            e.Use();
        }

        if (GUIUtility.hotControl == sliderControlId)
        {
            if (e.type == EventType.MouseDrag) { ApplyFromMouseX(e.mousePosition.x); e.Use(); }
            else if (e.type == EventType.MouseUp || (e.type == EventType.Ignore && e.rawType == EventType.MouseUp))
            {
                CommitSettingValue();
                GUIUtility.hotControl = 0;
                DrawSetting.activeSliderId = -1;
                DrawSetting.activeNumericSetting = null;
                e.Use();
            }
        }

        int controlId = inputRect.GetHashCode();
        bool isFocused = (DrawSetting.activeTextFieldId == controlId);

        if (isFocused && DrawSetting.lastFocusedNumericControlId != controlId)
        {
            DrawSetting.currentInputBuffer = val.ToString(formatString);
            DrawSetting.lastFocusedNumericControlId = controlId;
        }
        else if (!isFocused && DrawSetting.lastFocusedNumericControlId == controlId)
        {
            CommitSettingValue();
            DrawSetting.lastFocusedNumericControlId = -1;
        }

        string displayValue = isFocused ? DrawSetting.currentInputBuffer : val.ToString(formatString);
        string newText = DrawSetting.DrawManualTextField(inputRect, displayValue, "0");

        if (isFocused)
        {
            DrawSetting.currentInputBuffer = newText;
            if (double.TryParse(DrawSetting.currentInputBuffer, out double parsed))
            {
                if (isFloat) ((FloatSetting)setting).SetPending((float)Math.Round(Mathf.Clamp((float)parsed, trueMin, trueMax), decPlaces));
                else ((IntSetting)setting).SetPending((int)Math.Max(intTrueMin, Math.Min((long)parsed, intTrueMax)));
            }
        }

        if (DrawResetButton(resetRect))
        {
            if (isFloat) ((FloatSetting)setting).Reset();
            else ((IntSetting)setting).Reset();
        }

        y += Config.elementHeight + Config.spacing;
    }
}

public class FloatSetting : Setting
{
    private float _value;
    public float DefaultValue;
    public bool InstantUpdate = false;
    public float? PendingValue = null;

    public float Min;
    public float Max;
    public float TrueMin;
    public float TrueMax;
    public int DecimalPlaces;

    public Action<float> OnValueChanging { get; set; }
    public Action<float> OnValueChanged { get; set; }

    public float DisplayValue => PendingValue ?? _value;

    public float Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                OnValueChanging?.Invoke(value);
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    public void SetPending(float val)
    {
        if (InstantUpdate) { Value = val; PendingValue = null; }
        else PendingValue = val;
    }

    public void Commit()
    {
        if (PendingValue.HasValue) { Value = PendingValue.Value; PendingValue = null; }
    }

    public FloatSetting(string name, float min, float max, float defaultValue, int decimalPlaces = 1, float trueMin = float.MinValue, float trueMax = float.MaxValue, bool instantUpdate = false)
    {
        Name = name;
        Min = min; Max = max; TrueMin = trueMin; TrueMax = trueMax;
        DecimalPlaces = decimalPlaces;
        InstantUpdate = instantUpdate;
        _value = Mathf.Clamp(defaultValue, TrueMin, TrueMax);
        DefaultValue = _value;
    }

    public override void Reset()
    {
        Value = DefaultValue;
        PendingValue = null;
    }

    public override void Draw(ref float y, float width)
    {
        IntSetting.DrawNumeric(this, ref y, width, true);
    }
}