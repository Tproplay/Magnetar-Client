using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using UnityEngine;
using Magnetar_Client.Core;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting module representing an integer value with slider and numeric text field controls,
/// supporting slider clamping, absolute boundaries, and deferred commit buffering.
/// </summary>
public class IntSetting : Setting
{
    private int _value;

    /// <summary>
    /// The default fallback value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public int DefaultValue;

    /// <summary>
    /// Whether changes to the slider or text box immediately update <see cref="Value"/> rather than waiting for release/commit.
    /// </summary>
    public bool InstantUpdate;

    /// <summary>
    /// The uncommitted pending value during slider dragging or active text box entry.
    /// </summary>
    public int? PendingValue;

    /// <summary>
    /// The visual lower bound of the slider track.
    /// </summary>
    public int Min;

    /// <summary>
    /// The visual upper bound of the slider track.
    /// </summary>
    public int Max;

    /// <summary>
    /// The absolute lower clamping bound allowed when typing directly into the text field.
    /// </summary>
    public int TrueMin;

    /// <summary>
    /// The absolute upper clamping bound allowed when typing directly into the text field.
    /// </summary>
    public int TrueMax;

    /// <summary>
    /// Event invoked immediately before <see cref="Value"/> changes.
    /// </summary>
    public Action<int> OnValueChanging { get; set; }

    /// <summary>
    /// Event invoked after <see cref="Value"/> changes.
    /// </summary>
    public Action<int> OnValueChanged { get; set; }

    /// <summary>
    /// Gets the current value being displayed (either <see cref="PendingValue"/> if dragging/editing, or <see cref="Value"/>).
    /// </summary>
    public int DisplayValue => PendingValue ?? _value;

    /// <summary>
    /// Gets or sets the committed integer value.
    /// </summary>
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

    /// <summary>
    /// Sets the pending uncommitted value, or commits immediately if <see cref="InstantUpdate"/> is enabled.
    /// </summary>
    /// <param name="val">The proposed integer value.</param>
    public void SetPending(int val)
    {
        if (InstantUpdate) { Value = val; PendingValue = null; }
        else PendingValue = val;
    }

