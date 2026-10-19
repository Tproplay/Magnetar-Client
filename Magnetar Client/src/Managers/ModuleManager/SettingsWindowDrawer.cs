using Magnetar_Client.Modules;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.Core.ModuleManager_;

internal static class SettingsWindowDrawer
{
    private static readonly Dictionary<Modules.Module, Rect> _settingsPositions = new();
    private static readonly Dictionary<Modules.Module, Vector2> _settingsScrollPositions = new();
    private static readonly Dictionary<Modules.Module, float> _moduleContentHeights = new();
    private static readonly Dictionary<Modules.Module, float> _targetContentHeights = new();
    private static readonly Dictionary<Modules.Module, GUI.WindowFunction> _cachedSettingsDelegates = new();

    private static readonly Dictionary<Modules.Module, float> _touchStartY = new();
    private static readonly Dictionary<Modules.Module, float> _touchStartScroll = new();
    private static readonly Dictionary<Modules.Module, bool> _isDragging = new();
    private static bool _isDraggingScrollbar;
    private static Modules.Module _lastActiveModule;

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
        Modules.Module targetMod = ModuleManager.ActiveSettingsModule;
        float viewAlpha = AnimationHandler.GetViewAlpha(ModuleManager.Group, ModuleManager.WindowType.Settings.ToString());

        if (targetMod != null)
        {
            _lastActiveModule = targetMod;
        }
        else if (viewAlpha > 0.001f && _lastActiveModule != null)
        {
            targetMod = _lastActiveModule;
        }
        else
        {
            _lastActiveModule = null;
            return;
        }

        if (targetMod == null) return;

        float currentAlpha = AnimationHandler.CurrentEasedAlpha * viewAlpha;
        if (currentAlpha <= 0.001f) return;

