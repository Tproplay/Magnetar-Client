using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.WindowDrawing;
using System;
using UnityEngine;

namespace Magnetar_Client.Core.GUIManager_;

public static class GUISelectorDrawer
{
    private const float BaseSelectorWidth = 500f;
    private const float BaseSelectorHeight = 800f;

    public static Rect SelectorRect = new(
        (Config.NativeWidth - GUIManager.S(BaseSelectorWidth)) / 2f,
        (Config.NativeHeight - GUIManager.S(BaseSelectorHeight)) / 2f,
GUIManager.S(BaseSelectorWidth),
GUIManager.S(BaseSelectorHeight));

    public static MultiSelectSetting ActiveSetting { get; set; }

    private static GUI.WindowFunction _cachedSelector;
    private static GUI.WindowFunction SelectorDelegate => _cachedSelector ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawSelectorModal);

    public static void Open(MultiSelectSetting setting)
    {
        ActiveSetting = setting;
        DrawSetting.activeMultiSelect = setting;
        DrawSetting.multiSelectSearchQuery = "";
        DrawSetting.manualScrollY = 0f;

        float targetW = GUIManager.S(BaseSelectorWidth);
        float targetH = Mathf.Min(GUIManager.S(BaseSelectorHeight), Config.NativeHeight * 0.8f);
        SelectorRect = new Rect((Config.NativeWidth - targetW) / 2f, (Config.NativeHeight - targetH) / 2f, targetW, targetH);

        AnimationHandler.SwitchView(GUIManager.Group, GUIManager.ViewSelector);
    }

    public static void Render(GUIStyle windowBgStyle)
    {
        float viewAlpha = AnimationHandler.GetViewAlpha(GUIManager.Group, GUIManager.ViewSelector);
        if (viewAlpha <= 0.001f) return;

        float targetW = GUIManager.S(BaseSelectorWidth);
        float targetH = Mathf.Min(GUIManager.S(BaseSelectorHeight), Config.NativeHeight * 0.8f);
        Config.RescaleAroundCenter(ref SelectorRect, targetW, targetH);

        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * viewAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * viewAlpha);

        try
        {
            SelectorRect = GUI.Window(4001, SelectorRect, SelectorDelegate, "", windowBgStyle);
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static void DrawSelectorModal(int windowID)
    {
        Event e = Event.current;
        Rect multiSelectRect = new(0, 0, SelectorRect.width, SelectorRect.height);

        MultiSelectSetting settingToDraw = ActiveSetting ?? DrawSetting.activeMultiSelect;
        DrawSetting.DrawMultiSelectWindow(multiSelectRect, settingToDraw, GUIManager.OnClose);

#if ANDROID
        float titleHeight = Config.S(25f) * 1.30f;
#else
        float titleHeight = GUIManager.S(25f);
#endif
        float dragSafeMargin = GUIManager.ShowMobileButtons ? GUIManager.S(35f) : 0f;
        GUI.DragWindow(new Rect(0, 0, SelectorRect.width - dragSafeMargin, titleHeight));

        if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
        {
            Input.ResetInputAxes();
            e.Use();
        }
    }
}