using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.UI.Themes.Magnetar_Default;

namespace Magnetar_Client.UI.WindowDrawing
{
    public static class DrawSetting
    {
        public static int focusedControlId = -1;
        public static string currentInputBuffer = "";
        public static int activeSliderId = -1;

        public static void HandleStringSetting(Magnetar_Client.Modules.StringSetting strSet, ref float y, float width)
        {
            string translatedName = Magnetar_Client.Utils.Translator.Translate(strSet.Name);
            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, 
                Config.elementHeight), translatedName, Magnetar_Default.SettingDescriptionStyle);

            Rect inputRect = new Rect(width - Config.indent - Config.SettingWidth*1.3f, y, Config.SettingWidth*1.3f, Config.elementHeight);

            strSet.Value = DrawManualTextField(inputRect, strSet.Value, "", strSet.AutocompleteVars);
        }

        public static void HandleNumericSetting(object setting, ref float y, float width, bool isFloat)
        {
            float val, min, max;
            string name;
            int myId = setting.GetHashCode();
            int decPlaces = 0;

            int intMin = 0, intMax = 0;

            if (isFloat)
            {
                var s = (FloatSetting)setting;
                val = s.Value;
                min = s.Min;
                max = s.Max;
                name = s.Name;
                decPlaces = s.DecimalPlaces;
            }
            else
            {
                var s = (IntSetting)setting;
                val = (float)s.Value;
                min = (float)s.Min;
                max = (float)s.Max;
                name = s.Name;

                intMin = s.Min;
                intMax = s.Max;
            }

            string formatString = isFloat ? ("0." + new string('0', decPlaces)) : "0";

            string translatedName = Magnetar_Client.Utils.Translator.Translate(name);
            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingDescriptionStyle);

            float LogConvert(float v) => Mathf.Sign(v) * Mathf.Log10(Mathf.Abs(v) + 1.0f);
            float ExpConvert(float l) => Mathf.Sign(l) * (Mathf.Pow(10.0f, Mathf.Abs(l)) - 1.0f);

            float logMin = LogConvert(min);
            float logMax = LogConvert(max);
            float logVal = LogConvert(val);

            float percentage = Mathf.Clamp01((logVal - logMin) / (logMax - logMin));

            // --- UI RENDERING (SLIDER CONSTANT BOUNDS) ---
            float inputW = 45f;
            float sliderW = Config.SettingWidth - inputW - 10f; // 85px for slider track

            Rect sliderRect = new Rect(width - Config.indent - Config.SettingWidth, y + 10, sliderW, 8);
            Rect sliderHitBox = new Rect(sliderRect.x, y, sliderRect.width, 22);
            Rect inputRect = new Rect(width - Config.indent - inputW, y, inputW, 22);
            float fillWidth = sliderRect.width * percentage;
            Rect thumbRect = new Rect(sliderRect.x + fillWidth - 10, y + 1, 20, 20);

            GUI.Box(sliderRect, "", Magnetar_Default.SettingOff);
            if (fillWidth > 0) GUI.Box(new Rect(sliderRect.x, sliderRect.y, fillWidth, sliderRect.height), "", Magnetar_Default.SettingOn);

            GUIStyle thumbStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 40 };
            thumbStyle.normal.textColor = Magnetar_Default.AccentColor;

            GUI.Label(thumbRect, "●", thumbStyle);

            Event e = Event.current;

            if (e.type == EventType.ScrollWheel && (sliderHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition)))
            {
                float scrollDirection = Mathf.Sign(e.delta.y);
                float scrollStep = 0.04f;
                float newPercentage = Mathf.Clamp01(percentage + (scrollDirection * scrollStep));

                float newLogVal = logMin + (newPercentage * (logMax - logMin));
                float newVal = ExpConvert(newLogVal);

                if (isFloat)
                {
                    float currentVal = ((FloatSetting)setting).Value;
                    float finalVal = (float)System.Math.Round(Mathf.Clamp(newVal, min, max), decPlaces);

                    if (finalVal == currentVal && scrollDirection != 0)
                    {
                        float minStep = Mathf.Pow(10, -decPlaces);
                        finalVal = (float)System.Math.Round(Mathf.Clamp(currentVal + (scrollDirection * minStep), min, max), decPlaces);
                    }
                    ((FloatSetting)setting).Value = finalVal;
                }
                else
                {
                    int currentVal = ((IntSetting)setting).Value;
                    int finalVal = (int)System.Math.Max(intMin, System.Math.Min((long)newVal, intMax));

                    if (finalVal == currentVal && scrollDirection != 0)
                    {
                        finalVal = (int)System.Math.Max(intMin, System.Math.Min((long)currentVal + (long)scrollDirection, intMax));
                    }
                    ((IntSetting)setting).Value = finalVal;
                }
                e.Use();
            }

            if (e.type == EventType.MouseDown && (sliderHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition)))
            {
                activeSliderId = myId;
                focusedControlId = -1;
                e.Use();
            }

            if (activeSliderId == myId)
            {
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
                {
                    float mousePct = Mathf.Clamp01((e.mousePosition.x - sliderRect.x) / sliderRect.width);
                    float newLogVal = logMin + (mousePct * (logMax - logMin));
                    float newVal = ExpConvert(newLogVal);

                    if (isFloat) ((FloatSetting)setting).Value = (float)System.Math.Round(Mathf.Clamp(newVal, min, max), decPlaces);
                    else ((IntSetting)setting).Value = (int)System.Math.Max(intMin, System.Math.Min((long)newVal, intMax));

                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    activeSliderId = -1;
                    e.Use();
                }
            }

            int controlId = inputRect.GetHashCode();
            bool isFocused = (activeTextFieldId == controlId);

            string displayValue = isFocused ? currentInputBuffer : val.ToString(formatString);
            string newText = DrawManualTextField(inputRect, displayValue, "0");

            if (activeTextFieldId == controlId)
            {
                currentInputBuffer = newText;

                if (double.TryParse(currentInputBuffer, out double parsed))
                {
                    if (isFloat)
                    {
                        ((FloatSetting)setting).Value = (float)System.Math.Round(Mathf.Clamp((float)parsed, min, max), decPlaces);
                    }
                    else
                    {
                        ((IntSetting)setting).Value = (int)System.Math.Max(intMin, System.Math.Min((long)parsed, intMax));
                    }
                }
            }
        }

        public static void HandleBindSetting(BindSetting bSet, ref float y, float width)
        {
            Event e = Event.current;
            bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

            string translatedName = Magnetar_Client.Utils.Translator.Translate(bSet.Name);
            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingDescriptionStyle);

            string bindText = bSet.IsBinding ? "[...]" : bSet.GetBindString();
            Rect bindRect = new Rect(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
            bool bindHover = bindRect.Contains(e.mousePosition);

            if (bindHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(bindRect, bindText, bSet.IsBinding ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

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
            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingDescriptionStyle);

            Rect btnRect = new Rect(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);

            GUI.Box(btnRect, boolSet.Value ? Translator.Translate("ON") : Translator.Translate("OFF")
                , boolSet.Value ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);

            if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                boolSet.Value = !boolSet.Value;
                e.Use();
            }
        }

        public static MultiSelectSetting activeMultiSelect = null;
        public static string multiSelectSearchQuery = "";
        public static float manualScrollY = 0f;
        public static float totalContentHeight = 0f;
        public static float lastSliderUpdateTime = 0f;

        private static int lastHoveredIndex = -1;
        private static bool isShiftDragging = false;
        private static bool dragTargetState = false;
        private static HashSet<int> draggedItemsSession = new HashSet<int>();
             
        public static void DrawMultiSelectWindow(Rect multiSelectWindowRect, dynamic activeMultiSelect)
        {
            if (activeMultiSelect == null) return;

            Event e = Event.current;
            int sliderId = 1002;
            float ROW_HEIGHT = 22f;

            var options = activeMultiSelect.Options;

            float calcTotalHeight = 0;
            int visibleCount = 0;

            // --- HEIGHT PRE-CALCULATION & FILTERING ---
            foreach (var kvp in options)
            {
                int intVal = kvp.Key;
                string internalName = kvp.Value;

                string displayName = internalName;
                if (activeMultiSelect.CustomNames != null && activeMultiSelect.CustomNames.ContainsKey(intVal))
                {
                    displayName = activeMultiSelect.CustomNames[intVal];
                }

                if (activeMultiSelect.Blacklist != null && activeMultiSelect.Blacklist.Contains(intVal)) continue;
                if (activeMultiSelect.NameBlacklist != null && activeMultiSelect.NameBlacklist.Contains(internalName)) continue;

                if (!string.IsNullOrEmpty(multiSelectSearchQuery) && displayName.IndexOf(multiSelectSearchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                visibleCount++;
                calcTotalHeight += ROW_HEIGHT + 1;
            }
            totalContentHeight = calcTotalHeight;

            float headerHeight = 65f;
            float viewHeight = multiSelectWindowRect.height - headerHeight - 10f;
            float maxScrollDist = Mathf.Max(0, totalContentHeight - viewHeight);

            float scrollX = multiSelectWindowRect.width - 18;
            float trackStartY = headerHeight;
            float trackHeight = multiSelectWindowRect.height - trackStartY - 15f;
            float handleSize = Mathf.Max(30f, (viewHeight / Mathf.Max(1, totalContentHeight)) * trackHeight);

            Rect trackHitbox = new Rect(scrollX - 5, trackStartY, 20, trackHeight);

            // --- THE SLIDER ---
            if (activeSliderId == sliderId)
            {
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
                {
                    float localMouseY = e.mousePosition.y - trackStartY;
                    float scrollPct = Mathf.Clamp01((localMouseY - (handleSize / 2f)) / (trackHeight - handleSize));
                    manualScrollY = scrollPct * maxScrollDist;

                    lastSliderUpdateTime = Time.time;
                    e.Use();
                }
                if (e.type == EventType.MouseUp)
                {
                    activeSliderId = -1;
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDown && trackHitbox.Contains(e.mousePosition))
            {
                activeSliderId = sliderId;
                focusedControlId = -1;

                float localMouseY = e.mousePosition.y - trackStartY;
                float scrollPct = Mathf.Clamp01((localMouseY - (handleSize / 2f)) / (trackHeight - handleSize));
                manualScrollY = scrollPct * maxScrollDist;

                lastSliderUpdateTime = Time.time;
                e.Use();
            }

            // --- HEADER: SEARCH & TOGGLE ---
            float searchWidth = (multiSelectWindowRect.width - 30) * 0.75f;
            float toggleWidth = (multiSelectWindowRect.width - 30) * 0.25f;
            Rect searchRect = new Rect(10, 30, searchWidth, 22);
            Rect toggleRect = new Rect(10 + searchWidth, 30, toggleWidth, 22);

            // 1. Draw Advanced Search Input
            string oldQuery = multiSelectSearchQuery;

            multiSelectSearchQuery = DrawManualTextField(
                searchRect,
                multiSelectSearchQuery ?? "",
                Translator.Translate("Search...")
            );

            if (oldQuery != multiSelectSearchQuery)
            {
                manualScrollY = 0f;
            }

            // 2. Toggle Button
            int selectedCount = activeMultiSelect.SelectedValues.Count;
            bool allSelected = visibleCount > 0 && selectedCount >= visibleCount;

            if (activeMultiSelect.MaxSelection >= 0 && selectedCount >= activeMultiSelect.MaxSelection)
            {
                allSelected = true;
            }

            if (GUI.Button(toggleRect,
                allSelected ? Magnetar_Client.Utils.Translator.Translate("Deselect All") : Magnetar_Client.Utils.Translator.Translate("Select All"),
                !allSelected ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff))
            {
                foreach (var kvp in options)
                {
                    int intVal = kvp.Key;
                    string internalName = kvp.Value;

                    string displayName = internalName;
                    if (activeMultiSelect.CustomNames != null && activeMultiSelect.CustomNames.ContainsKey(intVal))
                    {
                        displayName = activeMultiSelect.CustomNames[intVal];
                    }

                    if (activeMultiSelect.Blacklist != null && activeMultiSelect.Blacklist.Contains(intVal)) continue;
                    if (activeMultiSelect.NameBlacklist != null && activeMultiSelect.NameBlacklist.Contains(internalName)) continue;
                    if (!string.IsNullOrEmpty(multiSelectSearchQuery) && displayName.IndexOf(multiSelectSearchQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    if (allSelected)
                    {
                        activeMultiSelect.Deselect(intVal);
                    }
                    else
                    {
                        if (activeMultiSelect.MaxSelection >= 0 && activeMultiSelect.SelectedValues.Count >= activeMultiSelect.MaxSelection)
                        {
                            break;
                        }
                        activeMultiSelect.Select(intVal);
                    }
                }
                e.Use();
            }

            // --- SCROLL WHEEL ---
            if (e.type == EventType.ScrollWheel && new Rect(0, 0, multiSelectWindowRect.width, multiSelectWindowRect.height).Contains(e.mousePosition))
            {
                manualScrollY = Mathf.Clamp(manualScrollY + (e.delta.y * 25f), 0, maxScrollDist);
                lastSliderUpdateTime = Time.time;
                e.Use();
            }

            // --- DRAG DETECTION ---
            if (e.rawType == EventType.MouseUp && e.button == 0)
            {
                isShiftDragging = false;
                draggedItemsSession.Clear();
                lastHoveredIndex = -1;
            }

            if (isShiftDragging && !Input.GetMouseButton(0))
            {
                isShiftDragging = false;
                draggedItemsSession.Clear();
                lastHoveredIndex = -1;
#if MELONLOADER || BEPINEX
                Magnetar_Client.Utils.Magnetar_Logger.DebugLogger.Msg("Drag Complete");
#endif
            }

            // --- THE LIST (CLIPPED) ---
            GUI.BeginGroup(new Rect(10, headerHeight, multiSelectWindowRect.width - 25, viewHeight));
            {
                float currentY = 0;
                string cleanQuery = multiSelectSearchQuery?.Replace(" ", "") ?? "";

                int hoveredIndexThisFrame = -1;
                List<int> visibleItemsThisFrame = new List<int>();

                foreach (var kvp in options)
                {
                    int intVal = kvp.Key;
                    string internalName = kvp.Value;

                    string displayName = internalName;
                    if (activeMultiSelect.CustomNames != null && activeMultiSelect.CustomNames.ContainsKey(intVal))
                    {
                        displayName = activeMultiSelect.CustomNames[intVal];
                    }

                    // --- FILTERING ---
                    if (activeMultiSelect.Blacklist != null && activeMultiSelect.Blacklist.Contains(intVal)) continue;
                    if (activeMultiSelect.NameBlacklist != null && activeMultiSelect.NameBlacklist.Contains(internalName)) continue;

                    if (!string.IsNullOrEmpty(cleanQuery))
                    {
                        if (displayName.Replace(" ", "").IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    }

                    visibleItemsThisFrame.Add(intVal);
                    int currentIndex = visibleItemsThisFrame.Count - 1;

                    // --- RENDERING & LOGIC ---
                    float drawY = currentY - manualScrollY;
                    Rect rowRect = new Rect(0, drawY, multiSelectWindowRect.width - 30, ROW_HEIGHT);

                    bool isHovering = rowRect.Contains(e.mousePosition);
                    bool isCurrentlySelected = activeMultiSelect.IsSelected(intVal);

                    if (isHovering) hoveredIndexThisFrame = currentIndex;

                    if (drawY + ROW_HEIGHT > 0 && drawY < viewHeight)
                    {
                        if (e.type == EventType.MouseDown && e.button == 0 && isHovering)
                        {
                            if (e.shift || Input.GetKey(KeyCode.LeftShift))
                            {
                                isShiftDragging = true;
                                draggedItemsSession.Clear();

                                dragTargetState = !isCurrentlySelected;
                                lastHoveredIndex = currentIndex;

                                if (dragTargetState) ToggleWithLimit(activeMultiSelect, intVal);
                                else activeMultiSelect.Deselect(intVal);

                                draggedItemsSession.Add(intVal);
                            }
                            else
                            {
                                if (isCurrentlySelected) activeMultiSelect.Deselect(intVal);
                                else ToggleWithLimit(activeMultiSelect, intVal);
                            }
                            e.Use();
                        }

                        GUI.Box(rowRect, displayName, activeMultiSelect.IsSelected(intVal) ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
                    }

                    currentY += ROW_HEIGHT + 1;
                }

                // B. CONTINUE DRAG (PAINT SELECT - FRAME SKIP FIX)
                if (isShiftDragging && hoveredIndexThisFrame != -1 && Input.GetMouseButton(0))
                {
                    if (lastHoveredIndex != -1)
                    {
                        int start = Mathf.Min(lastHoveredIndex, hoveredIndexThisFrame);
                        int end = Mathf.Max(lastHoveredIndex, hoveredIndexThisFrame);

                        for (int i = start; i <= end; i++)
                        {
                            int val = visibleItemsThisFrame[i];
                            if (!draggedItemsSession.Contains(val))
                            {
                                if (dragTargetState) ToggleWithLimit(activeMultiSelect, val);
                                else activeMultiSelect.Deselect(val);

                                draggedItemsSession.Add(val);
                            }
                        }
                    }
                    lastHoveredIndex = hoveredIndexThisFrame;
                }
            }
            GUI.EndGroup();

            // --- RENDER SCROLLBAR VISUALS ---
            GUI.Box(new Rect(scrollX + 5, trackStartY, 2, trackHeight), "", Magnetar_Default.SeparatorStyle);

            float scrollPctVisual = (maxScrollDist > 0) ? manualScrollY / maxScrollDist : 0;
            float handleY = trackStartY + (scrollPctVisual * (trackHeight - handleSize));

            bool shouldHighlight = (activeSliderId == sliderId) || (Time.time - lastSliderUpdateTime < 1.0f);
            GUI.Box(new Rect(scrollX, handleY, 12, handleSize), "", shouldHighlight ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
        }
        private static void ToggleWithLimit(dynamic activeMultiSelect, int val)
        {
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
            GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingDescriptionStyle);

            string currentValName = "Unknown";
            if (selSet.Options.ContainsKey(selSet.Value))
            {
                currentValName = selSet.Options[selSet.Value];
                if (selSet.CustomNames != null && selSet.CustomNames.ContainsKey(selSet.Value))
                {
                    currentValName = selSet.CustomNames[selSet.Value];
                }
            }

            Rect btnRect = new Rect(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
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
                float rowHeight = 22f;
                int maxVisibleRows = 6;
                int itemCount = selSet.Options.Count;
                float dropHeight = Mathf.Min(itemCount * rowHeight, maxVisibleRows * rowHeight);

                Rect dropRect = new Rect(btnRect.x, btnRect.y + btnRect.height, btnRect.width, dropHeight);

                if (dropRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
                {
                    dropdownScrollY = Mathf.Clamp(dropdownScrollY + e.delta.y * 15f, 0, Mathf.Max(0, (itemCount * rowHeight) - dropHeight));
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
                            Rect rowRect = new Rect(0, drawY, dropRect.width, rowHeight);

                            string displayName = kvp.Value;

                            if (selSet.CustomNames != null && selSet.CustomNames.ContainsKey(kvp.Key))
                            {
                                displayName = selSet.CustomNames[kvp.Key];
                            }

                            bool isSelected = (selSet.Value == kvp.Key);
                            bool isRowHovered = rowRect.Contains(Event.current.mousePosition);

                            GUIStyle style = isSelected ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff;

                            if (isRowHovered && !isSelected) GUI.backgroundColor = Magnetar_Default.AccentColor;

                            GUI.Box(rowRect, displayName, style);
                            GUI.backgroundColor = Color.white;
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

        // History tracking for Undo/Redo
        public struct TextState
        {
            public string Text;
            public int Cursor;
            public int Select;
            public TextState(string t, int c, int s) { Text = t; Cursor = c; Select = s; }
        }
        private static int undoStackCount = 200;
        private static List<TextState> undoStack = new List<TextState>();
        private static List<TextState> redoStack = new List<TextState>();
        private static int lastHistoryFieldId = -1;
        public static string DrawManualTextField(Rect rect, string text, string defaultText = "", List<string> autocompleteVars = null)
        {
            
            Event e = Event.current;
            int controlId = rect.GetHashCode();

            if (text == null) text = "";

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
            // --- AUTOCOMPLETE CONTEXT DETECTION ---
            bool showAutocomplete = false;
            string currentFilter = "";
            int bracketStartIndex = -1;
            List<string> filteredVars = new List<string>();

            if (activeTextFieldId == controlId && autocompleteVars != null)
            {
                for (int i = cursorIndex - 1; i >= 0; i--)
                {
                    if (text[i] == '}') break; // Closed block
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
                Rect dropRect = new Rect(rect.x, rect.y + rect.height, rect.width, 150f);
                bool clickingDropdown = showAutocomplete && dropRect.Contains(e.mousePosition);

                if (rect.Contains(e.mousePosition))
                {
                    if (activeTextFieldId != controlId)
                    {
                        activeTextFieldId = controlId;
                    }
                    cursorIndex = GetIndexFromMouse(e.mousePosition.x - rect.x);
                    if (!e.shift) selectIndex = cursorIndex;
                    e.Use();
                }
                else if (activeTextFieldId == controlId && !clickingDropdown)
                {
                    activeTextFieldId = -1;
                }
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && activeTextFieldId == controlId)
            {
                cursorIndex = GetIndexFromMouse(e.mousePosition.x - rect.x);
                e.Use();
            }

            #endregion

            #region Keyboard

            if (activeTextFieldId == controlId && e.type == EventType.KeyDown)
            {
                char c = e.character;
                KeyCode k = e.keyCode;
                bool ctrl = e.control || e.command;
                bool shift = e.shift;

                bool hasSelection = cursorIndex != selectIndex;
                int selStart = Mathf.Min(cursorIndex, selectIndex);
                int selEnd = Mathf.Max(cursorIndex, selectIndex);

                // --- HISTORY HELPER ---
                void SaveState()
                {
                    undoStack.Add(new TextState(text, cursorIndex, selectIndex));
                    if (undoStack.Count > undoStackCount) undoStack.RemoveAt(0); // Cap history
                    redoStack.Clear(); // New action clears redo
                }

                void DeleteSelection()
                {
                    text = text.Remove(selStart, selEnd - selStart);
                    cursorIndex = selectIndex = selStart;
                }

                // --- WORD JUMP HELPER ---
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

                // --- AUTOCOMPLETE NAVIGATION ---
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

                // --- STANDARD SHORTCUTS ---
                if (!interceptedForAutocomplete)
                {
                    // UNDO
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
                    // REDO
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
                    // COPY / CUT / PASTE / SELECT ALL
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
                    // HOME / END
                    else if (k == KeyCode.Home)
                    {
                        cursorIndex = 0; if (!shift) selectIndex = cursorIndex; e.Use();
                    }
                    else if (k == KeyCode.End)
                    {
                        cursorIndex = text.Length; if (!shift) selectIndex = cursorIndex; e.Use();
                    }
                    // ARROWS (Word jumps with Ctrl)
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
                    // DELETION
                    else if (k == KeyCode.Backspace)
                    {
                        if (hasSelection) { SaveState(); DeleteSelection(); }
                        else if (ctrl && cursorIndex > 0) // Ctrl+Backspace (Delete Word)
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
                        else if (ctrl && cursorIndex < text.Length) // Ctrl+Delete (Delete Next Word)
                        {
                            SaveState();
                            int bound = GetWordBoundary(cursorIndex, 1);
                            text = text.Remove(cursorIndex, bound - cursorIndex);
                        }
                        else if (cursorIndex < text.Length) { SaveState(); text = text.Remove(cursorIndex, 1); }
                        e.Use();
                    }
                    // DEFOCUS
                    else if (k == KeyCode.Return || k == KeyCode.Escape)
                    {
                        activeTextFieldId = -1; e.Use();
                    }
                    // TYPING
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

            #endregion

            // --- SCROLL MATH CALCULATION ---
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
                GUI.Label(new Rect(5, 0, rect.width, rect.height), defaultText, Magnetar_Default.SettingDescriptionStyle);
            }
            else
            {
                if (activeTextFieldId == controlId && cursorIndex != selectIndex)
                {
                    int selStart = Mathf.Min(cursorIndex, selectIndex);
                    int selEnd = Mathf.Max(cursorIndex, selectIndex);
                    float startX = TextStyle.CalcSize(new GUIContent(text.Substring(0, selStart))).x;
                    float endX = TextStyle.CalcSize(new GUIContent(text.Substring(0, selEnd))).x;

                    Rect selRect = new Rect(5 + startX - scrollOffset, 2, endX - startX, rect.height - 4);
                    GUI.Box(selRect, "", Magnetar_Default.SettingOn);
                }

                GUI.Label(new Rect(5 - scrollOffset, 0, 2000, rect.height), text, TextStyle);

                if (activeTextFieldId == controlId && (int)(Time.realtimeSinceStartup * 2) % 2 == 0)
                {
                    float cursorPixelX = TextStyle.CalcSize(new GUIContent(text.Substring(0, cursorIndex))).x;
                    Rect cursorRect = new Rect(5 + cursorPixelX - scrollOffset, 3, 1, rect.height - 6);
                    GUI.Box(cursorRect, "", Magnetar_Default.SettingOn);
                }
            }
            GUI.EndGroup();

            // --- DRAW THE AUTOCOMPLETE DROPDOWN ---
            if (showAutocomplete && filteredVars.Count > 0)
            {
                float rowHeight = 22f;
                float maxDropdownHeight = 150f;
                float totalHeight = filteredVars.Count * rowHeight;
                float dropHeight = Mathf.Min(totalHeight, maxDropdownHeight);

                Rect dropRect = new Rect(rect.x, rect.y + rect.height, rect.width, dropHeight);

                void SaveState()
                {
                    undoStack.Add(new TextState(text, cursorIndex, selectIndex));
                    if (undoStack.Count > undoStackCount) undoStack.RemoveAt(0);
                    redoStack.Clear(); // Any new action invalidates the redo chain
                }

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
                        autocompleteScrollY = Mathf.Clamp(autocompleteScrollY + e.delta.y * 15f, 0, Mathf.Max(0, totalHeight - dropHeight));
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        SaveState(); // Save state before clicking an autocomplete!
                        string chosen = filteredVars[autocompleteSelectedIndex];
                        text = text.Remove(bracketStartIndex + 1, cursorIndex - bracketStartIndex - 1);
                        text = text.Insert(bracketStartIndex + 1, chosen + "}");
                        cursorIndex = selectIndex = bracketStartIndex + chosen.Length + 2;
                        activeTextFieldId = controlId;
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
                            Rect rowRect = new Rect(0, drawY, dropRect.width, rowHeight);
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
    }
}
