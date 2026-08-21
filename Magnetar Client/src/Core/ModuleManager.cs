using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
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
        public static Dictionary<ModuleCategory, Rect> windowPositions = new Dictionary<ModuleCategory, Rect>();

        /// <summary>
        /// Stores the positions of settings windows for each module.
        /// </summary>
        private static Dictionary<Modules.Module, Rect> settingsPositions = new Dictionary<Modules.Module, Rect>();
        private static Dictionary<Modules.Module, Vector2> settingsScrollPositions = new Dictionary<Modules.Module, Vector2>();
        private static Dictionary<Modules.Module, float> moduleContentHeights = new Dictionary<Modules.Module, float>();
        private static Dictionary<Modules.Module, float> targetContentHeights = new Dictionary<Modules.Module, float>();

        public static bool showAddonCategory = false;

        public static bool showModules = true;
        public static bool showSettings = false;
        public static bool showSelectionGui = false;

        public static bool resetWindowPos = false;

        public static int bindingModuleId = -1;
        public static int activeSliderId = -1;

        public static MultiSelectSetting activeMultiSelect = null;
        private static Rect multiSelectWindowRect = new Rect(0, 0, Config.ModuleManager.MultiSelectWindowWidth,
            Config.ModuleManager.MultiSelectWindowHeight);

        public static string ModuleSearchQuery = "";
        public static Rect searchWindowRect;

        public static bool isSearchOpen = false;
        public static float searchAnimProgress = 0f;
        public static bool searchWasFocused = false;
        public static bool requestSearchFocus = false;
        #endregion

        public static void Init()
        {
            // load all the modules
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.StartsWith("Magnetar_Client.Modules")
                            && t.IsSubclassOf(typeof(Magnetar_Client.Modules.Module)) && !t.IsAbstract);

            foreach (var type in types)
            {
                RegisterModule(type);
            }

            int index = 0;
            foreach (ModuleCategory cat in System.Enum.GetValues(typeof(ModuleCategory)))
            {
                // Default category window positions
                windowPositions[cat] = new Rect(20 + (index * (Config.ModuleWindowWidth + 10)), 50, Config.ModuleWindowWidth, 50);
                index++;
            }

            multiSelectWindowRect = new Rect(
                        1920 / 2f - multiSelectWindowRect.width / 2f,
                        1080 / 2f - multiSelectWindowRect.height / 2f,
                        multiSelectWindowRect.width,
                        multiSelectWindowRect.height
                        );

            showModules = true; showSettings = false; showSelectionGui = false;
            IsInitialized = true;

            Utils.Magnetar_Logger.DebugLogger.Msg($"Loaded {Modules.Count} modules");
        }

        public static void RegisterModule(Type type)
        {
            try
            {
                Modules.Add((Magnetar_Client.Modules.Module)Activator.CreateInstance(type));
            }
            catch (Exception ex)
            {
                Utils.Magnetar_Logger.DebugLogger.Error("Failed to load ModuleManager: " + ex);
            }
        }

        public static void Render()
        {
            if (showModules)
            {
                resetWindowPos = true;

                if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    isSearchOpen = true;
                    requestSearchFocus = true;

                    float searchWidth = Config.ModuleWindowWidth;
                    float tfY = 10f;
                    Rect anticipatedTfRect = new Rect(Config.indent, tfY, searchWidth - (Config.indent * 2), 20);
                    activeTextFieldId = anticipatedTfRect.GetHashCode();

                    GUI.FocusWindow(999);
                    Event.current.Use();
                }

                if (isSearchOpen)
                {
                    if (activeTextFieldId != -1) searchWasFocused = true;

                    if (searchWasFocused && activeTextFieldId == -1 && string.IsNullOrEmpty(ModuleSearchQuery))
                    {
                        isSearchOpen = false;
                        requestSearchFocus = true;
                        searchWasFocused = false;
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    float targetProgress = isSearchOpen ? 1f : 0f;
                    searchAnimProgress = Mathf.Lerp(searchAnimProgress, targetProgress, Time.unscaledDeltaTime * Config.ModuleManager.SearchAnimationSpeed);
                }

                if (searchAnimProgress > 0.01f)
                {
                    float searchWidth = Config.ModuleWindowWidth * Config.ModuleManager.SearchWidthMultiplier;
                    float searchHeight = 30f;

                    float targetY = 1080f - searchHeight - 20f;
                    float hiddenY = 1080f + 10f;
                    float currentY = Mathf.Lerp(hiddenY, targetY, searchAnimProgress);
                    float currentX = (1920f / 2f) - (searchWidth / 2f);

                    searchWindowRect = new Rect(currentX, currentY, searchWidth, searchHeight);

                    searchWindowRect = GUI.Window(
                        999,
                        searchWindowRect,
                        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawSearchWindow),
                        "",
                        Magnetar_Default.ModuleWindow
                    );
                }

                // --- 2. Render Category Windows ---
                foreach (ModuleCategory cat in System.Enum.GetValues(typeof(ModuleCategory)))
                {
                    if (cat == ModuleCategory.Addon && !showAddonCategory) continue;
                    int id = (int)cat;
                    windowPositions[cat] = GUI.Window(
                        id,
                        windowPositions[cat],
                        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawCategoryWindow),
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

                        float maxNameWidth = 0f;
                        if (mod.Settings != null)
                        {
                            foreach (var setting in mod.Settings)
                            {
                                string name = "";
                                if (setting is FloatSetting fSet) name = fSet.Name;
                                else if (setting is IntSetting iSet) name = iSet.Name;
                                else if (setting is BoolSetting bSet) name = bSet.Name;
                                else if (setting is BindSetting bindSet) name = bindSet.Name;
                                else if (setting is MultiSelectSetting msSet) name = msSet.Name;
                                else if (setting is StringSetting strSet) name = strSet.Name;
                                else if (setting is SelectSetting selSet) name = selSet.Name;
                                else if (setting is CategorySetting catSet) name = catSet.Name;

                                if (!string.IsNullOrEmpty(name))
                                {
                                    float w = Magnetar_Default.SettingDescriptionStyle.CalcSize(new GUIContent(Translate(name))).x;
                                    if (w > maxNameWidth) maxNameWidth = w;
                                }
                            }
                        }

                        string[] builtIns = { "Hold Mode", "Enabled", "KeyBind" };
                        foreach (var b in builtIns)
                        {
                            float w = Magnetar_Default.SettingDescriptionStyle.CalcSize(new GUIContent(Translate(b))).x;
                            if (w > maxNameWidth) maxNameWidth = w;
                        }

                        float fixedControlWidth = 140f;
                        float calculatedWidth = Config.indent + maxNameWidth + 20f + fixedControlWidth + Config.indent;

                        float targetWidth = Mathf.Max(mod.SettingsWidth, calculatedWidth);

                        if (!settingsPositions.ContainsKey(mod) || resetWindowPos)
                        {
                            resetWindowPos = false;

                            float popupHeight = 25f;

                            settingsPositions[mod] = new Rect(
                                (Config.WindowWidth / 2f) - (targetWidth / 2f),
                                (Config.WindowHeight / 2f) - (popupHeight / 2f),
                                targetWidth,
                                popupHeight
                            );

                            moduleContentHeights[mod] = 0f;
                            targetContentHeights[mod] = 0f;
                        }
                        else
                        {
                            Rect currentRect = settingsPositions[mod];
                            if (Mathf.Abs(currentRect.width - targetWidth) > 0.5f)
                            {
                                float newWidth = Mathf.Lerp(currentRect.width, targetWidth,
                                    Time.deltaTime * Config.ModuleManager.PopupSpeed);
                                float widthDiff = newWidth - currentRect.width;

                                currentRect.width = newWidth;
                                currentRect.x -= widthDiff / 2f;
                                settingsPositions[mod] = currentRect;
                            }
                        }

                        settingsPositions[mod] = GUI.Window(
                            settingsId,
                            settingsPositions[mod],
                            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)(id => DrawSettingsWindow(id, mod))),
                            "",
                            Magnetar_Default.ModuleWindow
                        );

                        if (settingsPositions[mod].Contains(new Vector2(Input.mousePosition.x, 1080 - Input.mousePosition.y)))
                        {
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
                if (resetWindowPos)
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
                        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)MultiSelectBridge),
                        "",
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
                if (string.IsNullOrEmpty(cleanSearch)) return m.Category == category;

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
                    mod.ShowSettings = false;
                }

                if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0)
                    {
                        if (VanillaMode.instance.IsAllowed(mod))
                        {
                            mod.Toggle();
                        }
                            
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

            windowPositions[category] = new Rect(windowPositions[category].x, windowPositions[category].y, windowWidth, yOffset);
        }

        private static void DrawSettingsWindow(int id, Magnetar_Client.Modules.Module mod)
        {
            float windowWidth = settingsPositions[mod].width;
            float headerHeight = 25f;
            float maxWindowHeight = Screen.height * Config.ModuleManager.MaxSettingsWindowHeightPct;
            float maxViewHeight = maxWindowHeight - headerHeight;

            if (!moduleContentHeights.ContainsKey(mod)) moduleContentHeights[mod] = 0f;
            if (!targetContentHeights.ContainsKey(mod)) targetContentHeights[mod] = 0f;
            if (!settingsScrollPositions.ContainsKey(mod)) settingsScrollPositions[mod] = Vector2.zero;

            Rect headerBgRect = new Rect(0, 0, windowWidth, headerHeight);
            GUI.Box(headerBgRect, Translate(mod.Name), Magnetar_Default.SettingsWindow);

            moduleContentHeights[mod] = Mathf.Lerp(moduleContentHeights[mod], targetContentHeights[mod], 
                Time.unscaledDeltaTime * Config.ModuleManager.SettingsScrollLerpSpeed);

            if (Mathf.Abs(moduleContentHeights[mod] - targetContentHeights[mod]) < 0.5f)
                moduleContentHeights[mod] = targetContentHeights[mod];

            float contentHeight = moduleContentHeights[mod];

            float windowHeight = Mathf.Min(contentHeight + headerHeight, maxWindowHeight);
            float viewHeight = windowHeight - headerHeight;

            Event e = Event.current;

#if ANDROID
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

            if (e.type == EventType.Layout)
            {
                Rect r = settingsPositions[mod];
                float prevHeight = r.height;

                r.height = windowHeight;

                if (prevHeight > 0 && Mathf.Abs(windowHeight - prevHeight) > 0.1f)
                {
                    r.y -= (windowHeight - prevHeight) / 2f;
                }

                settingsPositions[mod] = r;
            }

            bool needsScrollbar = targetContentHeights[mod] > maxViewHeight;
            float maxScroll = needsScrollbar ? (targetContentHeights[mod] - maxViewHeight) : 0f;
            float contentWidth = needsScrollbar ? windowWidth - 16 : windowWidth;
            float currentScroll = settingsScrollPositions[mod].y;

            Rect outRect = new Rect(0, headerHeight, windowWidth, viewHeight);

            if (outRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                currentScroll = Mathf.Clamp(currentScroll + e.delta.y * Config.ModuleManager.ScrollSensitivity, 0, maxScroll);
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

            if (DrawSetting.OnPostDraw != null)
            {
                DrawSetting.OnPostDraw.Invoke();
                DrawSetting.OnPostDraw = null;
            }

            GUI.EndGroup();

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

            MiscDrawing.SeperatorFull(ref y, width, Config.spacing, Magnetar_Default.AccentColor);

            y += Config.spacing;

            float descriptionWidth = width - (Config.indent * 2);
            string translatedDescription = Translate(mod.Description);

            float calculatedHeight = Magnetar_Default.DescriptionStyle.CalcHeight(new GUIContent(translatedDescription), descriptionWidth);
            GUI.Label(new Rect(Config.indent, y, descriptionWidth, calculatedHeight), translatedDescription, Magnetar_Default.DescriptionStyle);
            y += calculatedHeight + Config.spacing;

            if (!string.IsNullOrEmpty(mod.Author))
            {
                GUI.Label(new Rect(Config.indent, y, width - (Config.indent * 2), 18), "by " + mod.Author, Magnetar_Default.AuthorStyle);
                y += 18 + Config.spacing;
            }

            bool skipSettings = false;

            foreach (var setting in mod.Settings)
            {
                if (setting is CategorySetting catSet)
                {
                    catSet.IsExpanded = MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, Color.white, Translate(catSet.Name), true, catSet.IsExpanded);
                    skipSettings = !catSet.IsExpanded;
                    if (skipSettings) y -= Config.spacing / 2;
                    continue;
                }
                else if (setting is EndCategorySetting)
                {
                    skipSettings = false;
                    continue;
                }

                if (skipSettings) continue;

                if (setting is FloatSetting floatSet) HandleNumericSetting(floatSet, ref y, width, true);
                else if (setting is IntSetting intSet) HandleNumericSetting(intSet, ref y, width, false);
                else if (setting is BoolSetting boolSet) HandleBoolSetting(boolSet, ref y, width);
                else if (setting is BindSetting bindSet) HandleBindSetting(bindSet, ref y, width);
                else if (setting is MultiSelectSetting multiSet) HandleMultiSelectSetting(multiSet, ref y, width);
                else if (setting is StringSetting strSet) HandleStringSetting(strSet, ref y, width);
                else if (setting is SelectSetting selSet) HandleSelectSetting(selSet, ref y, width);

                y += Config.elementHeight + Config.spacing;
            }

            MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, Color.white, Translate("KeyBind"));

            Event e = Event.current;
            bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

            HandleBindSetting(mod.KeyBind, ref y, width);
            y += Config.elementHeight + Config.spacing;

            string holdModeLabel = Translate("Hold Mode");
            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), holdModeLabel, Magnetar_Default.SettingDescriptionStyle);

            Rect holdRect = new Rect(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
            bool holdHover = holdRect.Contains(e.mousePosition);

            GUI.Box(holdRect, mod.HoldMode ? Translate("ON") : Translate("OFF"),
                mod.HoldMode ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);

            if (holdHover && isLeftClick)
            {
                mod.HoldMode = !mod.HoldMode;
                e.Use();
            }
            y += Config.elementHeight + Config.spacing;

            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), Translate("Enabled"), Magnetar_Default.SettingDescriptionStyle);

            Rect enabledRect = new Rect(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
            bool enabledHover = enabledRect.Contains(e.mousePosition);

            GUI.Box(enabledRect, mod.Active ? Translate("ON") : Translate("OFF"),
                mod.Active ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);

            if (enabledHover && isLeftClick)
            {
                if (VanillaMode.instance.IsAllowed(mod))
                {
                    mod.Toggle();
                }
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

            GUI.Label(new Rect(Config.indent, y, width * 0.4f, Config.elementHeight), Translate(set.Name), Magnetar_Default.SettingDescriptionStyle);

            Rect btnRect = new Rect(width - Config.SettingWidth/2f-Config.selectButtonWidth - 
                Magnetar_Default.SettingDescriptionStyle.CalcSize(new GUIContent('(' + Translate($"{set.SelectedValues.Count} selected") + ")")).x/2, y,
                Config.selectButtonWidth, Config.elementHeight);

            if (btnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

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

            GUI.Box(btnRect, Translate("Select"), Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            Color originalColor = GUI.contentColor;
            GUI.contentColor = Magnetar_Default.TextDim;
            GUI.Label(new Rect(btnRect.x + Config.selectButtonWidth + 5, y, width * 0.4f, Config.elementHeight),
                '(' + Translate($"{set.SelectedValues.Count} selected") + ")", Magnetar_Default.SettingDescriptionStyle);
            GUI.contentColor = originalColor;
        }

        public static void HandleHotkeys()
        {
            if (focusedControlId != -1 || bindingModuleId != -1) return;

            foreach (var mod in Modules)
            {
                if (mod.BindKeys != null && mod.BindKeys.Count > 0)
                {
                    bool allKeysHeld = true;
                    bool anyKeyJustPressed = false;
                    bool anyKeyJustReleased = false;

                    foreach (KeyCode key in mod.BindKeys)
                    {
                        if (!Input.GetKey(key)) allKeysHeld = false;
                        if (Input.GetKeyDown(key)) anyKeyJustPressed = true;
                        if (Input.GetKeyUp(key)) anyKeyJustReleased = true;
                    }

                    if (mod.HoldMode)
                    {
                        if (allKeysHeld && anyKeyJustPressed && !mod.Active)
                        {
                            if (VanillaMode.instance.IsAllowed(mod))
                            {
                                mod.Toggle();
                            }
                        }
                        else if (mod.Active && anyKeyJustReleased)
                        {
                            if (VanillaMode.instance.IsAllowed(mod))
                            {
                                mod.Toggle();
                            }
                        }
                    }
                    else
                    {
                        if (allKeysHeld && anyKeyJustPressed)
                        {
                            if (VanillaMode.instance.IsAllowed(mod))
                            {
                                mod.Toggle();
                            }
                        }
                    }

                }
            }
        }

        private static void DrawSearchWindow(int id)
        {

            Rect tfRect = new Rect(5, 5, searchWindowRect.width - 10, 20);

            if (requestSearchFocus)
            {
                activeTextFieldId = tfRect.GetHashCode();
                GUI.FocusWindow(999);

                if (Event.current.type == EventType.Repaint)
                {
                    requestSearchFocus = false;
                }
            }

            ModuleSearchQuery = DrawManualTextField(
                tfRect,
                ModuleSearchQuery, Translate("Search...")
            );
        }
    }
}