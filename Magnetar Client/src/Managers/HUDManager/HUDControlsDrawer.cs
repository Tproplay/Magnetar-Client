using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using UnityEngine;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core.HUDManager_;

public static class HUDControlsDrawer
{
    private const float BaseWidth = 500f;
    private const float BaseHeight = 300f;

    public static Rect WindowRect = new(
        (Config.NativeWidth - Config.S(BaseWidth)) / 2f,
        (Config.NativeHeight - Config.S(BaseHeight)) / 2f,
        Config.S(BaseWidth),
        Config.S(BaseHeight));

    private static GUI.WindowFunction _cachedControlsDelegate;
    private static GUI.WindowFunction ControlsDelegate => _cachedControlsDelegate ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawHUDControls);

    public static void Render(GUIStyle windowBgStyle)
    {
        float viewAlpha = UIAnimationHelper.GetViewAlpha(HUDManager.Group, HUDManager.ViewMain);
        if (viewAlpha <= 0.001f) return;

        Config.RescaleAroundCenter(ref WindowRect, Config.S(BaseWidth), WindowRect.height);

        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * viewAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * viewAlpha);

        try
        {
            WindowRect = GUI.Window(2000, WindowRect, ControlsDelegate, "", windowBgStyle);
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static void DrawHUDControls(int windowID)
    {
        float width = WindowRect.width;
        float indent = Config.S(10f);
        Event e = Event.current;
        float y = Config.S(35f);
        float elemH = HUDManager.elementHeight;

        Rect headerBgRect = new(0, 0, width, y - indent);
        GUI.Box(headerBgRect, Translate("Customize HUD"), ThemeManager.SettingsWndowStyle);

        int activeCount = HUDRenderer.HudToggles != null ? HUDRenderer.HudToggles.SelectedValues.Count : 0;
        GUI.Label(new Rect(indent, y, width * 0.45f, elemH),
            Translate("Elements") + $" ({activeCount})",
            ThemeManager.SettingLabelStyle);

        Rect selectBtnRect = new(width * 0.5f, y, width * 0.45f, elemH);
        bool isSelectHovered = selectBtnRect.Contains(e.mousePosition);

        Color prevBg = GUI.backgroundColor;
        if (isSelectHovered) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
        GUI.Box(selectBtnRect, Translate("Select"), ThemeManager.SettingOff);
        GUI.backgroundColor = prevBg;

        if (e.type == EventType.MouseDown && e.button == 0 && isSelectHovered)
        {
            e.Use();
            HUDSelectorDrawer.Open();
        }

        y += elemH + Config.S(5f);

        GUI.Label(new Rect(indent, y, width * 0.45f, elemH), Translate("Layout"), ThemeManager.SettingLabelStyle);

        Rect configBtnRect = new(width * 0.5f, y, width * 0.45f, elemH);
        bool isConfigHovered = configBtnRect.Contains(e.mousePosition);

        prevBg = GUI.backgroundColor;
        if (isConfigHovered) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
        GUI.Box(configBtnRect, Translate("Edit"), ThemeManager.SettingOff);
        GUI.backgroundColor = prevBg;

        if (e.type == EventType.MouseDown && e.button == 0 && isConfigHovered)
        {
            e.Use();
            HUDManager.forceShow = true;
            Config.showgui = false;
        }

        y += elemH + Config.S(5f);

        GUI.Label(new Rect(indent, y, width * 0.45f, elemH), Translate("Background"), ThemeManager.SettingLabelStyle);
        Rect bgRect = new(width * 0.5f, y, width * 0.45f, elemH);
        bool bgHover = bgRect.Contains(e.mousePosition);

        prevBg = GUI.backgroundColor;
        if (bgHover) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
        GUI.Box(bgRect, HUDManager.showBackground ? Translate("ON") : Translate("OFF"),
            HUDManager.showBackground ? ThemeManager.SettingOn : ThemeManager.SettingOff);
        GUI.backgroundColor = prevBg;

        if (bgHover && e.type == EventType.MouseDown && e.button == 0)
        {
            HUDManager.showBackground = !HUDManager.showBackground;
            e.Use();
        }

        y += elemH + Config.S(5f);

        GUI.Label(new Rect(indent, y, width * 0.45f, elemH), Translate("Enabled"), ThemeManager.SettingLabelStyle);
        Rect enabledRect = new(width * 0.5f, y, width * 0.45f, elemH);
        bool enabledHover = enabledRect.Contains(e.mousePosition);

        prevBg = GUI.backgroundColor;
        if (enabledHover) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
        GUI.Box(enabledRect, HUDManager.Enabled ? Translate("ON") : Translate("OFF"),
            HUDManager.Enabled ? ThemeManager.SettingOn : ThemeManager.SettingOff);
        GUI.backgroundColor = prevBg;

        if (enabledHover && e.type == EventType.MouseDown && e.button == 0)
        {
            HUDManager.Enabled = !HUDManager.Enabled;
            e.Use();
        }

        y += elemH + Config.S(10f);
        WindowRect.height = y;

        GUI.DragWindow(new Rect(0, 0, width, Config.S(25f)));

        Rect clientArea = new(0, 0, width, y);
        if (clientArea.Contains(e.mousePosition) && e.type == EventType.MouseDown)
        {
            Input.ResetInputAxes();
            e.Use();
        }
    }
}