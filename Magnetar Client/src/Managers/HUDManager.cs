using Magnetar_Client.Api;
using Magnetar_Client.Core.HUDManager_;
using Magnetar_Client.Core.Lifecycle;
using Magnetar_Client.Game;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

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

    private const float BaseElementHeight = 25f;
    public static float elementHeight => Config.S(BaseElementHeight);

    public static void Init()
    {
        if (IsInitialized) return;

        UIAnimationHelper.SetViewImmediate(Group, ViewMain, 1.0f);
        UIAnimationHelper.SetViewImmediate(Group, ViewSelector, 0.0f);

        ServiceRegistry.Register(new HUDManagerService());

        SafeToCloseManager.RegisterGuard(() =>
        {
            if (forceShow) return false;
            if (Config.CurrentTab == TabType.HUD && isSelectingElements) return false;
            return true;
        });

        HUDRenderer.Init();
        IsInitialized = true;
        DebugLogger.Msg("[HUDManager] Initialized and registered HUD service.");

        TabType.HUD.OnDeselected = () =>
        {
            MultiSelectTabStorage.SaveState(TabType.HUD);
        };

        TabType.HUD.OnSelected = () =>
        {
            MultiSelectTabStorage.RestoreState(TabType.HUD);
        };
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
        DrawSetting.activeMultiSelect = null;
    }

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

        if (e.type == EventType.MouseDown && DrawSetting.activeSliderId == -1 && DrawSetting.activeDropdownId == -1)
        {
            Input.ResetInputAxes();
        }
    }

    public static void RenderControlsWindow()
    {
        GUIStyle windowBgStyle = ThemeManager.SettingsWndowBgStyle ?? ThemeManager.SettingsWndowStyle;

        HUDSelectorDrawer.Render(windowBgStyle);
        HUDControlsDrawer.Render(windowBgStyle);
    }

    public static void Render()
    {
        try
        {
            Event e = Event.current;

            // ONLY handle Escape if in Edit Layout mode OR currently on the HUD tab
            if (forceShow && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                ExitLayoutMode();
                e.Use();
            }
            else if (Config.CurrentTab == TabType.HUD && isSelectingElements && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
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

        public void Initialize() { }
        public void OnWarmUp() { }
        public void OnUpdate() => HUDRenderer.UpdateElements();

        public void OnGUI() => HUDManager.Render();

        public void OnMenuGUI()
        {
            if (HUDManager.forceShow) return;

            UIAnimationHelper.RenderWithTabAlpha(TabType.HUD, RenderControlsWindow);
        }

        public bool CanClose()
        {
            if (Config.CurrentTab == TabType.HUD)
            {
                return !HUDManager.forceShow && !HUDManager.isSelectingElements;
            }
            return true;
        }

        public bool OnEscapePressed()
        {
            // Escape guard: do not consume event if user is on another tab
            if (Config.CurrentTab != TabType.HUD) return false;

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