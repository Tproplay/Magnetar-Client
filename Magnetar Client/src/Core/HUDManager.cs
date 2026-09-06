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

        private static void EnsureRects()
        {
            float targetSelectorWidth = Config.S(BaseSelectorWidth);
            float maxSelectorHeight = Config.WindowHeight * 0.8f;
            float targetSelectorHeight = Mathf.Min(Config.S(BaseSelectorHeight), maxSelectorHeight);

            if (!_rectsInitialized)
            {
                windowRect = new Rect(
                    (Config.WindowWidth - Config.S(BaseWidth)) / 2f,
                    (Config.WindowHeight - Config.S(BaseHeight)) / 2f,
                    Config.S(BaseWidth),
                    Config.S(BaseHeight));

                selectorRect = new Rect(
                    (Config.WindowWidth - targetSelectorWidth) / 2f,
                    (Config.WindowHeight - targetSelectorHeight) / 2f,
                    targetSelectorWidth,
                    targetSelectorHeight);

                _rectsInitialized = true;
            }

            Config.RescaleAroundCenter(ref windowRect, Config.S(BaseWidth), windowRect.height);
            Config.RescaleAroundCenter(ref selectorRect, targetSelectorWidth, targetSelectorHeight);

            // Clamp selector window inside screen canvas
            selectorRect.x = Mathf.Clamp(selectorRect.x, 0f, Mathf.Max(0f, Config.WindowWidth - selectorRect.width));
            selectorRect.y = Mathf.Clamp(selectorRect.y, 0f, Mathf.Max(0f, Config.WindowHeight - selectorRect.height));
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
                if (Config.dimBg && (Config.showgui || forceShow))
                {
#if !ANDROID
                    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                    {
                        Input.ResetInputAxes();
                    }
#endif

                    Matrix4x4 backupMatrix = GUI.matrix;
                    GUI.matrix = Matrix4x4.identity;
                    if (Magnetar_Default.DimStyle != null)
                    {
                        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", Magnetar_Default.DimStyle);
                    }
                    GUI.matrix = backupMatrix;
                }

                Event e = Event.current;

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
                    isSelectingElements = false;
                    e.Use();
                    return;
                }
                #endregion

                if (forceShow)
                {
                    DrawExitLayoutButton();
                }

                if (Config.CurrentTab == TabType.HUD && !forceShow && Config.showgui)
                {
                    if (isSelectingElements)
                    {
                        selectorRect = GUI.Window(
                            2001,
                            selectorRect,
                            GetSelectorDelegate(),
                            "",
                            Magnetar_Default.ModuleWindow
                        );
                    }
                    else
                    {
                        windowRect = GUI.Window(
                            2000,
                            windowRect,
                            GetControlsDelegate(),
                            "",
                            Magnetar_Default.ModuleWindow
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

        private static GUIStyle _exitBtnStyle;
        private static GUIStyle GetExitBtnStyle()
        {
            if (_exitBtnStyle == null)
            {
                _exitBtnStyle = new GUIStyle();

                if (Magnetar_Default.SettingsWindow != null)
                {
                    _exitBtnStyle.normal.background = Magnetar_Default.SettingsWindow.normal.background;
                    _exitBtnStyle.normal.textColor = Magnetar_Default.SettingsWindow.normal.textColor;
                    _exitBtnStyle.fontStyle = Magnetar_Default.SettingsWindow.fontStyle;
                }

                _exitBtnStyle.alignment = TextAnchor.MiddleCenter;

                _exitBtnStyle.padding = new RectOffset();
                _exitBtnStyle.padding.left = 0;
                _exitBtnStyle.padding.right = 0;
                _exitBtnStyle.padding.top = 0;
                _exitBtnStyle.padding.bottom = 0;

                _exitBtnStyle.margin = new RectOffset();
                _exitBtnStyle.margin.left = 0;
                _exitBtnStyle.margin.right = 0;
                _exitBtnStyle.margin.top = 0;
                _exitBtnStyle.margin.bottom = 0;
            }

            _exitBtnStyle.fontSize = Mathf.RoundToInt(Config.S(16f));
            return _exitBtnStyle;
        }

        private static void DrawExitLayoutButton()
        {
            Event e = Event.current;
            float btnWidth = Config.S(180f);
            float btnHeight = Config.S(36f);
            Rect exitRect = new Rect((Config.WindowWidth - btnWidth) / 2f, Config.S(16f), btnWidth, btnHeight);

            bool isHovered = exitRect.Contains(e.mousePosition);
            if (isHovered) GUI.backgroundColor = Magnetar_Default.AccentColor;

            GUI.Box(exitRect, Translator.Translate("Exit Layout"), GetExitBtnStyle());
            GUI.backgroundColor = Color.white;

            if (e.type == EventType.MouseDown && e.button == 0 && isHovered)
            {
                forceShow = false;
                Config.showgui = true;
                SaveLoad.Save();
#if !ANDROID
                Input.ResetInputAxes();
#endif
                e.Use();
            }
        }

        private static void DrawElementSelector(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, selectorRect.width, Config.S(34f)));
            Event e = Event.current;

            // Fill local window canvas directly; DrawMultiSelectWindow handles banner and search bar internally
            Rect multiSelectRect = new Rect(0, 0, selectorRect.width, selectorRect.height);
            UI.WindowDrawing.DrawSetting.DrawMultiSelectWindow(multiSelectRect, UI.WindowDrawing.DrawSetting.activeMultiSelect);

            if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
#if !ANDROID
                Input.ResetInputAxes();
#endif
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
            GUI.Box(headerBgRect, Translator.Translate("Customize HUD"), Magnetar_Default.SettingsWindow);

            int activeCount = HUDRenderer.HudToggles != null ? HUDRenderer.HudToggles.SelectedValues.Count : 0;
            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight),
                Translator.Translate("Elements") + $" ({activeCount})",
                Magnetar_Default.SettingDescriptionStyle);

            Rect selectBtnRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);

            if (selectBtnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

            if (e.type == EventType.MouseDown && e.button == 0 && selectBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                UI.WindowDrawing.DrawSetting.activeMultiSelect = HUDRenderer.HudToggles;
                UI.WindowDrawing.DrawSetting.multiSelectSearchQuery = "";
                UI.WindowDrawing.DrawSetting.manualScrollY = 0f;

                // Center using the 80% clamped target height
                float targetW = Config.S(BaseSelectorWidth);
                float targetH = Mathf.Min(Config.S(BaseSelectorHeight), Config.WindowHeight * 0.8f);

                selectorRect = new Rect(
                    (Config.WindowWidth - targetW) / 2f,
                    (Config.WindowHeight - targetH) / 2f,
                    targetW,
                    targetH
                );

                isSelectingElements = true;
            }

            GUI.Box(selectBtnRect, Translator.Translate("Select"), Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Layout"),
                Magnetar_Default.SettingDescriptionStyle);

            Rect configBtnRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);

            if (configBtnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

            if (e.type == EventType.MouseDown && e.button == 0 && configBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                forceShow = true;
                Config.showgui = false;
                DebugLogger.Msg("Escape Triggered : Hud Window -> Edit Layout");
            }

            GUI.Box(configBtnRect, Translator.Translate("Edit"), Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Background"),
                Magnetar_Default.SettingDescriptionStyle);
            Rect bgRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);
            bool bgHover = bgRect.Contains(e.mousePosition);

            if (bgHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(bgRect, showBackground ? Translator.Translate("ON") : Translator.Translate("OFF"),
                showBackground ? Magnetar_Default.ModuleOn : Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            if (bgHover && e.type == EventType.MouseDown && e.button == 0)
            {
                showBackground = !showBackground;
                e.Use();
            }

            y += elementHeight + Config.S(5f);

            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), Translator.Translate("Enabled"),
                Magnetar_Default.SettingDescriptionStyle);
            Rect enabledRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);
            bool enabledHover = enabledRect.Contains(e.mousePosition);

            if (enabledHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(enabledRect, Enabled ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Enabled ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

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
#if !ANDROID
                Input.ResetInputAxes();
#endif
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