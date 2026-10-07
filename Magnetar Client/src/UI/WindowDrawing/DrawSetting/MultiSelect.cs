using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    public static MultiSelectSetting activeMultiSelect = null;
    public static string multiSelectSearchQuery = "";

    public static float manualScrollY = 0f;
    public static float targetScrollY = 0f;
    private static float _scrollbarDragStartMouseY = 0f;
    private static float _scrollbarDragStartScrollY = 0f;

    public static float totalContentHeight = 0f;
    public static float lastSliderUpdateTime = 0f;

    private static int lastHoveredIndex = -1;
    private static bool isShiftDragging = false;
    private static bool dragTargetState = false;
    private static readonly HashSet<int> draggedItemsSession = new();

    private static Vector2 _listTouchStart = Vector2.zero;
    private static float _scrollStartVal = 0f;
    private static bool _isListSwiping = false;

#if ANDROID
    private static float _mobileHoldStartTime = 0f;
    private static Vector2 _mobileHoldStartPos = Vector2.zero;
    private static int _mobileHoldItemIdx = -1;
    private static bool _isMobileHolding = false;
    private static bool _mobileShiftDragActive = false;
    private const float MobileShiftHoldThreshold = 0.50f;
#endif

    public static void DrawMultiSelectWindow(Rect multiSelectWindowRect, dynamic activeMultiSelect, Action onClose = null)
    {
        if (activeMultiSelect == null) return;

        float maxAllowedHeight = Config.NativeHeight * 0.8f;
        if (multiSelectWindowRect.height > maxAllowedHeight)
        {
            multiSelectWindowRect.height = maxAllowedHeight;
        }

        Event e = Event.current;
        int sliderId = 1002;
        float ROW_HEIGHT = Config.SettingsInput.MultiSelectRowHeight;
        float rowStep = ROW_HEIGHT + Config.S(2f);

        var options = activeMultiSelect.Options;

#if ANDROID
        float titleHeight = Config.S(25f) * 1.30f;
#else
        float titleHeight = Config.S(25f);
#endif
        Rect headerBgRect = new(0, 0, multiSelectWindowRect.width, titleHeight);
        GUI.Box(headerBgRect, Translator.Translate("Select ") + Translator.Translate(activeMultiSelect.Name), ThemeManager.SettingsWndowStyle);

        if (Config.ShowMobileButtons)
        {
            float closeBtnSize = Config.S(20f);
            float btnX = multiSelectWindowRect.width - Config.S(26f);
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

        float spacing = Config.S(6f);
        float searchY = titleHeight + spacing;
        float searchHeight = Config.S(24f);

        float padX = Config.S(10f);
        float availWidth = multiSelectWindowRect.width - (padX * 2f);
        float toggleWidth = Mathf.Min(Config.S(115f), availWidth * 0.32f);
        float searchWidth = availWidth - toggleWidth - spacing;

        Rect searchRect = new(padX, searchY, searchWidth, searchHeight);
        Rect toggleRect = new(padX + searchWidth + spacing, searchY, toggleWidth, searchHeight);

        string oldQuery = multiSelectSearchQuery;
        multiSelectSearchQuery = DrawManualTextField(
            searchRect,
            multiSelectSearchQuery ?? "",
            Translator.Translate("Search...")
        );

        if (oldQuery != multiSelectSearchQuery)
        {
            manualScrollY = 0f;
            targetScrollY = 0f;
        }

        var filteredItems = new List<(int Key, string DisplayName)>();
        string cleanQuery = multiSelectSearchQuery?.Replace(" ", "") ?? "";
        bool hasQuery = !string.IsNullOrEmpty(cleanQuery);

        foreach (var kvp in options)
        {
            int intVal = kvp.Key;
            string internalName = kvp.Value;

            if (activeMultiSelect.Blacklist != null && activeMultiSelect.Blacklist.Contains(intVal)) continue;
            if (activeMultiSelect.NameBlacklist != null && activeMultiSelect.NameBlacklist.Contains(internalName)) continue;
            string displayName = activeMultiSelect.GetDisplayName(intVal, internalName);

            if (hasQuery)
            {
                bool matchesDisplay = displayName.Replace(" ", "").IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesInternal = !string.IsNullOrEmpty(internalName) &&
                                       internalName.Replace(" ", "").IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!matchesDisplay && !matchesInternal)
                    continue;
            }

            filteredItems.Add((intVal, displayName));
        }

        if (activeMultiSelect.DisplayAlphabetically)
        {
            filteredItems.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase));
        }

        totalContentHeight = filteredItems.Count * rowStep;
        int selectedCount = activeMultiSelect.SelectedValues.Count;
        bool allSelected = filteredItems.Count > 0 && selectedCount >= filteredItems.Count;

        if (activeMultiSelect.MaxSelection >= 0 && selectedCount >= activeMultiSelect.MaxSelection)
        {
            allSelected = true;
        }

        string toggleLabel = allSelected
            ? Translator.Translate("Deselect All")
            : Translator.Translate("Select All");

        GUIStyle toggleStyle = !allSelected
            ? ThemeManager.SettingOn
            : ThemeManager.SettingOff;

        GUI.Box(toggleRect, toggleLabel, toggleStyle);

        if (e.type == EventType.MouseDown && e.button == 0 && toggleRect.Contains(e.mousePosition))
        {
            foreach (var item in filteredItems)
            {
                if (allSelected)
                {
                    activeMultiSelect.Deselect(item.Key);
                }
                else
                {
                    if (activeMultiSelect.MaxSelection >= 0 && activeMultiSelect.SelectedValues.Count >= activeMultiSelect.MaxSelection)
                        break;

                    activeMultiSelect.Select(item.Key);
                }
            }
            e.Use();
        }

        float contentStartY = searchY + searchHeight + spacing;
        float viewHeight = multiSelectWindowRect.height - contentStartY - Config.S(8f);
        float maxScrollDist = Mathf.Max(0f, totalContentHeight - viewHeight);

        float scrollbarWidth = Config.S(12f);
        float scrollX = multiSelectWindowRect.width - padX - scrollbarWidth;
        float trackStartY = contentStartY;
        float trackHeight = viewHeight;
        float handleSize = Mathf.Max(Config.S(25f), (viewHeight / Mathf.Max(1f, totalContentHeight)) * trackHeight);
        float usableTrackRange = Mathf.Max(1f, trackHeight - handleSize);

        float scrollPctCurrent = (maxScrollDist > 0) ? manualScrollY / maxScrollDist : 0f;
        float handleY = trackStartY + (scrollPctCurrent * usableTrackRange);
        Rect handleRect = new(scrollX, handleY, scrollbarWidth, handleSize);
        Rect trackHitbox = new(scrollX - Config.S(4f), trackStartY, scrollbarWidth + Config.S(8f), trackHeight);

        if (activeSliderId == sliderId)
        {
            if (e.type == EventType.MouseDrag)
            {
                float mouseDeltaY = e.mousePosition.y - _scrollbarDragStartMouseY;
                float scrollDelta = (mouseDeltaY / usableTrackRange) * maxScrollDist;
                targetScrollY = Mathf.Clamp(_scrollbarDragStartScrollY + scrollDelta, 0f, maxScrollDist);
                manualScrollY = targetScrollY;
                lastSliderUpdateTime = Time.time;
                e.Use();
            }
            else if (e.type == EventType.MouseUp || (e.type == EventType.Ignore && e.rawType == EventType.MouseUp))
            {
                activeSliderId = -1;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDown && trackHitbox.Contains(e.mousePosition))
        {
            activeSliderId = sliderId;
            focusedControlId = -1;
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

            lastSliderUpdateTime = Time.time;
            e.Use();
        }

        if (e.type == EventType.ScrollWheel && new Rect(0, 0, multiSelectWindowRect.width, multiSelectWindowRect.height).Contains(e.mousePosition))
        {
            targetScrollY = Mathf.Clamp(targetScrollY + (e.delta.y * Config.S(40f)), 0f, maxScrollDist);
            lastSliderUpdateTime = Time.time;
            e.Use();
        }

        if (activeSliderId != sliderId && !_isListSwiping)
        {
            manualScrollY = Mathf.Lerp(manualScrollY, targetScrollY, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
            if (Mathf.Abs(manualScrollY - targetScrollY) < 0.01f)
            {
                manualScrollY = targetScrollY;
            }
        }

        if (e.type == EventType.MouseUp || (e.type == EventType.Ignore && e.rawType == EventType.MouseUp))
        {
            _isListSwiping = false;
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

        float listWidth = availWidth - scrollbarWidth - Config.S(6f);
        Rect listGroupRect = new(padX, contentStartY, listWidth, viewHeight);
        GUI.BeginGroup(listGroupRect);
        {
            Vector2 mousePos = e.mousePosition;
            bool isMouseInsideList = mousePos.x >= 0 && mousePos.x <= listGroupRect.width && mousePos.y >= 0 && mousePos.y <= listGroupRect.height;
            bool isShiftHeld = e.shift || ((e.modifiers & EventModifiers.Shift) != 0) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

#if ANDROID
            if (_isMobileHolding && !_mobileShiftDragActive)
            {
                if (Vector2.Distance(mousePos, _mobileHoldStartPos) > Config.S(12f))
                {
                    _isMobileHolding = false;
                }
                else if (Time.realtimeSinceStartup - _mobileHoldStartTime >= MobileShiftHoldThreshold)
                {
                    _mobileShiftDragActive = true;
                    _isMobileHolding = false;

                    if (_mobileHoldItemIdx >= 0 && _mobileHoldItemIdx < filteredItems.Count)
                    {
                        int itemKey = filteredItems[_mobileHoldItemIdx].Key;
                        bool isCurrentlySelected = activeMultiSelect.IsSelected(itemKey);

                        isShiftDragging = true;
                        dragTargetState = !isCurrentlySelected;
                        draggedItemsSession.Clear();
                        lastHoveredIndex = _mobileHoldItemIdx;

                        if (dragTargetState) ToggleWithLimit(activeMultiSelect, itemKey);
                        else activeMultiSelect.Deselect(itemKey);

                        draggedItemsSession.Add(itemKey);
                    }
                }
            }

            if (e.type == EventType.MouseDown && e.button == 0 && isMouseInsideList)
            {
                _listTouchStart = mousePos;
                _scrollStartVal = manualScrollY;
                targetScrollY = manualScrollY;
                _isListSwiping = false;

                float contentY = mousePos.y + manualScrollY;
                int clickedIdx = Mathf.FloorToInt(contentY / rowStep);

                _mobileHoldItemIdx = clickedIdx;
                _mobileHoldStartTime = Time.realtimeSinceStartup;
                _mobileHoldStartPos = mousePos;
                _isMobileHolding = (clickedIdx >= 0 && clickedIdx < filteredItems.Count);
                _mobileShiftDragActive = false;
            }

            if (_mobileShiftDragActive)
            {
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseMove || e.type == EventType.Repaint)
                {
                    if (isMouseInsideList && filteredItems.Count > 0)
                    {
                        float contentY = mousePos.y + manualScrollY;
                        int currentIdx = Mathf.Clamp(Mathf.FloorToInt(contentY / rowStep), 0, filteredItems.Count - 1);

                        if (lastHoveredIndex != -1)
                        {
                            int start = Mathf.Min(lastHoveredIndex, currentIdx);
                            int end = Mathf.Max(lastHoveredIndex, currentIdx);

                            for (int i = start; i <= end; i++)
                            {
                                int key = filteredItems[i].Key;
                                if (draggedItemsSession.Add(key))
                                {
                                    if (dragTargetState) ToggleWithLimit(activeMultiSelect, key);
                                    else activeMultiSelect.Deselect(key);
                                }
                            }
                        }

                        lastHoveredIndex = currentIdx;
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
                    if (Vector2.Distance(mousePos, _listTouchStart) > Config.S(12f))
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
                    e.Use();
                }
                else if (_isMobileHolding && !_isListSwiping)
                {
                    if (_mobileHoldItemIdx >= 0 && _mobileHoldItemIdx < filteredItems.Count)
                    {
                        int itemKey = filteredItems[_mobileHoldItemIdx].Key;

                        if (activeMultiSelect.MaxSelection == 1)
                        {
                            activeMultiSelect.SelectedValues.Clear();
                            activeMultiSelect.Select(itemKey);
                        }
                        else
                        {
                            if (activeMultiSelect.IsSelected(itemKey))
                                activeMultiSelect.Deselect(itemKey);
                            else
                                ToggleWithLimit(activeMultiSelect, itemKey);
                        }

                        e.Use();
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

                float contentY = mousePos.y + manualScrollY;
                int clickedIdx = Mathf.FloorToInt(contentY / rowStep);

                if (clickedIdx >= 0 && clickedIdx < filteredItems.Count)
                {
                    int itemKey = filteredItems[clickedIdx].Key;
                    bool isCurrentlySelected = activeMultiSelect.IsSelected(itemKey);

                    if (isShiftHeld && activeMultiSelect.MaxSelection != 1)
                    {
                        isShiftDragging = true;
                        dragTargetState = !isCurrentlySelected;
                        draggedItemsSession.Clear();
                        lastHoveredIndex = clickedIdx;

                        if (dragTargetState) ToggleWithLimit(activeMultiSelect, itemKey);
                        else activeMultiSelect.Deselect(itemKey);

                        draggedItemsSession.Add(itemKey);
                    }
                    else
                    {
                        isShiftDragging = false;
                        if (activeMultiSelect.MaxSelection == 1)
                        {
                            activeMultiSelect.SelectedValues.Clear();
                            activeMultiSelect.Select(itemKey);
                        }
                        else
                        {
                            if (isCurrentlySelected) activeMultiSelect.Deselect(itemKey);
                            else ToggleWithLimit(activeMultiSelect, itemKey);
                        }
                    }

                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag && isMouseInsideList && !_isListSwiping && !isShiftDragging)
            {
                if (Vector2.Distance(mousePos, _listTouchStart) > 12f)
                {
                    _isListSwiping = true;
                }
            }

            if (_isListSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
            {
                float deltaY = _listTouchStart.y - mousePos.y;
                targetScrollY = Mathf.Clamp(_scrollStartVal + deltaY, 0f, maxScrollDist);
                manualScrollY = targetScrollY;
                e.Use();
            }

            if (isShiftDragging && isShiftHeld)
            {
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseMove || e.type == EventType.Repaint)
                {
                    if (isMouseInsideList && filteredItems.Count > 0)
                    {
                        float contentY = mousePos.y + manualScrollY;
                        int currentIdx = Mathf.Clamp(Mathf.FloorToInt(contentY / rowStep), 0, filteredItems.Count - 1);

                        if (lastHoveredIndex != -1)
                        {
                            int start = Mathf.Min(lastHoveredIndex, currentIdx);
                            int end = Mathf.Max(lastHoveredIndex, currentIdx);

                            for (int i = start; i <= end; i++)
                            {
                                int key = filteredItems[i].Key;
                                if (draggedItemsSession.Add(key))
                                {
                                    if (dragTargetState) ToggleWithLimit(activeMultiSelect, key);
                                    else activeMultiSelect.Deselect(key);
                                }
                            }
                        }

                        lastHoveredIndex = currentIdx;
                    }
                }
            }
            else if (isShiftDragging && !isShiftHeld)
            {
                isShiftDragging = false;
                draggedItemsSession.Clear();
                lastHoveredIndex = -1;
            }
#endif

            int firstVisibleIdx = Mathf.Max(0, Mathf.FloorToInt(manualScrollY / rowStep));
            int lastVisibleIdx = Mathf.Min(filteredItems.Count - 1, Mathf.CeilToInt((manualScrollY + viewHeight) / rowStep));

            for (int i = firstVisibleIdx; i <= lastVisibleIdx; i++)
            {
                var item = filteredItems[i];
                float drawY = (i * rowStep) - manualScrollY;
                Rect rowRect = new(0, drawY, listWidth, ROW_HEIGHT);

                bool isSelected = activeMultiSelect.IsSelected(item.Key);
                GUI.Box(rowRect, item.DisplayName, isSelected ? ThemeManager.SettingOn : ThemeManager.SettingOff);
            }
        }
        GUI.EndGroup();

        GUI.Box(new Rect(scrollX + (scrollbarWidth / 2f) - 1f, trackStartY, 2, trackHeight), "", ThemeManager.SeparatorStyle);
        bool shouldHighlight = (activeSliderId == sliderId) || (Time.time - lastSliderUpdateTime < 1.0f);
        GUI.Box(handleRect, "", shouldHighlight ? ThemeManager.SettingOn : ThemeManager.SettingOff);
    }

    private static void ToggleWithLimit(dynamic activeMultiSelect, int val)
    {
        if (activeMultiSelect.MaxSelection == 1)
        {
            activeMultiSelect.SelectedValues.Clear();
            activeMultiSelect.Select(val);
            return;
        }

        if (activeMultiSelect.MaxSelection == -1 || activeMultiSelect.SelectedValues.Count < activeMultiSelect.MaxSelection)
        {
            activeMultiSelect.Select(val);
        }
    }
}