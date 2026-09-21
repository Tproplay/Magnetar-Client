using Magnetar_Client.Modules;
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

namespace Magnetar_Client.Core
{
    public static class HUDManager
    {
        public static bool Enabled = true;
        public static bool forceShow = false;
        public static bool isSelectingElements = false;
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

        public static void Render()
        {
            EnsureRects();

            try
            {
                Event e = Event.current;

                RenderModCredit();

                if (Config.dimBg && (Config.showgui || forceShow))
                {
                    if (e.type == EventType.Repaint && Magnetar_Default.DimBackgroundStyle != null)
                    {
                        Matrix4x4 prevMatrix = GUI.matrix;
                        GUI.matrix = Matrix4x4.identity;

                        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", Magnetar_Default.DimBackgroundStyle);

                        GUI.matrix = prevMatrix;
                    }

                    if (e.type == EventType.MouseDown && UI.WindowDrawing.DrawSetting.activeSliderId == -1 && UI.WindowDrawing.DrawSetting.activeDropdownId == -1)
                    {
                        Input.ResetInputAxes();
                    }
                }

                #region Handle Escape
                if (forceShow && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
                {
                    forceShow = false;
                    Config.showgui = true;
                    SaveLoad.Save();
                    e.Use();
                    return;
                }
                else if (isSelectingElements && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
                {
                    OnClose();
                    e.Use();
                    return;
                }
                #endregion

                if (forceShow)
                {
                    DrawExitLayoutButton();
                }

                GUIStyle windowBgStyle = Magnetar_Default.SettingsWndowBgStyle ?? Magnetar_Default.SettingsWndowStyle;

                if (Config.CurrentTab == TabType.HUD && !forceShow && Config.showgui)
                {
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
                Rect exitRect = new Rect((Config.NativeWidth - btnWidth) / 2f, Config.S(16f), btnWidth, btnHeight);

                bool isHovered = exitRect.Contains(e.mousePosition);

                GUI.Box(exitRect, Translator.Translate("Exit Layout"), Magnetar_Default.SettingOn);

                if (e.type == EventType.MouseDown && e.button == 0 && isHovered)
                {
                    forceShow = false;
                    Config.showgui = true;
                    SaveLoad.Save();
                    Input.ResetInputAxes();
                    e.Use();
                }
            }
        }

        private static void DrawElementSelector(int windowID)
        {
            Event e = Event.current;

            Rect multiSelectRect = new Rect(0, 0, selectorRect.width, selectorRect.height);
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

        private static void DrawHUDControls(int windowID)
        {
            float width = windowRect.width;
            float indent = Config.S(10f);
            Event e = Event.current;
            float y = Config.S(35f);

            Rect headerBgRect = new Rect(0, 0, width, y - indent);
            GUI.Box(headerBgRect, Translator.Translate("Customize HUD"), Magnetar_Default.SettingsWndowStyle);

            int activeCount = HUDRenderer.HudToggles != null ? HUDRenderer.HudToggles.SelectedValues.Count : 0;
            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight),
                Translator.Translate("Elements") + $" ({activeCount})",
                Magnetar_Default.SettingLabelStyle);

            Rect selectBtnRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);

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
            }

            GUI.Box(selectBtnRect, Translator.Translate("Select"), Magnetar_Default.SettingOff);

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Layout"),
                Magnetar_Default.SettingLabelStyle);

            Rect configBtnRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);

            if (e.type == EventType.MouseDown && e.button == 0 && configBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                forceShow = true;
                Config.showgui = false;
                DebugLogger.Msg("Escape Triggered : Hud Window -> Edit Layout");
            }

            GUI.Box(configBtnRect, Translator.Translate("Edit"), Magnetar_Default.SettingOff);

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Background"),
                Magnetar_Default.SettingLabelStyle);
            Rect bgRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);
            bool bgHover = bgRect.Contains(e.mousePosition);

            GUI.Box(bgRect, showBackground ? Translator.Translate("ON") : Translator.Translate("OFF"),
                showBackground ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);

            if (bgHover && e.type == EventType.MouseDown && e.button == 0)
            {
                showBackground = !showBackground;
                e.Use();
            }

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Enabled"),
                Magnetar_Default.SettingLabelStyle);
            Rect enabledRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);
            bool enabledHover = enabledRect.Contains(e.mousePosition);

            GUI.Box(enabledRect, Enabled ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Enabled ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);

            if (enabledHover && e.type == EventType.MouseDown && e.button == 0)
            {
                Enabled = !Enabled;
                e.Use();
            }

            y += elementHeight + Config.S(10f);
            windowRect.height = y;

            GUI.DragWindow(new Rect(0, 0, width, Config.S(25f)));

            Rect _windowRect = new Rect(0, 0, width, y);
            if (_windowRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
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
    }

    public static class HUDRenderer
    {
        public static List<HudElement> Elements = new List<HudElement>();
        public static MultiSelectSetting HudToggles;
        private static bool isMasterVisible;

        public static void Init()
        {
            HudToggles = new MultiSelectSetting("Active Elements")
            {
                CustomNames = new Dictionary<int, string>()
            };

            int currentWindowId = 4000;

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(HudElement)) && !t.IsAbstract);

            foreach (var type in types)
            {
                HudElement element = (HudElement)Activator.CreateInstance(type);
                element.WindowId = currentWindowId;
                RegisterElement(element);
                currentWindowId++;
            }

            DebugLogger.Msg($"Registered {Elements.Count} HUD elements");
        }

        public static void RegisterElement(HudElement element)
        {
            Elements.Add(element);
            HudToggles.AddOption(element.WindowId, element.Name);
            HudToggles.CustomNames[element.WindowId] = element.Name;
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

            foreach (var element in Elements)
            {
                bool isElementEnabled = isMasterVisible && HudToggles.IsSelected(element.WindowId);
                element.HandleLifecycle(isElementEnabled);
            }
        }
    }
}