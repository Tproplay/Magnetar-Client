using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using System.Collections.Generic;
using UnityEngine;
using System;
using MelonLoader;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.UI.WindowDrawing
{
    public static class DrawSetting
    {
        public static int focusedControlId = -1;
        public static string currentInputBuffer = "";
        public static int activeSliderId = -1;

        public static void HandleNumericSetting(object setting, ref float y, float width, bool isFloat)
        {
            float val, min, max;
            string name;
            int myId = setting.GetHashCode();
            int decPlaces = 0;

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
            }

            string formatString = isFloat ? ("0." + new string('0', decPlaces)) : "0";

            GUI.Label(new Rect(Config.indent, y, width * 0.45f, Config.elementHeight), name);

            // --- LOGARITHMIC MAPPING HELPERS ---
            float LogConvert(float v) => Mathf.Sign(v) * Mathf.Log10(Mathf.Abs(v) + 1.0f);
            float ExpConvert(float l) => Mathf.Sign(l) * (Mathf.Pow(10.0f, Mathf.Abs(l)) - 1.0f);

            float logMin = LogConvert(min);
            float logMax = LogConvert(max);
            float logVal = LogConvert(val);

            float percentage = Mathf.Clamp01((logVal - logMin) / (logMax - logMin));

            // --- UI RENDERING ---
            Rect sliderRect = new Rect(width * 0.35f, y + 10, width * 0.35f, 8);
            Rect sliderHitBox = new Rect(sliderRect.x, y, sliderRect.width, 22);
            Rect inputRect = new Rect(width * 0.72f, y, width * 0.23f, 22);
            float fillWidth = sliderRect.width * percentage;
            Rect thumbRect = new Rect(sliderRect.x + fillWidth - 10, y + 1, 20, 20);

            GUI.Box(sliderRect, "", Magnetar_Default.ModuleOff);
            if (fillWidth > 0) GUI.Box(new Rect(sliderRect.x, sliderRect.y, fillWidth, sliderRect.height), "", Magnetar_Default.ModuleOn);

            GUIStyle thumbStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 40 };
            thumbStyle.normal.textColor = Magnetar_Default.AccentColor;

            GUI.Label(thumbRect, "●", thumbStyle);
            GUI.Box(inputRect, "", Magnetar_Default.ModuleOff);

            // --- INPUT BOX LOGIC ---
            Event e = Event.current;

            if (e.type == EventType.ScrollWheel && (sliderHitBox.Contains(e.mousePosition) || thumbRect.Contains(e.mousePosition)))
            {
                float scrollDirection = -Mathf.Sign(e.delta.y);

                // Move the slider by 4% per scroll wheel "tick".
                float scrollStep = 0.04f;
                float newPercentage = Mathf.Clamp01(percentage + (scrollDirection * scrollStep));

                float newLogVal = logMin + (newPercentage * (logMax - logMin));
                float newVal = ExpConvert(newLogVal);

                // Apply the new value
                if (isFloat) ((FloatSetting)setting).Value = (float)System.Math.Round(Mathf.Clamp(newVal, min, max), decPlaces);
                else ((IntSetting)setting).Value = (int)Mathf.Clamp(newVal, min, max);

                e.Use();
            }

            if (e.type == EventType.MouseDown && inputRect.Contains(e.mousePosition))
            {
                focusedControlId = myId;
                currentInputBuffer = val.ToString(formatString);
                e.Use();
            }

            bool isFocused = (focusedControlId == myId);
            if (isFocused && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace && currentInputBuffer.Length > 0)
                    currentInputBuffer = currentInputBuffer.Substring(0, currentInputBuffer.Length - 1);
                else if (e.character != '\0' && (char.IsDigit(e.character) || e.character == '.' || e.character == '-'))
                    currentInputBuffer += e.character;

                if (float.TryParse(currentInputBuffer, out float parsed))
                {
                    float final = Mathf.Clamp(parsed, min, max);
                    if (isFloat) ((FloatSetting)setting).Value = final;
                    else ((IntSetting)setting).Value = (int)final;
                }

                if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return) focusedControlId = -1;
                e.Use();
            }

            string displayValue = isFocused ? currentInputBuffer : val.ToString(formatString);
            GUI.Label(new Rect(inputRect.x + 5, inputRect.y, inputRect.width - 5, 22), displayValue + ((isFocused && Time.time % 1.0f < 0.5f) ? "|" : ""));

            // --- LOGARITHMIC DRAG LOGIC ---
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
                    else ((IntSetting)setting).Value = (int)Mathf.Clamp(newVal, min, max);

                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    activeSliderId = -1;
                    e.Use();
                }
            }
        }

        public static void HandleBindSetting(BindSetting bSet, ref float y, float width)
        {
            Event e = Event.current;
            bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

            GUI.Label(new Rect(Config.indent, y, width * 0.45f, Config.elementHeight), bSet.Name);

            // --- UI DISPLAY ---
            string bindText = bSet.IsBinding ? "[...]" : bSet.GetBindString();
            Rect bindRect = new Rect(width * 0.5f, y, width * 0.45f, Config.elementHeight);
            bool bindHover = bindRect.Contains(e.mousePosition);

            if (bindHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(bindRect, bindText, bSet.IsBinding ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff);
            GUI.backgroundColor = Color.white;

            // --- CLICK LOGIC ---
            if (bindHover && isLeftClick)
            {
                bSet.IsBinding = !bSet.IsBinding;
                if (bSet.IsBinding)
                {
                    bSet.BindKeys.Clear();
                    focusedControlId = -1;
                }
                e.Use();
            }

            // --- KEY LISTENING LOGIC ---
            if (bSet.IsBinding && e.isKey)
            {
                KeyCode key = e.keyCode;
                if (key != KeyCode.None)
                {
                    if (e.type == EventType.KeyDown)
                    {
                        // Escape or RightShift cancels the bind
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
                        // Finalize the bind once keys are released
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

            GUI.Label(new Rect(Config.indent, y, width * 0.45f, Config.elementHeight), boolSet.Name);

            Rect btnRect = new Rect(width * 0.5f, y, width * 0.45f, Config.elementHeight);

            GUI.Box(btnRect, boolSet.Value ? "ON" : "OFF", boolSet.Value ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff);

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

        private static bool isShiftDragging = false;
        private static bool dragTargetState = false;
        private static HashSet<int> draggedItemsSession = new HashSet<int>();

        public static bool isSearchFocused = false;

        public static void DrawMultiSelectWindow(Rect multiSelectWindowRect, dynamic activeMultiSelect)
        {
            if (activeMultiSelect == null) return;

            Event e = Event.current;
            int searchBarId = 1001;
            int sliderId = 1002;
            float ROW_HEIGHT = 22f;

            var options = activeMultiSelect.Options;

            float calcTotalHeight = 0;
            int visibleCount = 0;

            // --- HEIGHT PRE-CALCULATION ---
            foreach (var kvp in options)
            {
                int intVal = kvp.Key;
                string name = kvp.Value;

                // Filter logic
                if (activeMultiSelect.Blacklist != null && activeMultiSelect.Blacklist.Contains(intVal)) continue;
                if (activeMultiSelect.NameBlacklist != null && activeMultiSelect.NameBlacklist.Contains(name)) continue;

                if (!string.IsNullOrEmpty(multiSelectSearchQuery) && !name.ToLower().Contains(multiSelectSearchQuery.ToLower()))
                    continue;

                visibleCount++;
                calcTotalHeight += ROW_HEIGHT + 2;
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
            float searchWidth = (multiSelectWindowRect.width - 50) * 0.65f;
            float toggleWidth = (multiSelectWindowRect.width - 50) * 0.35f;
            Rect searchRect = new Rect(10, 30, searchWidth, 22);
            GUI.Box(searchRect, "", Magnetar_Default.ModuleOff);

            if (e.type == EventType.MouseDown && searchRect.Contains(e.mousePosition))
            {
                focusedControlId = searchBarId;
                e.Use();
            }
            else if (e.type == EventType.MouseDown && !searchRect.Contains(e.mousePosition) && focusedControlId == searchBarId)
            {
                focusedControlId = -1;
            }

            // Toggle Button
            int selectedCount = activeMultiSelect.SelectedValues.Count;
            bool allSelected = visibleCount > 0 && selectedCount >= visibleCount;
            Rect toggleRect = new Rect(15 + searchWidth, 30, toggleWidth, 22);

            if (GUI.Button(toggleRect, allSelected ? "Deselect All" : "Select All", !allSelected ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff))
            {
                foreach (var kvp in options)
                {
                    int intVal = kvp.Key;
                    string name = kvp.Value;

                    if (activeMultiSelect.Blacklist != null && activeMultiSelect.Blacklist.Contains(intVal)) continue;
                    if (activeMultiSelect.NameBlacklist != null && activeMultiSelect.NameBlacklist.Contains(name)) continue;
                    if (!string.IsNullOrEmpty(multiSelectSearchQuery) && !name.ToLower().Contains(multiSelectSearchQuery.ToLower())) continue;

                    if (allSelected)
                        activeMultiSelect.Deselect(intVal);
                    else
                        activeMultiSelect.Select(intVal);
                }
                e.Use();
            }

            // Search Input
            bool isSearchFocused = (focusedControlId == searchBarId);
            if (isSearchFocused && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace && multiSelectSearchQuery.Length > 0)
                    multiSelectSearchQuery = multiSelectSearchQuery.Substring(0, multiSelectSearchQuery.Length - 1);
                else if (e.keyCode == KeyCode.Escape) focusedControlId = -1;
                else if (e.character != '\0' && !char.IsControl(e.character))
                    multiSelectSearchQuery += e.character;

                manualScrollY = 0f;
                e.Use();
            }

            string cursor = (isSearchFocused && Time.time % 1.0f < 0.5f) ? "|" : "";
            GUI.Label(searchRect, " Search: " + multiSelectSearchQuery + cursor, Magnetar_Default.DescriptionStyle);

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
            }

            if (isShiftDragging && !Input.GetMouseButton(0))
            {
                isShiftDragging = false;
                draggedItemsSession.Clear();
#if DEBUG
                DebugLogger.Msg("Drag Complete");
#endif
            }

            // --- THE LIST (CLIPPED) ---
            GUI.BeginGroup(new Rect(10, headerHeight, multiSelectWindowRect.width - 25, viewHeight));
            {
                float currentY = 0;

                string cleanQuery = multiSelectSearchQuery?.Replace(" ", "") ?? "";

                foreach (var kvp in options)
                {
                    int intVal = kvp.Key;
                    string name = kvp.Value;

                    // --- FILTERING ---
                    if (activeMultiSelect.Blacklist != null && activeMultiSelect.Blacklist.Contains(intVal)) continue;
                    if (activeMultiSelect.NameBlacklist != null && activeMultiSelect.NameBlacklist.Contains(name)) continue;

                    if (!string.IsNullOrEmpty(cleanQuery))
                    {
                        if (name.Replace(" ", "").IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    }

                    // --- RENDERING & LOGIC ---
                    float drawY = currentY - manualScrollY;

                    if (drawY + ROW_HEIGHT > 0 && drawY < viewHeight)
                    {
                        Rect rowRect = new Rect(0, drawY, multiSelectWindowRect.width - 30, ROW_HEIGHT);
                        bool isHovering = rowRect.Contains(e.mousePosition);
                        bool isCurrentlySelected = activeMultiSelect.IsSelected(intVal);

                        // A. START CLICK / START DRAG
                        if (e.type == EventType.MouseDown && e.button == 0 && isHovering)
                        {
                            if (e.shift || Input.GetKey(KeyCode.LeftShift))
                            {
                                // Initiate the Shift-Drag session
                                isShiftDragging = true;
                                draggedItemsSession.Clear();

                                dragTargetState = !isCurrentlySelected;

                                activeMultiSelect.Toggle(intVal);
                                draggedItemsSession.Add(intVal);
#if DEBUG
                                MelonLogger.Msg("Drag Started");
#endif
                            }
                            else
                            {
                                // Standard single click
                                activeMultiSelect.Toggle(intVal);
                            }
                            e.Use();
                        }

                        // B. CONTINUE DRAG (PAINT SELECT)
                        if (isShiftDragging && isHovering && Input.GetMouseButton(0))
                        {
                            // Only process this item if we haven't already hit it during this drag session
                            if (!draggedItemsSession.Contains(intVal))
                            {
                                if (isCurrentlySelected != dragTargetState)
                                {
                                    activeMultiSelect.Toggle(intVal);
                                }

                                draggedItemsSession.Add(intVal);
                            }
                        }

                        // Draw the Box
                        GUI.Box(rowRect, name, activeMultiSelect.IsSelected(intVal) ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff);
                    }
                    currentY += ROW_HEIGHT + 2;
                }
            }
            GUI.EndGroup();

            // --- RENDER SCROLLBAR VISUALS ---
            GUI.Box(new Rect(scrollX + 5, trackStartY, 2, trackHeight), "", Magnetar_Default.SeparatorStyle);

            float scrollPctVisual = (maxScrollDist > 0) ? manualScrollY / maxScrollDist : 0;
            float handleY = trackStartY + (scrollPctVisual * (trackHeight - handleSize));

            bool shouldHighlight = (activeSliderId == sliderId) || (Time.time - lastSliderUpdateTime < 1.0f);
            GUI.Box(new Rect(scrollX, handleY, 12, handleSize), "", shouldHighlight ? Magnetar_Default.ModuleOn : Magnetar_Default.ModuleOff);
        }

        public static string DrawManualTextField(Rect rect, string text, string defaultText = "")
        {
            Event e = Event.current;

            // 1. Handle Mouse Clicks (Focusing)
            if (e.type == EventType.MouseDown)
            {
                isSearchFocused = rect.Contains(e.mousePosition);
                if (isSearchFocused) e.Use(); // Consume the click
            }

            // 2. Handle Typing (Only if focused)
            if (isSearchFocused && e.type == EventType.KeyDown)
            {
                char c = e.character;
                KeyCode k = e.keyCode;

                if (k == KeyCode.Backspace)
                {
                    if (text.Length > 0)
                    {
                        text = text.Substring(0, text.Length - 1);
                    }
                    e.Use();
                }
                else if (k == KeyCode.Return || k == KeyCode.Escape || k == KeyCode.RightShift)
                {
                    isSearchFocused = false; // Defocus on Enter/Esc
                    e.Use();
                }
                else if (c != 0 && c != '\n' && c != '\r' && c != '\t') // Valid char
                {
                    text += c;
                    e.Use();
                }
            }

            // 3. Draw the Visuals

            GUI.Box(rect, "");

            string cursor = (isSearchFocused && (int)(Time.realtimeSinceStartup * 2) % 2 == 0) ? "|" : "";

            // Draw the text
            if (string.IsNullOrEmpty(text) && !isSearchFocused)
            {
                GUI.Label(new Rect(rect.x + 5, rect.y, rect.width - 5, rect.height), defaultText, Magnetar_Default.DescriptionStyle);
            }
            else
            {
                GUI.Label(new Rect(rect.x + 5, rect.y, rect.width - 5, rect.height), text + cursor);
            }

            return text;
        }
    }
}