    /// <summary>
    /// Commits <see cref="PendingValue"/> into <see cref="Value"/> and clears the pending state.
    /// </summary>
    public void Commit()
    {
        if (PendingValue.HasValue)
        {
            Value = PendingValue.Value;
            PendingValue = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntSetting"/>.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="min">The slider track lower bound.</param>
    /// <param name="max">The slider track upper bound.</param>
    /// <param name="defaultValue">The initial and default integer value.</param>
    /// <param name="trueMin">The absolute minimum value allowed via manual text entry.</param>
    /// <param name="trueMax">The absolute maximum value allowed via manual text entry.</param>
    /// <param name="instantUpdate">Whether changes are applied immediately while dragging.</param>
    public IntSetting(string name, int min, int max, int defaultValue, int trueMin = int.MinValue, int trueMax = int.MaxValue, bool instantUpdate = false)
    {
        Name = name;
        Min = min; Max = max; TrueMin = trueMin; TrueMax = trueMax;
        InstantUpdate = instantUpdate;
        _value = System.Math.Max(TrueMin, System.Math.Min(defaultValue, TrueMax));
        DefaultValue = _value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntSetting"/>.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="min">The slider track lower bound.</param>
    /// <param name="max">The slider track upper bound.</param>
    /// <param name="defaultValue">The initial and default integer value.</param>
    public IntSetting(string name, int min, int max, int defaultValue)
    {
        Name = name;
        Min = min; Max = max; 
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Restores the setting back to <see cref="DefaultValue"/> and clears pending edits.
    /// </summary>
    public override void Reset()
    {
        Value = DefaultValue;
        PendingValue = null;
    }

    /// <summary>
    /// Renders the setting row containing the label, slider track, numeric input field, and reset button.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        DrawNumeric(this, ref y, width, false);
    }

    /// <summary>
    /// Internal drawing routine shared between <see cref="IntSetting"/> and <see cref="FloatSetting"/>.
    /// </summary>
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
        string translatedName = Translate(name);
        string formatString = isFloat ? ("0." + new string('0', decPlaces)) : "0";

        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float inputW = SettingValues.NumericInputWidth;
        float sliderW = ClientConfig.SettingWidth - inputW - gap;
        float trackH = SettingValues.SliderHeight;
        float thumbSize = GUIManager.S(16f);

        // Right-to-left layout: [Reset] <- [Input] <- [Slider]
        float resetStartX = width - indent - resetBtnW;
        float inputStartX = resetStartX - gap - inputW;
        float sliderStartX = inputStartX - gap - sliderW;

        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        Rect inputRect = new(inputStartX, y, inputW, elemH);
        Rect sliderRect = new(sliderStartX, y + ((elemH - trackH) / 2f), sliderW, trackH);

        float labelW = sliderStartX - indent - gap;
        GUI.Label(new Rect(indent, y, labelW, elemH), translatedName, ThemeManager.SettingLabelStyle);

        Event e = Event.current;

        int sliderControlId = GUIUtility.GetControlID(name.GetHashCode(), FocusType.Passive);

        float percentage = Mathf.Clamp01((val - sliderMin) / (sliderMax - sliderMin));

        float fillWidth = sliderRect.width * percentage;
        float thumbX = sliderRect.x + fillWidth - (thumbSize / 2f);
        float thumbY = sliderRect.y + (trackH / 2f) - (thumbSize / 2f);
        Rect thumbRect = new(thumbX, thumbY, thumbSize, thumbSize);

        // Draw track & thumb
        GUI.Box(sliderRect, "", ThemeManager.SliderTrackOffStyle);
        if (fillWidth > 0f)
            GUI.Box(new Rect(sliderRect.x, sliderRect.y, fillWidth, sliderRect.height), "", ThemeManager.SliderTrackOnStyle);
        GUI.Box(thumbRect, "", ThemeManager.SliderThumbStyle);

        void CommitSettingValue()
        {
            if (isFloat) ((FloatSetting)setting).Commit();
            else ((IntSetting)setting).Commit();
        }

        void ApplyFromMouseX(float mouseX)
        {
            float mousePct = Mathf.Clamp01((mouseX - sliderRect.x) / sliderRect.width);
            float newVal = Mathf.Lerp(sliderMin, sliderMax, mousePct);

            if (isFloat)
            {
                float rounded = (float)Math.Round(newVal, decPlaces);
                ((FloatSetting)setting).SetPending(Mathf.Clamp(rounded, sliderMin, sliderMax));
            }
            else
            {
                int intVal = Mathf.RoundToInt(newVal);
                ((IntSetting)setting).SetPending(Math.Clamp(intVal, intSliderMin, intSliderMax));
            }
        }

        // Expanded grab zone for thumb + track
        Rect grabHitBox = new(sliderStartX - 8f, y - 2f, sliderW + 16f, elemH + 4f);

        bool isDraggingThis = ReferenceEquals(DrawSetting.activeNumericSetting, setting);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (grabHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition))
            {
                GUIUtility.hotControl = sliderControlId;
                GUIUtility.keyboardControl = 0;
                DrawSetting.ActiveSliderId = sliderControlId;
                DrawSetting.activeNumericSetting = setting;
                DrawSetting.FocusedControlId = -1;
                DrawSetting.ActiveTextFieldId = -1;

                ApplyFromMouseX(e.mousePosition.x);
                e.Use();
            }
        }
        else if (isDraggingThis)
        {
            if (Input.GetMouseButton(0))
            {
                ApplyFromMouseX(e.mousePosition.x);
                if (e.isMouse && e.type != EventType.Repaint && e.type != EventType.Layout)
                {
                    e.Use();
                }
            }
            else
            {
                CommitSettingValue();
                GUIUtility.hotControl = 0;
                DrawSetting.ActiveSliderId = -1;
                DrawSetting.activeNumericSetting = null;
                if (e.type == EventType.MouseUp)
                {
                    e.Use();
                }
            }
        }

        int controlId = inputRect.GetHashCode();
        bool isFocused = (DrawSetting.ActiveTextFieldId == controlId);

        if (isFocused && DrawSetting.lastFocusedNumericControlId != controlId)
        {
            DrawSetting.CurrentInputBuffer = val.ToString(formatString);
            DrawSetting.lastFocusedNumericControlId = controlId;
        }
        else if (!isFocused && DrawSetting.lastFocusedNumericControlId == controlId)
        {
            CommitSettingValue();
            DrawSetting.lastFocusedNumericControlId = -1;
        }

        string displayValue = isFocused ? DrawSetting.CurrentInputBuffer : val.ToString(formatString);
        string newText = DrawSetting.DrawManualTextField(inputRect, displayValue, "0");

        if (isFocused)
        {
            DrawSetting.CurrentInputBuffer = newText;
            if (double.TryParse(DrawSetting.CurrentInputBuffer, out double parsed))
            {
                if (isFloat) ((FloatSetting)setting).SetPending((float)Math.Round(Mathf.Clamp((float)parsed, trueMin, trueMax), decPlaces));
                else ((IntSetting)setting).SetPending((int)Math.Max(intTrueMin, Math.Min((long)parsed, intTrueMax)));
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                CommitSettingValue();
                DrawSetting.ActiveTextFieldId = -1;
                GUIUtility.keyboardControl = 0;
                e.Use();
            }
        }

        if (DrawResetButton(resetRect))
        {
            if (isFloat) ((FloatSetting)setting).Reset();
            else ((IntSetting)setting).Reset();
        }

        y += elemH + ClientConfig.spacing;
    }
}

