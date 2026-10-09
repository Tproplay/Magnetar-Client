using Magnetar_Client.Modules;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.Core.ModuleManager_;

public static class SettingsWindowDrawer
{
    private static readonly Dictionary<Modules.Module, Rect> _settingsPositions = new();
    private static readonly Dictionary<Modules.Module, Vector2> _settingsScrollPositions = new();
    private static readonly Dictionary<Modules.Module, float> _moduleContentHeights = new();
    private static readonly Dictionary<Modules.Module, float> _targetContentHeights = new();
    private static readonly Dictionary<Modules.Module, GUI.WindowFunction> _cachedSettingsDelegates = new();

    // Cache the active module while it fades out to prevent instant popping
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
        float headerHeight = Config.S(26f);
#endif
        float maxWindowHeight = Config.NativeHeight * Config.ModuleManager.MaxSettingsWindowHeightPct;
        float maxViewHeight = maxWindowHeight - headerHeight;

        if (!_moduleContentHeights.ContainsKey(mod)) _moduleContentHeights[mod] = 0f;
        if (!_targetContentHeights.ContainsKey(mod)) _targetContentHeights[mod] = 0f;
        if (!_settingsScrollPositions.ContainsKey(mod)) _settingsScrollPositions[mod] = Vector2.zero;

        // Header Banner using SettingsWndowStyle
        Rect headerBgRect = new(0, 0, windowWidth, headerHeight);
        GUI.Box(headerBgRect, ModuleManager.Domain.Translate(mod.Name), ThemeManager.SettingsWndowStyle);

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
            float btnX = windowWidth - Config.S(26f);
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

        GUI.color = prevColor;
        GUI.contentColor = prevContentColor;
}

    private static float CalculateTargetWidth(Modules.Module mod)
    {
        float maxNameWidth = 0f;
        if (mod.Settings != null)
        {
            foreach (var setting in mod.Settings)
            {
                if (setting == null || string.IsNullOrEmpty(setting.Name)) continue;
                float w = ThemeManager.SettingLabelStyle.CalcSize(new GUIContent(ModuleManager.Domain.Translate(setting.Name))).x;
                if (w > maxNameWidth) maxNameWidth = w;
            }
        }

        string[] builtIns = { "Hold Mode", "Enabled", "KeyBind" };
        foreach (var b in builtIns)
        {
            float w = ThemeManager.SettingLabelStyle.CalcSize(new GUIContent(ModuleManager.Domain.Translate(b))).x;
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
        string translatedDescription = ModuleManager.Domain.Translate(mod.Description);

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
                catSet.IsExpanded = MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, ModuleManager.Domain.Translate(catSet.Name), true, catSet.IsExpanded);
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

            // Wire up ModuleManager's window routing decoupled from the setting class itself
            if (setting is MultiSelectSetting multiSet)
            {
                multiSet.OnWindowOpen = () =>
                {
                    MultiSelectWindowDrawer.ActiveMultiSelect = multiSet;
                    ModuleManager.CurrentWindow = ModuleManager.WindowType.SelectionGUI;
                    AnimationHandler.SwitchView(ModuleManager.Group, ModuleManager.CurrentWindow.ToString());
                };
            }

            // Direct polymorphic draw call
            setting.Draw(ref y, width);
        }

        MiscDrawing.Seperator(ref y, width, Config.indent, Config.spacing, ModuleManager.Domain.Translate("KeyBind"));

        Event e = Event.current;
        bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

        // 1. Draw Keybind
        mod.KeyBind.Draw(ref y, width);

        float resetBtnW = Config.S(22f);
        float gap = Config.S(6f);
        float elemH = Config.elementHeight;
        float labelWidth = Mathf.Max(width * 0.40f, width - Config.indent * 2 - Config.SettingWidth - resetBtnW - gap);

        // --- 2. Hold Mode Toggle Row ---
        GUI.Label(new Rect(Config.indent, y, labelWidth, elemH), ModuleManager.Domain.Translate("Hold Mode"), ThemeManager.SettingLabelStyle);

        Rect holdRect = new(width - Config.indent - resetBtnW - gap - Config.SettingWidth, y, Config.SettingWidth, elemH);
        Rect holdResetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, elemH);

        GUI.Box(holdRect, mod.HoldMode ? ModuleManager.Domain.Translate("ON") : ModuleManager.Domain.Translate("OFF"), mod.HoldMode ? ThemeManager.SettingOn : ThemeManager.SettingOff);
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
        GUI.Label(new Rect(Config.indent, y, labelWidth, elemH), ModuleManager.Domain.Translate("Enabled"), ThemeManager.SettingLabelStyle);

        Rect enabledRect = new(width - Config.indent - resetBtnW - gap - Config.SettingWidth, y, Config.SettingWidth, elemH);
        Rect enabledResetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, elemH);

        GUI.Box(enabledRect, mod.Active ? ModuleManager.Domain.Translate("ON") : ModuleManager.Domain.Translate("OFF"), mod.Active ? ThemeManager.SettingOn : ThemeManager.SettingOff);
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
