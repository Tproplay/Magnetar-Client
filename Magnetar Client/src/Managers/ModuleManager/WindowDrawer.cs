using Magnetar_Client.Modules;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.UI.WindowDrawing.DrawSetting;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core;

internal static class CategoryWindowDrawer
{
    public static readonly Dictionary<ModuleCategory, Rect> WindowPositions = new();
    public static readonly Dictionary<ModuleCategory, bool> CategoryFolded = new();
    public static readonly Dictionary<ModuleCategory, float> CategoryScrollPositions = new();

    private static ModuleCategory _clickCategory = null;
    private static Vector2 _clickStartMousePos = Vector2.zero;
    private static Vector2 _clickStartWindowPos = Vector2.zero;

#if ANDROID
    private static readonly Dictionary<ModuleCategory, float> _touchStartY = new();
    private static readonly Dictionary<ModuleCategory, float> _touchStartScroll = new();
    private static readonly Dictionary<ModuleCategory, bool> _isDragging = new();
#endif

    private static GUI.WindowFunction _cachedCategoryDelegate;
    private static GUI.WindowFunction CategoryDelegate => _cachedCategoryDelegate ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((Action<int>)DrawCategoryWindow);

    public static void InitializeLayout()
    {
        // Clear any previous positions before laying out afresh
        WindowPositions.Clear();
        CategoryFolded.Clear();
        CategoryScrollPositions.Clear();

        // 1. Sort all registered categories by DisplayOrder, then by Name
        var sortedCategories = ModuleCategory.AllCategories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToList();

        // 2. Initialize each category at its explicit deterministic column index
        for (int index = 0; index < sortedCategories.Count; index++)
        {
            EnsureCategoryInitialized(sortedCategories[index], index);
        }
    }

    public static void EnsureCategoryInitialized(ModuleCategory cat, int? indexHint = null)
    {
        if (cat == null || WindowPositions.ContainsKey(cat)) return;

        int index;
        if (indexHint.HasValue)
        {
            index = indexHint.Value;
        }
        else
        {
            // Sort against the master list to guarantee distinct horizontal placement
            var sortedCategories = ModuleCategory.AllCategories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();

            index = sortedCategories.IndexOf(cat);
            if (index < 0) index = WindowPositions.Count;
        }

        // Horizontal spacing: start at margin + index * (windowWidth + gap)
        float startX = Config.S(20f);
        float columnStep = Config.ModuleWindowWidth + Config.S(10f);
        float initX = startX + (index * columnStep);

        float initY = Config.S(50f);
        float w = Config.ModuleWindowWidth;
        float h = Config.S(50f);

        WindowPositions[cat] = ScreenBoundaryHelper.Clamp(new Rect(initX, initY, w, h));
        CategoryFolded[cat] = false;
        CategoryScrollPositions[cat] = 0f;
    }

    public static void Render()
    {
        Color prevColor = GUI.color;
        // Apply smooth composite alpha
        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.SubWindowAlpha);

        foreach (var cat in ModuleCategory.AllCategories.ToList())
        {
            int id = cat.Id;
            Rect syncedPos = WindowPositions[cat];
            if (!Mathf.Approximately(syncedPos.width, Config.ModuleWindowWidth))
            {
                syncedPos.width = Config.ModuleWindowWidth;
            }

            WindowPositions[cat] = ScreenBoundaryHelper.Clamp(syncedPos);

            WindowPositions[cat] = GUI.Window(
                id,
                WindowPositions[cat],
                CategoryDelegate,
                "",
                ThemeManager.CategoryWindowStyle
            );

            WindowPositions[cat] = ScreenBoundaryHelper.Clamp(WindowPositions[cat]);

            if (WindowPositions[cat].Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                {
                    Input.ResetInputAxes();
                }
            }
        }

