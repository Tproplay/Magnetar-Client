using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    private static int cursorIndex = 0;
    private static int selectIndex = 0;
    private static float scrollOffset = 0f;
    public static float autocompleteScrollY = 0f;
    public static int autocompleteSelectedIndex = 0;

    public struct TextState
    {
        public string Text;
        public int Cursor;
        public int Select;
        public TextState(string t, int c, int s) { Text = t; Cursor = c; Select = s; }
    }

    private static int undoStackCount = Config.SettingsInput.TextFieldUndoLimit;
    private static readonly List<TextState> undoStack = new();
    private static readonly List<TextState> redoStack = new();
    private static int lastHistoryFieldId = -1;

#if ANDROID
    private static TouchScreenKeyboard _mobileKeyboard = null;
    private static int _activeMobileKeyboardId = -1;
#endif

    public static void HandleStringSetting(StringSetting strSet, ref float y, float width)
    {
        float elemH = Config.elementHeight;
        string translatedName = Translator.Translate(strSet.Name);

        float controlW = Mathf.Min(Config.SettingWidth * 1.25f, width * 0.55f);
        float gap = Config.S(8f);
        float labelW = Mathf.Max(width * 0.38f, width - (Config.indent * 2f) - controlW - gap);

        Rect labelRect = new(Config.indent, y, labelW, elemH);
        Rect inputRect = new(width - Config.indent - controlW, y, controlW, elemH);

        GUI.Label(labelRect, translatedName, ThemeManager.SettingLabelStyle);
        strSet.Value = DrawManualTextField(inputRect, strSet.Value, "", strSet.AutocompleteVars);
    }

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

        if (activeTextFieldId == controlId)
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
                float startX = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, selStart))).x;
                float endX = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, selEnd))).x;

                Rect selRect = new(5 + startX - scrollOffset, 2, endX - startX, rect.height - 4);
                GUI.Box(selRect, "", ThemeManager.TextHighlightedStyle);
            }

            GUI.Label(new Rect(5 - scrollOffset, 0, 2000, rect.height), text, ThemeManager.TextStyle);

            if (activeTextFieldId == controlId && (int)(Time.realtimeSinceStartup * 2) % 2 == 0)
            {
                float cursorPixelX = ThemeManager.TextStyle.CalcSize(new GUIContent(text.Substring(0, cursorIndex))).x;
                Rect cursorRect = new(5 + cursorPixelX - scrollOffset, 3, 1, rect.height - 6);
                GUI.Box(cursorRect, "", ThemeManager.SettingOn);
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

        return text;
    }
}