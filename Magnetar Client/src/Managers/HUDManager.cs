using Magnetar_Client.HUDElements;
using Magnetar_Client.UI.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Magnetar_Logger;
using Magnetar_Client.UI;
using Magnetar_Client.Game;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Core.Lifecycle;
using Magnetar_Client.Api;

namespace Magnetar_Client.Core;

public static class HUDManager
{
    public static bool IsInitialized { get; private set; } = false;
    public static bool Enabled = true;
    public static bool forceShow = false;
    public static bool isSelectingElements = false;
    public static bool showBackground = false;

    // Flag to suppress sub-window fade transition when returning from Edit Layout mode
    private static bool _suppressNextTransition = false;

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

        EnsureRects();

        // Register to centralized ServiceRegistry
        ServiceRegistry.Register(new HUDManagerService());

        // Register close guard: block GUI closure while editing layout or picking elements
        SafeToCloseManager.RegisterGuard(() =>
        {
            if (forceShow || (Config.CurrentTab == TabType.HUD && isSelectingElements))
                return false;

            return true;
        });

        // Register Escape interceptor to back out of element picker or layout edit mode
        SafeToCloseManager.RegisterInterceptor(() =>
        {
            if (forceShow)
            {
                ExitLayoutMode();
                return true;
            }

            if (isSelectingElements)
            {
                OnClose();
                Main.ResetInputBind();
                UIAnimationHelper.TriggerSubWindowTransition();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        });

        IsInitialized = true;
        DebugLogger.Msg("[HUDManager] Initialized and registered HUD service.");
    }

    public static void ExitLayoutMode()
    {
        forceShow = false;
        Config.showgui = true;
        _suppressNextTransition = true;

        // Force both main fade and sub-window alpha to 1 immediately
        UIAnimationHelper.SnapToVisible();

        SaveLoad.Save();
        Main.ResetInputBind();
        Input.ResetInputAxes();
    }

    public static void OnClose()
    {
        isSelectingElements = false;
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

    private static GUI.WindowFunction GetSelectorDelegate()
    {
        if (_cachedSelectorDelegate == null)
        {
            _cachedSelectorDelegate = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawElementSelector);
        }
        return _cachedSelectorDelegate;
    }