        Color prevColor = GUI.color;
        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);

        int settingsId = Mathf.Abs(targetMod.GetHashCode()) + 1000;
        float targetWidth = CalculateTargetWidth(targetMod);

        if (!_settingsPositions.ContainsKey(targetMod) || ModuleManager.resetWindowPos)
        {
            ModuleManager.resetWindowPos = false;
            float popupHeight = GUIManager.S(25f);

            _settingsPositions[targetMod] = new Rect(
                (ClientConfig.NativeWidth / 2f) - (targetWidth / 2f),
                (ClientConfig.NativeHeight / 2f) - (popupHeight / 2f),
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
                float newWidth = Mathf.Lerp(currentRect.width, targetWidth, Time.deltaTime * ModuleManager.PopupSpeed);
                float widthDiff = newWidth - currentRect.width;

                currentRect.width = newWidth;
                currentRect.x -= widthDiff / 2f;
                _settingsPositions[targetMod] = currentRect;
            }
        }

        // 1. Draw Window
        Rect winRect = GUI.Window(
            settingsId,
            _settingsPositions[targetMod],
            GetSettingsDelegate(targetMod),
            "",
            ThemeManager.SettingsWndowBgStyle
        );

        // 2. Clamp window strictly within screen bounds
        float maxX = Mathf.Max(0f, ClientConfig.NativeWidth - winRect.width);
        float maxY = Mathf.Max(0f, ClientConfig.NativeHeight - winRect.height);

        winRect.x = Mathf.Clamp(winRect.x, 0f, maxX);
        winRect.y = Mathf.Clamp(winRect.y, 0f, maxY);

        _settingsPositions[targetMod] = winRect;

        GUI.color = prevColor;
    }

    private static void DrawSettingsWindow(int id, Modules.Module mod)
    {
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float viewAlpha = AnimationHandler.GetViewAlpha(ModuleManager.Group, ModuleManager.WindowType.Settings.ToString());
        float currentAlpha = AnimationHandler.CurrentEasedAlpha * viewAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        ModuleManager.ActiveSettingsModule = mod;
        float windowWidth = _settingsPositions[mod].width;

#if ANDROID
        float headerHeight = Config.S(26f) * 1.30f;
#else
        float headerHeight = GUIManager.S(26f);
#endif
        float maxWindowHeight = ClientConfig.NativeHeight * ModuleManager.MaxSettingsWindowHeightPct;
        float maxViewHeight = maxWindowHeight - headerHeight;

        if (!_moduleContentHeights.ContainsKey(mod)) _moduleContentHeights[mod] = 0f;
        if (!_targetContentHeights.ContainsKey(mod)) _targetContentHeights[mod] = 0f;
        if (!_settingsScrollPositions.ContainsKey(mod)) _settingsScrollPositions[mod] = Vector2.zero;

        // Top DragWindow call so it catches drag events immediately
        float closeBtnSize = GUIManager.S(20f);
        GUI.DragWindow(new Rect(0, 0, windowWidth - closeBtnSize - GUIManager.S(10f), headerHeight));

        // Header Banner using SettingsWndowStyle
        Rect headerBgRect = new(0, 0, windowWidth, headerHeight);
        GUI.Box(headerBgRect, Translate(mod.Name), ThemeManager.SettingsWndowStyle);

        _moduleContentHeights[mod] = Mathf.Lerp(_moduleContentHeights[mod], _targetContentHeights[mod],
            Time.unscaledDeltaTime * ModuleManager.SettingsScrollLerpSpeed);

        if (Mathf.Abs(_moduleContentHeights[mod] - _targetContentHeights[mod]) < 0.5f)
            _moduleContentHeights[mod] = _targetContentHeights[mod];

        float contentHeight = _moduleContentHeights[mod];
        float windowHeight = Mathf.Min(contentHeight + headerHeight, maxWindowHeight);
        float viewHeight = windowHeight - headerHeight;

        Event e = Event.current;

        if (GUIManager.ShowMobileButtons)
        {
            float btnSize = GUIManager.S(22f);
            float btnY = (headerHeight - btnSize) / 2f;
            float btnX = windowWidth - GUIManager.S(26f);
            Rect closeButtonRect = new(btnX, btnY, btnSize, btnSize);

            bool isHovered = closeButtonRect.Contains(e.mousePosition);

            if (e.type == EventType.MouseDown && e.button == 0 && isHovered)
            {
                e.Use();
                ModuleManager.CurrentWindow = ModuleManager.WindowType.Modules;
                AnimationHandler.SwitchView(ModuleManager.Group, ModuleManager.CurrentWindow.ToString());
                return;
            }

            GUI.Box(closeButtonRect, "✕", ThemeManager.CloseButtonStyle);
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

            // Clamp on dynamic resize
            r.x = Mathf.Clamp(r.x, 0f, Mathf.Max(0f, ClientConfig.NativeWidth - r.width));
            r.y = Mathf.Clamp(r.y, 0f, Mathf.Max(0f, ClientConfig.NativeHeight - r.height));

            _settingsPositions[mod] = r;
        }

        bool needsScrollbar = _targetContentHeights[mod] > maxViewHeight;
        float maxScroll = needsScrollbar ? (_targetContentHeights[mod] - maxViewHeight) : 0f;
        float contentWidth = needsScrollbar ? windowWidth - GUIManager.S(16f) : windowWidth;
        float currentScroll = Mathf.Clamp(_settingsScrollPositions[mod].y, 0f, maxScroll);
        _settingsScrollPositions[mod] = new Vector2(0, currentScroll);

        Rect outRect = new(0, headerHeight, windowWidth, viewHeight);

        // Unified Scroll Handling (Mouse Wheel + Mobile Touch Drag + Scrollbar Drag)
        currentScroll = HandleScrollInput(mod, outRect, currentScroll, maxScroll, needsScrollbar, windowWidth, headerHeight, viewHeight, _targetContentHeights[mod], maxViewHeight, e);

        // Ensure clamped after input
        currentScroll = Mathf.Clamp(currentScroll, 0f, maxScroll);
        _settingsScrollPositions[mod] = new Vector2(0, currentScroll);

        GUI.BeginGroup(outRect);
        float actualHeightDrawn = DrawSettingsBody(mod, -currentScroll, contentWidth);

        // Update target content height and immediately clamp if content shrunk this frame
        _targetContentHeights[mod] = actualHeightDrawn;
        float dynamicMaxScroll = Mathf.Max(0f, actualHeightDrawn - maxViewHeight);
        if (currentScroll > dynamicMaxScroll)
        {
            currentScroll = dynamicMaxScroll;
            _settingsScrollPositions[mod] = new Vector2(0, currentScroll);
        }

        if (DrawSetting.OnPostDraw != null)
        {
            DrawSetting.OnPostDraw.Invoke();
            DrawSetting.OnPostDraw = null;
        }
        GUI.EndGroup();

        if (needsScrollbar)
        {
            DrawSettingsScrollbar(windowWidth, headerHeight, viewHeight, _targetContentHeights[mod], maxViewHeight, currentScroll, dynamicMaxScroll);
        }

        GUI.color = prevColor;
        GUI.contentColor = prevContentColor;
    }

    private static float HandleScrollInput(
        Modules.Module mod,
        Rect outRect,
        float currentScroll,
        float maxScroll,
        bool needsScrollbar,
        float windowWidth,
        float headerHeight,
        float viewHeight,
        float targetContentHeight,
        float maxViewHeight,
        Event e)
    {
        if (!needsScrollbar) return 0f;

        // 1. Mouse ScrollWheel
        if (outRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
        {
            currentScroll = Mathf.Clamp(currentScroll + e.delta.y * ModuleManager.ScrollSensitivity, 0f, maxScroll);
            e.Use();
            return currentScroll;
        }

        // 2. Direct Scrollbar Dragging
        float trackX = windowWidth - GUIManager.S(14f);
        float trackY = headerHeight + GUIManager.S(5f);
        float trackHeight = viewHeight - GUIManager.S(10f);
        float handleHeight = Mathf.Max(GUIManager.S(20f), (maxViewHeight / targetContentHeight) * trackHeight);
        Rect scrollbarHitBox = new(trackX - GUIManager.S(10f), trackY, GUIManager.S(25f), trackHeight);

        if (e.type == EventType.MouseDown && e.button == 0 && scrollbarHitBox.Contains(e.mousePosition))
        {
            _isDraggingScrollbar = true;
            float clickPct = Mathf.Clamp01((e.mousePosition.y - trackY - (handleHeight / 2f)) / Mathf.Max(1f, trackHeight - handleHeight));
            currentScroll = clickPct * maxScroll;
            e.Use();
            return currentScroll;
        }

        if (_isDraggingScrollbar)
        {
            if (Input.GetMouseButton(0))
            {
                float dragPct = Mathf.Clamp01((e.mousePosition.y - trackY - (handleHeight / 2f)) / Mathf.Max(1f, trackHeight - handleHeight));
                currentScroll = dragPct * maxScroll;
                if (e.isMouse && e.type != EventType.Repaint && e.type != EventType.Layout)
                {
                    e.Use();
                }
                return currentScroll;
            }

            _isDraggingScrollbar = false;
        }

#if ANDROID
        bool canTouchScroll = !DrawSetting.IsFocused && !_isDraggingScrollbar;
#else
        bool canTouchScroll = !DrawSetting.IsFocused && !_isDraggingScrollbar && Input.touchSupported && Input.touchCount > 0;
#endif

        if (canTouchScroll)
        {
            if (e.type == EventType.MouseDown && outRect.Contains(e.mousePosition) && !scrollbarHitBox.Contains(e.mousePosition))
            {
                _touchStartY[mod] = e.mousePosition.y;
                _touchStartScroll[mod] = currentScroll;
                _isDragging[mod] = false;
            }
            else if ((e.type == EventType.MouseDrag || Input.GetMouseButton(0)) && _touchStartY.ContainsKey(mod))
            {
                float deltaY = _touchStartY[mod] - e.mousePosition.y;
                if (Mathf.Abs(deltaY) > GUIManager.S(6f) || (_isDragging.TryGetValue(mod, out bool dragging) && dragging))
                {
                    _isDragging[mod] = true;
                    MobileInputHandler.Reset();
                    currentScroll = Mathf.Clamp(_touchStartScroll[mod] + deltaY, 0f, maxScroll);
                    if (e.type == EventType.MouseDrag) e.Use();
                }
            }
            else if (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp || !Input.GetMouseButton(0))
            {
                _isDragging[mod] = false;
                _touchStartY.Remove(mod);
            }
        }
        else if (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp || !Input.GetMouseButton(0))
        {
            _isDragging[mod] = false;
            _touchStartY.Remove(mod);
        }

        return currentScroll;
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

        float calculatedWidth = ClientConfig.indent + maxNameWidth + GUIManager.S(35f) + ClientConfig.SettingWidth + ClientConfig.indent;
        return Mathf.Max(ModuleManager.SettingsWidth, Mathf.Max(mod.SettingsWidth, calculatedWidth));
    }

    private static float DrawSettingsBody(Modules.Module mod, float y, float width)
    {
        float startY = y;
        y += 3 * ClientConfig.spacing;

        float descriptionWidth = width - (ClientConfig.indent * 2);
        string translatedDescription = Translate(mod.Description);

        float calculatedHeight = ThemeManager.SettingsDescriptionStyle.CalcHeight(new GUIContent(translatedDescription), descriptionWidth);
        GUI.Label(new Rect(ClientConfig.indent, y, descriptionWidth, calculatedHeight), translatedDescription, ThemeManager.SettingsDescriptionStyle);
        y += calculatedHeight + ClientConfig.spacing;

        if (!string.IsNullOrEmpty(mod.Author))
        {
            float authorLineHeight = GUIManager.S(18f);
            GUI.Label(new Rect(ClientConfig.indent, y, width - (ClientConfig.indent * 2), authorLineHeight), "by " + mod.Author, ThemeManager.SettingAuthorStyle);
            y += authorLineHeight + ClientConfig.spacing;
        }

        bool skipSettings = false;
        foreach (var setting in mod.Settings)
        {
            if (setting is CategorySetting catSet)
            {
                catSet.IsExpanded = MiscDrawing.Seperator(ref y, width, ClientConfig.indent, ClientConfig.spacing, Translate(catSet.Name), true, catSet.IsExpanded);
                skipSettings = !catSet.IsExpanded;
                if (skipSettings) y -= ClientConfig.spacing / 2;
                continue;
            }
            else if (setting is EndCategorySetting)
            {
                skipSettings = false;
                continue;
            }

            if (skipSettings) continue;

            if (setting is MultiSelectSetting multiSet)
            {
                BindMultiSelectRouting(multiSet);
            }
            else if (setting is SectionSetting secSet)
            {
                foreach (var sectionInstance in secSet.Sections)
                {
                    if (sectionInstance?.ChildSettings == null) continue;
                    foreach (var child in sectionInstance.ChildSettings)
                    {
                        if (child is MultiSelectSetting nestedMulti)
                        {
                            BindMultiSelectRouting(nestedMulti);
                        }
                    }
                }
            }

            setting.Draw(ref y, width);
        }

        MiscDrawing.Seperator(ref y, width, ClientConfig.indent, ClientConfig.spacing, Translate("KeyBind"));

        Event e = Event.current;

#if ANDROID
        bool wasDragging = _isDragging.TryGetValue(mod, out bool isDrag) && isDrag;
        bool isLeftClick = (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && e.button == 0 && !wasDragging;
#else
        bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;
#endif

        mod.KeyBind.Draw(ref y, width);

        float resetBtnW = GUIManager.S(22f);
        float gap = GUIManager.S(6f);
        float elemH = ClientConfig.elementHeight;
        float labelWidth = Mathf.Max(width * 0.40f, width - ClientConfig.indent * 2 - ClientConfig.SettingWidth - resetBtnW - gap);

        // --- Hold Mode Toggle Row ---
        GUI.Label(new Rect(ClientConfig.indent, y, labelWidth, elemH), Translate("Hold Mode"), ThemeManager.SettingLabelStyle);

        Rect holdRect = new(width - ClientConfig.indent - resetBtnW - gap - ClientConfig.SettingWidth, y, ClientConfig.SettingWidth, elemH);
        Rect holdResetRect = new(width - ClientConfig.indent - resetBtnW, y, resetBtnW, elemH);

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

        y += elemH + ClientConfig.spacing;

        // --- Enabled Toggle Row ---
        GUI.Label(new Rect(ClientConfig.indent, y, labelWidth, elemH), Translate("Enabled"), ThemeManager.SettingLabelStyle);

        Rect enabledRect = new(width - ClientConfig.indent - resetBtnW - gap - ClientConfig.SettingWidth, y, ClientConfig.SettingWidth, elemH);
        Rect enabledResetRect = new(width - ClientConfig.indent - resetBtnW, y, resetBtnW, elemH);

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

        y += elemH + ClientConfig.spacing;

        return y - startY;
    }

    private static void BindMultiSelectRouting(MultiSelectSetting multiSet)
    {
        multiSet.OnWindowOpen = () =>
        {
            MultiSelectWindowDrawer.ActiveMultiSelect = multiSet;
            ModuleManager.CurrentWindow = ModuleManager.WindowType.SelectionGUI;
            AnimationHandler.SwitchView(ModuleManager.Group, ModuleManager.CurrentWindow.ToString());
        };
    }

    private static void DrawSettingsScrollbar(float windowWidth, float headerHeight, float viewHeight, float targetContentHeight, float maxViewHeight, float currentScroll, float maxScroll)
    {
        float trackX = windowWidth - GUIManager.S(14f);
        float trackY = headerHeight + GUIManager.S(5f);
        float trackHeight = viewHeight - GUIManager.S(10f);

        float handleHeight = Mathf.Max(GUIManager.S(20f), (maxViewHeight / targetContentHeight) * trackHeight);
        float scrollPct = maxScroll > 0 ? currentScroll / maxScroll : 0f;
        float handleY = trackY + (scrollPct * (trackHeight - handleHeight));

        GUI.Box(new Rect(trackX + GUIManager.S(5f), trackY, GUIManager.S(2f), trackHeight), "", ThemeManager.SeparatorStyle);
        GUI.Box(new Rect(trackX, handleY, GUIManager.S(12f), handleHeight), "", _isDraggingScrollbar ? ThemeManager.CategoryModuleOnStyle : ThemeManager.CategoryModuleOffStyle);
    }
}