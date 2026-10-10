using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using System;
using UnityEngine;
using static Magnetar_Client.UI.WindowDrawing.DrawSetting;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core.ModuleManager_;

public static class MultiSelectWindowDrawer
{
    public static Rect WindowRect;
    public static MultiSelectSetting ActiveMultiSelect { get; set; }

    private static GUI.WindowFunction _cachedMultiSelectDelegate;
    private static GUI.WindowFunction MultiSelectDelegate => _cachedMultiSelectDelegate ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawBridge);

    public static void InitializeLayout()
    {
        WindowRect = new Rect(
            Config.NativeWidth / 2f - Config.ModuleManager.MultiSelectWindowWidth / 2f,
            Config.NativeHeight / 2f - Config.ModuleManager.MultiSelectWindowHeight / 2f,
            Config.ModuleManager.MultiSelectWindowWidth,
            Config.ModuleManager.MultiSelectWindowHeight
        );
    }

    public static void Render(Event currentEvent)
    {
        float viewAlpha = AnimationHandler.GetViewAlpha(ModuleManager.Group, ModuleManager.WindowType.SelectionGUI.ToString());
        float currentAlpha = AnimationHandler.CurrentEasedAlpha * viewAlpha;

        if (currentAlpha <= 0.001f) return;

        float targetW = Mathf.Min(Config.ModuleManager.MultiSelectWindowWidth, Config.NativeWidth * 0.95f);
        float targetH = Mathf.Min(Config.ModuleManager.MultiSelectWindowHeight, Config.NativeHeight * 0.8f);

        if (ModuleManager.resetWindowPos)
        {
            WindowRect = new Rect(
                (Config.NativeWidth - targetW) / 2f,
                (Config.NativeHeight - targetH) / 2f,
                targetW,
                targetH
            );
            ModuleManager.resetWindowPos = false;
        }
        else
        {
            Config.RescaleAroundCenter(ref WindowRect, targetW, targetH);
        }

        WindowRect = ScreenBoundaryHelper.Clamp(WindowRect);

        if (ActiveMultiSelect != null)
        {
            Color prevColor = GUI.color;
            Color prevContentColor = GUI.contentColor;

            GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
            GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

            try
            {
                WindowRect = GUI.Window(1000, WindowRect, MultiSelectDelegate, "", ThemeManager.CategoryWindowStyle);

                if (currentEvent != null && WindowRect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDown)
                {
                    Input.ResetInputAxes();
                }
            }
            finally
            {
                GUI.color = prevColor;
                GUI.contentColor = prevContentColor;
            }
        }
        else
        {
            ModuleManager.CurrentWindow = ModuleManager.WindowType.Settings;
        }
    }

    private static void DrawBridge(int id)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float viewAlpha = AnimationHandler.GetViewAlpha(ModuleManager.Group, ModuleManager.WindowType.SelectionGUI.ToString());
        float currentAlpha = AnimationHandler.CurrentEasedAlpha * viewAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            DrawMultiSelectWindow(WindowRect, ActiveMultiSelect, () =>
            {
                ActiveMultiSelect = null;
                ModuleManager.CurrentWindow = ModuleManager.WindowType.Settings;
                AnimationHandler.SwitchView(ModuleManager.Group, ModuleManager.CurrentWindow.ToString());
            });

            float titleHeight = Config.S(34f);
            float closeBtnWidth = Config.S(40f);
            GUI.DragWindow(new Rect(0, 0, WindowRect.width - closeBtnWidth, titleHeight));
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    public static void HandleMultiSelectSetting(MultiSelectSetting set, ref float y, float width)
    {
        Event e = Event.current;
        GUI.Label(new Rect(Config.indent, y, width * 0.4f, Config.elementHeight), Translate(set.Name), ThemeManager.SettingLabelStyle);

        string countLabel = '(' + Translate($"{set.SelectedValues.Count} selected") + ')';
        float countTextWidth = ThemeManager.SettingLabelStyle.CalcSize(new GUIContent(countLabel)).x;

        Rect btnRect = new(
            width - Config.SettingWidth / 2f - Config.selectButtonWidth - countTextWidth / 2f,
            y,
            Config.selectButtonWidth,
            Config.elementHeight
        );

        if (btnRect.Contains(e.mousePosition))
            GUI.backgroundColor = ThemeManager.AccentColor;

        if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
        {
            if (set.OnWindowOpen != null)
            {
                set.OnWindowOpen.Invoke();
            }
            else
            {
                ActiveMultiSelect = set;
                ModuleManager.CurrentWindow = ModuleManager.WindowType.SelectionGUI;
                AnimationHandler.SwitchView(ModuleManager.Group, ModuleManager.WindowType.SelectionGUI.ToString());
            }

            multiSelectSearchQuery = "";
            manualScrollY = 0f;
            e.Use();
        }

        GUI.Box(btnRect, Translate("Select"), ThemeManager.SettingOff);
        GUI.backgroundColor = Color.white;

        Color originalColor = GUI.contentColor;
        GUI.contentColor = ThemeManager.TextDim;
        GUI.Label(new Rect(btnRect.x + Config.selectButtonWidth + Config.S(5f), y, width * 0.4f, Config.elementHeight),
            countLabel, ThemeManager.SettingLabelStyle);
        GUI.contentColor = originalColor;

        y += Config.elementHeight + Config.spacing;
    }
}