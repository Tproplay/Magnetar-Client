using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.WindowDrawing;
using System;
using UnityEngine;

namespace Magnetar_Client.Core.HUDManager_;

public static class HUDSelectorDrawer
{
    private const float BaseSelectorWidth = 500f;
    private const float BaseSelectorHeight = 800f;

    public static Rect SelectorRect = new(
        (Config.NativeWidth - Config.S(BaseSelectorWidth)) / 2f,
        (Config.NativeHeight - Config.S(BaseSelectorHeight)) / 2f,
        Config.S(BaseSelectorWidth),
        Config.S(BaseSelectorHeight));

    /// <summary>
    /// Explicitly bound to HudToggles so external multi-select cleans never wipe it to null.
    /// </summary>
    public static MultiSelectSetting ActiveSetting => HUDRenderer.HudToggles;

    private static GUI.WindowFunction _cachedSelectorDelegate;
    private static GUI.WindowFunction SelectorDelegate => _cachedSelectorDelegate ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawElementSelector);

    public static void Open()
    {
        DrawSetting.activeMultiSelect = ActiveSetting;
        DrawSetting.multiSelectSearchQuery = "";
        DrawSetting.manualScrollY = 0f;

        float targetW = Config.S(BaseSelectorWidth);
        float targetH = Mathf.Min(Config.S(BaseSelectorHeight), Config.NativeHeight * 0.8f);
        SelectorRect = new Rect((Config.NativeWidth - targetW) / 2f, (Config.NativeHeight - targetH) / 2f, targetW, targetH);

        AnimationHandler.SwitchView(HUDManager.Group, HUDManager.ViewSelector);
    }

    public static void Render(GUIStyle windowBgStyle)
    {
        float viewAlpha = AnimationHandler.GetViewAlpha(HUDManager.Group, HUDManager.ViewSelector);
        if (viewAlpha <= 0.001f) return;

        float targetW = Config.S(BaseSelectorWidth);
        float targetH = Mathf.Min(Config.S(BaseSelectorHeight), Config.NativeHeight * 0.8f);
        Config.RescaleAroundCenter(ref SelectorRect, targetW, targetH);

        SelectorRect.x = Mathf.Clamp(SelectorRect.x, 0f, Mathf.Max(0f, Config.NativeWidth - SelectorRect.width));
        SelectorRect.y = Mathf.Clamp(SelectorRect.y, 0f, Mathf.Max(0f, Config.NativeHeight - SelectorRect.height));

        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * viewAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * viewAlpha);

        try
        {
            SelectorRect = GUI.Window(2001, SelectorRect, SelectorDelegate, "", windowBgStyle);
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static void DrawElementSelector(int windowID)
    {
        Event e = Event.current;
        Rect multiSelectRect = new(0, 0, SelectorRect.width, SelectorRect.height);

        // Uses ActiveSetting (HudToggles) directly: guaranteed not null upon tab return
        DrawSetting.DrawMultiSelectWindow(multiSelectRect, ActiveSetting, HUDManager.OnClose);

        float titleHeight = Config.S(34f);
        float dragSafeMargin = Config.ShowMobileButtons ? Config.S(35f) : 0f;
        GUI.DragWindow(new Rect(0, 0, SelectorRect.width - dragSafeMargin, titleHeight));

        if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
        {
            Input.ResetInputAxes();
            e.Use();
        }
    }
}