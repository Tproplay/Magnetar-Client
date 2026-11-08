using Magnetar_Client.Core;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    private static readonly Dictionary<int, GUIStyle> _cachedEntityStyles = new();
    private static EntitySelectSetting _lastEntitySelectSetting = null;

    public static GUIStyle GetCachedEntityStyle(Texture2D texture)
    {
        if (texture == null) return null;
        int key = texture.GetInstanceID();
        if (_cachedEntityStyles.TryGetValue(key, out GUIStyle style) && style != null && style.normal.background == texture)
        {
            return style;
        }

        GUIStyle newStyle = new();
        newStyle.normal.background = texture;
        _cachedEntityStyles[key] = newStyle;
        return newStyle;
    }

    public static GUIStyle GetCachedEntityStyle(int entityType, int id, Texture2D texture) => GetCachedEntityStyle(texture);

    /// <summary>
    /// Renders the entity-select modal window dialog with a NEF-style textured block pattern grid.
    /// </summary>
    public static void DrawEntitySelectWindow(Rect windowRect, EntitySelectSetting entitySelect, Action onClose = null)
    {
        if (entitySelect == null) return;

        if (_lastEntitySelectSetting != entitySelect)
        {
            _lastEntitySelectSetting = entitySelect;
            manualScrollY = 0f;
            targetScrollY = 0f;
            _scrollVelocity = 0f;
            multiSelectSearchQuery = "";
        }

        if (!ResourceManager.EntitiesWarmedUp)
        {
            ResourceManager.WarmUpAllEntityTextures();
        }

        float maxAllowedHeight = ClientConfig.NativeHeight * 0.92f;
        if (windowRect.height > maxAllowedHeight)
        {
            windowRect.height = maxAllowedHeight;
        }

        Event e = Event.current;
        int sliderId = 1003;

#if ANDROID
        float titleHeight = GUIManager.S(25f) * 1.30f;
#else
        float titleHeight = GUIManager.S(25f);
#endif
        Rect headerBgRect = new(0, 0, windowRect.width, titleHeight);
        GUI.Box(headerBgRect, Translate("Select ") + entitySelect.DisplayName, ThemeManager.SettingsWndowStyle);

        if (GUIManager.ShowMobileButtons)
        {
            float closeBtnSize = GUIManager.S(20f);
            float btnX = windowRect.width - GUIManager.S(26f);
            float btnY = (titleHeight - closeBtnSize) / 2f;
            Rect closeButtonRect = new(btnX, btnY, closeBtnSize, closeBtnSize);

            bool isHovered = closeButtonRect.Contains(e.mousePosition);
            if (e.type == EventType.MouseDown && e.button == 0 && isHovered)
            {
                e.Use();
                onClose?.Invoke();
                return;
            }

            GUI.Box(closeButtonRect, "✕", ThemeManager.CloseButtonStyle);
        }

        float spacing = GUIManager.S(6f);
        float searchY = titleHeight + spacing;
        float searchHeight = GUIManager.S(24f);

        float padX = GUIManager.S(10f);
        float availWidth = windowRect.width - (padX * 2f);
        float toggleWidth = Mathf.Min(GUIManager.S(115f), availWidth * 0.32f);
        float searchWidth = availWidth - toggleWidth - spacing;

        Rect searchRect = new(padX, searchY, searchWidth, searchHeight);
        string oldEntityQuery = multiSelectSearchQuery;
        multiSelectSearchQuery = DrawManualTextField(searchRect, multiSelectSearchQuery, Translate("Search..."));
        if (oldEntityQuery != multiSelectSearchQuery)
        {
            manualScrollY = 0f;
            targetScrollY = 0f;
            _scrollVelocity = 0f;
        }

        Rect toggleRect = new(padX + searchWidth + spacing, searchY, toggleWidth, searchHeight);
        bool isAnySelected = entitySelect.SelectedCount > 0;
        string toggleText = isAnySelected ? Translate("Clear All") : Translate("Select All");

        if (toggleRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (isAnySelected) entitySelect.DeselectAll();
            else entitySelect.SelectAll(false);
            e.Use();
        }

        GUI.Box(toggleRect, toggleText, ThemeManager.SettingOff);

        // Filter items
        var filteredItems = new List<(int Key, string DisplayName)>();
        string query = multiSelectSearchQuery?.Trim().ToLower() ?? "";
        foreach (var opt in entitySelect.GetRenderableOptions())
        {
            if (string.IsNullOrEmpty(query) ||
                opt.DisplayName.ToLower().Contains(query) ||
                opt.InternalName.ToLower().Contains(query))
            {
                filteredItems.Add(((int)opt.Key, opt.DisplayName));
            }
        }

        if (entitySelect.DisplayAlphabetically)
        {
            filteredItems.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        }

        float contentStartY = searchY + searchHeight + spacing;
        float viewHeight = windowRect.height - contentStartY - GUIManager.S(8f);
        float scrollbarWidth = GUIManager.S(12f);
        float gridWidth = availWidth - scrollbarWidth - GUIManager.S(6f);

        // Responsive columns: ~155-165px per block (50% bigger)
        int columns = Mathf.Max(2, Mathf.FloorToInt(gridWidth / GUIManager.S(160f)));
        float cellPadding = GUIManager.S(8f);
        float itemSize = (gridWidth - (cellPadding * (columns - 1))) / columns;
        int rowCount = Mathf.CeilToInt((float)filteredItems.Count / columns);
        float totalGridHeight = (rowCount * itemSize) + (Mathf.Max(0, rowCount - 1) * cellPadding);
        float maxScrollDist = Mathf.Max(0f, totalGridHeight - viewHeight);
        targetScrollY = Mathf.Clamp(targetScrollY, 0f, maxScrollDist);
        manualScrollY = Mathf.Clamp(manualScrollY, 0f, maxScrollDist);
        if (maxScrollDist <= 0f)
        {
            _scrollVelocity = 0f;
        }

        // Inertia Momentum Scrolling
        if (!_isListSwiping && ActiveSliderId != sliderId && Mathf.Abs(_scrollVelocity) > 0.5f)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            targetScrollY = Mathf.Clamp(targetScrollY + (_scrollVelocity * dt), 0f, maxScrollDist);
            _scrollVelocity = Mathf.Lerp(_scrollVelocity, 0f, 1f - Mathf.Exp(-FrictionDecay * dt));
            if (targetScrollY <= 0f || targetScrollY >= maxScrollDist || Mathf.Abs(_scrollVelocity) < 1f)
            {
                _scrollVelocity = 0f;
            }
        }

        // Scrollbar
        float scrollX = windowRect.width - padX - scrollbarWidth;
        float trackStartY = contentStartY;
        float trackHeight = viewHeight;
        float handleSize = (totalGridHeight <= viewHeight) ? trackHeight : Mathf.Max(GUIManager.S(24f), (viewHeight / totalGridHeight) * trackHeight);
        float usableTrackRange = trackHeight - handleSize;
        float currentScrollPct = (maxScrollDist > 0f) ? Mathf.Clamp01(manualScrollY / maxScrollDist) : 0f;
        float handleY = trackStartY + (currentScrollPct * usableTrackRange);
        Rect handleRect = new(scrollX, handleY, scrollbarWidth, handleSize);
        Rect trackHitbox = new(scrollX - GUIManager.S(4f), trackStartY, scrollbarWidth + GUIManager.S(8f), trackHeight);

        if (ActiveSliderId == sliderId)
        {
            if (e.type == EventType.MouseDrag)
            {
                float mouseDeltaY = e.mousePosition.y - _scrollbarDragStartMouseY;
                float scrollDelta = (mouseDeltaY / usableTrackRange) * maxScrollDist;
                targetScrollY = Mathf.Clamp(_scrollbarDragStartScrollY + scrollDelta, 0f, maxScrollDist);
                manualScrollY = targetScrollY;
                _scrollVelocity = 0f;
                lastSliderUpdateTime = Time.unscaledTime;
                e.Use();
            }
            else if (e.type == EventType.MouseUp || (e.type == EventType.Ignore && e.rawType == EventType.MouseUp))
            {
                ActiveSliderId = -1;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDown && trackHitbox.Contains(e.mousePosition))
        {
            ActiveSliderId = sliderId;
            FocusedControlId = -1;
            _scrollVelocity = 0f;
            _scrollbarDragStartMouseY = e.mousePosition.y;

            if (handleRect.Contains(e.mousePosition))
            {
                _scrollbarDragStartScrollY = targetScrollY;
            }
            else
            {
                float localMouseY = e.mousePosition.y - trackStartY;
                float scrollPct = Mathf.Clamp01((localMouseY - (handleSize / 2f)) / usableTrackRange);
                targetScrollY = scrollPct * maxScrollDist;
                manualScrollY = targetScrollY;
                _scrollbarDragStartScrollY = targetScrollY;
            }

            lastSliderUpdateTime = Time.unscaledTime;
            e.Use();
        }

        if (e.type == EventType.ScrollWheel && new Rect(0, 0, windowRect.width, windowRect.height).Contains(e.mousePosition))
        {
            _scrollVelocity = 0f;
            targetScrollY = Mathf.Clamp(targetScrollY + (e.delta.y * GUIManager.S(40f)), 0f, maxScrollDist);
            lastSliderUpdateTime = Time.unscaledTime;
            e.Use();
        }

        if (ActiveSliderId != sliderId && !_isListSwiping)
        {
            manualScrollY = Mathf.Lerp(manualScrollY, targetScrollY, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
            if (Mathf.Abs(manualScrollY - targetScrollY) < 0.01f)
            {
                manualScrollY = targetScrollY;
            }
        }

        Rect gridGroupRect = new(padX, contentStartY, gridWidth, viewHeight);
        GUI.BeginGroup(gridGroupRect);
        {
            Vector2 mousePos = e.mousePosition;
            bool isMouseInsideList = mousePos.x >= 0 && mousePos.x <= gridGroupRect.width && mousePos.y >= 0 && mousePos.y <= gridGroupRect.height;
            bool isShiftHeld = e.shift || ((e.modifiers & EventModifiers.Shift) != 0) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            int GetItemIndexAtPos(Vector2 localPos)
            {
                if (localPos.x < 0 || localPos.x > gridWidth || localPos.y < 0 || localPos.y > viewHeight) return -1;
                float absY = localPos.y + manualScrollY;
                int col = Mathf.FloorToInt(localPos.x / (itemSize + cellPadding));
                int row = Mathf.FloorToInt(absY / (itemSize + cellPadding));
                if (col < 0 || col >= columns || row < 0 || row >= rowCount) return -1;

                float cellLocalX = localPos.x - (col * (itemSize + cellPadding));
                float cellLocalY = absY - (row * (itemSize + cellPadding));
                if (cellLocalX > itemSize || cellLocalY > itemSize) return -1;

                int idx = row * columns + col;
                return (idx >= 0 && idx < filteredItems.Count) ? idx : -1;
            }

#if ANDROID
            if (_isMobileHolding && !_mobileShiftDragActive)
            {
                if (Vector2.Distance(mousePos, _mobileHoldStartPos) > GUIManager.S(12f))
                {
                    _isMobileHolding = false;
                }
                else if (Time.realtimeSinceStartup - _mobileHoldStartTime >= MobileShiftHoldThreshold)
                {
                    _mobileShiftDragActive = true;
                    _isMobileHolding = false;
                    _scrollVelocity = 0f;

                    if (_mobileHoldItemIdx >= 0 && _mobileHoldItemIdx < filteredItems.Count)
                    {
                        int itemKey = filteredItems[_mobileHoldItemIdx].Key;
                        bool isCurrentlySelected = entitySelect.IsSelected(itemKey);

                        isShiftDragging = true;
                        dragTargetState = !isCurrentlySelected;
                        draggedItemsSession.Clear();
                        lastHoveredIndex = _mobileHoldItemIdx;

                        if (dragTargetState) ToggleWithLimit(entitySelect, itemKey);
                        else entitySelect.DeselectRaw(itemKey);

                        draggedItemsSession.Add(itemKey);
                        _mobileHoldItemIdx = -1;
                    }
                }
            }

            if (e.type == EventType.MouseDown && e.button == 0 && isMouseInsideList)
            {
                _listTouchStart = mousePos;
                _scrollStartVal = manualScrollY;
                targetScrollY = manualScrollY;
                _isListSwiping = false;
                _scrollVelocity = 0f;

                int clickedIdx = GetItemIndexAtPos(mousePos);
                _mobileHoldItemIdx = clickedIdx;
                _mobileHoldStartTime = Time.realtimeSinceStartup;
                _mobileHoldStartPos = mousePos;
                _isMobileHolding = (clickedIdx >= 0 && clickedIdx < filteredItems.Count);
                _mobileShiftDragActive = false;

                _lastTouchSampleY = mousePos.y;
                _lastTouchSampleTime = Time.realtimeSinceStartup;
            }

            if (_mobileShiftDragActive)
            {
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseMove || e.type == EventType.Repaint)
                {
                    if (isMouseInsideList && filteredItems.Count > 0)
                    {
                        int currentIdx = GetItemIndexAtPos(mousePos);
                        if (currentIdx >= 0 && currentIdx < filteredItems.Count)
                        {
                            int key = filteredItems[currentIdx].Key;
                            if (draggedItemsSession.Add(key))
                            {
                                if (dragTargetState) ToggleWithLimit(entitySelect, key);
                                else entitySelect.DeselectRaw(key);
                            }
                            lastHoveredIndex = currentIdx;
                        }
                    }

                    if (e.type == EventType.MouseDrag || e.type == EventType.MouseMove)
                    {
                        e.Use();
                    }
                }
            }
            else
            {
                if (e.type == EventType.MouseDrag && isMouseInsideList && !_isListSwiping)
                {
                    if (Vector2.Distance(mousePos, _listTouchStart) > GUIManager.S(10f))
                    {
                        _isListSwiping = true;
                        _isMobileHolding = false;
                    }
                }

                if (_isListSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
                {
                    float deltaY = _listTouchStart.y - mousePos.y;
                    targetScrollY = Mathf.Clamp(_scrollStartVal + deltaY, 0f, maxScrollDist);
                    manualScrollY = targetScrollY;

                    float dt = Time.realtimeSinceStartup - _lastTouchSampleTime;
                    if (dt > 0.001f)
                    {
                        float rawVel = (_lastTouchSampleY - mousePos.y) / dt;
                        _scrollVelocity = Mathf.Lerp(_scrollVelocity, rawVel, 0.4f);
                        _lastTouchSampleY = mousePos.y;
                        _lastTouchSampleTime = Time.realtimeSinceStartup;
                    }

                    e.Use();
                }
            }

            if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
            {
                if (_mobileShiftDragActive)
                {
                    _mobileShiftDragActive = false;
                    isShiftDragging = false;
                    draggedItemsSession.Clear();
                    lastHoveredIndex = -1;
                    _scrollVelocity = 0f;
                    e.Use();
                }
                else if (_isMobileHolding && !_isListSwiping)
                {
                    if (_mobileHoldItemIdx >= 0 && _mobileHoldItemIdx < filteredItems.Count)
                    {
                        int itemKey = filteredItems[_mobileHoldItemIdx].Key;

                        if (entitySelect.MaxSelection == 1)
                        {
                            entitySelect.ClearSelections();
                            entitySelect.SelectRaw(itemKey);
                        }
                        else
                        {
                            if (entitySelect.IsSelected(itemKey))
                                entitySelect.DeselectRaw(itemKey);
                            else
                                ToggleWithLimit(entitySelect, itemKey);
                        }

                        e.Use();
                    }
                }

                if (_isListSwiping)
                {
                    float timeSinceSample = Time.realtimeSinceStartup - _lastTouchSampleTime;
                    if (timeSinceSample > 0.08f)
                    {
                        _scrollVelocity = 0f;
                    }
                }

                _isMobileHolding = false;
                _isListSwiping = false;
            }
#else
            if (e.type == EventType.MouseDown && e.button == 0 && isMouseInsideList)
            {
                _listTouchStart = mousePos;
                _scrollStartVal = manualScrollY;
                targetScrollY = manualScrollY;
                _isListSwiping = false;
                _scrollVelocity = 0f;

                _lastTouchSampleY = mousePos.y;
                _lastTouchSampleTime = Time.realtimeSinceStartup;

                _clickedItemIdx = GetItemIndexAtPos(mousePos);

                if (isShiftHeld && entitySelect.MaxSelection != 1 && _clickedItemIdx >= 0 && _clickedItemIdx < filteredItems.Count)
                {
                    int itemKey = filteredItems[_clickedItemIdx].Key;
                    bool isCurrentlySelected = entitySelect.IsSelected(itemKey);

                    isShiftDragging = true;
                    dragTargetState = !isCurrentlySelected;
                    draggedItemsSession.Clear();
                    lastHoveredIndex = _clickedItemIdx;

                    if (dragTargetState) ToggleWithLimit(entitySelect, itemKey);
                    else entitySelect.DeselectRaw(itemKey);

                    draggedItemsSession.Add(itemKey);
                    _clickedItemIdx = -1;
                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag && isMouseInsideList && !_isListSwiping && !isShiftDragging)
            {
                if (Vector2.Distance(mousePos, _listTouchStart) > 10f)
                {
                    _isListSwiping = true;
                    _clickedItemIdx = -1;
                }
            }

            if (_isListSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
            {
                float deltaY = _listTouchStart.y - mousePos.y;
                targetScrollY = Mathf.Clamp(_scrollStartVal + deltaY, 0f, maxScrollDist);
                manualScrollY = targetScrollY;

                float dt = Time.realtimeSinceStartup - _lastTouchSampleTime;
                if (dt > 0.001f)
                {
                    float rawVel = (_lastTouchSampleY - mousePos.y) / dt;
                    _scrollVelocity = Mathf.Lerp(_scrollVelocity, rawVel, 0.4f);
                    _lastTouchSampleY = mousePos.y;
                    _lastTouchSampleTime = Time.realtimeSinceStartup;
                }

                e.Use();
            }

            if (isShiftDragging && isShiftHeld)
            {
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseMove || e.type == EventType.Repaint)
                {
                    if (isMouseInsideList && filteredItems.Count > 0)
                    {
                        int currentIdx = GetItemIndexAtPos(mousePos);
                        if (currentIdx >= 0 && currentIdx < filteredItems.Count)
                        {
                            int key = filteredItems[currentIdx].Key;
                            if (draggedItemsSession.Add(key))
                            {
                                if (dragTargetState) ToggleWithLimit(entitySelect, key);
                                else entitySelect.DeselectRaw(key);
                            }
                            lastHoveredIndex = currentIdx;
                        }
                    }
                }
            }
            else if (isShiftDragging && !isShiftHeld)
            {
                isShiftDragging = false;
                draggedItemsSession.Clear();
                lastHoveredIndex = -1;
            }

            if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
            {
                if (isShiftDragging)
                {
                    isShiftDragging = false;
                    draggedItemsSession.Clear();
                    lastHoveredIndex = -1;
                    _clickedItemIdx = -1;
                    e.Use();
                }
                else if (!_isListSwiping && _clickedItemIdx >= 0 && _clickedItemIdx < filteredItems.Count)
                {
                    int itemKey = filteredItems[_clickedItemIdx].Key;
                    bool isCurrentlySelected = entitySelect.IsSelected(itemKey);

                    if (entitySelect.MaxSelection == 1)
                    {
                        entitySelect.ClearSelections();
                        entitySelect.SelectRaw(itemKey);
                    }
                    else
                    {
                        if (isCurrentlySelected) entitySelect.DeselectRaw(itemKey);
                        else ToggleWithLimit(entitySelect, itemKey);
                    }

                    e.Use();
                }

                if (_isListSwiping)
                {
                    float timeSinceSample = Time.realtimeSinceStartup - _lastTouchSampleTime;
                    if (timeSinceSample > 0.08f)
                    {
                        _scrollVelocity = 0f;
                    }
                }

                _clickedItemIdx = -1;
                _isListSwiping = false;
            }
#endif

            // Visible row range viewport culling
            int firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(manualScrollY / (itemSize + cellPadding)));
            int lastVisibleRow = Mathf.Min(rowCount - 1, Mathf.CeilToInt((manualScrollY + viewHeight) / (itemSize + cellPadding)));

            for (int r = firstVisibleRow; r <= lastVisibleRow; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int idx = (r * columns) + c;
                    if (idx >= filteredItems.Count) break;

                    var item = filteredItems[idx];
                    float btnX = c * (itemSize + cellPadding);
                    float btnY = (r * (itemSize + cellPadding)) - manualScrollY;
                    Rect blockRect = new(btnX, btnY, itemSize, itemSize);

                    bool isSelected = entitySelect.IsSelected(item.Key);
                    float scale = itemSize / GUIManager.S(160f);
                    GUIStyle blockStyle = isSelected ? ThemeManager.EntitySelectOn : ThemeManager.EntitySelectOff;
                    blockStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(GUIManager.S(10.5f) * scale));

                    GUI.Box(blockRect, item.DisplayName, blockStyle);

                    Texture2D tex = entitySelect.GetTexture(item.Key);
                    if (tex != null)
                    {
                        float pad = GUIManager.S(10f) * scale;
                        float bottomTextSpace = GUIManager.S(28f) * scale;

                        float availW = blockRect.width - (pad * 2f);
                        float availH = blockRect.height - pad - bottomTextSpace;

                        float texAspect = (float)tex.width / Mathf.Max(1, tex.height);
                        float availAspect = availW / Mathf.Max(1f, availH);

                        float drawW = availW;
                        float drawH = availH;
                        if (texAspect > availAspect) drawH = availW / texAspect;
                        else drawW = availH * texAspect;

                        float cx = blockRect.x + pad + (availW / 2f);
                        float cy = blockRect.y + pad + (availH / 2f);

                        Rect imgRect = new(cx - (drawW / 2f), cy - (drawH / 2f), drawW, drawH);
                        GUIStyle imgStyle = GetCachedEntityStyle(tex);
                        if (imgStyle != null)
                        {
                            GUI.Box(imgRect, GUIContent.none, imgStyle);
                        }
                    }
                }
            }
        }
        GUI.EndGroup();

        if (e.type == EventType.MouseUp || (e.type == EventType.Ignore && e.rawType == EventType.MouseUp))
        {
            _isListSwiping = false;
            _clickedItemIdx = -1;
            if (isShiftDragging)
            {
                isShiftDragging = false;
                draggedItemsSession.Clear();
                lastHoveredIndex = -1;
            }
#if ANDROID
            _mobileShiftDragActive = false;
            _isMobileHolding = false;
#endif
        }

        GUI.Box(new Rect(scrollX + (scrollbarWidth / 2f) - 1f, trackStartY, 2, trackHeight), "", ThemeManager.SeparatorStyle);
        bool shouldHighlight = (ActiveSliderId == sliderId) || (Time.unscaledTime - lastSliderUpdateTime < 1.0f);
        GUI.Box(handleRect, "", shouldHighlight ? ThemeManager.SettingOn : ThemeManager.SettingOff);
    }
}
