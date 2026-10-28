using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.Core;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    private static int cursorIndex;
    private static int selectIndex;
    private static float scrollOffset;
    private static float lastClickTime = -1f;
    private static int lastClickIndex = -1;
    private static float lastDoubleClickTime = -1f;

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
    /// <summary>
    /// Gets or sets the vertical scroll offset of the autocomplete dropdown list.
    /// </summary>
    public static float autocompleteScrollY;

    /// <summary>
    /// Gets or sets the zero-based index of the currently selected autocomplete item.
    /// </summary>
    public static int autocompleteSelectedIndex;

    /// <summary>
    /// Represents an undoable/redoable snapshot of a text field's content and selection state.
    /// </summary>
    public struct TextState
    {
        /// <summary>
        /// The text value in this snapshot.
        /// </summary>
        public string Text;

        /// <summary>
        /// The cursor position index in this snapshot.
        /// </summary>
        public int Cursor;

        /// <summary>
        /// The selection anchor index in this snapshot.
        /// </summary>
        public int Select;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextState"/> struct.
        /// </summary>
        /// <param name="t">The text content.</param>
        /// <param name="c">The cursor position index.</param>
        /// <param name="s">The selection anchor index.</param>
        public TextState(string t, int c, int s) { Text = t; Cursor = c; Select = s; }
    }

    private static int undoStackCount = SettingValues.TextFieldUndoLimit;
    private static readonly List<TextState> undoStack = new();
    private static readonly List<TextState> redoStack = new();
    private static int lastHistoryFieldId = -1;

#if ANDROID
    private static TouchScreenKeyboard _mobileKeyboard = null;
    private static int _activeMobileKeyboardId = -1;