        GUI.color = prevColor;
    }

    private static void DrawCategoryWindow(int id)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.SubWindowAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            ModuleCategory category = ModuleCategory.AllCategories.FirstOrDefault(c => c.Id == id);
            if (category == null) return;

            var categoryModules = FilterModulesByCategory(category);

            float windowWidth = WindowPositions[category].width;
            float headerHeight = Config.S(28f);
            float buttonHeight = Config.S(28f);
            Event e = Event.current;

            if (!CategoryFolded.ContainsKey(category)) CategoryFolded[category] = false;
            bool isFolded = CategoryFolded[category];

            Rect titleBarRect = new(0, 0, windowWidth, headerHeight);

            // 1. Dedicated Header Background Box & Title
            GUI.Box(titleBarRect, Translate(category.Name), ThemeManager.CategoryHeaderStyle);

            // 2. Invisible Background Triangle Fold Indicator
            Rect foldBtnRect = new(windowWidth - Config.S(24f), (headerHeight - Config.S(20f)) / 2f, Config.S(20f), Config.S(20f));
            string foldIndicator = isFolded ? "▶" : "▼";

            GUIStyle arrowStyle = new()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Config.S(11f)),
                normal = { textColor = ThemeManager.TextWhite }
            };
            GUI.Label(foldBtnRect, foldIndicator, arrowStyle);

            // 3. Title Bar: Click to Collapse vs Drag to Move
            if (e.type == EventType.MouseDown && e.button == 0 && titleBarRect.Contains(e.mousePosition))
            {
                _clickCategory = category;
                _clickStartMousePos = e.mousePosition;
                _clickStartWindowPos = WindowPositions[category].position;
            }

            if ((e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && e.button == 0)
            {
                if (_clickCategory == category)
                {
                    float mouseDelta = Vector2.Distance(e.mousePosition, _clickStartMousePos);
                    float windowDelta = Vector2.Distance(WindowPositions[category].position, _clickStartWindowPos);

                    if (mouseDelta < Config.S(6f) && windowDelta < Config.S(4f))
                    {
                        CategoryFolded[category] = !isFolded;
                        isFolded = CategoryFolded[category];
                        e.Use();
                    }
                    _clickCategory = null;
                }
            }
            else if (e.type == EventType.MouseDown && e.button == 1 && titleBarRect.Contains(e.mousePosition))
            {
                CategoryFolded[category] = !isFolded;
                isFolded = CategoryFolded[category];
                e.Use();
            }

            if (isFolded)
            {
                GUI.DragWindow(titleBarRect);
                WindowPositions[category] = ScreenBoundaryHelper.Clamp(new Rect(WindowPositions[category].x, WindowPositions[category].y, windowWidth, headerHeight));
                return;
            }

            // 4. 70% Max Screen Height Calculation
            float totalContentHeight = categoryModules.Count * buttonHeight;
            float maxCategoryHeight = Config.NativeHeight * 0.70f;
            float maxViewHeight = maxCategoryHeight - headerHeight;

            bool needsScroll = (headerHeight + totalContentHeight) > maxCategoryHeight;
            float viewHeight = needsScroll ? maxViewHeight : totalContentHeight;
            float finalWindowHeight = headerHeight + viewHeight;
            float maxScroll = needsScroll ? (totalContentHeight - viewHeight) : 0f;

            if (!CategoryScrollPositions.ContainsKey(category)) CategoryScrollPositions[category] = 0f;
            float currentScroll = needsScroll ? Mathf.Clamp(CategoryScrollPositions[category], 0f, maxScroll) : 0f;
            CategoryScrollPositions[category] = currentScroll;

            Rect viewRect = new(0, headerHeight, windowWidth, viewHeight);

            // 5. Scroll Input Handling
            currentScroll = HandleScrollInput(category, viewRect, currentScroll, maxScroll, needsScroll, e);
            CategoryScrollPositions[category] = currentScroll;

            // 6. Render Scrollable Group
            float contentWidth = needsScroll ? windowWidth - Config.S(8f) : windowWidth;
            GUI.BeginGroup(viewRect);
            DrawCategoryItems(category, categoryModules, buttonHeight, currentScroll, viewHeight, contentWidth, headerHeight, e);
            GUI.EndGroup();

            // 7. Scrollbar
            if (needsScroll)
            {
                DrawScrollbar(windowWidth, headerHeight, viewHeight, totalContentHeight, currentScroll, maxScroll);
            }

            GUI.DragWindow(titleBarRect);
            WindowPositions[category] = ScreenBoundaryHelper.Clamp(new Rect(WindowPositions[category].x, WindowPositions[category].y, windowWidth, finalWindowHeight));
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[DrawCategoryWindow] Error rendering category ID {id}: {ex}");
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static List<Modules.Module> FilterModulesByCategory(ModuleCategory category)
    {
        string cleanSearch = (SearchWindowDrawer.SearchQuery ?? "").Trim().Replace(" ", "").ToLower();

        return ModuleManager.Modules.Where(m =>
        {
            if (m.Category != category) return false;
            if (string.IsNullOrEmpty(cleanSearch)) return true;

            // Search ONLY the search hints
            string cleanHints = (m.SearchHints ?? "").Replace(" ", "").ToLower();
            return cleanHints.Contains(cleanSearch);
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

            if (currentY + thisButtonHeight < 0 || currentY > viewHeight) continue;

            GUIStyle currentStyle = mod.Active ? ThemeManager.CategoryModuleOnStyle : ThemeManager.CategoryModuleOffStyle;
            Rect btnRect = new(0, currentY, contentWidth, thisButtonHeight);

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

        GUI.Box(new Rect(trackX + Config.S(1f), trackY, Config.S(2f), trackHeight), "", ThemeManager.SeparatorStyle);
        GUI.Box(new Rect(trackX, handleY, Config.S(5f), handleHeight), "", ThemeManager.CategoryModuleOffStyle);
    }
}
internal static class SettingsWindowDrawer
{
    private static readonly Dictionary<Modules.Module, Rect> _settingsPositions = new();
    private static readonly Dictionary<Modules.Module, Vector2> _settingsScrollPositions = new();
    private static readonly Dictionary<Modules.Module, float> _moduleContentHeights = new();
    private static readonly Dictionary<Modules.Module, float> _targetContentHeights = new();
    private static readonly Dictionary<Modules.Module, GUI.WindowFunction> _cachedSettingsDelegates = new();

    // Cache the active module while it fades out to prevent instant popping
    private static Modules.Module _lastActiveModule = null;

    private static GUI.WindowFunction GetSettingsDelegate(Modules.Module mod)
    {
        if (!_cachedSettingsDelegates.TryGetValue(mod, out var del))
        {
            del = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>(
                (Action<int>)(id => DrawSettingsWindow(id, mod))
            );
            _cachedSettingsDelegates[mod] = del;
        }
        return del;
    }

    public static void Render(Event currentEvent)
    {
        Modules.Module targetMod = ModuleManager.activeSettingsModule;

        // Keep targetMod alive during fade out
        if (targetMod != null && targetMod.ShowSettings)
        {
            _lastActiveModule = targetMod;
        }
        else if (UIAnimationHelper.SubWindowAlpha > 0.01f && _lastActiveModule != null)
        {
            targetMod = _lastActiveModule;
        }
        else
        {
            _lastActiveModule = null;
            return;
        }

        if (targetMod == null) return;

        // Calculate blended alpha
        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.SubWindowAlpha;
        if (currentAlpha <= 0.001f) return;

        Color prevColor = GUI.color;
        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);

        int settingsId = Mathf.Abs(targetMod.GetHashCode()) + 1000;
        float targetWidth = CalculateTargetWidth(targetMod);

        if (!_settingsPositions.ContainsKey(targetMod) || ModuleManager.resetWindowPos)
        {
            ModuleManager.resetWindowPos = false;
            float popupHeight = Config.S(25f);

            _settingsPositions[targetMod] = new Rect(
                (Config.NativeWidth / 2f) - (targetWidth / 2f),
                (Config.NativeHeight / 2f) - (popupHeight / 2f),
                targetWidth,
                popupHeight
            );

            _moduleContentHeights[targetMod] = 0f;
            _targetContentHeights[targetMod] = 0f;
        }
        else
        {
            Rect currentRect = _settingsPositions[targetMod];
            if (Mathf.Abs(currentRect.width - targetWidth) > 0.5f)
            {
                float newWidth = Mathf.Lerp(currentRect.width, targetWidth, Time.deltaTime * Config.ModuleManager.PopupSpeed);
                float widthDiff = newWidth - currentRect.width;

                currentRect.width = newWidth;
                currentRect.x -= widthDiff / 2f;
                _settingsPositions[targetMod] = currentRect;
            }
        }

        _settingsPositions[targetMod] = GUI.Window(
            settingsId,
            _settingsPositions[targetMod],
            GetSettingsDelegate(targetMod),
            "",
            ThemeManager.SettingsWndowBgStyle
        );

        GUI.color = prevColor;
    }

    private static void DrawSettingsWindow(int id, Modules.Module mod)
    {
        // Re-apply both GUI.color and GUI.contentColor inside the window callback scope
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float currentAlpha = UIAnimationHelper.CurrentEasedAlpha * UIAnimationHelper.SubWindowAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);
        try
        {
            ModuleManager.activeSettingsModule = mod;
            float windowWidth = _settingsPositions[mod].width;

#if ANDROID
            float headerHeight = Config.S(26f) * 1.30f;
#else
            float headerHeight = Config.S(26f);
#endif
            float maxWindowHeight = Config.NativeHeight * Config.ModuleManager.MaxSettingsWindowHeightPct;
            float maxViewHeight = maxWindowHeight - headerHeight;

            if (!_moduleContentHeights.ContainsKey(mod)) _moduleContentHeights[mod] = 0f;
            if (!_targetContentHeights.ContainsKey(mod)) _targetContentHeights[mod] = 0f;
            if (!_settingsScrollPositions.ContainsKey(mod)) _settingsScrollPositions[mod] = Vector2.zero;

            // Header Banner using SettingsWndowStyle
            Rect headerBgRect = new(0, 0, windowWidth, headerHeight);
            GUI.Box(headerBgRect, Translator.Translate(mod.Name), ThemeManager.SettingsWndowStyle);

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
                Rect closeButtonRect = new(windowWidth - Config.S(26f), btnY, btnSize, btnSize);
                GUI.Box(closeButtonRect, "X", ThemeManager.CloseButtonStyle);
                if (e.type == EventType.MouseDown && closeButtonRect.Contains(e.mousePosition))
                {
                    ModuleManager.showSettings = false;
                    ModuleManager.showModules = true;
                    ModuleManager.showSelectionGui = false;
                    UIAnimationHelper.TriggerSubWindowTransition();
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

            Rect outRect = new(0, headerHeight, windowWidth, viewHeight);
            if (outRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                currentScroll = Mathf.Clamp(currentScroll + e.delta.y * Config.ModuleManager.ScrollSensitivity, 0, maxScroll);
                _settingsScrollPositions[mod] = new Vector2(0, currentScroll);
                e.Use();
            }

            GUI.BeginGroup(outRect);
            // DrawSettingsBody draws SettingOn and SettingOff
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
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private static float CalculateTargetWidth(Modules.Module mod)
    {
        float maxNameWidth = 0f;
        if (mod.Settings != null)
        {
            foreach (var setting in mod.Settings)
            {
                if (setting == null || string.IsNullOrEmpty(setting.Name)) continue;
                float w = ThemeManager.SettingLabelStyle.CalcSize(new GUIContent(Translate(setting.Name))).x;
                if (w > maxNameWidth) maxNameWidth = w;
            }
        }

        string[] builtIns = { "Hold Mode", "Enabled", "KeyBind" };
        foreach (var b in builtIns)
        {
            float w = ThemeManager.SettingLabelStyle.CalcSize(new GUIContent(Translate(b))).x;
            if (w > maxNameWidth) maxNameWidth = w;
        }

        float calculatedWidth = Config.indent + maxNameWidth + Config.S(35f) + Config.SettingWidth + Config.indent;
        return Mathf.Max(Config.ModuleManager.SettingsWidth, Mathf.Max(mod.SettingsWidth, calculatedWidth));
    }

    private static float DrawSettingsBody(Modules.Module mod, float y, float width)
    {
        float startY = y;
        y += 3 * Config.spacing;

        float descriptionWidth = width - (Config.indent * 2);
        string translatedDescription = Translate(mod.Description);

        float calculatedHeight = ThemeManager.SettingsDescriptionStyle.CalcHeight(new GUIContent(translatedDescription), descriptionWidth);
        GUI.Label(new Rect(Config.indent, y, descriptionWidth, calculatedHeight), translatedDescription, ThemeManager.SettingsDescriptionStyle);
        y += calculatedHeight + Config.spacing;

        if (!string.IsNullOrEmpty(mod.Author))
        {
            float authorLineHeight = Config.S(18f);
            GUI.Label(new Rect(Config.indent, y, width - (Config.indent * 2), authorLineHeight), "by " + mod.Author, ThemeManager.SettingAuthorStyle);
            y += authorLineHeight + Config.spacing;
        }

        bool skipSettings = false;
        foreach (var setting in mod.Settings)
        {
            if (setting is CategorySetting catSet)
            {
                catSet.IsExpanded = MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, Translate(catSet.Name), true, catSet.IsExpanded);
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

            // Direct polymorphic draw call
            setting.Draw(ref y, width);
        }

        MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, Translate("KeyBind"));

        Event e = Event.current;
        bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

        // 1. Draw Keybind
        mod.KeyBind.Draw(ref y, width);

        float resetBtnW = Config.S(22f);
        float gap = Config.S(6f);
        float elemH = Config.elementHeight;
        float labelWidth = Mathf.Max(width * 0.40f, width - Config.indent * 2 - Config.SettingWidth - resetBtnW - gap);

        // --- 2. Hold Mode Toggle Row ---
        GUI.Label(new Rect(Config.indent, y, labelWidth, elemH), Translate("Hold Mode"), ThemeManager.SettingLabelStyle);

        Rect holdRect = new(width - Config.indent - resetBtnW - gap - Config.SettingWidth, y, Config.SettingWidth, elemH);
        Rect holdResetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, elemH);

        GUI.Box(holdRect, mod.HoldMode ? Translate("ON") : Translate("OFF"), mod.HoldMode ? ThemeManager.SettingOn : ThemeManager.SettingOff);
        if (holdRect.Contains(e.mousePosition) && isLeftClick)
        {
            mod.HoldMode = !mod.HoldMode;
            e.Use();
        }

        if (GUI.Button(holdResetRect, Setting.ResetSymbol, ThemeManager.ResetButtonStyle))
        {
            mod.HoldMode = mod.defaultHoldMode;
            e.Use();
        }

        y += elemH + Config.spacing;

        // --- 3. Enabled Toggle Row ---
        GUI.Label(new Rect(Config.indent, y, labelWidth, elemH), Translate("Enabled"), ThemeManager.SettingLabelStyle);

        Rect enabledRect = new(width - Config.indent - resetBtnW - gap - Config.SettingWidth, y, Config.SettingWidth, elemH);
        Rect enabledResetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, elemH);

        GUI.Box(enabledRect, mod.Active ? Translate("ON") : Translate("OFF"), mod.Active ? ThemeManager.SettingOn : ThemeManager.SettingOff);
        if (enabledRect.Contains(e.mousePosition) && isLeftClick)
        {
            if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
            e.Use();
        }

        if (GUI.Button(enabledResetRect, Setting.ResetSymbol, ThemeManager.ResetButtonStyle))
        {
            if (mod.Active != mod.defaultActive)
            {
                if (VanillaMode.instance.IsAllowed(mod)) mod.Toggle();
            }
            e.Use();
        }

        y += elemH + Config.spacing;

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

        GUI.Box(new Rect(trackX + Config.S(5f), trackY, Config.S(2f), trackHeight), "", ThemeManager.SeparatorStyle);
        GUI.Box(new Rect(trackX, handleY, Config.S(12f), handleHeight), "", ThemeManager.CategoryModuleOffStyle);
    }
}

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
            Rect anticipatedTfRect = new(Config.indent, tfY, searchWidth - (Config.indent * 2), Config.S(20f));
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

            float targetY = Config.NativeHeight - searchHeight - Config.S(20f);
            float hiddenY = Config.NativeHeight + Config.S(10f);
            float currentY = Mathf.Lerp(hiddenY, targetY, SearchAnimProgress);
            float currentX = (Config.NativeWidth / 2f) - (searchWidth / 2f);

            SearchWindowRect = new Rect(currentX, currentY, searchWidth, searchHeight);
            SearchWindowRect = GUI.Window(999, SearchWindowRect, SearchDelegate, "", ThemeManager.CategoryWindowStyle);
        }
    }

    private static void DrawSearchWindow(int id)
    {
        Rect tfRect = new(Config.S(5f), Config.S(5f), SearchWindowRect.width - Config.S(10f), Config.S(20f));

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
            Config.NativeWidth / 2f - Config.ModuleManager.MultiSelectWindowWidth / 2f,
            Config.NativeHeight / 2f - Config.ModuleManager.MultiSelectWindowHeight / 2f,
            Config.ModuleManager.MultiSelectWindowWidth,
            Config.ModuleManager.MultiSelectWindowHeight
        );
    }

    public static void Render(Event currentEvent)
    {
        float targetW = Mathf.Min(Config.ModuleManager.MultiSelectWindowWidth, Config.NativeWidth * 0.95f);
        float targetH = Mathf.Min(Config.ModuleManager.MultiSelectWindowHeight, Config.NativeHeight * 0.8f);

        if (ModuleManager.resetWindowPos)
        {
            WindowRect = new Rect(
                (Config.NativeWidth - targetW) / 2f,
                (Config.NativeHeight - targetH) / 2f,
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
            WindowRect = GUI.Window(1000, WindowRect, MultiSelectDelegate, "", ThemeManager.CategoryWindowStyle);

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
        GUI.Label(new Rect(Config.indent, y, width * 0.4f, Config.elementHeight), Translate(set.Name), ThemeManager.SettingLabelStyle);

        Rect btnRect = new(width - Config.SettingWidth / 2f - Config.selectButtonWidth -
            ThemeManager.SettingLabelStyle.CalcSize(new GUIContent('(' + Translate($"{set.SelectedValues.Count} selected") + ")")).x / 2, y,
            Config.selectButtonWidth, Config.elementHeight);

        if (btnRect.Contains(e.mousePosition))
            GUI.backgroundColor = ThemeManager.AccentColor;

        if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
        {
            ModuleManager.showModules = false;
            ModuleManager.showSettings = false;
            ModuleManager.showSelectionGui = true;

            ActiveMultiSelect = set;
            multiSelectSearchQuery = "";
            manualScrollY = 0f;
        }

        GUI.Box(btnRect, Translate("Select"), ThemeManager.SettingOff);
        GUI.backgroundColor = Color.white;

        Color originalColor = GUI.contentColor;
        GUI.contentColor = ThemeManager.TextDim;
        GUI.Label(new Rect(btnRect.x + Config.selectButtonWidth + Config.S(5f), y, width * 0.4f, Config.elementHeight),
            '(' + Translate($"{set.SelectedValues.Count} selected") + ")", ThemeManager.SettingLabelStyle);
        GUI.contentColor = originalColor;
    }
}