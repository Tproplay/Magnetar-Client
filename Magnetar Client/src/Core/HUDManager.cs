using Magnetar_Client.Modules;
using Magnetar_Client.UI.HUDElements;
using Magnetar_Client.UI.Themes;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Magnetar_Client.Core
{
    public static class HUDManager
    {
        public static bool Enabled = true;
        public static bool forceShow = false;
        public static bool isSelectingElements = false;

        public static bool showBackground = false;

        static float width = 500;
        public static float elementHeight = 25f;

        // Main HUDManager Menu Rect
        public static Rect windowRect = new Rect((Config.WindowWidth - width) / 2, (Config.WindowHeight - 300) / 2, width, 300);

        static float selectorWidth = 500;
        static float selectorHeight = 800;
        public static Rect selectorRect = new Rect((Config.WindowWidth - selectorWidth) / 2, (Config.WindowHeight - selectorHeight) / 2,
            selectorWidth, selectorHeight);

        public static void Render()
        {
            if (Config.dimBg && (Config.showgui || forceShow))
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", Magnetar_Default.DimStyle);
            }

            Event e = Event.current;

            #region Handle Escape
            // Layout Editing -> HUDManager Window
            if (forceShow && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                forceShow = false;
                Config.showgui = true;
                e.Use();
#if DEBUG
                MelonLogger.Msg("Escape Triggerd : Layout Editing -> HUDManager Window");
#endif
                return;

            }

            // Select Elemets -> HUDManager Window
            else if (isSelectingElements && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                isSelectingElements = false;
                e.Use();
#if DEBUG
                MelonLogger.Msg("Escape Triggerd : Select Elemets -> HUDManager Window");
#endif
                return;
            }



            #endregion

            if (Config.CurrentTab == TabType.HUD && !forceShow && Config.showgui)
            {
                if (isSelectingElements)
                {
                    selectorRect = GUI.Window(
                        2001,
                        selectorRect,
                        (GUI.WindowFunction)DrawElementSelector,
                        "Select HUD Elements",
                        Magnetar_Default.ModuleWindow
                    );
                }
                else
                {
                    windowRect = GUI.Window(
                        2000,
                        windowRect,
                        (GUI.WindowFunction)DrawHUDControls,
                        "Customize HUD",
                        Magnetar_Default.ModuleWindow
                    );
                }
            }

            HUDRenderer.RenderOverlay();
        }

        private static void DrawElementSelector(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, selectorRect.width, 25));
            Event e = Event.current;

            float startY = 35 + elementHeight + 10;
            Rect multiSelectRect = new Rect(0, startY, selectorRect.width, selectorRect.height);

            UI.WindowDrawing.DrawSetting.DrawMultiSelectWindow(multiSelectRect, UI.WindowDrawing.DrawSetting.activeMultiSelect);

            if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
                e = null;
            }
            
        }

        private static void DrawHUDControls(int windowID)
        {
            float width = windowRect.width;
            float elementWidth = width - 20;
            float indent = 10;
            Event e = Event.current;
            float y = 35;

            // 1. SELECT ELEMENTS BUTTON
            int activeCount = HUDRenderer.HudToggles != null ? HUDRenderer.HudToggles.SelectedValues.Count : 0;
            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), $"Elements ({activeCount})");

            Rect selectBtnRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);

            if (selectBtnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

            if (e.type == EventType.MouseDown && e.button == 0 && selectBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                UI.WindowDrawing.DrawSetting.activeMultiSelect = HUDRenderer.HudToggles;
                UI.WindowDrawing.DrawSetting.multiSelectSearchQuery = "";
                UI.WindowDrawing.DrawSetting.manualScrollY = 0f;

                // Re-center the selector window every time its opened
                selectorRect.x = (1920 - selectorWidth) / 2;
                selectorRect.y = (1080 - selectorHeight) / 2;

                isSelectingElements = true;
            }

            GUI.Box(selectBtnRect, "Select", Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            y += elementHeight + 5;

            // 2. CONFIGURE HUDManager
            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), "Layout");

            Rect configBtnRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);

            if (configBtnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

            // Hud Window -> Edit Layout
            if (e.type == EventType.MouseDown && e.button == 0 && configBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                forceShow = true;
                Config.showgui = false;
#if DEBUG
                MelonLogger.Msg("Escape Triggerd : Hud Window -> Edit Layout");
#endif
            }

            GUI.Box(configBtnRect, "Edit", Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            y += elementHeight + 5;

            // 3. SHOW BACKGROUND TOGGLE
            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), "Background");
            Rect bgRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);
            bool bgHover = bgRect.Contains(e.mousePosition);

            if (bgHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(bgRect, showBackground ? "ON" : "OFF", showBackground ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (bgHover && e.type == EventType.MouseDown && e.button == 0)
            {
                showBackground = !showBackground;
                e.Use();
            }

            y += elementHeight + 5;
            // 4. ENABLED TOGGLE
            GUI.Label(new Rect(indent, y, width * 0.45f, elementHeight), "Enabled");
            Rect enabledRect = new Rect(width * 0.5f, y, width * 0.45f, elementHeight);
            bool enabledHover = enabledRect.Contains(e.mousePosition);

            if (enabledHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(enabledRect, Enabled ? "ON" : "OFF", Enabled ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (enabledHover && e.type == EventType.MouseDown && e.button == 0)
            {
                Enabled = !Enabled;
                e.Use();
            }

            y += elementHeight + 10;

            windowRect.height = y;

            GUI.DragWindow(new Rect(0, 0, width, 25));

            Rect _windowRect = new Rect(0,0, width, y);

            if (_windowRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
                e = null;
            }
        }
    }

    public static class HUDRenderer
    {
        /// <summary>
        /// Gets the collection of HUD elements currently managed by the client.
        /// </summary>
        public static List<HudElement> Elements = new List<HudElement>();

        /// <summary>
        /// Gets or sets the collection of HUD toggle options available/Selected.
        /// </summary>
        public static MultiSelectSetting HudToggles;

        public static void Init()
        {
            HudToggles = new MultiSelectSetting("Active HUDManager Elements");
                
            int currentWindowId = 3000;

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(HudElement)) && !t.IsAbstract);

            foreach (var type in types)
            {
                HudElement element = (HudElement)Activator.CreateInstance(type);
                element.WindowId = currentWindowId;
                RegisterElement(element);
                currentWindowId++;
            }

        }

        public static void RegisterElement(HudElement element)
        {
            Elements.Add(element);
            HudToggles.AddOption(element.WindowId, element.Name);
        }

        public static void RenderOverlay()
        {
            bool isMasterVisible = HUDManager.Enabled;

            if (Config.showgui && Config.CurrentTab != TabType.HUD)
            {
                isMasterVisible = false;
            }

            if (!isMasterVisible) return;

            foreach (var element in Elements)
            {
                bool isElementEnabled = isMasterVisible && HudToggles.IsSelected(element.WindowId);

                element.HandleLifecycle(isElementEnabled);

                if (isElementEnabled)
                {
                    element.Render();
                }
            }
        }

    }
}
