using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using UnityEngine;
using Magnetar_Client.Core;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    /// <summary>
    /// Renders a numeric setting control (slider and manual input field) and handles user interaction.
    /// </summary>
    /// <param name="setting">The numeric setting object, which must be a <see cref="FloatSetting"/> or <see cref="IntSetting"/>.</param>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    /// <param name="isFloat"><see langword="true"/> if the setting is a <see cref="FloatSetting"/>; otherwise <see langword="false"/> for <see cref="IntSetting"/>.</param>
    public static void HandleNumericSetting(object setting, ref float y, float width, bool isFloat)
    {
        float val, sliderMin, sliderMax, trueMin, trueMax;
        string name;
        int decPlaces = 0;

        int intTrueMin = 0, intTrueMax = 0;
        int intSliderMin = 0, intSliderMax = 0;

        if (isFloat)
        {
            var s = (FloatSetting)setting;
            val = s.DisplayValue;
            sliderMin = s.Min;
            sliderMax = s.Max;
            trueMin = (s.TrueMin == 0f && s.TrueMax == 0f) ? float.MinValue : s.TrueMin;
            trueMax = (s.TrueMin == 0f && s.TrueMax == 0f) ? float.MaxValue : s.TrueMax; if (trueMax < trueMin) trueMax = float.MaxValue;
            name = s.Name;
            decPlaces = s.DecimalPlaces;
        }
        else
        {
            var s = (IntSetting)setting;
            val = s.DisplayValue;
            sliderMin = s.Min;
            sliderMax = s.Max;
            trueMin = (s.TrueMin == 0f && s.TrueMax == 0f) ? float.MinValue : s.TrueMin;
            trueMax = (s.TrueMin == 0f && s.TrueMax == 0f) ? float.MaxValue : s.TrueMax; if (trueMax < trueMin) trueMax = float.MaxValue;
            name = s.Name;
            

            intSliderMin = s.Min;
            intSliderMax = s.Max;
            intTrueMin = (s.TrueMin == 0 && s.TrueMax == 0) ? int.MinValue : s.TrueMin; trueMin = intTrueMin;
            intTrueMax = (s.TrueMin == 0 && s.TrueMax == 0) ? int.MaxValue : s.TrueMax; if (intTrueMax < intTrueMin) intTrueMax = int.MaxValue; trueMax = intTrueMax;
        }

        string translatedName = (setting as Magnetar_Client.UI.Setting.Setting)?.DisplayName ?? Translate(name);
        string formatString = isFloat ? ("0." + new string('0', decPlaces)) : "0";
        bool isLogarithmic = isFloat ? ((FloatSetting)setting).IsLogarithmic : ((IntSetting)setting).IsLogarithmic;
        
        GUI.Label(new Rect(ClientConfig.indent, y, width - ClientConfig.indent * 2 - ClientConfig.SettingWidth, ClientConfig.elementHeight), translatedName, ThemeManager.SettingLabelStyle);

        float LogConvert(float v) => Mathf.Sign(v) * Mathf.Log10(Mathf.Abs(v) + 1.0f);
        float ExpConvert(float l) => Mathf.Sign(l) * (Mathf.Pow(10.0f, Mathf.Abs(l)) - 1.0f);

        float visualVal = Mathf.Clamp(val, sliderMin, sliderMax);
        if (ReferenceEquals(activeNumericSetting, setting))
        {
            if (isFloat && ((FloatSetting)setting).DragValue.HasValue) visualVal = Mathf.Clamp(((FloatSetting)setting).DragValue.Value, sliderMin, sliderMax);
            else if (!isFloat && ((IntSetting)setting).DragValue.HasValue) visualVal = Mathf.Clamp(((IntSetting)setting).DragValue.Value, sliderMin, sliderMax);
        }

        float percentage;
        if (isLogarithmic)
        {
            float logMin = LogConvert(sliderMin);
            float logMax = LogConvert(sliderMax);
            float logRange = logMax - logMin;
            float logVal = LogConvert(visualVal);
            percentage = Mathf.Abs(logRange) > 0.00001f ? Mathf.Clamp01((logVal - logMin) / logRange) : 0f;
        }
        else
        {
            float range = sliderMax - sliderMin;
            percentage = range > 0.0001f ? Mathf.Clamp01((visualVal - sliderMin) / range) : 0f;
        }

        float inputW = SettingValues.NumericInputWidth;
        float sliderW = ClientConfig.SettingWidth - inputW - 10f;
        float trackH = SettingValues.SliderHeight;
        float thumbSize = GUIManager.S(16f);

        Rect sliderRect = new(width - ClientConfig.indent - ClientConfig.SettingWidth, y + ((ClientConfig.elementHeight - trackH) / 2f), sliderW, trackH);
        Rect sliderHitBox = new(sliderRect.x, y, sliderRect.width, ClientConfig.elementHeight);
        Rect inputRect = new(width - ClientConfig.indent - inputW, y, inputW, ClientConfig.elementHeight);
        float fillWidth = sliderRect.width * percentage;

        float thumbX = sliderRect.x + fillWidth - (thumbSize / 2f);
        float thumbY = sliderRect.y + (trackH / 2f) - (thumbSize / 2f);
        Rect thumbRect = new(thumbX, thumbY, thumbSize, thumbSize);

        // Segmented track to avoid overlapping track on/off or drawing track underneath the thumb (which causes double alpha-blend / opaque artifacts)
        float overlapTolerance = GUIManager.S(1f);

        // Active track (left of thumb)
        float activeEndX = Mathf.Clamp(thumbX + overlapTolerance, sliderRect.x, sliderRect.x + sliderRect.width);
        float activeWidth = activeEndX - sliderRect.x;
        if (activeWidth > 0f)
        {
            GUI.Box(new Rect(sliderRect.x, sliderRect.y, activeWidth, sliderRect.height), "", ThemeManager.SliderTrackOnStyle);
        }

        // Inactive track (right of thumb)
        float inactiveStartX = Mathf.Clamp(thumbX + thumbSize - overlapTolerance, sliderRect.x, sliderRect.x + sliderRect.width);
        float inactiveWidth = (sliderRect.x + sliderRect.width) - inactiveStartX;
        if (inactiveWidth > 0f)
        {
            GUI.Box(new Rect(inactiveStartX, sliderRect.y, inactiveWidth, sliderRect.height), "", ThemeManager.SliderTrackOffStyle);
        }

        // Thumb
        GUI.Box(thumbRect, "", ThemeManager.SliderThumbStyle);

        Event e = Event.current;

        void CommitSettingValue()
        {
            if (isFloat) ((FloatSetting)setting).Commit();
            else ((IntSetting)setting).Commit();
        }

        void ApplyFromMouseX(float mouseX)
        {
            float mousePct = Mathf.Clamp01((mouseX - sliderRect.x) / sliderRect.width);
            float newVal;
            if (isLogarithmic)
            {
                float logMin = LogConvert(sliderMin);
                float logMax = LogConvert(sliderMax);
                float logVal = Mathf.Lerp(logMin, logMax, mousePct);
                newVal = Mathf.Clamp(ExpConvert(logVal), Mathf.Min(sliderMin, sliderMax), Mathf.Max(sliderMin, sliderMax));
            }
            else
            {
                newVal = Mathf.Lerp(sliderMin, sliderMax, mousePct);
            }

            if (isFloat)
            {
                var fs = (FloatSetting)setting;
                fs.DragValue = newVal;
                fs.SetPending((float)System.Math.Round(Mathf.Clamp(newVal, sliderMin, sliderMax), decPlaces));
            }
            else
            {
                var ints = (IntSetting)setting;
                ints.DragValue = newVal;
                ints.SetPending((int)System.Math.Max(intSliderMin, System.Math.Min((long)newVal, intSliderMax)));
            }
        }

        bool inHitbox = sliderHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition);
        int sliderControlId = GUIUtility.GetControlID(name.GetHashCode(), FocusType.Passive);

        if (e.type == EventType.MouseDown && e.button == 1 && inHitbox)
        {
            if (isFloat) ((FloatSetting)setting).IsLogarithmic = !((FloatSetting)setting).IsLogarithmic;
            else ((IntSetting)setting).IsLogarithmic = !((IntSetting)setting).IsLogarithmic;
            e.Use();
        }

        if (e.type == EventType.MouseDown && e.button == 0 && inHitbox)
        {
            GUIUtility.hotControl = sliderControlId;
            ActiveSliderId = sliderControlId;
            activeNumericSetting = setting;
            FocusedControlId = -1;
            ActiveTextFieldId = -1;
            ActiveTextSetting = null;

            ApplyFromMouseX(e.mousePosition.x);
            e.Use();
        }

        if (GUIUtility.hotControl == sliderControlId)
        {
            if (e.type == EventType.MouseDrag)
            {
                ApplyFromMouseX(e.mousePosition.x);
                e.Use();
            }
            else if (e.type == EventType.MouseUp || (e.type == EventType.Ignore && e.rawType == EventType.MouseUp) || !Input.GetMouseButton(0))
            {
                if (isFloat)
                {
                    var fs = (FloatSetting)setting;
                    if (fs.DragValue.HasValue)
                    {
                        fs.SetPending((float)System.Math.Round(Mathf.Clamp(fs.DragValue.Value, sliderMin, sliderMax), decPlaces));
                        fs.DragValue = null;
                    }
                }
                else
                {
                    var ints = (IntSetting)setting;
                    if (ints.DragValue.HasValue)
                    {
                        ints.SetPending((int)System.Math.Max(intSliderMin, System.Math.Min((long)ints.DragValue.Value, intSliderMax)));
                        ints.DragValue = null;
                    }
                }

                CommitSettingValue();
                GUIUtility.hotControl = 0;
                ActiveSliderId = -1;
                activeNumericSetting = null;
                e.Use();
            }
        }

        int controlId = inputRect.GetHashCode();
        bool isFocused = (ActiveTextFieldId == controlId);

        if (isFocused && lastFocusedNumericControlId != controlId)
        {
            CurrentInputBuffer = val.ToString(formatString);
            lastFocusedNumericControlId = controlId;
            ActiveTextSetting = setting as Magnetar_Client.UI.Setting.Setting;
        }
        else if (!isFocused && lastFocusedNumericControlId == controlId)
        {
            CommitSettingValue();
            lastFocusedNumericControlId = -1;
            if (ReferenceEquals(ActiveTextSetting, setting))
            {
                ActiveTextSetting = null;
            }
        }

        string displayValue = isFocused ? CurrentInputBuffer : val.ToString(formatString);
        string newText = DrawManualTextField(inputRect, displayValue, "0", null, setting as Magnetar_Client.UI.Setting.Setting);

        if (isFocused)
        {
            CurrentInputBuffer = newText;
            if (!isFloat && long.TryParse(CurrentInputBuffer, out long parsedLong))
            {
                long clamped = System.Math.Max((long)intTrueMin, System.Math.Min(parsedLong, (long)intTrueMax));
                ((IntSetting)setting).SetPending((int)clamped);
            }
            else if (double.TryParse(CurrentInputBuffer, out double parsed))
            {
                if (isFloat)
                {
                    ((FloatSetting)setting).SetPending((float)System.Math.Round(Mathf.Clamp((float)parsed, trueMin, trueMax), decPlaces));
                }
                else
                {
                    ((IntSetting)setting).SetPending((int)System.Math.Max(intTrueMin, System.Math.Min((long)parsed, intTrueMax)));
                }
            }
        }
    }
}