    private static GUI.WindowFunction GetControlsDelegate()
    {
        if (_cachedControlsDelegate == null)
        {
            _cachedControlsDelegate = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawHUDControls);
        }
        return _cachedControlsDelegate;
    }

    public static void RenderDimBackground()
    {
        if (!Config.dimBg) return;

        // When in forceShow OR when we just exited layout mode, force alpha to 1.0f solid
        float currentAlpha;
        if (forceShow || _suppressNextTransition)
        {
            currentAlpha = 1.0f;
        }
        else
        {
            currentAlpha = UIAnimationHelper.FadeProgress;
        }

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

        if (isSelectingElements)
        {
            selectorRect = GUI.Window(
                2001,
                selectorRect,
                GetSelectorDelegate(),
                "",
                windowBgStyle
            );
        }
        else
        {
            windowRect = GUI.Window(
                2000,
                windowRect,
                GetControlsDelegate(),
                "",
                windowBgStyle
            );
        }
    }

    public static void Render()
    {
        EnsureRects();

        try
        {
            Event e = Event.current;

            #region Handle Escape Key In-GUI (Do not bail out early!)
            if (forceShow && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                ExitLayoutMode();
                e.Use();
                // Notice: DO NOT return here! Proceed directly to draw the background and window on this exact frame.
            }
            else if (isSelectingElements && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                OnClose();
                UIAnimationHelper.TriggerSubWindowTransition();
                e.Use();
            }
            #endregion

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
        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.SubWindowAlpha;

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

        float subAlpha = _suppressNextTransition ? 1.0f : UIAnimationHelper.SubWindowAlpha;
        if (_suppressNextTransition && Event.current.type == EventType.Repaint)
        {
            _suppressNextTransition = false;
        }

        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * subAlpha;

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

            if (e.type == EventType.MouseDown && e.button == 0 && selectBtnRect.Contains(e.mousePosition))
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

                isSelectingElements = true;
                UIAnimationHelper.TriggerSubWindowTransition();
            }

            GUI.Box(selectBtnRect, Translator.Translate("Select"), ThemeManager.SettingOff);

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Layout"),
                ThemeManager.SettingLabelStyle);

            Rect configBtnRect = new(width * 0.5f, y, width * 0.45f, elementHeight);

            if (e.type == EventType.MouseDown && e.button == 0 && configBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                forceShow = true;
                Config.showgui = false;
                DebugLogger.Msg("Escape Triggered : Hud Window -> Edit Layout");
            }

            GUI.Box(configBtnRect, Translator.Translate("Edit"), ThemeManager.SettingOff);

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Background"),
                ThemeManager.SettingLabelStyle);
            Rect bgRect = new(width * 0.5f, y, width * 0.45f, elementHeight);
            bool bgHover = bgRect.Contains(e.mousePosition);

            GUI.Box(bgRect, showBackground ? Translator.Translate("ON") : Translator.Translate("OFF"),
                showBackground ? ThemeManager.SettingOn : ThemeManager.SettingOff);

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

            GUI.Box(enabledRect, Enabled ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Enabled ? ThemeManager.SettingOn : ThemeManager.SettingOff);

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
        };
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = (int)Config.NativeHeight / 36;

        float width = style.CalcSize(content).x;

        Rect rect = new()
        {
            x = Config.NativeWidth * 0.995f - width,
            width = width
        };

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
            // 1. Exiting layout editing mode back to the main GUI menu (no sub-window fade)
            if (HUDManager.forceShow)
            {
                ExitLayoutMode();
                return true;
            }

            // 2. Stepping out of the MultiSelect element picker modal
            if (HUDManager.isSelectingElements)
            {
                HUDManager.OnClose();
                Main.ResetInputBind();
                UIAnimationHelper.TriggerSubWindowTransition();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }

        public void OnLanguageChanged()
        {
            HUDManager.OnLanguageChange();
        }
    }
}

public static class HUDRenderer
{
    public static List<HudElement> Elements = new();
    public static MultiSelectSetting HudToggles = new("Active Elements")
    {
        CustomNames = new Dictionary<int, string>(),
        DisplayAlphabetically = true,
    };
    private static bool isMasterVisible;

    public static int currentWindowId = 4000;

    public static void Init()
    {
        HUDManager.Init();

        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(HudElement)) && !t.IsAbstract);

        foreach (var type in types)
        {
            RegisterElement(type);
        }

        DebugLogger.Msg($"Registered {Elements.Count} HUD elements");
    }

    public static void RegisterElement(Type element)
    {
        HudElement instance = (HudElement)Activator.CreateInstance(element);
        instance.WindowId = currentWindowId;
        Elements.Add(instance);
        HudToggles.AddOption(instance.WindowId, element.Name);
        HudToggles.CustomNames[instance.WindowId] = element.Name;
        currentWindowId++;
    }

    public static void RenderOverlay()
    {
        isMasterVisible = HUDManager.Enabled;

        if (Config.showgui && Config.CurrentTab != TabType.HUD)
        {
            isMasterVisible = false;
        }

        if (!isMasterVisible) return;

        for (int i = 0; i < Elements.Count; i++)
        {
            var element = Elements[i];
            if (element == null) continue;

            bool isElementEnabled = HudToggles != null && HudToggles.IsSelected(element.WindowId);

            if (isElementEnabled)
            {
                try
                {
                    element.Render();
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[HUDRenderer] CRASH in element '{element.Name}': {ex}");
                }
            }
        }
    }

    public static void UpdateElements()
    {
        if (!isMasterVisible) return;

        for (int i = 0; i < Elements.Count; i++)
        {
            var element = Elements[i];
            if (element == null) continue;

            bool isElementEnabled = isMasterVisible && HudToggles.IsSelected(element.WindowId);
            element.HandleLifecycle(isElementEnabled);
        }
    }
}