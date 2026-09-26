using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Utils.Translator;
using Magnetar_Client.UI.Setting;

namespace Magnetar_Client.UI.WindowDrawing;

public static class DrawSetting
{
    public static int focusedControlId = -1;
    public static string currentInputBuffer = "";
    public static int activeSliderId = -1;

    public static object activeNumericSetting = null;
    public static int lastFocusedNumericControlId = -1;

    // Dedicated theme helper styles
    private static GUIStyle _placeholderStyle;
    private static GUIStyle PlaceholderStyle
    {
        get
        {
            if (_placeholderStyle == null)
            {
                _placeholderStyle = new GUIStyle
                {
                    wordWrap = false,
                    clipping = TextClipping.Clip,
                    alignment = Magnetar_Default.TextStyle.alignment
                };
            }
            _placeholderStyle.fontSize = Magnetar_Default.TextStyle.fontSize;
            _placeholderStyle.normal.textColor = Magnetar_Default.TextDim;
            return _placeholderStyle;
        }
    }

#if ANDROID
    private static TouchScreenKeyboard _mobileKeyboard = null;
    private static int _activeMobileKeyboardId = -1;
#endif

    public static void HandleStringSetting(StringSetting strSet, ref float y, float width)
    {
        float elemH = Config.elementHeight;
        string translatedName = Translate(strSet.Name);

        // 1. Responsive control width (caps at 55% of window width)
        float controlW = Mathf.Min(Config.SettingWidth * 1.25f, width * 0.55f);
        float gap = Config.S(8f);

        // 2. Safe label width calculation to eliminate text overlapping
        float labelW = Mathf.Max(width * 0.38f, width - (Config.indent * 2f) - controlW - gap);

        Rect labelRect = new(Config.indent, y, labelW, elemH);
        Rect inputRect = new(width - Config.indent - controlW, y, controlW, elemH);

        GUI.Label(labelRect, translatedName, Magnetar_Default.SettingLabelStyle);
        strSet.Value = DrawManualTextField(inputRect, strSet.Value, "", strSet.AutocompleteVars);
    }

    public static void HandleNumericSetting(object setting, ref float y, float width, bool isFloat)
    {
        float val, sliderMin, sliderMax, trueMin, trueMax;
        string name;
        int decPlaces = 0;

        int intTrueMin = 0, intTrueMax = 0;
        int intSliderMin = 0, intSliderMax = 0;

        if (isFloat)
        {
            var s = (FloatSetting)setting;
            val = s.DisplayValue;
            sliderMin = s.Min;
            sliderMax = s.Max;
            trueMin = s.TrueMin;
            trueMax = s.TrueMax;
            name = s.Name;
            decPlaces = s.DecimalPlaces;
        }
        else
        {
            var s = (IntSetting)setting;
            val = (float)s.DisplayValue;
            sliderMin = (float)s.Min;
            sliderMax = (float)s.Max;
            trueMin = (float)s.TrueMin;
            trueMax = (float)s.TrueMax;
            name = s.Name;

            intSliderMin = s.Min;
            intSliderMax = s.Max;
            intTrueMin = s.TrueMin;
            intTrueMax = s.TrueMax;
        }

        string formatString = isFloat ? ("0." + new string('0', decPlaces)) : "0";

        string translatedName = Magnetar_Client.Utils.Translator.Translate(name);
        GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingLabelStyle);

        float LogConvert(float v) => Mathf.Sign(v) * Mathf.Log10(Mathf.Abs(v) + 1.0f);
        float ExpConvert(float l) => Mathf.Sign(l) * (Mathf.Pow(10.0f, Mathf.Abs(l)) - 1.0f);

        float logMin = LogConvert(sliderMin);
        float logMax = LogConvert(sliderMax);
        float visualVal = Mathf.Clamp(val, sliderMin, sliderMax);
        float logVal = LogConvert(visualVal);
        float percentage = Mathf.Clamp01((logVal - logMin) / (logMax - logMin));

        // Input and slider dimensions
        float inputW = Config.SettingsInput.NumericInputWidth;
        float sliderW = Config.SettingWidth - inputW - 10f;

        float trackH = Config.SettingsInput.SliderHeight;
        float thumbSize = Config.S(16f);

        Rect sliderRect = new(width - Config.indent - Config.SettingWidth, y + ((Config.elementHeight - trackH) / 2f), sliderW, trackH);
        Rect sliderHitBox = new(sliderRect.x, y, sliderRect.width, Config.elementHeight);
        Rect inputRect = new(width - Config.indent - inputW, y, inputW, Config.elementHeight);
        float fillWidth = sliderRect.width * percentage;

        // Center circular thumb vertically with the track
        float thumbX = sliderRect.x + fillWidth - (thumbSize / 2f);
        float thumbY = sliderRect.y + (trackH / 2f) - (thumbSize / 2f);
        Rect thumbRect = new(thumbX, thumbY, thumbSize, thumbSize);