#endif

    /// <summary>
    /// Renders a string setting control with a label and editable text input box.
    /// </summary>
    /// <param name="strSet">The <see cref="StringSetting"/> to draw and handle.</param>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    public static void HandleStringSetting(StringSetting strSet, ref float y, float width)
    {
        float elemH = ClientConfig.elementHeight;
        string translatedName = Translate(strSet.Name);

        float controlW = Mathf.Min(ClientConfig.SettingWidth * 1.25f, width * 0.55f);
        float gap = GUIManager.S(8f);
        float labelW = Mathf.Max(width * 0.38f, width - (ClientConfig.indent * 2f) - controlW - gap);

        Rect labelRect = new(ClientConfig.indent, y, labelW, elemH);
        Rect inputRect = new(width - ClientConfig.indent - controlW, y, controlW, elemH);

        GUI.Label(labelRect, translatedName, ThemeManager.SettingLabelStyle);
        strSet.Value = DrawManualTextField(inputRect, strSet.Value, "", strSet.AutocompleteVars);
    }

    /// <summary>
    /// Wraps text inside brackets (including brackets) with rich text color tags using <see cref="ThemeManager.RichTextColor"/>.
    /// </summary>
    /// <param name="input">The raw text string to format.</param>
    /// <returns>The formatted string with rich text color tags around bracket tokens.</returns>
    public static string FormatBracketRichText(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains("{"))
            return input;

        string colorHex = ThemeData.ColorToHex(ThemeManager.RichTextColor);
        return System.Text.RegularExpressions.Regex.Replace(input, @"\{[^}\r\n]*(?:\}|$)", match => $"<color={colorHex}>{match.Value}</color>");
    }

    /// <summary>
    /// Draws and handles an interactive manual text field supporting text editing, selection, undo/redo, and optional autocomplete.
    /// </summary>
    /// <param name="rect">The bounding box where the text field is drawn.</param>
    /// <param name="text">The current text content of the field.</param>
    /// <param name="defaultText">The placeholder text displayed when the field is empty and unfocused.</param>
    /// <param name="autocompleteVars">Optional list of variable names for bracket autocomplete.</param>
    /// <returns>The updated text string after processing user input.</returns>
    public static string DrawManualTextField(Rect rect, string text, string defaultText = "", List<string> autocompleteVars = null)
    {
        Event e = Event.current;
        int controlId = rect.GetHashCode();
        text ??= "";

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

        if (ActiveTextFieldId == controlId && lastHistoryFieldId != controlId)
        {
            undoStack.Clear();
            redoStack.Clear();
            lastHistoryFieldId = controlId;
        }

        if (ActiveTextFieldId == controlId)
        {
            cursorIndex = Mathf.Clamp(cursorIndex, 0, text.Length);
            selectIndex = Mathf.Clamp(selectIndex, 0, text.Length);
        }

        int GetIndexFromMouse(float mouseX)
        {
            float localX = mouseX - 5 + scrollOffset;
            if (localX <= 0) return 0;
            for (int i = 1; i <= text.Length; i++)
            {
                float wThis = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, i))).x;
                float wPrev = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, i - 1))).x;
                if (localX < (wThis + wPrev) / 2f) return i - 1;
            }
            return text.Length;
        }

        bool showAutocomplete = false;
        string currentFilter = "";
        int bracketStartIndex = -1;
        List<string> filteredVars = new();

        if (ActiveTextFieldId == controlId && autocompleteVars != null)
        {
            for (int i = cursorIndex - 1; i >= 0; i--)
            {
                if (text[i] == '}') break;
                if (text[i] == '{')
                {
                    showAutocomplete = true;
                    bracketStartIndex = i;
                    currentFilter = text.Substring(i + 1, cursorIndex - i - 1);
                    break;
                }
            }

            if (showAutocomplete)
            {
                HashSet<string> seen = new(System.StringComparer.OrdinalIgnoreCase);

                int lastDot = currentFilter.LastIndexOf('.');
                string committedPrefix = lastDot >= 0 ? currentFilter.Substring(0, lastDot + 1) : "";
                string subFilter = lastDot >= 0 ? currentFilter.Substring(lastDot + 1) : currentFilter;

                foreach (var v in autocompleteVars)
                {
                    if (string.IsNullOrEmpty(v)) continue;

                    if (!string.IsNullOrEmpty(committedPrefix))
                    {
                        if (!v.StartsWith(committedPrefix, System.StringComparison.OrdinalIgnoreCase))
                            continue;

                        string remainder = v.Substring(committedPrefix.Length);
                        int nextDot = remainder.IndexOf('.');
                        string suggestion = nextDot >= 0 ? committedPrefix + remainder.Substring(0, nextDot + 1) : v;
                        string segment = nextDot >= 0 ? remainder.Substring(0, nextDot) : remainder;

                        if (string.IsNullOrEmpty(subFilter) || segment.IndexOf(subFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (seen.Add(suggestion))
                                filteredVars.Add(suggestion);
                        }
                    }
                    else
                    {
                        int firstDot = v.IndexOf('.');
                        string suggestion = firstDot >= 0 ? v.Substring(0, firstDot + 1) : v;
                        string segment = firstDot >= 0 ? v.Substring(0, firstDot) : v;

                        if (string.IsNullOrEmpty(subFilter) || segment.IndexOf(subFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (seen.Add(suggestion))
                                filteredVars.Add(suggestion);
                        }
                        else if (v.IndexOf(subFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int matchIdx = v.IndexOf(subFilter, System.StringComparison.OrdinalIgnoreCase);
                            int nextDotAfter = v.IndexOf('.', matchIdx);
                            string deepSuggestion = nextDotAfter >= 0 ? v.Substring(0, nextDotAfter + 1) : v;
                            if (seen.Add(deepSuggestion))
                                filteredVars.Add(deepSuggestion);
                        }
                    }
                }

                if (filteredVars.Count == 0) showAutocomplete = false;
                else autocompleteSelectedIndex = Mathf.Clamp(autocompleteSelectedIndex, 0, filteredVars.Count - 1);
            }
        }

        float rowHeight = SettingValues.AutocompleteRowHeight;
        float maxDropdownHeight = SettingValues.AutocompleteMaxHeight;
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

        void ApplyAutocomplete(string chosen)
        {
#if !ANDROID
            SaveState();
#endif
            int endBracket = -1;
            for (int i = cursorIndex; i < text.Length; i++)
            {
                if (text[i] == '}')
                {
                    endBracket = i;
                    break;
                }
                if (text[i] == '{')
                {
                    break;
                }
            }

            int removeStart = bracketStartIndex + 1;

            if (chosen.EndsWith("."))
            {
                if (endBracket >= 0)
                {
                    int removeLength = endBracket - removeStart;
                    text = text.Remove(removeStart, removeLength);
                    text = text.Insert(removeStart, chosen);
                    cursorIndex = selectIndex = removeStart + chosen.Length;
                }
                else
                {
                    int removeLength = cursorIndex - removeStart;
                    text = text.Remove(removeStart, removeLength);
                    text = text.Insert(removeStart, chosen + "}");
                    cursorIndex = selectIndex = removeStart + chosen.Length;
                }
                showAutocomplete = true;
                autocompleteSelectedIndex = 0;
            }
            else
            {
                if (endBracket >= 0)
                {
                    int removeLength = (endBracket + 1) - removeStart;
                    text = text.Remove(removeStart, removeLength);
                    text = text.Insert(removeStart, chosen + "}");
                    cursorIndex = selectIndex = removeStart + chosen.Length + 1;
                }
                else
                {
                    int removeLength = cursorIndex - removeStart;
                    text = text.Remove(removeStart, removeLength);
                    text = text.Insert(removeStart, chosen + "}");
                    cursorIndex = selectIndex = removeStart + chosen.Length + 1;
                }
                showAutocomplete = false;
            }
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            bool clickingDropdown = showAutocomplete && dropRect.Contains(e.mousePosition);

            if (rect.Contains(e.mousePosition))
            {
                if (ActiveTextFieldId != controlId)
                {
                    ActiveTextFieldId = controlId;
#if ANDROID
                    _mobileKeyboard = TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.Default);
                    _activeMobileKeyboardId = controlId;
#endif
                }

                int clickIdx = GetIndexFromMouse(e.mousePosition.x - rect.x);

                bool isDoubleClick = e.clickCount == 2 ||
                    (e.clickCount < 2 && Time.unscaledTime - lastClickTime < 0.35f && Mathf.Abs(clickIdx - lastClickIndex) <= 1);
                bool isTripleClick = e.clickCount >= 3 ||
                    (isDoubleClick && Time.unscaledTime - lastDoubleClickTime < 0.35f);

                if (isTripleClick)
                {
                    selectIndex = 0;
                    cursorIndex = text.Length;
                    lastClickTime = -1f;
                    lastDoubleClickTime = -1f;
                }
                else if (isDoubleClick)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        selectIndex = cursorIndex = 0;
                    }
                    else
                    {
                        int idx = Mathf.Clamp(clickIdx, 0, text.Length);
                        if (idx > 0 && (idx == text.Length || !IsWordChar(text[idx])) && IsWordChar(text[idx - 1]))
                            idx--;
                        if (idx >= text.Length) idx = text.Length - 1;

                        int start = idx;
                        int end = idx;

                        if (IsWordChar(text[idx]))
                        {
                            while (start > 0 && IsWordChar(text[start - 1])) start--;
                            while (end < text.Length && IsWordChar(text[end])) end++;
                        }
                        else if (char.IsWhiteSpace(text[idx]))
                        {
                            while (start > 0 && char.IsWhiteSpace(text[start - 1])) start--;
                            while (end < text.Length && char.IsWhiteSpace(text[end])) end++;
                        }
                        else
                        {
                            while (start > 0 && text[start - 1] == text[idx]) start--;
                            while (end < text.Length && text[end] == text[idx]) end++;
                        }

                        selectIndex = start;
                        cursorIndex = end;
                    }

                    lastDoubleClickTime = Time.unscaledTime;
                    lastClickTime = Time.unscaledTime;
                    lastClickIndex = clickIdx;
                }
                else
                {
                    cursorIndex = clickIdx;
                    if (!e.shift) selectIndex = cursorIndex;
                    lastClickTime = Time.unscaledTime;
                    lastClickIndex = clickIdx;
                }

                e.Use();
            }
            else if (ActiveTextFieldId == controlId && !clickingDropdown)
            {
                ActiveTextFieldId = -1;
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
        else if (e.type == EventType.MouseDrag && e.button == 0 && ActiveTextFieldId == controlId)
        {
            cursorIndex = GetIndexFromMouse(e.mousePosition.x - rect.x);
            e.Use();
        }

#if !ANDROID
        if (ActiveTextFieldId == controlId && e.type == EventType.KeyDown)
        {
            char c = e.character;
            KeyCode k = e.keyCode;
            bool ctrl = e.control || e.command;
            bool shift = e.shift;

            bool hasSelection = cursorIndex != selectIndex;
            int selStart = Mathf.Min(cursorIndex, selectIndex);
            int selEnd = Mathf.Max(cursorIndex, selectIndex);

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
                    if (autocompleteSelectedIndex >= 0 && autocompleteSelectedIndex < filteredVars.Count)
                    {
                        string chosen = filteredVars[autocompleteSelectedIndex];
                        ApplyAutocomplete(chosen);
                    }
                    interceptedForAutocomplete = true;
                    e.Use();
                }
                else if (k == KeyCode.Escape)
                {
                    showAutocomplete = false;
                    interceptedForAutocomplete = true;
                    e.Use();
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
                    if (hasSelection) GUIUtility.systemCopyBuffer = text[selStart..selEnd];
                    e.Use();
                }
                else if (ctrl && k == KeyCode.X)
                {
                    if (hasSelection) { GUIUtility.systemCopyBuffer = text[selStart..selEnd]; SaveState(); DeleteSelection(); }
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
                else if (k == KeyCode.Return)
                {
                    ActiveTextFieldId = -1;
                    e.Use();
                }
                else if (k == KeyCode.Escape)
                {
                    ActiveTextFieldId = -1;
                    LastEscapeTime = Time.unscaledTime;
                    LastEscapeFrame = Time.frameCount;
                    e.Use();
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

        if (ActiveTextFieldId == controlId)
        {
            float visibleWidth = rect.width - 10;
            float cursorPixelX = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, cursorIndex))).x;

            if (cursorPixelX - scrollOffset > visibleWidth) scrollOffset = cursorPixelX - visibleWidth;
            else if (cursorPixelX - scrollOffset < 0) scrollOffset = cursorPixelX;

            float totalWidth = ThemeManager.TextStyle.CalcSize(new GUIContent(text)).x;
            if (totalWidth - scrollOffset < visibleWidth && scrollOffset > 0)
                scrollOffset = Mathf.Max(0, totalWidth - visibleWidth);
        }
        else scrollOffset = 0f;

        GUI.Box(rect, "", ThemeManager.SettingOff);
        GUI.BeginGroup(rect);

        if (string.IsNullOrEmpty(text) && ActiveTextFieldId != controlId)
        {
            GUI.Label(new Rect(5, 0, rect.width, rect.height), defaultText, PlaceholderStyle);
        }
        else
        {
            if (ActiveTextFieldId == controlId && cursorIndex != selectIndex)
            {
                int selStart = Mathf.Min(cursorIndex, selectIndex);
                int selEnd = Mathf.Max(cursorIndex, selectIndex);
                float startX = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, selStart))).x;
                float endX = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, selEnd))).x;

                Rect selRect = new(5 + startX - scrollOffset, 2, endX - startX, rect.height - 4);
                GUI.Box(selRect, "", ThemeManager.TextHighlightedStyle);
            }

            GUI.Label(new Rect(5 - scrollOffset, 0, 2000, rect.height), FormatBracketRichText(text), ThemeManager.TextRichStyle);

            if (ActiveTextFieldId == controlId && (int)(Time.realtimeSinceStartup * 2) % 2 == 0)
            {
                float cursorPixelX = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, cursorIndex))).x;
                Rect cursorRect = new(5 + cursorPixelX - scrollOffset, 3, 1, rect.height - 6);
                GUI.Box(cursorRect, "", ThemeManager.SettingOn);
            }
        }
        GUI.EndGroup();

        if (showAutocomplete && filteredVars.Count > 0)
        {
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
                    autocompleteScrollY = Mathf.Clamp(autocompleteScrollY + e.delta.y * SettingValues.AutocompleteScrollSensitivity,
                        0, Mathf.Max(0, totalHeight - dropHeight));
                    e.Use();
                }
                else if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (autocompleteSelectedIndex >= 0 && autocompleteSelectedIndex < filteredVars.Count)
                    {
                        string chosen = filteredVars[autocompleteSelectedIndex];
                        ApplyAutocomplete(chosen);
                        ActiveTextFieldId = controlId;

#if ANDROID
                        if (_mobileKeyboard != null)
                        {
                            _mobileKeyboard.text = text;
                        }
#endif
                    }
                    e.Use();
                }
            }

            if (showAutocomplete)
            {
                float _autocompleteScrollY = autocompleteScrollY;
                int _autocompleteSelectedIndex = autocompleteSelectedIndex;

                OnPostDraw = () =>
                {
                    GUI.Box(dropRect, "", ThemeManager.SettingOff);
                    GUI.BeginGroup(dropRect);
                    for (int i = 0; i < filteredVars.Count; i++)
                    {
                        float drawY = (i * rowHeight) - _autocompleteScrollY;
                        if (drawY + rowHeight > 0 && drawY < dropHeight)
                        {
                            Rect rowRect = new(0, drawY, dropRect.width, rowHeight);
                            GUIStyle rowStyle = (i == _autocompleteSelectedIndex) ? ThemeManager.SettingOn : ThemeManager.SettingOff;
                            GUI.Box(rowRect, "{" + filteredVars[i] + "}", rowStyle);
                        }
                    }
                    GUI.EndGroup();
                };
            }
        }

        return text;
    }
}