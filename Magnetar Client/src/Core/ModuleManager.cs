using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Magnetar_Client.UI.WindowDrawing.DrawSetting;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core
{
    public static class ModuleManager
    {
        #region static values
        public static bool IsInitialized = false;

        /// <summary>
        /// Gets the collection of loaded modules available in the client.
        /// </summary>
        public static List<Modules.Module> Modules = new List<Modules.Module>();

        /// <summary>
        /// Stores the window positions for each module category.
        /// </summary>
        /// <remarks>The dictionary maps each <see cref="ModuleCategory"/> to a <see cref="Rect"/>
        /// representing the window's position and size for that category. Modifying this collection affects the stored
        /// layout for all module categories.</remarks>
        public static Dictionary<ModuleCategory, Rect> windowPositions = new Dictionary<ModuleCategory, Rect>();

        /// <summary>
        /// Stores the positions of settings windows for each module.
        /// </summary>
        /// <remarks>The dictionary maps each module to a corresponding rectangle representing the
        /// window's position and size. This allows the application to remember and restore window layouts for
        /// individual modules.</remarks>
        private static Dictionary<Modules.Module, Rect> settingsPositions = new Dictionary<Modules.Module, Rect>();
        private static Dictionary<Modules.Module, Vector2> settingsScrollPositions = new Dictionary<Modules.Module, Vector2>();
        private static Dictionary<Modules.Module, float> moduleContentHeights = new Dictionary<Modules.Module, float>();
        private static Dictionary<Modules.Module, float> targetContentHeights = new Dictionary<Modules.Module, float>();

        public static bool showModules = true;
        public static bool showSettings = false;
        public static bool showSelectionGui = false;

        public static bool resetWindowPos = false;


        public static int bindingModuleId = -1;
        public static int activeSliderId = -1;

        
        public static MultiSelectSetting activeMultiSelect = null;
        private static Rect multiSelectWindowRect = new Rect(0, 0, 500, 800);

        public static string ModuleSearchQuery = "";

        public static Rect searchWindowRect;
        

        #endregion
        public static void Init()
        {
            int index = 0;
            foreach (ModuleCategory cat in System.Enum.GetValues(typeof(ModuleCategory)))
            {
                // Default category window positions
                windowPositions[cat] = new Rect(20 + (index * (Config.ModuleWindowWidth + 10)), 50, Config.ModuleWindowWidth, 50);
                index++;
            }

            // Init Search window rect
            searchWindowRect = new Rect(20 + (index * (Config.ModuleWindowWidth + 10)), 50, Config.ModuleWindowWidth, 55);

            // load all the modules
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.StartsWith("Magnetar_Client.Modules")
                            && t.IsSubclassOf(typeof(Magnetar_Client.Modules.Module)) && !t.IsAbstract);

            foreach (var type in types)
            {
                RegisterModule(type);
            }

            multiSelectWindowRect = new Rect(
                        1920 / 2f - multiSelectWindowRect.width / 2f,
                        1080 / 2f - multiSelectWindowRect.height / 2f,
                        multiSelectWindowRect.width,
                        multiSelectWindowRect.height
                        );

            showModules = true; showSettings = false; showSelectionGui = false;
            IsInitialized = true;

            MelonLogger.Msg($"Loaded {Modules.Count} modules");


        }

        public static void RegisterModule(Type type)
        {
            try
            {
                Modules.Add((Magnetar_Client.Modules.Module)Activator.CreateInstance(type));
            }

            catch (Exception ex)
            {
                Utils.Magnetar_Logger.DebugLogger.Error("Failed to load Module: " + ex);
            }
            
        }

        public static void Render()
        {
            if (showModules)
            {
                resetWindowPos = true;

                // 1. Render the Search Window
                searchWindowRect = GUI.Window(
                    999, // Unique ID so it doesn't conflict with categories
                    searchWindowRect,
                    (GUI.WindowFunction)new Action<int>(DrawSearchWindow),
                    Translate("Search Modules"),
                    Magnetar_Default.ModuleWindow
                );

                // 2. Render Category Windows
                foreach (ModuleCategory cat in System.Enum.GetValues(typeof(ModuleCategory)))
                {
                    int id = (int)cat;
                    windowPositions[cat] = GUI.Window(
                        id,
                        windowPositions[cat],
                        (GUI.WindowFunction)new Action<int>(DrawCategoryWindow),
                        Translate(cat.ToString()),
                        Magnetar_Default.ModuleWindow
                    );

                    // Block the mouse input to get recieved by game
                    if (windowPositions[cat].Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                    {
                        if (Input.GetMouseButtonDown(0) | Input.GetMouseButtonDown(1) | Input.GetMouseButtonDown(2))
                        {
                            Input.ResetInputAxes();
                        }
                    }
                }
            }

            else if (showSettings) 
            {
                // 3. Render Settings Popups
                foreach (var mod in Modules)
                {
                    if (mod.ShowSettings)
                    {
                        int settingsId = Mathf.Abs(mod.GetHashCode()) + 1000;

                        // Re-Center the window
                        if (!settingsPositions.ContainsKey(mod) || resetWindowPos)
                        {
                            resetWindowPos = false;

                            float popupWidth = mod.SettingsWidth;
                            float popupHeight = 25f; // Start collapsed to just the header!

                            settingsPositions[mod] = new Rect(
                                (Config.WindowWidth / 2f) - (popupWidth / 2f),
                                (Config.WindowHeight / 2f) - (popupHeight / 2f),
                                popupWidth,
                                popupHeight
                            );

                            // Reset animation states so it springs open!
                            moduleContentHeights[mod] = 0f;
                            targetContentHeights[mod] = 0f;
                        }


                        settingsPositions[mod] = GUI.Window(
                            settingsId,
                            settingsPositions[mod],
                            (GUI.WindowFunction)new Action<int>(id => DrawSettingsWindow(id, mod)),
                            Translate($"{mod.Name}"),
                            Magnetar_Default.ModuleWindow
                        );

                        if (settingsPositions[mod].Contains(new Vector2(Input.mousePosition.x, 1080 - Input.mousePosition.y)))
                        {
                            // This stops the game from seeing the mouse click
                            if (Input.GetMouseButtonDown(0) | Input.GetMouseButtonDown(1) | Input.GetMouseButtonDown(2))
                            {
                                Input.ResetInputAxes();
                            }
                        }

                    }
                }
            }

            else if (showSelectionGui)
            {
                if (multiSelectWindowRect == null || resetWindowPos)
                {
                    // Re-Center the window
                    multiSelectWindowRect = new Rect(
                        1920 / 2f - multiSelectWindowRect.width / 2f,
                        1080 / 2f - multiSelectWindowRect.height / 2f,
                        multiSelectWindowRect.width,
                        multiSelectWindowRect.height
                        );
                }

                if (activeMultiSelect != null)
                {

                    multiSelectWindowRect = GUI.Window(
                        1000,
                        multiSelectWindowRect,
                        (GUI.WindowFunction)MultiSelectBridge,
                        Translate("Select ") + Translate(activeMultiSelect.Name),
                        Magnetar_Default.ModuleWindow
                    );
                
                }
            }
        }

        private static void DrawCategoryWindow(int id)
        {
            ModuleCategory category = (ModuleCategory)id;

            string cleanSearch = ModuleSearchQuery.Replace(" ", "").ToLower();

            var categoryModules = Modules.Where(m => {
                // If search is empty, show everything in category
                if (string.IsNullOrEmpty(cleanSearch)) return m.Category == category;

                // Prepare module hints (lowercase and no spaces)
                string cleanHints = m.SearchHints.Replace(" ", "").ToLower();
                string cleanName = m.Name.Replace(" ", "").ToLower();

                return m.Category == category &&
                       (cleanName.Contains(cleanSearch) || cleanHints.Contains(cleanSearch));
            }).ToList();

            float windowWidth = windowPositions[category].width;
            GUI.DragWindow(new Rect(0, 0, windowWidth, 25));

            

            float yOffset = 28;
            float buttonHeight = 28;
            foreach (var mod in categoryModules)
            {
                GUIStyle currentStyle = mod.Active ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff;
                Rect btnRect = new Rect(0, yOffset, windowWidth, buttonHeight);

                if (showModules)
                {
                    mod.ShowSettings = false; // Ensure settings are hidden when category window is open
                }

                // Manual Input Check
                if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0)
                    {
                        mod.Toggle();
                        Event.current.Use();
                    }
                    else if (Event.current.button == 1) 
                    {
                        showModules = false;
                        showSelectionGui = false;
                        showSettings = true;

                        mod.ShowSettings = true;
                        Event.current.Use();
                    }
                }

                string translatedModName = Magnetar_Client.Utils.Translator.Translate(mod.Name);
                GUI.Box(btnRect, translatedModName, currentStyle);
                yOffset += buttonHeight;
            }

            // Update the new window Position
            windowPositions[category] = new Rect(windowPositions[category].x, windowPositions[category].y, windowWidth, yOffset);

            
        }

        private static void DrawSettingsWindow(int id, Magnetar_Client.Modules.Module mod)
        {
            float windowWidth = settingsPositions[mod].width;
            float headerHeight = 25f;
            float maxWindowHeight = Screen.height * 0.7f;
            float maxViewHeight = maxWindowHeight - headerHeight;

            if (!moduleContentHeights.ContainsKey(mod)) moduleContentHeights[mod] = 0f;
            if (!targetContentHeights.ContainsKey(mod)) targetContentHeights[mod] = 0f;
            if (!settingsScrollPositions.ContainsKey(mod)) settingsScrollPositions[mod] = Vector2.zero;

            Rect headerBgRect = new Rect(0, 0, windowWidth, headerHeight);
            GUI.Box(headerBgRect, Translate(mod.Name), Magnetar_Default.SettingsWindow);


            // 1. SMOOTH ANIMATION LOGIC
            moduleContentHeights[mod] = Mathf.Lerp(moduleContentHeights[mod], targetContentHeights[mod], Time.deltaTime * 15f);

            if (Mathf.Abs(moduleContentHeights[mod] - targetContentHeights[mod]) < 0.5f)
                moduleContentHeights[mod] = targetContentHeights[mod];

            float contentHeight = moduleContentHeights[mod];

            float windowHeight = Mathf.Min(contentHeight + headerHeight, maxWindowHeight);
            float viewHeight = windowHeight - headerHeight;

            Event e = Event.current;

#if ANDROID
            // --- CLOSE Button ---
            Rect closeButtonRect = new Rect(windowWidth - 26, 4, 22, 22);
            GUI.Box(closeButtonRect, "X", Magnetar_Default.ModuleOff);
            if (e.type == EventType.MouseDown && closeButtonRect.Contains(e.mousePosition))
            {
                showSettings = false;
                showModules = true;
                showSelectionGui = false;
                e.Use();
                return;
            }
#endif

            // 2. CENTER-ANCHOR EXPANSION
            if (e.type == EventType.Layout)
            {
                Rect r = settingsPositions[mod];
                float prevHeight = r.height;

                r.height = windowHeight;

                // Shift the Y position by half the height difference to expand from both top and bottom!
                if (prevHeight > 0 && Mathf.Abs(windowHeight - prevHeight) > 0.1f)
                {
                    r.y -= (windowHeight - prevHeight) / 2f;
                }

                settingsPositions[mod] = r;
            }

            // 3. SCROLL VIEW & RENDERING

            bool needsScrollbar = targetContentHeights[mod] > maxViewHeight;
            float maxScroll = needsScrollbar ? (targetContentHeights[mod] - maxViewHeight) : 0f;
            float contentWidth = needsScrollbar ? windowWidth - 16 : windowWidth;
            float currentScroll = settingsScrollPositions[mod].y;

            Rect outRect = new Rect(0, headerHeight, windowWidth, viewHeight);

            if (outRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                currentScroll = Mathf.Clamp(currentScroll + e.delta.y * 25f, 0, maxScroll);
                settingsScrollPositions[mod] = new Vector2(0, currentScroll);
                e.Use();
            }

            GUI.BeginGroup(outRect);

            float startY = -currentScroll;

            float actualHeightDrawn = DrawModuleSettings(mod, startY, contentWidth);

            if (e.type == EventType.Repaint)
            {
                targetContentHeights[mod] = actualHeightDrawn;
            }

            // Deferred Dropdowns overlay
            if (DrawSetting.OnPostDraw != null)
            {
                DrawSetting.OnPostDraw.Invoke();
                DrawSetting.OnPostDraw = null;
            }

            GUI.EndGroup();

            // --- Custom Visual Scrollbar ---
            if (needsScrollbar)
            {
                float trackX = windowWidth - 14;
                float trackY = headerHeight + 5;
                float trackHeight = viewHeight - 10;

                float handleHeight = Mathf.Max(20f, (maxViewHeight / targetContentHeights[mod]) * trackHeight);
                float scrollPct = maxScroll > 0 ? currentScroll / maxScroll : 0f;
                float handleY = trackY + (scrollPct * (trackHeight - handleHeight));

                GUI.Box(new Rect(trackX + 5, trackY, 2, trackHeight), "", Magnetar_Default.SeparatorStyle);
                GUI.Box(new Rect(trackX, handleY, 12, handleHeight), "", Magnetar_Default.ModuleOff);
            }

            GUI.DragWindow(new Rect(0, 0, windowWidth, headerHeight));
        }

        private static float DrawModuleSettings(Magnetar_Client.Modules.Module mod, float y, float width)
        {
            float startY = y;

            MiscDrawing.SeperatorFull(ref y,width,Config.spacing,Magnetar_Default.AccentColor);

            y+= Config.spacing;


            // 1. TOP SECTION: Info
            float descriptionWidth = width - (Config.indent * 2);
            string translatedDescription = Translate(mod.Description);

            float calculatedHeight = Magnetar_Default.DescriptionStyle.CalcHeight(new GUIContent(translatedDescription), descriptionWidth);
            GUI.Label(new Rect(Config.indent, y, descriptionWidth, calculatedHeight), translatedDescription, Magnetar_Default.DescriptionStyle);
            y += calculatedHeight + Config.spacing;

            // Author (if provided)
            if (!string.IsNullOrEmpty(mod.Author))
            {
                GUI.Label(new Rect(Config.indent, y, width - (Config.indent * 2), 18), "by " + mod.Author, Magnetar_Default.AuthorStyle);
                y += 18 + Config.spacing;
            }

            // 2. MIDDLE SECTION: Custom Settings (Sliders, etc.)

            bool skipSettings = false;

            foreach (var setting in mod.Settings)
            {
                // --- Category Logic ---
                if (setting is CategorySetting catSet)
                {
                    catSet.IsExpanded = MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, Color.white, Translate(catSet.Name), true, catSet.IsExpanded);
                    skipSettings = !catSet.IsExpanded; // If collapsed, skip drawing!
                    if (skipSettings) y -= Config.spacing / 2;
                    continue;
                }
                else if (setting is EndCategorySetting)
                {
                    skipSettings = false;
                    y -= Config.spacing;
                    continue;
                }

                if (skipSettings) continue; // Skip settings if parent category is closed

                if (setting is FloatSetting floatSet) HandleNumericSetting(floatSet, ref y, width, true);
                else if (setting is IntSetting intSet) HandleNumericSetting(intSet, ref y, width, false);
                else if (setting is BoolSetting boolSet) HandleBoolSetting(boolSet, ref y, width);
                else if (setting is BindSetting bindSet) HandleBindSetting(bindSet, ref y, width);
                else if (setting is MultiSelectSetting multiSet) HandleMultiSelectSetting(multiSet, ref y, width);
                else if (setting is StringSetting strSet) HandleStringSetting(strSet, ref y, width);
                else if (setting is SelectSetting selSet) HandleSelectSetting(selSet, ref y, width);

                y += Config.elementHeight + Config.spacing;
            }

            //y+= Config.elementHeight/2;

            // 3. BOTTOM SECTION: Core Configuration

            MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, Color.white, Translate("KeyBind"));

            Event e = Event.current;

            bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

            // --- Keybind ---
            HandleBindSetting(mod.KeyBind, ref y, width);
            y += Config.elementHeight + Config.spacing;

            // --- Hold Mode ---
            string holdModeLabel = Translate("Hold Mode");
            GUI.Label(new Rect(Config.indent, y, width * 0.45f, Config.elementHeight), holdModeLabel);

            Rect holdRect = new Rect(width * 0.5f, y, width * 0.45f, Config.elementHeight);
            bool holdHover = holdRect.Contains(e.mousePosition);

            if (holdHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(holdRect, mod.HoldMode ? Translate("ON") : Translate("OFF"),
                mod.HoldMode ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (holdHover && isLeftClick)
            {
                mod.HoldMode = !mod.HoldMode;
                e.Use();
            }
            y += Config.elementHeight + Config.spacing;

            // --- Active State ---
            GUI.Label(new Rect(Config.indent, y, width * 0.45f, Config.elementHeight), Translate("Enabled"));

            Rect enabledRect = new Rect(width * 0.5f, y, width * 0.45f, Config.elementHeight);
            bool enabledHover = enabledRect.Contains(e.mousePosition);

            if (enabledHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(enabledRect, mod.Active ? Translate("ON") : Translate("OFF"), mod.Active ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            if (enabledHover && isLeftClick)
            {
                mod.Toggle();
                e.Use();
            }
            y += Config.elementHeight + Config.spacing / 2;

            return y - startY;
        }

        private static void MultiSelectBridge(int id)
        {
            GUI.DragWindow(new Rect(0, 0, multiSelectWindowRect.width, 25));

            DrawMultiSelectWindow(multiSelectWindowRect, activeMultiSelect);
        }

        private static void HandleMultiSelectSetting(MultiSelectSetting set, ref float y, float width)
        {

            Event e = Event.current;

            // Setting Name
            GUI.Label(new Rect(Config.indent, y, width * 0.4f, Config.elementHeight), Translate(set.Name));

            // "Select" Button Rect
            Rect btnRect = new Rect(width * 0.58f, y, Config.selectButtonWidth, Config.elementHeight);

            // Hover Feedback with Accent Color
            if (btnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

            // Manual Click Check
            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            {
                showModules = false;
                showSettings = false;
                showSelectionGui = true;

                activeMultiSelect = set;
                multiSelectSearchQuery = "";
                manualScrollY = 0f;
                showModules = false;
            }

            GUI.Box(btnRect, Translate("Select"), Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            // Selection Count Text
            Color originalColor = GUI.contentColor;
            GUI.contentColor = Magnetar_Default.TextDim;
            GUI.Label(new Rect(btnRect.x + Config.selectButtonWidth + 5, y, width * 0.4f, Config.elementHeight), '('+Translate($"{set.SelectedValues.Count} selected") + ")");
            GUI.contentColor = originalColor;

        }
        public static void HandleHotkeys()
        {
            // Safety check: Do not trigger hotkeys if typing or binding
            if (focusedControlId != -1 || bindingModuleId != -1) return;

            foreach (var mod in Modules)
            {
                if (mod.BindKeys != null && mod.BindKeys.Count > 0)
                {
                    bool allKeysHeld = true;
                    bool anyKeyJustPressed = false;
                    bool anyKeyJustReleased = false;

                    // Check the status of every key in the combo
                    foreach (KeyCode key in mod.BindKeys)
                    {
                        if (!Input.GetKey(key)) allKeysHeld = false; // Is this key currently down?
                        if (Input.GetKeyDown(key)) anyKeyJustPressed = true; // Was this key pressed exactly this frame?
                        if (Input.GetKeyUp(key)) anyKeyJustReleased = true; // Was this key released exactly this frame?
                    }

                    if (mod.HoldMode)
                    {
                        // --- Hold Mode ---
                        if (allKeysHeld && anyKeyJustPressed && !mod.Active)
                        {
                            mod.Toggle();
                        }
                        else if (mod.Active && anyKeyJustReleased)
                        {
                            mod.Toggle();
                        }
                    }
                    else
                    {
                        // --- Standard Toggle Mode ---
                        if (allKeysHeld && anyKeyJustPressed)
                        {
                            mod.Toggle();
                        }
                    }

                }
            }
        }

        private static void DrawSearchWindow(int id)
        {
            Rect rect = new Rect(0, 0, searchWindowRect.width, 25);

            GUI.DragWindow(rect);

            float y = 28;

            ModuleSearchQuery = DrawManualTextField(
                new Rect(Config.indent, y, searchWindowRect.width - (Config.indent * 2), 20), 
                ModuleSearchQuery, Translate("Search...")
            );
        }

        
    }
}