        // Draw Track
        GUI.Box(sliderRect, "", Magnetar_Default.SliderTrackOffStyle);
        if (fillWidth > 0f)
        {
            GUI.Box(new Rect(sliderRect.x, sliderRect.y, fillWidth, sliderRect.height), "", Magnetar_Default.SliderTrackOnStyle);
        }

        // Draw Circular Thumb using dedicated circular style (no text)
        GUI.Box(thumbRect, "", Magnetar_Default.SliderThumbStyle);

        Event e = Event.current;

        void CommitSettingValue()
        {
            if (isFloat) ((FloatSetting)setting).Commit();
            else ((IntSetting)setting).Commit();
        }

        void ApplyFromMouseX(float mouseX)
        {
            float mousePct = Mathf.Clamp01((mouseX - sliderRect.x) / sliderRect.width);
            float newLogVal = logMin + (mousePct * (logMax - logMin));
            float newVal = ExpConvert(newLogVal);

            if (isFloat) ((FloatSetting)setting).SetPending((float)System.Math.Round(Mathf.Clamp(newVal, sliderMin, sliderMax), decPlaces));
            else ((IntSetting)setting).SetPending((int)System.Math.Max(intSliderMin, System.Math.Min((long)newVal, intSliderMax)));
        }

        bool inHitbox = sliderHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition);

        int sliderControlId = GUIUtility.GetControlID(name.GetHashCode(), FocusType.Passive);

        if (e.type == EventType.MouseDown && e.button == 0 && inHitbox)
        {
            GUIUtility.hotControl = sliderControlId;
            activeSliderId = sliderControlId;
            activeNumericSetting = setting;
            focusedControlId = -1;
            activeTextFieldId = -1;

            ApplyFromMouseX(e.mousePosition.x);
            e.Use();
        }

        if (GUIUtility.hotControl == sliderControlId)
        {
            if (e.type == EventType.MouseDrag)
            {
                ApplyFromMouseX(e.mousePosition.x);
                e.Use();
            }
            else if (e.type == EventType.MouseUp || (e.type == EventType.Ignore && e.rawType == EventType.MouseUp))
            {
                CommitSettingValue();
                GUIUtility.hotControl = 0;
                activeSliderId = -1;
                activeNumericSetting = null;
                e.Use();
            }
        }

        // Text Input Handling
        int controlId = inputRect.GetHashCode();
        bool isFocused = (activeTextFieldId == controlId);

        if (isFocused && lastFocusedNumericControlId != controlId)
        {
            currentInputBuffer = val.ToString(formatString);
            lastFocusedNumericControlId = controlId;
        }
        else if (!isFocused && lastFocusedNumericControlId == controlId)
        {
            CommitSettingValue();
            lastFocusedNumericControlId = -1;
        }

        string displayValue = isFocused ? currentInputBuffer : val.ToString(formatString);
        string newText = DrawManualTextField(inputRect, displayValue, "0");

        if (isFocused)
        {
            currentInputBuffer = newText;

            if (double.TryParse(currentInputBuffer, out double parsed))
            {
                if (isFloat)
                {
                    ((FloatSetting)setting).SetPending((float)System.Math.Round(Mathf.Clamp((float)parsed, trueMin, trueMax), decPlaces));
                }
                else
                {
                    ((IntSetting)setting).SetPending((int)System.Math.Max(intTrueMin, System.Math.Min((long)parsed, intTrueMax)));
                }
            }
        }
    }

    public static void HandleBindSetting(BindSetting bSet, ref float y, float width)
    {
        Event e = Event.current;
        bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

        string translatedName = Magnetar_Client.Utils.Translator.Translate(bSet.Name);
        float labelWidth = Mathf.Max(width * 0.45f, width - Config.indent * 2 - Config.SettingWidth);

        GUI.Label(new Rect(Config.indent, y, labelWidth, Config.elementHeight),
            translatedName, Magnetar_Default.SettingLabelStyle);

        string bindText = bSet.IsBinding ? "[...]" : bSet.GetBindString();
        Rect bindRect = new(width - Config.indent - Config.SettingWidth, y,
            Config.SettingWidth, Config.elementHeight);
        bool bindHover = bindRect.Contains(e.mousePosition);

        GUI.Box(bindRect, bindText, bSet.IsBinding ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);

        if (bindHover && isLeftClick)
        {
            bSet.IsBinding = !bSet.IsBinding;
            if (bSet.IsBinding)
            {
                bSet.BindKeys.Clear();
                activeTextFieldId = -1;
                focusedControlId = -1;
            }
            e.Use();
        }

        if (bSet.IsBinding && e.isKey)
        {
            KeyCode key = e.keyCode;
            if (key != KeyCode.None)
            {
                if (e.type == EventType.KeyDown)
                {
                    if (key == KeyCode.Escape || key == KeyCode.RightShift)
                    {
                        bSet.BindKeys.Clear();
                        bSet.IsBinding = false;
                    }
                    else if (!bSet.BindKeys.Contains(key))
                    {
                        bSet.BindKeys.Add(key);
                    }
                    e.Use();
                }
                else if (e.type == EventType.KeyUp)
                {
                    if (bSet.BindKeys.Count > 0)
                    {
                        bSet.IsBinding = false;
                    }
                    e.Use();
                }
            }
        }
    }

    public static void HandleBoolSetting(BoolSetting boolSet, ref float y, float width)
    {
        Event e = Event.current;

        string translatedName = Magnetar_Client.Utils.Translator.Translate(boolSet.Name);
        float labelWidth = Mathf.Max(width * 0.45f, width - Config.indent * 2 - Config.SettingWidth);

        GUI.Label(new Rect(Config.indent, y, labelWidth, Config.elementHeight),
            translatedName, Magnetar_Default.SettingLabelStyle);

        Rect btnRect = new(width - Config.indent - Config.SettingWidth, y,
            Config.SettingWidth, Config.elementHeight);

        GUI.Box(btnRect, boolSet.Value ? Translator.Translate("ON") : Translator.Translate("OFF"),
            boolSet.Value ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            boolSet.Value = !boolSet.Value;
            e.Use();
        }
    }

    public static MultiSelectSetting activeMultiSelect = null;
    public static string multiSelectSearchQuery = "";

    // Smooth scroll state tracking
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

        // --- 1. TITLE BANNER WITH WORKING CLOSE BUTTON ---
