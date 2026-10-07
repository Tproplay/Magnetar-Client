using Magnetar_Client.Api;
using Magnetar_Client.Core.Lifecycle;
using Magnetar_Client.Game;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;
using Magnetar_Client.Core.HUDManager_;

namespace Magnetar_Client.Core;

public static class HUDManager
{
    public const string Group = "HUD";
    public const string ViewMain = "Controls";
    public const string ViewSelector = "Selector";

    public static bool IsInitialized { get; private set; } = false;
    public static bool Enabled = true;
    public static bool forceShow = false;
    public static bool isSelectingElements => UIAnimationHelper.GetViewAlpha(Group, ViewSelector) > 0.001f;
    public static bool showBackground = false;

    private const float BaseWidth = 500f;
    private const float BaseHeight = 300f;
    private const float BaseElementHeight = 25f;
    private const float BaseSelectorWidth = 500f;
    private const float BaseSelectorHeight = 800f;

    public static float elementHeight => Config.S(BaseElementHeight);

    public static Rect windowRect = Rect.zero;
    public static Rect selectorRect = Rect.zero;

    private static bool _rectsInitialized = false;
    private static readonly Action _cachedOnClose = OnClose;

    public static void Init()
    {
        if (IsInitialized) return;

        UIAnimationHelper.SetViewImmediate(Group, ViewMain, 1.0f);
        UIAnimationHelper.SetViewImmediate(Group, ViewSelector, 0.0f);

        EnsureRects();
        ServiceRegistry.Register(new HUDManagerService());

        SafeToCloseManager.RegisterGuard(() =>
        {
            if (forceShow || (Config.CurrentTab == TabType.HUD && isSelectingElements))
                return false;

            return true;
        });

        HUDRenderer.Init();
        IsInitialized = true;
        DebugLogger.Msg("[HUDManager] Initialized and registered HUD service.");
    }

    public static void ExitLayoutMode()
    {
        forceShow = false;
        Config.showgui = true;
        UIAnimationHelper.SnapToVisible();
        UIAnimationHelper.SwitchView(Group, ViewMain);

        SaveLoad.Save();
        Main.ResetInputBind();
        Input.ResetInputAxes();
    }

    public static void OnClose()
    {
        UIAnimationHelper.SwitchView(Group, ViewMain);
        UI.WindowDrawing.DrawSetting.activeMultiSelect = null;
    }

    private static void EnsureRects()
    {
        float targetSelectorWidth = Config.S(BaseSelectorWidth);
        float maxSelectorHeight = Config.NativeHeight * 0.8f;
        float targetSelectorHeight = Mathf.Min(Config.S(BaseSelectorHeight), maxSelectorHeight);

        if (!_rectsInitialized)
        {
            windowRect = new Rect(
                (Config.NativeWidth - Config.S(BaseWidth)) / 2f,
                (Config.NativeHeight - Config.S(BaseHeight)) / 2f,
                Config.S(BaseWidth),
                Config.S(BaseHeight));

            selectorRect = new Rect(
                (Config.NativeWidth - targetSelectorWidth) / 2f,
                (Config.NativeHeight - targetSelectorHeight) / 2f,
                targetSelectorWidth,
                targetSelectorHeight);

            _rectsInitialized = true;
        }

        Config.RescaleAroundCenter(ref windowRect, Config.S(BaseWidth), windowRect.height);
        Config.RescaleAroundCenter(ref selectorRect, targetSelectorWidth, targetSelectorHeight);

        selectorRect.x = Mathf.Clamp(selectorRect.x, 0f, Mathf.Max(0f, Config.NativeWidth - selectorRect.width));
        selectorRect.y = Mathf.Clamp(selectorRect.y, 0f, Mathf.Max(0f, Config.NativeHeight - selectorRect.height));
    }

    private static GUI.WindowFunction _cachedSelectorDelegate;
    private static GUI.WindowFunction _cachedControlsDelegate;

