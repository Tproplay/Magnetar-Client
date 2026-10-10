using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using System;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core.GUIManager_;

public static class GUIControlsDrawer
{
    private const float BaseWidth = 520f;
    private const float BaseHeight = 360f;

    public static Rect WindowRect = new(
        (Config.NativeWidth - GUIManager.S(BaseWidth)) / 2f,
        (Config.NativeHeight - GUIManager.S(BaseHeight)) / 2f,
GUIManager.S(BaseWidth),
GUIManager.S(BaseHeight));

    private static GUI.WindowFunction _cachedGuiControls;
    private static GUI.WindowFunction GuiControlsDelegate => _cachedGuiControls ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawGUIControls);

    public static void Render(GUIStyle windowBgStyle)
    {
        float viewAlpha = AnimationHandler.GetViewAlpha(GUIManager.Group, GUIManager.ViewMain);
        if (viewAlpha <= 0.001f) return;

        Config.RescaleAroundCenter(ref WindowRect, GUIManager.S(BaseWidth), WindowRect.height);

        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;

        // Apply alpha to GUI.color BEFORE calling GUI.Window so the window background fades
        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * viewAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * viewAlpha);

        try
        {
            WindowRect = GUI.Window(4000, WindowRect, GuiControlsDelegate, "", windowBgStyle);
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static void DrawGUIControls(int windowID)
    {
        float w = WindowRect.width;
        float indent = Config.indent;
        float rightMargin = GUIManager.S(16f);
        Event e = Event.current;
        float rowSpacing = GUIManager.S(8f);
        float elemH = GUIManager.elementHeight;

#if ANDROID
        float headerHeight = Config.S(26f) * 1.30f;
#else
        float headerHeight = GUIManager.S(26f);
#endif
        float y = headerHeight + GUIManager.S(12f);

        Rect headerBgRect = new(0, 0, w, headerHeight);
        GUI.Box(headerBgRect, Translate("GUI Configuration"), ThemeManager.SettingsWndowStyle);

        float controlWidth = Mathf.Min(Config.SettingWidth, w * 0.45f);
        float controlX = w - rightMargin - controlWidth;
        float labelWidth = controlX - indent - GUIManager.S(10f);

        void DrawButtonRow(string labelText, string btnText, Action onClick, GUIStyle btnStyle = null)
        {
            Rect lblRect = new(indent, y, labelWidth, elemH);
            GUI.Label(lblRect, labelText, ThemeManager.SettingLabelStyle);

            Rect btnRect = new(controlX, y, controlWidth, elemH);
            bool isHovered = btnRect.Contains(e.mousePosition);

            Color prevBg = GUI.backgroundColor;
            if (isHovered) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);

            GUI.Box(btnRect, btnText, btnStyle ?? ThemeManager.SettingOff);
            GUI.backgroundColor = prevBg;

            if (isHovered && e.type == EventType.MouseDown && e.button == 0)
            {
                onClick?.Invoke();
                e.Use();
            }

            y += elemH + rowSpacing;
        }

        void DrawSliderRow(FloatSetting setting)
        {
            string labelText = Translate(setting.Name);
            Rect lblRect = new(indent, y, labelWidth, elemH);
            GUI.Label(lblRect, labelText, ThemeManager.SettingLabelStyle);

            float inputW = SettingValues.NumericInputWidth;
            float gap = SettingValues.Gap;
            float sliderW = controlWidth - inputW - gap;
            float trackH = SettingValues.SliderHeight;
            float thumbSize = GUIManager.S(16f);

            Rect sliderRect = new(controlX, y + ((elemH - trackH) / 2f), sliderW, trackH);
            Rect inputRect = new(controlX + sliderW + gap, y, inputW, elemH);

            float percentage = Mathf.Clamp01((setting.DisplayValue - setting.Min) / (setting.Max - setting.Min));
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

            int sliderControlId = GUIUtility.GetControlID(setting.Name.GetHashCode(), FocusType.Passive);
            Rect grabHitBox = new(sliderRect.x - 6f, y, sliderW + 12f, elemH);

            void ApplyMouseValue(float mouseX)
            {
                float pct = Mathf.Clamp01((mouseX - sliderRect.x) / sliderRect.width);
                float newVal = Mathf.Lerp(setting.Min, setting.Max, pct);
                float rounded = (float)Math.Round(newVal, setting.DecimalPlaces);
                setting.SetPending(Mathf.Clamp(rounded, setting.Min, setting.Max));
            }

            if (e.type == EventType.MouseDown && e.button == 0 && (grabHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition)))
            {
                GUIUtility.hotControl = sliderControlId;
                GUIUtility.keyboardControl = 0;
                DrawSetting.ActiveSliderId = sliderControlId;
                DrawSetting.activeNumericSetting = setting;
                DrawSetting.FocusedControlId = -1;
                DrawSetting.ActiveTextFieldId = -1;

                ApplyMouseValue(e.mousePosition.x);
                e.Use();
            }

            if (GUIUtility.hotControl == sliderControlId)
            {
                if (e.type == EventType.MouseDrag)
                {
                    ApplyMouseValue(e.mousePosition.x);
                    e.Use();
                }
                else if (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp)
                {
                    setting.Commit();
                    GUIUtility.hotControl = 0;
                    DrawSetting.ActiveSliderId = -1;
                    DrawSetting.activeNumericSetting = null;
                    e.Use();
                }
            }

            int controlId = inputRect.GetHashCode();
            bool isFocused = (DrawSetting.ActiveTextFieldId == controlId);
            string formatString = "0." + new string('0', setting.DecimalPlaces);

            if (isFocused && DrawSetting.lastFocusedNumericControlId != controlId)
            {
                DrawSetting.CurrentInputBuffer = setting.DisplayValue.ToString(formatString);
                DrawSetting.lastFocusedNumericControlId = controlId;
            }
            else if (!isFocused && DrawSetting.lastFocusedNumericControlId == controlId)
            {
                setting.Commit();
                DrawSetting.lastFocusedNumericControlId = -1;
            }

            string displayStr = isFocused ? DrawSetting.CurrentInputBuffer : setting.DisplayValue.ToString(formatString);
            string newText = DrawSetting.DrawManualTextField(inputRect, displayStr, "0");

            if (isFocused)
            {
                DrawSetting.CurrentInputBuffer = newText;
                if (double.TryParse(DrawSetting.CurrentInputBuffer, out double parsed))
                {
                    setting.SetPending((float)Math.Round(Mathf.Clamp((float)parsed, setting.TrueMin, setting.TrueMax), setting.DecimalPlaces));
                }
            }

            y += elemH + rowSpacing;
        }

        // 1. Language Row
        DrawButtonRow(
            $"{Translate("Language")}: <color=yellow>{Config.Language}</color>",
            Translate("Change"),
            () => GUISelectorDrawer.Open(GUIManager.LanguageSetting)
        );

        // 2. Theme Row
        string currentTheme = ThemeManager.CurrentThemeName;
        if (GUIManager.ThemeSetting?.SelectedValues != null && GUIManager.ThemeSetting.SelectedValues.Count > 0)
        {
            int selThemeId = GUIManager.ThemeSetting.SelectedValues.First();
            if (GUIManager.ThemeSetting.Options.ContainsKey(selThemeId))
                currentTheme = GUIManager.ThemeSetting.Options[selThemeId];
        }
        Config.Theme = currentTheme;

        DrawButtonRow(
            $"{Translate("Theme")}: <color=yellow>{Config.Theme}</color>",
            Translate("Change"),
            () =>
            {
                GUIManager.RefreshThemeOptions();
                GUISelectorDrawer.Open(GUIManager.ThemeSetting);
            }
        );

        // 3. GUI Scale
        if (GUIManager.ScaleSetting != null)
        {
            if (DrawSetting.ActiveSliderId != GUIManager.ScaleSetting.GetHashCode() && Mathf.Abs(GUIManager.ScaleSetting.Value - Config.GUIScale) > 0.001f)
            {
                GUIManager.ScaleSetting.Value = Config.GUIScale;
            }
            DrawSliderRow(GUIManager.ScaleSetting);
        }

        // 4. Element Scale
        if (GUIManager.ElementScaleSetting != null)
        {
            if (DrawSetting.ActiveSliderId != GUIManager.ElementScaleSetting.GetHashCode() && Mathf.Abs(GUIManager.ElementScaleSetting.Value - Config.ElementScale) > 0.001f)
            {
                GUIManager.ElementScaleSetting.Value = Config.ElementScale;
            }
            DrawSliderRow(GUIManager.ElementScaleSetting);
        }

        // 5. Floating Icon
        DrawButtonRow(
            Translate("Floating Icon"),
            Translate(GUIManager.ShowFloatingIcon ? "ON" : "OFF"),
            () => GUIManager.SetFloatingIcon(!GUIManager.ShowFloatingIcon),
GUIManager.ShowFloatingIcon ? ThemeManager.SettingOn : ThemeManager.SettingOff
        );

        // 6. Mobile Close Buttons
        DrawButtonRow(
            Translate("Mobile Close Buttons"),
            Translate(GUIManager.ShowMobileButtons ? "ON" : "OFF"),
            () => GUIManager.ShowMobileButtons = !GUIManager.ShowMobileButtons,
GUIManager.ShowMobileButtons ? ThemeManager.SettingOn : ThemeManager.SettingOff
        );

        // 7. Show Credits
        DrawButtonRow(
            Translate("Show Main Menu Credits"),
            Translate(Config.ShowMainMenuCredits ? "ON" : "OFF"),
            () => Config.ShowMainMenuCredits = !Config.ShowMainMenuCredits,
            Config.ShowMainMenuCredits ? ThemeManager.SettingOn : ThemeManager.SettingOff
        );

        // 8. Opacity Slider
        if (GUIManager.ShowFloatingIcon && GUIManager.FloatingIconOpacitySetting != null)
        {
            if (DrawSetting.ActiveSliderId != GUIManager.FloatingIconOpacitySetting.GetHashCode() && Mathf.Abs(GUIManager.FloatingIconOpacitySetting.Value - Config.FloatingIconOpacity) > 0.001f)
            {
                GUIManager.FloatingIconOpacitySetting.Value = Config.FloatingIconOpacity;
            }
            DrawSliderRow(GUIManager.FloatingIconOpacitySetting);
        }

        if (DrawSetting.OnPostDraw != null)
        {
            DrawSetting.OnPostDraw.Invoke();
            DrawSetting.OnPostDraw = null;
        }

        WindowRect.height = y + GUIManager.S(12f);

        GUI.DragWindow(new Rect(0, 0, w, headerHeight));

        Rect clientArea = new(0, 0, w, WindowRect.height);
        if (clientArea.Contains(e.mousePosition) && e.type == EventType.MouseDown)
        {
            Input.ResetInputAxes();
            e.Use();
        }
    }
}