using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.UI.WindowDrawing.DrawSetting;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core
{
    // =========================================================================
    // Main Coordinator
    // =========================================================================
    public static class ModuleManager
    {
        public static bool IsInitialized = false;
        public static List<Modules.Module> Modules = new List<Modules.Module>();

        public static bool showAddonCategory = false;
        public static bool showModules = true;
        public static bool showSettings = false;
        public static bool showSelectionGui = false;
        public static Modules.Module activeSettingsModule = null;

        public static bool resetWindowPos = false;
        public static int bindingModuleId = -1;

        // Exposed properties for backward compatibility with other systems
        public static Dictionary<ModuleCategory, Rect> windowPositions => CategoryWindowDrawer.WindowPositions;
        public static string ModuleSearchQuery
        {
            get => SearchWindowDrawer.SearchQuery;
            set => SearchWindowDrawer.SearchQuery = value;
        }

        public static void Init()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.StartsWith("Magnetar_Client.Modules")
                            && t.IsSubclassOf(typeof(Magnetar_Client.Modules.Module)) && !t.IsAbstract);

            foreach (var type in types)
            {
                RegisterModule(type);
            }

            CategoryWindowDrawer.InitializeLayout();
            MultiSelectWindowDrawer.InitializeLayout();
            SearchWindowDrawer.Initialize();

            showModules = true;
            showSettings = false;
            showSelectionGui = false;

            IsInitialized = true;
            DebugLogger.Msg($"Loaded {Modules.Count} modules");
        }

        public static void RegisterModule(Type type)
        {
            try
            {
                Modules.Add((Magnetar_Client.Modules.Module)Activator.CreateInstance(type));
            }
            catch (Exception ex)
            {
                DebugLogger.Error("Failed to load ModuleManager: " + ex);
            }
        }

        public static void OpenModuleSettings(Modules.Module mod)
        {
            MobileInputHandler.Reset();
            showModules = false;
            showSelectionGui = false;
            showSettings = true;
            activeSettingsModule = mod;

            foreach (var m in Modules)
                m.ShowSettings = false;

            mod.ShowSettings = true;
            resetWindowPos = true;
        }

        public static void Render()
        {
            Event currentEvent = Event.current;
            MobileInputHandler.Update(currentEvent);

            if (showModules)
            {
                resetWindowPos = true;
                SearchWindowDrawer.Render(currentEvent);
                CategoryWindowDrawer.Render();
            }
            else if (showSettings)
            {
                SettingsWindowDrawer.Render(currentEvent);
            }
            else if (showSelectionGui)
            {
                MultiSelectWindowDrawer.Render(currentEvent);
            }
        }

        public static void HandleHotkeys()
        {
            if (focusedControlId != -1 || bindingModuleId != -1) return;

            foreach (var mod in Modules)
            {
                if (mod.BindKeys == null || mod.BindKeys.Count == 0) continue;

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
                        if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                    }
                    else if (mod.Active && anyKeyJustReleased)
                    {
                        if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                    }
                }
                else
                {
                    if (allKeysHeld && anyKeyJustPressed)
                    {
                        if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                    }
                }
            }
        }
    }

    // =========================================================================
    // Category Windows (Folding, 70% Height Scrolling & Boundary Clamping)
    // =========================================================================
    internal static class CategoryWindowDrawer
    {
        public static readonly Dictionary<ModuleCategory, Rect> WindowPositions = new Dictionary<ModuleCategory, Rect>();
        public static readonly Dictionary<ModuleCategory, bool> CategoryFolded = new Dictionary<ModuleCategory, bool>();
        public static readonly Dictionary<ModuleCategory, float> CategoryScrollPositions = new Dictionary<ModuleCategory, float>();

#if ANDROID
        private static readonly Dictionary<ModuleCategory, float> _touchStartY = new Dictionary<ModuleCategory, float>();
        private static readonly Dictionary<ModuleCategory, float> _touchStartScroll = new Dictionary<ModuleCategory, float>();
        private static readonly Dictionary<ModuleCategory, bool> _isDragging = new Dictionary<ModuleCategory, bool>();
#endif

        private static GUI.WindowFunction _cachedCategoryDelegate;
        private static GUI.WindowFunction CategoryDelegate => _cachedCategoryDelegate ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawCategoryWindow);

        public static void InitializeLayout()
        {
            int index = 0;
            foreach (ModuleCategory cat in Enum.GetValues(typeof(ModuleCategory)))
            {
                float initX = Config.S(20f) + (index * (Config.ModuleWindowWidth + Config.S(10f)));
                float initY = Config.S(50f);
                float w = Config.ModuleWindowWidth;
                float h = Config.S(50f);

                WindowPositions[cat] = ScreenBoundaryHelper.Clamp(new Rect(initX, initY, w, h));
                CategoryFolded[cat] = false;
                CategoryScrollPositions[cat] = 0f;
                index++;
            }
        }

        public static void Render()
        {
            foreach (ModuleCategory cat in Enum.GetValues(typeof(ModuleCategory)))
            {
                if (cat == ModuleCategory.Addon && !ModuleManager.showAddonCategory) continue;
                int id = (int)cat;

                Rect syncedPos = WindowPositions[cat];
                if (!Mathf.Approximately(syncedPos.width, Config.ModuleWindowWidth))
                {
                    syncedPos.width = Config.ModuleWindowWidth;
                }

                // Pre-draw clamp to ensure window stays in viewport
                WindowPositions[cat] = ScreenBoundaryHelper.Clamp(syncedPos);

                WindowPositions[cat] = GUI.Window(
                    id,
                    WindowPositions[cat],
                    CategoryDelegate,
                    Translate(cat.ToString()),
                    Magnetar_Default.ModuleWindow
                );

                // Post-drag clamp
                WindowPositions[cat] = ScreenBoundaryHelper.Clamp(WindowPositions[cat]);

                if (WindowPositions[cat].Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                    {
                        Input.ResetInputAxes();
                    }
                }
            }
        }

        private static void DrawCategoryWindow(int id)
        {
            try
            {
                ModuleCategory category = (ModuleCategory)id;
                var categoryModules = FilterModulesByCategory(category);

                float windowWidth = WindowPositions[category].width;
                float headerHeight = Config.S(28f);
                float buttonHeight = Config.S(28f);
                Event e = Event.current;

                if (!CategoryFolded.ContainsKey(category)) CategoryFolded[category] = false;
                bool isFolded = CategoryFolded[category];

                // --- 1. Triangle Fold Indicator & Header Click ---
                Rect foldBtnRect = new Rect(windowWidth - Config.S(28f), (headerHeight - Config.S(20f)) / 2f, Config.S(22f), Config.S(20f));
                string foldIndicator = isFolded ? "▶" : "▼";
                GUI.Box(foldBtnRect, foldIndicator, Magnetar_Default.ModuleOff);

                if (e.type == EventType.MouseDown && foldBtnRect.Contains(e.mousePosition))
                {
                    CategoryFolded[category] = !isFolded;
                    e.Use();
                    return;
                }
                else if (e.type == EventType.MouseDown && e.button == 1 && new Rect(0, 0, windowWidth, headerHeight).Contains(e.mousePosition))
                {
                    CategoryFolded[category] = !isFolded;
                    e.Use();
                    return;
                }

                // If collapsed, resize to titlebar only
                if (isFolded)
                {
                    GUI.DragWindow(new Rect(0, 0, windowWidth - Config.S(30f), headerHeight));
                    WindowPositions[category] = ScreenBoundaryHelper.Clamp(new Rect(WindowPositions[category].x, WindowPositions[category].y, windowWidth, headerHeight));
                    return;
                }

                // --- 2. Calculate Heights & 70% Max Screen Height Threshold ---
                float totalContentHeight = categoryModules.Count * buttonHeight;
                float maxCategoryHeight = Config.WindowHeight * 0.70f;
                float maxViewHeight = maxCategoryHeight - headerHeight;

                bool needsScroll = (headerHeight + totalContentHeight) > maxCategoryHeight;
                float viewHeight = needsScroll ? maxViewHeight : totalContentHeight;
                float finalWindowHeight = headerHeight + viewHeight;
                float maxScroll = needsScroll ? (totalContentHeight - viewHeight) : 0f;

                if (!CategoryScrollPositions.ContainsKey(category)) CategoryScrollPositions[category] = 0f;
                float currentScroll = needsScroll ? Mathf.Clamp(CategoryScrollPositions[category], 0f, maxScroll) : 0f;
                CategoryScrollPositions[category] = currentScroll;

                Rect viewRect = new Rect(0, headerHeight, windowWidth, viewHeight);

                // --- 3. Scroll Interactions (Wheel + Mobile Drag) ---
                currentScroll = HandleScrollInput(category, viewRect, currentScroll, maxScroll, needsScroll, e);
                CategoryScrollPositions[category] = currentScroll;

                // --- 4. Render Module Buttons Inside Scroll Area ---
                float contentWidth = needsScroll ? windowWidth - Config.S(8f) : windowWidth;
                GUI.BeginGroup(viewRect);
                DrawCategoryItems(category, categoryModules, buttonHeight, currentScroll, viewHeight, contentWidth, headerHeight, e);
                GUI.EndGroup();

                // --- 5. Scrollbar ---
                if (needsScroll)
                {
                    DrawScrollbar(windowWidth, headerHeight, viewHeight, totalContentHeight, currentScroll, maxScroll);
                }

                GUI.DragWindow(new Rect(0, 0, windowWidth - Config.S(30f), headerHeight));
                WindowPositions[category] = ScreenBoundaryHelper.Clamp(new Rect(WindowPositions[category].x, WindowPositions[category].y, windowWidth, finalWindowHeight));
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[DrawCategoryWindow] Error rendering category {id}: {ex}");
            }
        }

        private static List<Modules.Module> FilterModulesByCategory(ModuleCategory category)
        {
            string cleanSearch = SearchWindowDrawer.SearchQuery.Replace(" ", "").ToLower();
            return ModuleManager.Modules.Where(m =>
            {
                if (string.IsNullOrEmpty(cleanSearch)) return m.Category == category;
                string cleanHints = m.SearchHints.Replace(" ", "").ToLower();
                string cleanName = m.Name.Replace(" ", "").ToLower();
                return m.Category == category && (cleanName.Contains(cleanSearch) || cleanHints.Contains(cleanSearch));
            }).ToList();
        }

        private static float HandleScrollInput(ModuleCategory category, Rect viewRect, float currentScroll, float maxScroll, bool needsScroll, Event e)
        {
            if (!needsScroll) return 0f;

            if (viewRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                currentScroll = Mathf.Clamp(currentScroll + e.delta.y * Config.ModuleManager.ScrollSensitivity, 0f, maxScroll);
                e.Use();
            }

#if ANDROID
            if (e.type == EventType.MouseDown && viewRect.Contains(e.mousePosition))
            {
                _touchStartY[category] = e.mousePosition.y;
                _touchStartScroll[category] = currentScroll;
                _isDragging[category] = false;
            }
            else if (e.type == EventType.MouseDrag && _touchStartY.ContainsKey(category))
            {
                float deltaY = _touchStartY[category] - e.mousePosition.y;
                if (Mathf.Abs(deltaY) > Config.S(8f))
                {
                    _isDragging[category] = true;
                    MobileInputHandler.Reset();
                    currentScroll = Mathf.Clamp(_touchStartScroll[category] + deltaY, 0f, maxScroll);
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                _isDragging[category] = false;
            }
#endif
            return currentScroll;
        }

        private static void DrawCategoryItems(
            ModuleCategory category,
            List<Modules.Module> categoryModules,
            float buttonHeight,
            float currentScroll,
            float viewHeight,
            float contentWidth,
            float headerHeight,
            Event e)
        {
            for (int i = 0; i < categoryModules.Count; i++)
            {
                var mod = categoryModules[i];
                float currentY = Mathf.Round((i * buttonHeight) - currentScroll);
                float nextY = Mathf.Round(((i + 1) * buttonHeight) - currentScroll);
                float thisButtonHeight = nextY - currentY;

                // Offscreen culling
                if (currentY + thisButtonHeight < 0 || currentY > viewHeight) continue;

                GUIStyle currentStyle = mod.Active ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff;
                Rect btnRect = new Rect(0, currentY, contentWidth, thisButtonHeight);

                if (ModuleManager.showModules)
                {
                    mod.ShowSettings = false;
                }

                if (btnRect.Contains(e.mousePosition))
                {
#if !ANDROID
                    if (e.type == EventType.MouseDown)
                    {
                        if (e.button == 0)
                        {
                            if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                            e.Use();
                        }
                        else if (e.button == 1)
                        {
                            ModuleManager.OpenModuleSettings(mod);
                            e.Use();
                        }
                    }
#else
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        MobileInputHandler.OnTouchDown(mod, WindowPositions[category], headerHeight, e.mousePosition);
                        e.Use();
                    }
                    else if (e.type == EventType.MouseUp && e.button == 0)
                    {
                        bool wasDragging = _isDragging.TryGetValue(category, out bool isDrag) && isDrag;
                        if (!wasDragging)
                        {
                            MobileInputHandler.OnTouchUp(mod);
                            e.Use();
                        }
                    }
#endif
                }

                GUI.Box(btnRect, Translate(mod.Name), currentStyle);
            }
        }

        private static void DrawScrollbar(float windowWidth, float headerHeight, float viewHeight, float totalContentHeight, float currentScroll, float maxScroll)
        {
            float trackX = windowWidth - Config.S(6f);
            float trackY = headerHeight + Config.S(2f);
            float trackHeight = viewHeight - Config.S(4f);

            float handleHeight = Mathf.Max(Config.S(16f), (viewHeight / totalContentHeight) * trackHeight);
            float scrollPct = maxScroll > 0 ? currentScroll / maxScroll : 0f;
            float handleY = trackY + (scrollPct * (trackHeight - handleHeight));

            GUI.Box(new Rect(trackX + Config.S(1f), trackY, Config.S(2f), trackHeight), "", Magnetar_Default.SeparatorStyle);
            GUI.Box(new Rect(trackX, handleY, Config.S(5f), handleHeight), "", Magnetar_Default.ModuleOff);
        }
    }

    // =========================================================================
    // Settings Popup Window & Controls Drawing
    // =========================================================================
    internal static class SettingsWindowDrawer
    {
        private static readonly Dictionary<Modules.Module, Rect> _settingsPositions = new Dictionary<Modules.Module, Rect>();
        private static readonly Dictionary<Modules.Module, Vector2> _settingsScrollPositions = new Dictionary<Modules.Module, Vector2>();
        private static readonly Dictionary<Modules.Module, float> _moduleContentHeights = new Dictionary<Modules.Module, float>();
        private static readonly Dictionary<Modules.Module, float> _targetContentHeights = new Dictionary<Modules.Module, float>();
        private static readonly Dictionary<Modules.Module, GUI.WindowFunction> _cachedSettingsDelegates = new Dictionary<Modules.Module, GUI.WindowFunction>();

        private static GUI.WindowFunction GetSettingsDelegate(Modules.Module mod)
        {
            if (!_cachedSettingsDelegates.TryGetValue(mod, out var del))
            {
                del = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)(id => DrawSettingsWindow(id, mod)));
                _cachedSettingsDelegates[mod] = del;
            }
            return del;
        }

        public static void Render(Event currentEvent)
        {
            foreach (var mod in ModuleManager.Modules)
            {
                if (!mod.ShowSettings) continue;

                int settingsId = Mathf.Abs(mod.GetHashCode()) + 1000;
                float targetWidth = CalculateTargetWidth(mod);

                if (!_settingsPositions.ContainsKey(mod) || ModuleManager.resetWindowPos)
                {
                    ModuleManager.resetWindowPos = false;
                    float popupHeight = Config.S(25f);

                    _settingsPositions[mod] = new Rect(
                        (Config.WindowWidth / 2f) - (targetWidth / 2f),
                        (Config.WindowHeight / 2f) - (popupHeight / 2f),
                        targetWidth,
                        popupHeight
                    );

                    _moduleContentHeights[mod] = 0f;
                    _targetContentHeights[mod] = 0f;
                }
                else
                {
                    Rect currentRect = _settingsPositions[mod];
                    if (Mathf.Abs(currentRect.width - targetWidth) > 0.5f)
                    {
                        float newWidth = Mathf.Lerp(currentRect.width, targetWidth, Time.deltaTime * Config.ModuleManager.PopupSpeed);
                        float widthDiff = newWidth - currentRect.width;

                        currentRect.width = newWidth;
                        currentRect.x -= widthDiff / 2f;
                        _settingsPositions[mod] = currentRect;
                    }
                }

                _settingsPositions[mod] = GUI.Window(
                    settingsId,
                    _settingsPositions[mod],
                    GetSettingsDelegate(mod),
                    "",
                    Magnetar_Default.ModuleWindow
                );
            }
        }

        private static float CalculateTargetWidth(Modules.Module mod)
        {
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

            float calculatedWidth = Config.indent + maxNameWidth + Config.S(25f) + Config.SettingWidth + Config.indent;
            return Mathf.Max(Config.ModuleManager.SettingsWidth, Mathf.Max(mod.SettingsWidth, calculatedWidth));
        }

        private static void DrawSettingsWindow(int id, Modules.Module mod)
        {
            ModuleManager.activeSettingsModule = mod;
            float windowWidth = _settingsPositions[mod].width;

#if ANDROID
            // 30% header size increase on Android
            float headerHeight = Config.S(25f) * 1.30f;
#else
            float headerHeight = Config.S(25f);
#endif
            float maxWindowHeight = Config.WindowHeight * Config.ModuleManager.MaxSettingsWindowHeightPct;
            float maxViewHeight = maxWindowHeight - headerHeight;

            if (!_moduleContentHeights.ContainsKey(mod)) _moduleContentHeights[mod] = 0f;
            if (!_targetContentHeights.ContainsKey(mod)) _targetContentHeights[mod] = 0f;
            if (!_settingsScrollPositions.ContainsKey(mod)) _settingsScrollPositions[mod] = Vector2.zero;

            Rect headerBgRect = new Rect(0, 0, windowWidth, headerHeight);
            GUI.Box(headerBgRect, Translate(mod.Name), Magnetar_Default.SettingsWindow);

            _moduleContentHeights[mod] = Mathf.Lerp(_moduleContentHeights[mod], _targetContentHeights[mod],
                Time.unscaledDeltaTime * Config.ModuleManager.SettingsScrollLerpSpeed);

            if (Mathf.Abs(_moduleContentHeights[mod] - _targetContentHeights[mod]) < 0.5f)
                _moduleContentHeights[mod] = _targetContentHeights[mod];

            float contentHeight = _moduleContentHeights[mod];
            float windowHeight = Mathf.Min(contentHeight + headerHeight, maxWindowHeight);
            float viewHeight = windowHeight - headerHeight;

            Event e = Event.current;
            float closeBtnSize = Config.S(20f);

            if (Config.ShowMobileButtons)
            {
                float btnSize = Config.S(22f);
                float btnY = (headerHeight - btnSize) / 2f;
                Rect closeButtonRect = new Rect(windowWidth - Config.S(26f), btnY, btnSize, btnSize);
                GUI.Box(closeButtonRect, "X", Magnetar_Default.ModuleOn);
                if (e.type == EventType.MouseDown && closeButtonRect.Contains(e.mousePosition))
                {
                    ModuleManager.showSettings = false;
                    ModuleManager.showModules = true;
                    ModuleManager.showSelectionGui = false;
                    return;
                }
            }

            if (e.type == EventType.Layout)
            {
                Rect r = _settingsPositions[mod];
                float prevHeight = r.height;
                r.height = windowHeight;
                if (prevHeight > 0 && Mathf.Abs(windowHeight - prevHeight) > 0.1f)
                {
                    r.y -= (windowHeight - prevHeight) / 2f;
                }
                _settingsPositions[mod] = r;
            }

            bool needsScrollbar = _targetContentHeights[mod] > maxViewHeight;
            float maxScroll = needsScrollbar ? (_targetContentHeights[mod] - maxViewHeight) : 0f;
            float contentWidth = needsScrollbar ? windowWidth - Config.S(16f) : windowWidth;
            float currentScroll = _settingsScrollPositions[mod].y;

            Rect outRect = new Rect(0, headerHeight, windowWidth, viewHeight);
            if (outRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                currentScroll = Mathf.Clamp(currentScroll + e.delta.y * Config.ModuleManager.ScrollSensitivity, 0, maxScroll);
                _settingsScrollPositions[mod] = new Vector2(0, currentScroll);
                e.Use();
            }

            GUI.BeginGroup(outRect);
            float actualHeightDrawn = DrawSettingsBody(mod, -currentScroll, contentWidth);
            if (e.type == EventType.Repaint)
            {
                _targetContentHeights[mod] = actualHeightDrawn;
            }

            if (DrawSetting.OnPostDraw != null)
            {
                DrawSetting.OnPostDraw.Invoke();
                DrawSetting.OnPostDraw = null;
            }
            GUI.EndGroup();

            if (needsScrollbar)
            {
                DrawSettingsScrollbar(windowWidth, headerHeight, viewHeight, _targetContentHeights[mod], maxViewHeight, currentScroll, maxScroll);
            }

            GUI.DragWindow(new Rect(0, 0, windowWidth - closeBtnSize - Config.S(10f), headerHeight));
        }

        private static float DrawSettingsBody(Modules.Module mod, float y, float width)
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
                float authorLineHeight = Config.S(18f);
                GUI.Label(new Rect(Config.indent, y, width - (Config.indent * 2), authorLineHeight), "by " + mod.Author, Magnetar_Default.AuthorStyle);
                y += authorLineHeight + Config.spacing;
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
                else if (setting is MultiSelectSetting multiSet) MultiSelectWindowDrawer.HandleMultiSelectSetting(multiSet, ref y, width);
                else if (setting is StringSetting strSet) HandleStringSetting(strSet, ref y, width);
                else if (setting is SelectSetting selSet) HandleSelectSetting(selSet, ref y, width);
                else if (setting is ButtonSetting btnSet) HandleButtonSetting(btnSet, ref y, width);
                else if (setting is LabelSetting lblSet) HandleLabelSetting(lblSet, ref y, width);

                y += Config.elementHeight + Config.spacing;
            }

            MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, Color.white, Translate("KeyBind"));

            Event e = Event.current;
            bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

            HandleBindSetting(mod.KeyBind, ref y, width);
            y += Config.elementHeight + Config.spacing;

            // Hold Mode Toggle
            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), Translate("Hold Mode"), Magnetar_Default.SettingDescriptionStyle);
            Rect holdRect = new Rect(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
            GUI.Box(holdRect, mod.HoldMode ? Translate("ON") : Translate("OFF"), mod.HoldMode ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
            if (holdRect.Contains(e.mousePosition) && isLeftClick)
            {
                mod.HoldMode = !mod.HoldMode;
                e.Use();
            }
            y += Config.elementHeight + Config.spacing;

            // Enabled Toggle
            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), Translate("Enabled"), Magnetar_Default.SettingDescriptionStyle);
            Rect enabledRect = new Rect(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
            GUI.Box(enabledRect, mod.Active ? Translate("ON") : Translate("OFF"), mod.Active ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
            if (enabledRect.Contains(e.mousePosition) && isLeftClick)
            {
                if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                e.Use();
            }
            y += Config.elementHeight + Config.spacing / 2;

            return y - startY;
        }

        private static void DrawSettingsScrollbar(float windowWidth, float headerHeight, float viewHeight, float targetContentHeight, float maxViewHeight, float currentScroll, float maxScroll)
        {
            float trackX = windowWidth - Config.S(14f);
            float trackY = headerHeight + Config.S(5f);
            float trackHeight = viewHeight - Config.S(10f);

            float handleHeight = Mathf.Max(Config.S(20f), (maxViewHeight / targetContentHeight) * trackHeight);
            float scrollPct = maxScroll > 0 ? currentScroll / maxScroll : 0f;
            float handleY = trackY + (scrollPct * (trackHeight - handleHeight));

            GUI.Box(new Rect(trackX + Config.S(5f), trackY, Config.S(2f), trackHeight), "", Magnetar_Default.SeparatorStyle);
            GUI.Box(new Rect(trackX, handleY, Config.S(12f), handleHeight), "", Magnetar_Default.ModuleOff);
        }
    }

    // =========================================================================
    // Search Window & Text Field Logic
    // =========================================================================
    internal static class SearchWindowDrawer
    {
        public static string SearchQuery = "";
        public static Rect SearchWindowRect;
        public static bool IsSearchOpen = false;
        public static float SearchAnimProgress = 0f;
        public static bool SearchWasFocused = false;
        public static bool RequestSearchFocus = false;

        private static GUI.WindowFunction _cachedSearchDelegate;
        private static GUI.WindowFunction SearchDelegate => _cachedSearchDelegate ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawSearchWindow);

        public static void Initialize()
        {
#if ANDROID
            IsSearchOpen = true;
            SearchAnimProgress = 1f;
#endif
        }

        public static void Render(Event currentEvent)
        {
#if ANDROID
            IsSearchOpen = true;
            SearchAnimProgress = 1f;
#else
            if (currentEvent.type == EventType.KeyDown && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
            {
                IsSearchOpen = true;
                RequestSearchFocus = true;

                float searchWidth = Config.ModuleWindowWidth;
                float tfY = Config.S(10f);
                Rect anticipatedTfRect = new Rect(Config.indent, tfY, searchWidth - (Config.indent * 2), Config.S(20f));
                activeTextFieldId = anticipatedTfRect.GetHashCode();

                GUI.FocusWindow(999);
                currentEvent.Use();
            }

            if (IsSearchOpen)
            {
                if (activeTextFieldId != -1) SearchWasFocused = true;

                if (SearchWasFocused && activeTextFieldId == -1 && string.IsNullOrEmpty(SearchQuery))
                {
                    IsSearchOpen = false;
                    RequestSearchFocus = true;
                    SearchWasFocused = false;
                }
            }

            if (currentEvent.type == EventType.Repaint)
            {
                float targetProgress = IsSearchOpen ? 1f : 0f;
                SearchAnimProgress = Mathf.Lerp(SearchAnimProgress, targetProgress, Time.unscaledDeltaTime * Config.ModuleManager.SearchAnimationSpeed);
            }
#endif

            if (SearchAnimProgress > 0.01f)
            {
                float searchWidth = Config.ModuleWindowWidth * Config.ModuleManager.SearchWidthMultiplier;
                float searchHeight = Config.S(30f);

                float targetY = Config.WindowHeight - searchHeight - Config.S(20f);
                float hiddenY = Config.WindowHeight + Config.S(10f);
                float currentY = Mathf.Lerp(hiddenY, targetY, SearchAnimProgress);
                float currentX = (Config.WindowWidth / 2f) - (searchWidth / 2f);

                SearchWindowRect = new Rect(currentX, currentY, searchWidth, searchHeight);
                SearchWindowRect = GUI.Window(999, SearchWindowRect, SearchDelegate, "", Magnetar_Default.ModuleWindow);
            }
        }

        private static void DrawSearchWindow(int id)
        {
            Rect tfRect = new Rect(Config.S(5f), Config.S(5f), SearchWindowRect.width - Config.S(10f), Config.S(20f));

            if (RequestSearchFocus)
            {
                activeTextFieldId = tfRect.GetHashCode();
                GUI.FocusWindow(999);

                if (Event.current.type == EventType.Repaint)
                {
                    RequestSearchFocus = false;
                }
            }

            SearchQuery = DrawManualTextField(tfRect, SearchQuery, Translate("Search..."));
        }
    }

    // =========================================================================
    // MultiSelect Modal Window
    // =========================================================================
    internal static class MultiSelectWindowDrawer
    {
        public static MultiSelectSetting ActiveMultiSelect = null;
        public static Rect WindowRect;

        private static GUI.WindowFunction _cachedMultiSelectDelegate;
        private static GUI.WindowFunction MultiSelectDelegate => _cachedMultiSelectDelegate ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawBridge);

        public static void InitializeLayout()
        {
            WindowRect = new Rect(
                Config.WindowWidth / 2f - Config.ModuleManager.MultiSelectWindowWidth / 2f,
                Config.WindowHeight / 2f - Config.ModuleManager.MultiSelectWindowHeight / 2f,
                Config.ModuleManager.MultiSelectWindowWidth,
                Config.ModuleManager.MultiSelectWindowHeight
            );
        }

        public static void Render(Event currentEvent)
        {
            float targetW = Mathf.Min(Config.ModuleManager.MultiSelectWindowWidth, Config.WindowWidth * 0.95f);
            float targetH = Mathf.Min(Config.ModuleManager.MultiSelectWindowHeight, Config.WindowHeight * 0.8f);

            if (ModuleManager.resetWindowPos)
            {
                WindowRect = new Rect(
                    (Config.WindowWidth - targetW) / 2f,
                    (Config.WindowHeight - targetH) / 2f,
                    targetW,
                    targetH
                );
                ModuleManager.resetWindowPos = false;
            }
            else
            {
                Config.RescaleAroundCenter(ref WindowRect, targetW, targetH);
            }

            WindowRect = ScreenBoundaryHelper.Clamp(WindowRect);

            if (ActiveMultiSelect != null)
            {
                WindowRect = GUI.Window(1000, WindowRect, MultiSelectDelegate, "", Magnetar_Default.ModuleWindow);

                if (currentEvent != null && WindowRect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDown)
                {
                    Input.ResetInputAxes();
                }
            }
            else
            {
                ModuleManager.showSelectionGui = false;
            }
        }

        private static void DrawBridge(int id)
        {
            DrawMultiSelectWindow(WindowRect, ActiveMultiSelect, () =>
            {
                ModuleManager.showSelectionGui = false;
                if (ModuleManager.activeSettingsModule != null)
                {
                    ModuleManager.activeSettingsModule.ShowSettings = true;
                    ModuleManager.showSettings = true;
                }
                else
                {
                    ModuleManager.showModules = true;
                }
            });

            float titleHeight = Config.S(34f);
            float closeBtnWidth = Config.S(40f);
            GUI.DragWindow(new Rect(0, 0, WindowRect.width - closeBtnWidth, titleHeight));
        }

        public static void HandleMultiSelectSetting(MultiSelectSetting set, ref float y, float width)
        {
            Event e = Event.current;
            GUI.Label(new Rect(Config.indent, y, width * 0.4f, Config.elementHeight), Translate(set.Name), Magnetar_Default.SettingDescriptionStyle);

            Rect btnRect = new Rect(width - Config.SettingWidth / 2f - Config.selectButtonWidth -
                Magnetar_Default.SettingDescriptionStyle.CalcSize(new GUIContent('(' + Translate($"{set.SelectedValues.Count} selected") + ")")).x / 2, y,
                Config.selectButtonWidth, Config.elementHeight);

            if (btnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            {
                ModuleManager.showModules = false;
                ModuleManager.showSettings = false;
                ModuleManager.showSelectionGui = true;

                ActiveMultiSelect = set;
                multiSelectSearchQuery = "";
                manualScrollY = 0f;
            }

            GUI.Box(btnRect, Translate("Select"), Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            Color originalColor = GUI.contentColor;
            GUI.contentColor = Magnetar_Default.TextDim;
            GUI.Label(new Rect(btnRect.x + Config.selectButtonWidth + Config.S(5f), y, width * 0.4f, Config.elementHeight),
                '(' + Translate($"{set.SelectedValues.Count} selected") + ")", Magnetar_Default.SettingDescriptionStyle);
            GUI.contentColor = originalColor;
        }
    }

    // =========================================================================
    // Mobile Touch & Long-Press Handler
    // =========================================================================
    internal static class MobileInputHandler
    {
        private static Modules.Module _pressedModule = null;
#if ANDROID
        private static float _pressStartTime = 0f;
        private static Vector2 _pressStartScreenPos = Vector2.zero;
        private static bool _hasTriggeredLongPress = false;
        private const float LongPressThreshold = 0.40f; // 400ms hold opens settings
#endif

        public static void Reset()
        {
            _pressedModule = null;
#if ANDROID
            _hasTriggeredLongPress = false;
#endif
        }

#if ANDROID
        public static void OnTouchDown(Modules.Module mod, Rect windowPos, float headerHeight, Vector2 mousePos)
        {
            _pressedModule = mod;
            _pressStartTime = Time.realtimeSinceStartup;
            _pressStartScreenPos = new Vector2(windowPos.x + mousePos.x, windowPos.y + headerHeight + mousePos.y);
            _hasTriggeredLongPress = false;
        }

        public static void OnTouchUp(Modules.Module mod)
        {
            if (_pressedModule == mod && !_hasTriggeredLongPress)
            {
                if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
                Reset();
            }
        }
#endif

        public static void Update(Event currentEvent)
        {
#if ANDROID
            if (_pressedModule != null && !_hasTriggeredLongPress)
            {
                float moveDist = Vector2.Distance(currentEvent.mousePosition, _pressStartScreenPos);
                if (moveDist > Config.S(22f))
                {
                    _pressedModule = null;
                }
                else if (Time.realtimeSinceStartup - _pressStartTime >= LongPressThreshold)
                {
                    _hasTriggeredLongPress = true;
                    ModuleManager.OpenModuleSettings(_pressedModule);
                    _pressedModule = null;
                    if (currentEvent.isMouse) currentEvent.Use();
                }
            }

            if (currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp)
            {
                if (_pressedModule != null)
                {
                    if (!_hasTriggeredLongPress)
                    {
                        float moveDist = Vector2.Distance(currentEvent.mousePosition, _pressStartScreenPos);
                        if (moveDist <= Config.S(22f) && VanillaMode.instance.IsAllowed(_pressedModule))
                        {
                            _pressedModule.Toggle();
                        }
                    }
                    Reset();
                }
            }
#endif
        }
    }

    // =========================================================================
    // Boundary Helpers
    // =========================================================================
    internal static class ScreenBoundaryHelper
    {
        public static Rect Clamp(Rect rect)
        {
            rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, Config.WindowWidth - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, Config.WindowHeight - rect.height));
            return rect;
        }
    }
}