/// <summary>
/// A setting module representing a floating-point value with slider and numeric text field controls,
/// supporting decimal precision formatting, slider clamping, and deferred commit buffering.
/// </summary>
public class FloatSetting : Setting
{
    private float _value;

    /// <summary>
    /// The default fallback value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public float DefaultValue;

    /// <summary>
    /// Whether changes to the slider or text box immediately update <see cref="Value"/> rather than waiting for release/commit.
    /// </summary>
    public bool InstantUpdate;

    /// <summary>
    /// The uncommitted pending value during slider dragging or active text box entry.
    /// </summary>
    public float? PendingValue;

    /// <summary>
    /// The visual lower bound of the slider track.
    /// </summary>
    public float Min;

    /// <summary>
    /// The visual upper bound of the slider track.
    /// </summary>
    public float Max;

    /// <summary>
    /// The absolute lower clamping bound allowed when typing directly into the text field.
    /// </summary>
    public float TrueMin;

    /// <summary>
    /// The absolute upper clamping bound allowed when typing directly into the text field.
    /// </summary>
    public float TrueMax;

    /// <summary>
    /// The number of decimal digits displayed and rounded to.
    /// </summary>
    public int DecimalPlaces;

    /// <summary>
    /// Event invoked immediately before <see cref="Value"/> changes.
    /// </summary>
    public Action<float> OnValueChanging { get; set; }

    /// <summary>
    /// Event invoked after <see cref="Value"/> changes.
    /// </summary>
    public Action<float> OnValueChanged { get; set; }

    /// <summary>
    /// Gets the current value being displayed (either <see cref="PendingValue"/> if dragging/editing, or <see cref="Value"/>).
    /// </summary>
    public float DisplayValue => PendingValue ?? _value;

    /// <summary>
    /// Gets or sets the committed floating-point value.
    /// </summary>
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

    /// <summary>
    /// Sets the pending uncommitted value, or commits immediately if <see cref="InstantUpdate"/> is enabled.
    /// </summary>
    /// <param name="val">The proposed float value.</param>
    public void SetPending(float val)
    {
        if (InstantUpdate) { Value = val; PendingValue = null; }
        else PendingValue = val;
    }

    /// <summary>
    /// Commits <see cref="PendingValue"/> into <see cref="Value"/> and clears the pending state.
    /// </summary>
    public void Commit()
    {
        if (PendingValue.HasValue)
        {
            Value = PendingValue.Value;
            PendingValue = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatSetting"/>.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="min">The slider track lower bound.</param>
    /// <param name="max">The slider track upper bound.</param>
    /// <param name="defaultValue">The initial and default float value.</param>
    /// <param name="decimalPlaces">The number of decimal places to format and round to.</param>
    /// <param name="trueMin">The absolute minimum value allowed via manual text entry.</param>
    /// <param name="trueMax">The absolute maximum value allowed via manual text entry.</param>
    /// <param name="instantUpdate">Whether changes are applied immediately while dragging.</param>
    public FloatSetting(string name, float min, float max, float defaultValue, int decimalPlaces = 1, float trueMin = float.MinValue, float trueMax = float.MaxValue, bool instantUpdate = false)
    {
        Name = name;
        Min = min; Max = max; TrueMin = trueMin; TrueMax = trueMax;
        DecimalPlaces = decimalPlaces;
        InstantUpdate = instantUpdate;
        _value = Mathf.Clamp(defaultValue, TrueMin, TrueMax);
        DefaultValue = _value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatSetting"/>.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="min">The slider track lower bound.</param>
    /// <param name="max">The slider track upper bound.</param>
    /// <param name="defaultValue">The initial and default float value.</param>
    /// <param name="decimalPlaces">The number of decimal places to format and round to.</param>
    public FloatSetting(string name, float min, float max, float defaultValue, int decimalPlaces = 1)
    {
        Name=name; 
        Min = min; Max=max; 
        DefaultValue = defaultValue;
        DecimalPlaces=decimalPlaces;
    }

    /// <summary>
    /// Restores the setting back to <see cref="DefaultValue"/> and clears pending edits.
    /// </summary>
    public override void Reset()
    {
        Value = DefaultValue;
        PendingValue = null;
    }

    /// <summary>
    /// Renders the setting row containing the label, slider track, numeric input field, and reset button.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        IntSetting.DrawNumeric(this, ref y, width, true);
    }
}