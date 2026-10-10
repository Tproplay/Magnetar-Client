using static Magnetar_Client.Utils.Translator;
using Magnetar_Client.Api;
using Magnetar_Client.Core.HUDManager_;
using Magnetar_Client.Game;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

public static class HUDManager
{
    public const string Group = "HUD";
    public const string ViewMain = "Controls";
    public const string ViewSelector = "Selector";
    public static bool IsInitialized { get; private set; }
    public static bool Enabled = true;
    public static bool forceShow { get; set; }
    public static bool IsSelectingElements => AnimationHandler.GetViewAlpha(Group, ViewSelector) > 0.001f;
    public static bool showBackground;

    private const float BaseElementHeight = 25f;
    public static float elementHeight => GUIManager.S(BaseElementHeight);

    public static void Init()
    {
        if (IsInitialized) return;

        AnimationHandler.SetViewImmediate(Group, ViewMain, 1.0f);
        AnimationHandler.SetViewImmediate(Group, ViewSelector, 0.0f);

        ServiceRegistry.Register(new HUDManagerService());

        HUDRenderer.Init();

        TabType.HUD.OnDeselected = () =>
        {
            MultiSelectTabStorage.SaveState(TabType.HUD);
        };

        TabType.HUD.OnSelected = () =>
        {
            MultiSelectTabStorage.RestoreState(TabType.HUD);
        };

        SaveLoad.RegisterHandler(new HUDSaveHandler());

        IsInitialized = true;

        DebugLogger.Msg("Initialized HUD Manager");
    }

    public static void ExitLayoutMode()
    {
        forceShow = false;
        Config.showgui = true;
        AnimationHandler.SnapToVisible();
        AnimationHandler.SwitchView(Group, ViewMain);

        SaveLoad.Save();
        DrawSetting.ResetInputBind();
        Input.ResetInputAxes();
    }

    public static void OnClose()
    {
        AnimationHandler.SwitchView(Group, ViewMain);
        DrawSetting.activeMultiSelect = null;
    }

    public static void RenderDimBackground()
    {
        float currentAlpha = forceShow ? 1.0f : AnimationHandler.CurrentEasedDimAlpha;
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

        if (e.type == EventType.MouseDown && DrawSetting.ActiveSliderId == -1 && DrawSetting.ActiveDropdownId == -1)
        {
            Input.ResetInputAxes();
        }
    }

    public static void RenderControlsWindow()
    {
        GUIStyle windowBgStyle = ThemeManager.SettingsWndowBgStyle;

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
            else if (Config.CurrentTab == TabType.HUD && IsSelectingElements && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
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
        if (GUIManager.ShowMobileButtons)
        {
            Event e = Event.current;
            float btnWidth = GUIManager.S(180f);
            float btnHeight = GUIManager.S(36f);
            Rect exitRect = new((Config.NativeWidth - btnWidth) / 2f, GUIManager.S(16f), btnWidth, btnHeight);

            bool isHovered = exitRect.Contains(e.mousePosition);

            GUI.Box(exitRect, Translate("Exit Layout"), ThemeManager.SettingOn);

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

        HUDRenderer.HudToggles.CustomNames ??= new Dictionary<int, string>();

        foreach (var element in HUDRenderer.Elements)
        {
            if (element == null) continue;

            string translated = Translate(element.Name);

            HUDRenderer.HudToggles.Options[element.WindowId] = translated;
            HUDRenderer.HudToggles.CustomNames[element.WindowId] = translated;
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

    public static void RegisterElement(Type element)
    {
        HUDRenderer.RegisterElement(element);
    }

    private class HUDManagerService : IUpdatable, IRenderable, IMenuRenderable, ICloseHandler, ILanguageAware
    {
        public string Name => "HUDManager";
        public int Priority => ServicePriority.HUD;
        public void OnUpdate() => HUDRenderer.UpdateElements();

        public void OnGUI() => HUDManager.Render();

        public void OnMenuGUI()
        {
            if (HUDManager.forceShow) return;

            AnimationHandler.RenderWithTabAlpha(TabType.HUD, RenderControlsWindow);
        }

        public bool CanClose()
        {
            if (Config.CurrentTab == TabType.HUD)
            {
                return !HUDManager.forceShow && !HUDManager.IsSelectingElements;
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

            if (HUDManager.IsSelectingElements)
            {
                HUDManager.OnClose();
                DrawSetting.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }

        public void OnLanguageChanged() => HUDManager.OnLanguageChange();
    }

    #region HUD Save Handler Implementation

    public class HUDSaveHandler : ISaveHandler
    {
        public string SectionKey => "HUD";

        public object ExportData()
        {
            var data = new HUDSaveData
            {
                Enabled = HUDManager.Enabled,
                ShowBackground = HUDManager.showBackground,
                SelectedElements = HUDRenderer.HudToggles?.SelectedValues?.ToList() ?? new List<int>()
            };

            if (HUDRenderer.Elements != null)
            {
                foreach (var el in HUDRenderer.Elements)
                {
                    if (el != null && !string.IsNullOrEmpty(el.Name))
                        data.Positions[el.Name] = el.Bounds;
                }
            }

            return data;
        }

        public void ImportData(JToken token)
        {
            if (token == null) return;

            // 1. Enabled & Background states (checking both new and legacy keys)
            if (token["Enabled"] != null) HUDManager.Enabled = token["Enabled"].Value<bool>();
            else if (token["HudEnabled"] != null) HUDManager.Enabled = token["HudEnabled"].Value<bool>();

            if (token["ShowBackground"] != null) HUDManager.showBackground = token["ShowBackground"].Value<bool>();

            // 2. Selected Elements
            JToken selectedToken = token["SelectedElements"] ?? token["SelectedHudElements"];
            if (selectedToken != null && HUDRenderer.HudToggles != null)
            {
                var elements = selectedToken.ToObject<List<int>>();
                if (elements != null)
                    HUDRenderer.HudToggles.SelectedValues = new HashSet<int>(elements);
            }

            // 3. Positions
            JToken posToken = token["Positions"] ?? token["HudPositions"];
            if (posToken != null && HUDRenderer.Elements != null)
            {
                var posDict = posToken.ToObject<Dictionary<string, SaveLoadData.SimpleRect>>();
                if (posDict != null)
                {
                    foreach (var el in HUDRenderer.Elements)
                    {
                        if (el != null && !string.IsNullOrEmpty(el.Name) && posDict.TryGetValue(el.Name, out var rect))
                        {
                            el.Bounds = rect;
                        }
                    }
                }
            }
        }

        public void ResetToDefault()
        {
            HUDManager.Enabled = true;
            HUDManager.showBackground = false;
        }
    }

    public class HUDSaveData
    {
        public bool Enabled = true;
        public bool ShowBackground;
        public List<int> SelectedElements = new();
        public Dictionary<string, SaveLoadData.SimpleRect> Positions = new();
    }

    #endregion
}