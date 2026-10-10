using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
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
            trueMin = s.TrueMin;
            trueMax = s.TrueMax;
            name = s.Name;
            decPlaces = s.DecimalPlaces;
        }
        else
        {
            var s = (IntSetting)setting;
            val = s.DisplayValue;
            sliderMin = s.Min;
            sliderMax = s.Max;
            trueMin = s.TrueMin;
            trueMax = s.TrueMax;
            name = s.Name;
            

            intSliderMin = s.Min;
            intSliderMax = s.Max;
            intTrueMin = s.TrueMin;
            intTrueMax = s.TrueMax;
        }

        string translatedName = Translate(name);
        string formatString = isFloat ? ("0." + new string('0', decPlaces)) : "0";
        
        GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, ThemeManager.SettingLabelStyle);

        float LogConvert(float v) => Mathf.Sign(v) * Mathf.Log10(Mathf.Abs(v) + 1.0f);
        float ExpConvert(float l) => Mathf.Sign(l) * (Mathf.Pow(10.0f, Mathf.Abs(l)) - 1.0f);

        float logMin = LogConvert(sliderMin);
        float logMax = LogConvert(sliderMax);
        float visualVal = Mathf.Clamp(val, sliderMin, sliderMax);
        float logVal = LogConvert(visualVal);
        float percentage = Mathf.Clamp01((logVal - logMin) / (logMax - logMin));

        float inputW = Config.SettingsInput.NumericInputWidth;
        float sliderW = Config.SettingWidth - inputW - 10f;
        float trackH = Config.SettingsInput.SliderHeight;
        float thumbSize = Config.S(16f);

        Rect sliderRect = new(width - Config.indent - Config.SettingWidth, y + ((Config.elementHeight - trackH) / 2f), sliderW, trackH);
        Rect sliderHitBox = new(sliderRect.x, y, sliderRect.width, Config.elementHeight);
        Rect inputRect = new(width - Config.indent - inputW, y, inputW, Config.elementHeight);
        float fillWidth = sliderRect.width * percentage;

        float thumbX = sliderRect.x + fillWidth - (thumbSize / 2f);
        float thumbY = sliderRect.y + (trackH / 2f) - (thumbSize / 2f);
        Rect thumbRect = new(thumbX, thumbY, thumbSize, thumbSize);

        GUI.Box(sliderRect, "", ThemeManager.SliderTrackOffStyle);
        if (fillWidth > 0f)
        {
            GUI.Box(new Rect(sliderRect.x, sliderRect.y, fillWidth, sliderRect.height), "", ThemeManager.SliderTrackOnStyle);
        }

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
            float newLogVal = logMin + (mousePct * (logMax - logMin));
            float newVal = ExpConvert(newLogVal);

            if (isFloat) ((FloatSetting)setting).SetPending((float)System.Math.Round(Mathf.Clamp(newVal, sliderMin, sliderMax), decPlaces));
            else ((IntSetting)setting).SetPending((int)System.Math.Max(intSliderMin, System.Math.Min((long)newVal, intSliderMax)));
        }

        bool inHitbox = sliderHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition);
        int sliderControlId = GUIUtility.GetControlID(name.GetHashCode(), FocusType.Passive);

        if (e.type == EventType.MouseDown && e.button == 0 && inHitbox)
        {
            GUIUtility.hotControl = sliderControlId;
            ActiveSliderId = sliderControlId;
            activeNumericSetting = setting;
            FocusedControlId = -1;
            ActiveTextFieldId = -1;

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
            else if (e.type == EventType.MouseUp || (e.type == EventType.Ignore && e.rawType == EventType.MouseUp))
            {
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
        }
        else if (!isFocused && lastFocusedNumericControlId == controlId)
        {
            CommitSettingValue();
            lastFocusedNumericControlId = -1;
        }

        string displayValue = isFocused ? CurrentInputBuffer : val.ToString(formatString);
        string newText = DrawManualTextField(inputRect, displayValue, "0");

        if (isFocused)
        {
            CurrentInputBuffer = newText;
            if (double.TryParse(CurrentInputBuffer, out double parsed))
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