#if ANDROID
        float titleHeight = Config.S(25f) * 1.30f;
#else
        float titleHeight = Config.S(25f);
#endif
        Rect headerBgRect = new(0, 0, multiSelectWindowRect.width, titleHeight);
        GUI.Box(headerBgRect, Translate("Select ") + Translate(activeMultiSelect.Name), Magnetar_Default.SettingsWndowStyle);

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
                if (onClose != null)
                {
                    onClose.Invoke();
                }
                else
                {
                    Core.ModuleManager.showSelectionGui = false;
                    Core.ModuleManager.showModules = true;
                    Core.GUIManager.isSelectingSubWindow = false;
                    Core.HUDManager.isSelectingElements = false;
                }
                return;
            }

            GUI.Box(closeButtonRect, "✕", Magnetar_Default.CloseButtonStyle);
        }

        // --- 2. HEADER: SEARCH & TOGGLE ALL ---
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

        // --- 3. FILTER & CACHE VISIBLE ITEMS ---
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

        totalContentHeight = filteredItems.Count * rowStep;

        int selectedCount = activeMultiSelect.SelectedValues.Count;
        bool allSelected = filteredItems.Count > 0 && selectedCount >= filteredItems.Count;

        if (activeMultiSelect.MaxSelection >= 0 && selectedCount >= activeMultiSelect.MaxSelection)
        {
            allSelected = true;
        }

        if (GUI.Button(toggleRect,
            allSelected ? Translator.Translate("Deselect All") : Translator.Translate("Select All"),
            !allSelected ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff))
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

        // --- 4. VIEWPORT & SCROLLBAR ---
        float contentStartY = searchY + searchHeight + spacing;
        float viewHeight = multiSelectWindowRect.height - contentStartY - Config.S(8f);
        float maxScrollDist = Mathf.Max(0f, totalContentHeight - viewHeight);

        float scrollbarWidth = Config.S(12f);
        float scrollX = multiSelectWindowRect.width - padX - scrollbarWidth;
        float trackStartY = contentStartY;
        float trackHeight = viewHeight;
        float handleSize = Mathf.Max(Config.S(25f), (viewHeight / Mathf.Max(1f, totalContentHeight)) * trackHeight);
        float usableTrackRange = Mathf.Max(1f, trackHeight - handleSize);

        // Compute current handle rect for drag precision
        float scrollPctCurrent = (maxScrollDist > 0) ? manualScrollY / maxScrollDist : 0f;
        float handleY = trackStartY + (scrollPctCurrent * usableTrackRange);
        Rect handleRect = new(scrollX, handleY, scrollbarWidth, handleSize);
        Rect trackHitbox = new(scrollX - Config.S(4f), trackStartY, scrollbarWidth + Config.S(8f), trackHeight);

        // --- 5. SCROLLBAR DRAG & MOUSE WHEEL ---
        if (activeSliderId == sliderId)
        {
            if (e.type == EventType.MouseDrag)
            {
                float mouseDeltaY = e.mousePosition.y - _scrollbarDragStartMouseY;
                float scrollDelta = (mouseDeltaY / usableTrackRange) * maxScrollDist;
                targetScrollY = Mathf.Clamp(_scrollbarDragStartScrollY + scrollDelta, 0f, maxScrollDist);
                manualScrollY = targetScrollY; // Immediate tracking during physical scrollbar drag
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
                // Drag initiated directly on handle: lock anchor
                _scrollbarDragStartScrollY = targetScrollY;
            }
            else
            {
                // Clicked track background: center handle to cursor smoothly
                float localMouseY = e.mousePosition.y - trackStartY;
                float scrollPct = Mathf.Clamp01((localMouseY - (handleSize / 2f)) / usableTrackRange);
                targetScrollY = scrollPct * maxScrollDist;
                manualScrollY = targetScrollY;
                _scrollbarDragStartScrollY = targetScrollY;
            }

            lastSliderUpdateTime = Time.time;
            e.Use();
        }

        // Smooth ScrollWheel handling
        if (e.type == EventType.ScrollWheel && new Rect(0, 0, multiSelectWindowRect.width, multiSelectWindowRect.height).Contains(e.mousePosition))
        {
            targetScrollY = Mathf.Clamp(targetScrollY + (e.delta.y * Config.S(40f)), 0f, maxScrollDist);
            lastSliderUpdateTime = Time.time;
            e.Use();
        }

        // Framerate-independent interpolation smoothing
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

        // --- 6. THE LIST VIEWPORT ---
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
                GUI.Box(rowRect, item.DisplayName, isSelected ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
            }
        }
        GUI.EndGroup();

        // --- 7. SCROLLBAR VISUALS ---
        GUI.Box(new Rect(scrollX + (scrollbarWidth / 2f) - 1f, trackStartY, 2, trackHeight), "", Magnetar_Default.SeparatorStyle);

        bool shouldHighlight = (activeSliderId == sliderId) || (Time.time - lastSliderUpdateTime < 1.0f);
        GUI.Box(handleRect, "", shouldHighlight ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
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

    public static int activeDropdownId = -1;
    public static float dropdownScrollY = 0f;

    public static void HandleSelectSetting(SelectSetting selSet, ref float y, float width)
    {
        Event e = Event.current;
        int controlId = selSet.GetHashCode();

        string translatedName = Translator.Translate(selSet.Name);
        GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingLabelStyle);

        string currentValName = "Unknown";
        if (selSet.Options.ContainsKey(selSet.Value))
        {
            currentValName = selSet.Options[selSet.Value];
            if (selSet.CustomNames != null && selSet.CustomNames.ContainsKey(selSet.Value))
            {
                currentValName = selSet.CustomNames[selSet.Value];
            }
        }

        Rect btnRect = new(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
        bool isHovered = btnRect.Contains(e.mousePosition);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            if (activeDropdownId == controlId)
            {
                activeDropdownId = -1;
            }
            else
            {
                activeDropdownId = controlId;
                dropdownScrollY = 0f;
                focusedControlId = -1;
            }
            e.Use();
        }

        string arrow = (activeDropdownId == controlId) ? " ▲" : " ▼";
        GUI.Box(btnRect, currentValName + arrow, Magnetar_Default.SettingOff);

        if (activeDropdownId == controlId)
        {
            float rowHeight = Config.SettingsInput.DropdownRowHeight;
            int maxVisibleRows = Config.SettingsInput.DropdownMaxVisibleRows;
            int itemCount = selSet.Options.Count;
            float dropHeight = Mathf.Min(itemCount * rowHeight, maxVisibleRows * rowHeight);

            Rect dropRect = new(btnRect.x, btnRect.y + btnRect.height, btnRect.width, dropHeight);

            if (dropRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                dropdownScrollY = Mathf.Clamp(dropdownScrollY + e.delta.y * Config.SettingsInput.DropdownScrollSensitivity,
                    0, Mathf.Max(0, (itemCount * rowHeight) - dropHeight));
                e.Use();
            }

            if (dropRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                float localY = e.mousePosition.y - dropRect.y + dropdownScrollY;
                int clickedIndex = (int)(localY / rowHeight);

                int i = 0;
                foreach (var kvp in selSet.Options)
                {
                    if (i == clickedIndex)
                    {
                        selSet.Value = kvp.Key;
                        activeDropdownId = -1;
                        e.Use();
                        break;
                    }
                    i++;
                }
            }

            if (e.type == EventType.MouseDown && !btnRect.Contains(e.mousePosition) && !dropRect.Contains(e.mousePosition))
            {
                activeDropdownId = -1;
            }

            float _dropdownScrollY = dropdownScrollY;

            OnPostDraw += () =>
            {
                GUI.Box(dropRect, "", Magnetar_Default.SettingOff);
                GUI.BeginGroup(dropRect);

                int i = 0;
                foreach (var kvp in selSet.Options)
                {
                    float drawY = (i * rowHeight) - _dropdownScrollY;

                    if (drawY + rowHeight > 0 && drawY < dropHeight)
                    {
                        Rect rowRect = new(0, drawY, dropRect.width, rowHeight);
                        string displayName = kvp.Value;

                        if (selSet.CustomNames != null && selSet.CustomNames.ContainsKey(kvp.Key))
                        {
                            displayName = selSet.CustomNames[kvp.Key];
                        }

                        bool isSelected = (selSet.Value == kvp.Key);
                        GUIStyle style = isSelected ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff;

                        GUI.Box(rowRect, displayName, style);
                    }
                    i++;
                }
                GUI.EndGroup();

                if (itemCount * rowHeight > dropHeight)
                {
                    float maxScroll = (itemCount * rowHeight) - dropHeight;
                    float scrollPct = _dropdownScrollY / maxScroll;
                    float handleHeight = Mathf.Max(10f, dropHeight * (dropHeight / (itemCount * rowHeight)));
                    float handleY = dropRect.y + (scrollPct * (dropHeight - handleHeight));

                    GUI.Box(new Rect(dropRect.x + dropRect.width - 4, handleY, 4, handleHeight), "", Magnetar_Default.SettingOn);
                }
            };
        }
    }

    public static int activeTextFieldId = -1;
    private static int cursorIndex = 0;
    private static int selectIndex = 0;
    private static float scrollOffset = 0f;
    public static float autocompleteScrollY = 0f;
    public static int autocompleteSelectedIndex = 0;
    public static System.Action OnPostDraw = null;

    public struct TextState
    {
        public string Text;
        public int Cursor;
        public int Select;
        public TextState(string t, int c, int s) { Text = t; Cursor = c; Select = s; }
    }
    private static int undoStackCount = Config.SettingsInput.TextFieldUndoLimit;
    private static List<TextState> undoStack = new();
    private static List<TextState> redoStack = new();
    private static int lastHistoryFieldId = -1;

    public static string DrawManualTextField(Rect rect, string text, string defaultText = "", List<string> autocompleteVars = null)
    {
        Event e = Event.current;
        int controlId = rect.GetHashCode();

        if (text == null) text = "";

#if ANDROID
        if (activeTextFieldId == controlId)
        {
            if (_activeMobileKeyboardId != controlId || _mobileKeyboard == null)
            {
                _mobileKeyboard = TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.Default);
                _activeMobileKeyboardId = controlId;
            }
            else
            {
                text = _mobileKeyboard.text;
                cursorIndex = text.Length;
                selectIndex = cursorIndex;

                if (_mobileKeyboard.status == TouchScreenKeyboard.Status.Done ||
                    _mobileKeyboard.status == TouchScreenKeyboard.Status.Canceled ||
                    !_mobileKeyboard.active)
                {
                    activeTextFieldId = -1;
                    _activeMobileKeyboardId = -1;
                    _mobileKeyboard = null;
                }
            }
        }
        else if (_activeMobileKeyboardId == controlId && _mobileKeyboard != null)
        {
            _mobileKeyboard.active = false;
            _mobileKeyboard = null;
            _activeMobileKeyboardId = -1;
        }
