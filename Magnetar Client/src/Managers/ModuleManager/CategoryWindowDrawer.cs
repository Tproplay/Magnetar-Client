using Magnetar_Client.Modules;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Core.ModuleManager_;

public static class CategoryWindowDrawer
{
    public static readonly Dictionary<ModuleCategory, Rect> WindowPositions = new();
    public static readonly Dictionary<ModuleCategory, bool> CategoryFolded = new();
    public static readonly Dictionary<ModuleCategory, float> CategoryScrollPositions = new();

    private static ModuleCategory _clickCategory;
    private static Vector2 _clickStartMousePos = Vector2.zero;
    private static Vector2 _clickStartWindowPos = Vector2.zero;

#if ANDROID
    private static readonly Dictionary<ModuleCategory, float> _touchStartY = new();
    private static readonly Dictionary<ModuleCategory, float> _touchStartScroll = new();
    private static readonly Dictionary<ModuleCategory, bool> _isDragging = new();
#endif

    private static GUI.WindowFunction _cachedCategoryDelegate;
    private static GUI.WindowFunction CategoryDelegate => _cachedCategoryDelegate ??=
        Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawCategoryWindow);

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
        float viewAlpha = AnimationHandler.GetViewAlpha(ModuleManager.Group, ModuleManager.WindowType.Modules.ToString());
        float compositeAlpha = AnimationHandler.CurrentEasedAlpha * viewAlpha;
        if (compositeAlpha <= 0.001f) return;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * compositeAlpha);

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

    public static void DrawCategoryWindow(int id)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float viewAlpha = AnimationHandler.GetViewAlpha(ModuleManager.Group, ModuleManager.WindowType.Modules.ToString());
        float currentAlpha = AnimationHandler.CurrentEasedAlpha * viewAlpha;

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