    private static GUI.WindowFunction GetSelectorDelegate() =>
        _cachedSelectorDelegate ??= Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawElementSelector);

    private static GUI.WindowFunction GetControlsDelegate() =>
        _cachedControlsDelegate ??= Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawHUDControls);

    public static void RenderDimBackground()
    {
        float currentAlpha = forceShow ? 1.0f : UIAnimationHelper.CurrentEasedDimAlpha;
        if (currentAlpha <= 0.001f) return;

        Event e = Event.current;
        if (e == null) return;

        if (e.type == EventType.Repaint && ThemeManager.DimBackgroundStyle != null)
        {
            Matrix4x4 prevMatrix = GUI.matrix;
            Color prevColor = GUI.color;

            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(1f, 1f, 1f, currentAlpha);

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", ThemeManager.DimBackgroundStyle);

            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        if (e.type == EventType.MouseDown && UI.WindowDrawing.DrawSetting.activeSliderId == -1 && UI.WindowDrawing.DrawSetting.activeDropdownId == -1)
        {
            Input.ResetInputAxes();
        }
    }

    public static void RenderControlsWindow()
    {
        GUIStyle windowBgStyle = ThemeManager.SettingsWndowBgStyle ?? ThemeManager.SettingsWndowStyle;

        if (UIAnimationHelper.GetViewAlpha(Group, ViewSelector) > 0.001f)
        {
            selectorRect = GUI.Window(2001, selectorRect, GetSelectorDelegate(), "", windowBgStyle);
        }

        if (UIAnimationHelper.GetViewAlpha(Group, ViewMain) > 0.001f)
        {
            windowRect = GUI.Window(2000, windowRect, GetControlsDelegate(), "", windowBgStyle);
        }
    }

    public static void Render()
    {
        EnsureRects();

        try
        {
            Event e = Event.current;

            if (forceShow && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                ExitLayoutMode();
                e.Use();
            }
            else if (isSelectingElements && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                OnClose();
                e.Use();
            }

            RenderModCredit();
            RenderDimBackground();

            if (forceShow)
            {
                DrawExitLayoutButton();
            }

            if (Config.CurrentTab == TabType.HUD && !forceShow && Config.showgui)
            {
                RenderControlsWindow();
            }

            HUDRenderer.RenderOverlay();
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[HUDManager] Fatal error inside HUDManager.Render: {ex}");
        }
    }

    private static void DrawExitLayoutButton()
    {
        if (Config.ShowMobileButtons)
        {
            Event e = Event.current;
            float btnWidth = Config.S(180f);
            float btnHeight = Config.S(36f);
            Rect exitRect = new((Config.NativeWidth - btnWidth) / 2f, Config.S(16f), btnWidth, btnHeight);

            bool isHovered = exitRect.Contains(e.mousePosition);

            GUI.Box(exitRect, Translator.Translate("Exit Layout"), ThemeManager.SettingOn);

            if (e.type == EventType.MouseDown && e.button == 0 && isHovered)
            {
                ExitLayoutMode();
                e.Use();
            }
        }
    }

    private static void DrawElementSelector(int windowID)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.GetViewAlpha(Group, ViewSelector);

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            Event e = Event.current;
            Rect multiSelectRect = new(0, 0, selectorRect.width, selectorRect.height);
            UI.WindowDrawing.DrawSetting.DrawMultiSelectWindow(multiSelectRect, UI.WindowDrawing.DrawSetting.activeMultiSelect, _cachedOnClose);

            float titleHeight = Config.S(34f);
            float dragSafeMargin = Config.ShowMobileButtons ? Config.S(35f) : 0f;
            GUI.DragWindow(new Rect(0, 0, selectorRect.width - dragSafeMargin, titleHeight));

            if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
            }
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static void DrawHUDControls(int windowID)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.GetViewAlpha(Group, ViewMain);

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            float width = windowRect.width;
            float indent = Config.S(10f);
            Event e = Event.current;
            float y = Config.S(35f);

            Rect headerBgRect = new(0, 0, width, y - indent);
            GUI.Box(headerBgRect, Translator.Translate("Customize HUD"), ThemeManager.SettingsWndowStyle);

            int activeCount = HUDRenderer.HudToggles != null ? HUDRenderer.HudToggles.SelectedValues.Count : 0;
            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight),
                Translator.Translate("Elements") + $" ({activeCount})",
                ThemeManager.SettingLabelStyle);

            Rect selectBtnRect = new(width * 0.5f, y, width * 0.45f, elementHeight);
            bool isSelectHovered = selectBtnRect.Contains(e.mousePosition);

            Color prevBg = GUI.backgroundColor;
            if (isSelectHovered) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
            GUI.Box(selectBtnRect, Translator.Translate("Select"), ThemeManager.SettingOff);
            GUI.backgroundColor = prevBg;

            if (e.type == EventType.MouseDown && e.button == 0 && isSelectHovered)
            {
                e.Use();
                UI.WindowDrawing.DrawSetting.activeMultiSelect = HUDRenderer.HudToggles;
                UI.WindowDrawing.DrawSetting.multiSelectSearchQuery = "";
                UI.WindowDrawing.DrawSetting.manualScrollY = 0f;

                float targetW = Config.S(BaseSelectorWidth);
                float targetH = Mathf.Min(Config.S(BaseSelectorHeight), Config.NativeHeight * 0.8f);

                selectorRect = new Rect(
                    (Config.NativeWidth - targetW) / 2f,
                    (Config.NativeHeight - targetH) / 2f,
                    targetW,
                    targetH
                );

                UIAnimationHelper.SwitchView(Group, ViewSelector);
            }

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Layout"),
                ThemeManager.SettingLabelStyle);

            Rect configBtnRect = new(width * 0.5f, y, width * 0.45f, elementHeight);
            bool isConfigHovered = configBtnRect.Contains(e.mousePosition);

            prevBg = GUI.backgroundColor;
            if (isConfigHovered) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
            GUI.Box(configBtnRect, Translator.Translate("Edit"), ThemeManager.SettingOff);
            GUI.backgroundColor = prevBg;

            if (e.type == EventType.MouseDown && e.button == 0 && isConfigHovered)
            {
                e.Use();
                forceShow = true;
                Config.showgui = false;
                DebugLogger.Msg("Escape Triggered : Hud Window -> Edit Layout");
            }

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Background"),
                ThemeManager.SettingLabelStyle);
            Rect bgRect = new(width * 0.5f, y, width * 0.45f, elementHeight);
            bool bgHover = bgRect.Contains(e.mousePosition);

            prevBg = GUI.backgroundColor;
            if (bgHover) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
            GUI.Box(bgRect, showBackground ? Translator.Translate("ON") : Translator.Translate("OFF"),
                showBackground ? ThemeManager.SettingOn : ThemeManager.SettingOff);
            GUI.backgroundColor = prevBg;

            if (bgHover && e.type == EventType.MouseDown && e.button == 0)
            {
                showBackground = !showBackground;
                e.Use();
            }

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Enabled"),
                ThemeManager.SettingLabelStyle);
            Rect enabledRect = new(width * 0.5f, y, width * 0.45f, elementHeight);
            bool enabledHover = enabledRect.Contains(e.mousePosition);

            prevBg = GUI.backgroundColor;
            if (enabledHover) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
            GUI.Box(enabledRect, Enabled ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Enabled ? ThemeManager.SettingOn : ThemeManager.SettingOff);
            GUI.backgroundColor = prevBg;

            if (enabledHover && e.type == EventType.MouseDown && e.button == 0)
            {
                Enabled = !Enabled;
                e.Use();
            }

            y += elementHeight + Config.S(10f);
            windowRect.height = y;

            GUI.DragWindow(new Rect(0, 0, width, Config.S(25f)));

            Rect _windowRect = new(0, 0, width, y);
            if (_windowRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
            }
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    public static void OnLanguageChange()
    {
        if (HUDRenderer.HudToggles?.Options == null) return;
        foreach (var keypair in HUDRenderer.HudToggles.Options)
        {
            HUDRenderer.HudToggles.CustomNames[keypair.Key] = Translator.Translate(keypair.Value);
        }
    }

    public static void RenderModCredit()
    {
        if (!Config.ShowMainMenuCredits) return;
        if (Config.showgui || HUDManager.forceShow) return;
        if (!AppData.InMainMenu) return;

        string Text = "Magnetar Client <color=white>by</color> <color=red>Tproplay</color>";
        GUIContent content = new(Text);

        GUIStyle style = new()
        {
            alignment = TextAnchor.UpperRight,
            richText = true,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
            fontSize = (int)Config.NativeHeight / 36
        };

        float width = style.CalcSize(content).x;
        Rect rect = new() { x = Config.NativeWidth * 0.995f - width, width = width };

        GUIHelper.DrawBoxWithOutlinedText(rect, Text, style, GUIHelper.RainbowColor, Color.black);
    }

    private class HUDManagerService : IInitializable, IWarmUp, IUpdatable, IRenderable, IMenuRenderable, ICloseHandler, ILanguageAware
    {
        public string Name => "HUDManager";
        public int Priority => ServicePriority.HUD;

        public void Initialize() => HUDManager.EnsureRects();
        public void OnWarmUp() => HUDManager.EnsureRects();
        public void OnUpdate() => HUDRenderer.UpdateElements();

        public void OnGUI()
        {
            HUDManager.EnsureRects();
            HUDManager.RenderModCredit();
            HUDManager.RenderDimBackground();

            if (HUDManager.forceShow)
            {
                HUDManager.DrawExitLayoutButton();
            }

            HUDRenderer.RenderOverlay();
        }

        public void OnMenuGUI()
        {
            if (Config.CurrentTab == TabType.HUD && !HUDManager.forceShow)
            {
                HUDManager.RenderControlsWindow();
            }
        }

        public bool CanClose()
        {
            return !HUDManager.forceShow && !HUDManager.isSelectingElements;
        }

        public bool OnEscapePressed()
        {
            if (HUDManager.forceShow)
            {
                ExitLayoutMode();
                return true;
            }

            if (HUDManager.isSelectingElements)
            {
                HUDManager.OnClose();
                Main.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }

        public void OnLanguageChanged() => HUDManager.OnLanguageChange();
    }
}