#endif

        #region Delete old Data
        if (activeTextFieldId == controlId && lastHistoryFieldId != controlId)
        {
            undoStack.Clear();
            redoStack.Clear();
            lastHistoryFieldId = controlId;
        }

        if (activeTextFieldId == controlId)
        {
            cursorIndex = Mathf.Clamp(cursorIndex, 0, text.Length);
            selectIndex = Mathf.Clamp(selectIndex, 0, text.Length);
        }
        #endregion

        int GetIndexFromMouse(float mouseX)
        {
            float localX = mouseX - 5 + scrollOffset;
            if (localX <= 0) return 0;
            for (int i = 1; i <= text.Length; i++)
            {
                float wThis = TextStyle.CalcSize(new GUIContent(text.Substring(0, i))).x;
                float wPrev = TextStyle.CalcSize(new GUIContent(text.Substring(0, i - 1))).x;
                if (localX < (wThis + wPrev) / 2f) return i - 1;
            }
            return text.Length;
        }

        #region AutoComplete
        bool showAutocomplete = false;
        string currentFilter = "";
        int bracketStartIndex = -1;
        List<string> filteredVars = new();

        if (activeTextFieldId == controlId && autocompleteVars != null)
        {
            for (int i = cursorIndex - 1; i >= 0; i--)
            {
                if (text[i] == '}') break;
                if (text[i] == '{')
                {
                    showAutocomplete = true;
                    bracketStartIndex = i;
                    currentFilter = text.Substring(i + 1, cursorIndex - i - 1).ToLower();
                    break;
                }
            }

            if (showAutocomplete)
            {
                foreach (var v in autocompleteVars)
                    if (v.ToLower().Contains(currentFilter)) filteredVars.Add(v);
                if (filteredVars.Count == 0) showAutocomplete = false;
            }
        }
        #endregion

        #region Handle Mouse
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Rect dropRect = new(rect.x, rect.y + rect.height, rect.width, 150f);
            bool clickingDropdown = showAutocomplete && dropRect.Contains(e.mousePosition);

            if (rect.Contains(e.mousePosition))
            {
                if (activeTextFieldId != controlId)
                {
                    activeTextFieldId = controlId;
#if ANDROID
                    _mobileKeyboard = TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.Default);
                    _activeMobileKeyboardId = controlId;
#endif
                }
                cursorIndex = GetIndexFromMouse(e.mousePosition.x - rect.x);
                if (!e.shift) selectIndex = cursorIndex;
                e.Use();
            }
            else if (activeTextFieldId == controlId && !clickingDropdown)
            {
                activeTextFieldId = -1;
#if ANDROID
                if (_mobileKeyboard != null)
                {
                    _mobileKeyboard.active = false;
                    _mobileKeyboard = null;
                    _activeMobileKeyboardId = -1;
                }
#endif
            }
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && activeTextFieldId == controlId)
        {
            cursorIndex = GetIndexFromMouse(e.mousePosition.x - rect.x);
            e.Use();
        }
        #endregion

        #region Keyboard
#if !ANDROID
        if (activeTextFieldId == controlId && e.type == EventType.KeyDown)
        {
            char c = e.character;
            KeyCode k = e.keyCode;
            bool ctrl = e.control || e.command;
            bool shift = e.shift;

            bool hasSelection = cursorIndex != selectIndex;
            int selStart = Mathf.Min(cursorIndex, selectIndex);
            int selEnd = Mathf.Max(cursorIndex, selectIndex);

            void SaveState()
            {
                undoStack.Add(new TextState(text, cursorIndex, selectIndex));
                if (undoStack.Count > undoStackCount) undoStack.RemoveAt(0);
                redoStack.Clear();
            }

            void DeleteSelection()
            {
                text = text.Remove(selStart, selEnd - selStart);
                cursorIndex = selectIndex = selStart;
            }

            int GetWordBoundary(int current, int dir)
            {
                if (dir < 0)
                {
                    if (current <= 0) return 0;
                    int i = current - 1;
                    while (i > 0 && char.IsWhiteSpace(text[i])) i--;
                    while (i > 0 && !char.IsWhiteSpace(text[i - 1])) i--;
                    return i;
                }
                else
                {
                    if (current >= text.Length) return text.Length;
                    int i = current;
                    while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                    while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
                    return i;
                }
            }

            bool interceptedForAutocomplete = false;

            if (showAutocomplete && filteredVars.Count > 0)
            {
                if (k == KeyCode.DownArrow) { autocompleteSelectedIndex = Mathf.Min(autocompleteSelectedIndex + 1, filteredVars.Count - 1); interceptedForAutocomplete = true; e.Use(); }
                else if (k == KeyCode.UpArrow) { autocompleteSelectedIndex = Mathf.Max(autocompleteSelectedIndex - 1, 0); interceptedForAutocomplete = true; e.Use(); }
                else if (k == KeyCode.Return || k == KeyCode.Tab)
                {
                    SaveState();
                    string chosen = filteredVars[autocompleteSelectedIndex];
                    text = text.Remove(bracketStartIndex + 1, cursorIndex - bracketStartIndex - 1);
                    text = text.Insert(bracketStartIndex + 1, chosen + "}");
                    cursorIndex = selectIndex = bracketStartIndex + chosen.Length + 2;
                    interceptedForAutocomplete = true; showAutocomplete = false; e.Use();
                }
            }

            if (!interceptedForAutocomplete)
            {
                if (ctrl && k == KeyCode.Z)
                {
                    if (undoStack.Count > 0)
                    {
                        redoStack.Add(new TextState(text, cursorIndex, selectIndex));
                        var state = undoStack[undoStack.Count - 1];
                        undoStack.RemoveAt(undoStack.Count - 1);
                        text = state.Text; cursorIndex = state.Cursor; selectIndex = state.Select;
                    }
                    e.Use();
                }
                else if (ctrl && k == KeyCode.Y)
                {
                    if (redoStack.Count > 0)
                    {
                        undoStack.Add(new TextState(text, cursorIndex, selectIndex));
                        var state = redoStack[redoStack.Count - 1];
                        redoStack.RemoveAt(redoStack.Count - 1);
                        text = state.Text; cursorIndex = state.Cursor; selectIndex = state.Select;
                    }
                    e.Use();
                }
                else if (ctrl && k == KeyCode.C)
                {
                    if (hasSelection) GUIUtility.systemCopyBuffer = text.Substring(selStart, selEnd - selStart);
                    e.Use();
                }
                else if (ctrl && k == KeyCode.X)
                {
                    if (hasSelection) { GUIUtility.systemCopyBuffer = text.Substring(selStart, selEnd - selStart); SaveState(); DeleteSelection(); }
                    e.Use();
                }
                else if (ctrl && k == KeyCode.V)
                {
                    string paste = GUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(paste))
                    {
                        SaveState();
                        if (hasSelection) DeleteSelection();
                        text = text.Insert(cursorIndex, paste);
                        cursorIndex += paste.Length; selectIndex = cursorIndex;
                    }
                    e.Use();
                }
                else if (ctrl && k == KeyCode.A)
                {
                    selectIndex = 0; cursorIndex = text.Length; e.Use();
                }
                else if (k == KeyCode.Home)
                {
                    cursorIndex = 0; if (!shift) selectIndex = cursorIndex; e.Use();
                }
                else if (k == KeyCode.End)
                {
                    cursorIndex = text.Length; if (!shift) selectIndex = cursorIndex; e.Use();
                }
                else if (k == KeyCode.LeftArrow)
                {
                    if (ctrl) cursorIndex = GetWordBoundary(cursorIndex, -1);
                    else if (cursorIndex > 0) cursorIndex--;
                    if (!shift) selectIndex = cursorIndex;
                    e.Use();
                }
                else if (k == KeyCode.RightArrow)
                {
                    if (ctrl) cursorIndex = GetWordBoundary(cursorIndex, 1);
                    else if (cursorIndex < text.Length) cursorIndex++;
                    if (!shift) selectIndex = cursorIndex;
                    e.Use();
                }
                else if (k == KeyCode.Backspace)
                {
                    if (hasSelection) { SaveState(); DeleteSelection(); }
                    else if (ctrl && cursorIndex > 0)
                    {
                        SaveState();
                        int bound = GetWordBoundary(cursorIndex, -1);
                        text = text.Remove(bound, cursorIndex - bound);
                        cursorIndex = selectIndex = bound;
                    }
                    else if (cursorIndex > 0)
                    {
                        SaveState();
                        text = text.Remove(cursorIndex - 1, 1);
                        cursorIndex--; selectIndex = cursorIndex;
                    }
                    e.Use();
                }
                else if (k == KeyCode.Delete)
                {
                    if (hasSelection) { SaveState(); DeleteSelection(); }
                    else if (ctrl && cursorIndex < text.Length)
                    {
                        SaveState();
                        int bound = GetWordBoundary(cursorIndex, 1);
                        text = text.Remove(cursorIndex, bound - cursorIndex);
                    }
                    else if (cursorIndex < text.Length) { SaveState(); text = text.Remove(cursorIndex, 1); }
                    e.Use();
                }
                else if (k == KeyCode.Return || k == KeyCode.Escape)
                {
                    activeTextFieldId = -1; e.Use();
                }
                else if (c != '\0' && !char.IsControl(c))
                {
                    SaveState();
                    if (hasSelection) DeleteSelection();
                    text = text.Insert(cursorIndex, c.ToString());
                    cursorIndex++; selectIndex = cursorIndex;
                    e.Use();
                }
            }
        }
#endif
        #endregion

        if (activeTextFieldId == controlId)
        {
            float visibleWidth = rect.width - 10;
            float cursorPixelX = TextStyle.CalcSize(new GUIContent(text.Substring(0, cursorIndex))).x;

            if (cursorPixelX - scrollOffset > visibleWidth) scrollOffset = cursorPixelX - visibleWidth;
            else if (cursorPixelX - scrollOffset < 0) scrollOffset = cursorPixelX;

            float totalWidth = TextStyle.CalcSize(new GUIContent(text)).x;
            if (totalWidth - scrollOffset < visibleWidth && scrollOffset > 0)
                scrollOffset = Mathf.Max(0, totalWidth - visibleWidth);
        }
        else scrollOffset = 0f;

        #region Input Text Field
        GUI.Box(rect, "", Magnetar_Default.SettingOff);
        GUI.BeginGroup(rect);

        if (string.IsNullOrEmpty(text) && activeTextFieldId != controlId)
        {
            GUI.Label(new Rect(5, 0, rect.width, rect.height), defaultText, PlaceholderStyle);
        }
        else
        {
            if (activeTextFieldId == controlId && cursorIndex != selectIndex)
            {
                int selStart = Mathf.Min(cursorIndex, selectIndex);
                int selEnd = Mathf.Max(cursorIndex, selectIndex);
                float startX = TextStyle.CalcSize(new GUIContent(text.Substring(0, selStart))).x;
                float endX = TextStyle.CalcSize(new GUIContent(text.Substring(0, selEnd))).x;

                Rect selRect = new(5 + startX - scrollOffset, 2, endX - startX, rect.height - 4);
                GUI.Box(selRect, "", Magnetar_Default.TextHighlightedStyle);
            }

            GUI.Label(new Rect(5 - scrollOffset, 0, 2000, rect.height), text, TextStyle);

            if (activeTextFieldId == controlId && (int)(Time.realtimeSinceStartup * 2) % 2 == 0)
            {
                float cursorPixelX = TextStyle.CalcSize(new GUIContent(text.Substring(0, cursorIndex))).x;
                Rect cursorRect = new(5 + cursorPixelX - scrollOffset, 3, 1, rect.height - 6);
                GUI.Box(cursorRect, "", Magnetar_Default.SettingOn);
            }
        }
        GUI.EndGroup();

        if (showAutocomplete && filteredVars.Count > 0)
        {
            float rowHeight = Config.SettingsInput.AutocompleteRowHeight;
            float maxDropdownHeight = Config.SettingsInput.AutocompleteMaxHeight;
            float totalHeight = filteredVars.Count * rowHeight;
            float dropHeight = Mathf.Min(totalHeight, maxDropdownHeight);

            Rect dropRect = new(rect.x, rect.y + rect.height, rect.width, dropHeight);

#if !ANDROID
            void SaveState()
            {
                undoStack.Add(new TextState(text, cursorIndex, selectIndex));
                if (undoStack.Count > undoStackCount) undoStack.RemoveAt(0);
                redoStack.Clear();
            }
#endif

            if (e.type == EventType.Layout || e.type == EventType.Repaint)
            {
                autocompleteSelectedIndex = Mathf.Clamp(autocompleteSelectedIndex, 0, filteredVars.Count - 1);
                float selectedY = autocompleteSelectedIndex * rowHeight;
                if (selectedY < autocompleteScrollY) autocompleteScrollY = selectedY;
                else if (selectedY + rowHeight > autocompleteScrollY + dropHeight)
                    autocompleteScrollY = selectedY + rowHeight - dropHeight;
            }

            if (dropRect.Contains(e.mousePosition))
            {
                float localY = e.mousePosition.y - dropRect.y + autocompleteScrollY;
                int hoveredIndex = (int)(localY / rowHeight);
                if (hoveredIndex >= 0 && hoveredIndex < filteredVars.Count) autocompleteSelectedIndex = hoveredIndex;

                if (e.type == EventType.ScrollWheel)
                {
                    autocompleteScrollY = Mathf.Clamp(autocompleteScrollY + e.delta.y * Config.SettingsInput.AutocompleteScrollSensitivity,
                        0, Mathf.Max(0, totalHeight - dropHeight));
                    e.Use();
                }
                else if (e.type == EventType.MouseDown && e.button == 0)
                {
#if !ANDROID
                    SaveState();
#endif
                    string chosen = filteredVars[autocompleteSelectedIndex];
                    text = text.Remove(bracketStartIndex + 1, cursorIndex - bracketStartIndex - 1);
                    text = text.Insert(bracketStartIndex + 1, chosen + "}");
                    cursorIndex = selectIndex = bracketStartIndex + chosen.Length + 2;
                    activeTextFieldId = controlId;

#if ANDROID
                    if (_mobileKeyboard != null)
                    {
                        _mobileKeyboard.text = text;
                    }
#endif
                    e.Use();
                }
            }

            float _autocompleteScrollY = autocompleteScrollY;
            int _autocompleteSelectedIndex = autocompleteSelectedIndex;

            OnPostDraw = () =>
            {
                GUI.Box(dropRect, "", Magnetar_Default.SettingOff);
                GUI.BeginGroup(dropRect);
                for (int i = 0; i < filteredVars.Count; i++)
                {
                    float drawY = (i * rowHeight) - _autocompleteScrollY;
                    if (drawY + rowHeight > 0 && drawY < dropHeight)
                    {
                        Rect rowRect = new(0, drawY, dropRect.width, rowHeight);
                        GUIStyle rowStyle = (i == _autocompleteSelectedIndex) ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff;
                        GUI.Box(rowRect, "{" + filteredVars[i] + "}", rowStyle);
                    }
                }
                GUI.EndGroup();
            };
        }
        #endregion

        return text;
    }

    public static void HandleButtonSetting(ButtonSetting btnSet, ref float y, float width)
    {
        Event e = Event.current;
        string translatedName = Translator.Translate(btnSet.Name);
        GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingLabelStyle);

        Rect btnRect = new(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
        bool isHovered = btnRect.Contains(e.mousePosition);

        GUI.Box(btnRect, Translator.Translate(btnSet.ButtonText), Magnetar_Default.SettingOff);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!btnSet.IsDisabled)
            {
                btnSet.OnClick?.Invoke();
            }
            e.Use();
        }
    }

    public static void HandleLabelSetting(LabelSetting lblSet, ref float y, float width)
    {
        if (lblSet == null || string.IsNullOrEmpty(lblSet.Name)) return;

        string displayText = Translator.Translate(lblSet.Name);

        float labelWidth = width - (Config.indent * 2f);
        float calculatedHeight = Magnetar_Default.SettingsDescriptionStyle.CalcHeight(
            new GUIContent(displayText),
            labelWidth
        );

        Rect labelRect = new(Config.indent, y, labelWidth, calculatedHeight);
        GUI.Label(labelRect, displayText, Magnetar_Default.SettingsDescriptionStyle);

        y += calculatedHeight + Config.spacing;
    }
}