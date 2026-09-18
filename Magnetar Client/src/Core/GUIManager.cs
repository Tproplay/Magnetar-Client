using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using System;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core
{
    public static class GUIManager
    {
        public static bool isSelectingSubWindow = false;

        public static MultiSelectSetting LanguageSetting;
        public static MultiSelectSetting ThemeSetting;
        public static FloatSetting ScaleSetting;
        public static FloatSetting ElementScaleSetting;

        private const float BaseWidth = 500f;
        private const float BaseHeight = 340f;
        private const float BaseElementHeight = 25f;
        private const float BaseSelectorWidth = 500f;
        private const float BaseSelectorHeight = 800f;

        public static float elementHeight => Config.S(BaseElementHeight);

        public static Rect windowRect = new Rect(
            (Config.WindowWidth - Config.S(BaseWidth)) / 2,
            (Config.WindowHeight - Config.S(BaseHeight)) / 2,
            Config.S(BaseWidth),
            Config.S(BaseHeight));

        public static Rect selectorRect = new Rect(
            (Config.WindowWidth - Config.S(BaseSelectorWidth)) / 2,
            (Config.WindowHeight - Config.S(BaseSelectorHeight)) / 2,
            Config.S(BaseSelectorWidth),
            Config.S(BaseSelectorHeight));

        private static GUI.WindowFunction _cachedSelector;
        private static GUI.WindowFunction _cachedGuiControls;
        private static readonly Action _cachedOnClose = OnClose;

        private static GUI.WindowFunction SelectorDelegate => _cachedSelector ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((System.Action<int>)DrawSelectorModal);

        private static GUI.WindowFunction GuiControlsDelegate => _cachedGuiControls ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((System.Action<int>)DrawGUIControls);

        public static void OnClose()
        {
            isSelectingSubWindow = false;
            UI.WindowDrawing.DrawSetting.activeMultiSelect = null;
        }

        public static void Init()
        {
            // --- 1. Language Setting ---
            LanguageSetting = new MultiSelectSetting("Language")
            {
                MaxSelection = 1,
                Options = new Dictionary<int, string>(),
                CustomNames = new Dictionary<int, string>()
            };

            string translationRoot = Path.Combine(SaveLoad.ModsDir, "Magnetar Translation");
            try { if (!Directory.Exists(translationRoot)) Directory.CreateDirectory(translationRoot); } catch { }

            LanguageSetting.AddOption(0, "English");

            if (Directory.Exists(translationRoot))
            {
                try
                {
                    var languageDirs = Directory.GetDirectories(translationRoot);
                    int idx = 1;
                    int activeIndex = 0;

                    foreach (var dir in languageDirs)
                    {
                        string langName = Path.GetFileName(dir);
                        if (string.Equals(langName, "English", StringComparison.OrdinalIgnoreCase)) continue;

                        LanguageSetting.AddOption(idx, langName);
                        if (string.Equals(Config.Language, langName, StringComparison.OrdinalIgnoreCase))
                        {
                            activeIndex = idx;
                        }
                        idx++;
                    }
                    LanguageSetting.SelectedValues.Add(activeIndex);
                }
                catch (Exception ex)
                {
                    TranslatorLogger.Error($"[GUIManager] Error reading translation directories: {ex.Message}");
                    LanguageSetting.SelectedValues.Add(0);
                }
            }
            else
            {
                LanguageSetting.SelectedValues.Add(0);
            }

            // --- 2. Theme Setting Initialization ---
            ThemeSetting = new MultiSelectSetting("Theme")
            {
                MaxSelection = 1,
                Options = new Dictionary<int, string>(),
                CustomNames = new Dictionary<int, string>()
            };

            RefreshThemeOptions();

            // --- 3. GUI Scale Setting ---
            if (Config.GUIScale <= 0.1f) Config.GUIScale = 1.0f;
            ScaleSetting = new FloatSetting("GUI Scale", 0.5f, 2.0f, Config.GUIScale, decimalPlaces: 2, trueMin: 0.25f, trueMax: 3.0f)
            {
                OnValueChanged = (val) => Config.GUIScale = val
            };

            // --- 4. Element Scale Setting ---
            if (Config.ElementScale <= 0.1f) Config.ElementScale = 1.0f;
            ElementScaleSetting = new FloatSetting("Element Scale", 0.5f, 2.0f, Config.ElementScale, decimalPlaces: 2, trueMin: 0.25f, trueMax: 3.0f)
            {
                OnValueChanged = (val) => Config.ElementScale = val
            };
        }

        public static void RefreshThemeOptions()
        {
            if (ThemeSetting == null) return;

            // Ensure JSON themes are loaded
            if (Magnetar_Default.LoadedThemes == null || Magnetar_Default.LoadedThemes.Count == 0)
            {
                Magnetar_Default.LoadThemesFromJson();
            }

            ThemeSetting.Options.Clear();
            ThemeSetting.SelectedValues.Clear();

            int tIdx = 0;
            int activeThemeIdx = 0;

            foreach (var kvp in Magnetar_Default.LoadedThemes)
            {
                ThemeSetting.AddOption(tIdx, kvp.Key);
                if (string.Equals(Config.Theme, kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    activeThemeIdx = tIdx;
                }
                tIdx++;
            }

            // Fallback safety if no themes matched
            if (ThemeSetting.Options.Count == 0)
            {
                ThemeSetting.AddOption(0, Magnetar_Default.InternalDefaultTheme.Name);
                activeThemeIdx = 0;
            }

            ThemeSetting.SelectedValues.Add(activeThemeIdx);
        }

        public static void Render()
        {
            Magnetar_Default.Rescale();

            Event e = Event.current;

            if (isSelectingSubWindow && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                OnClose();
                e.Use();
                return;
            }

            Config.RescaleAroundCenter(ref windowRect, Config.S(BaseWidth), windowRect.height);

            float targetSelectorWidth = Config.S(BaseSelectorWidth);
            float maxSelectorHeight = Config.WindowHeight * 0.8f;
            float targetSelectorHeight = Mathf.Min(Config.S(BaseSelectorHeight), maxSelectorHeight);
            Config.RescaleAroundCenter(ref selectorRect, targetSelectorWidth, targetSelectorHeight);

            if (isSelectingSubWindow)
            {
                selectorRect = GUI.Window(
                    4001,
                    selectorRect,
                    SelectorDelegate,
                    "",
                    Magnetar_Default.ModuleWindow
                );
            }
            else
            {
                windowRect = GUI.Window(
                    4000,
                    windowRect,
                    GuiControlsDelegate,
                    "",
                    Magnetar_Default.ModuleWindow
                );
            }
        }

        private static void DrawSelectorModal(int windowID)
        {
            Event e = Event.current;
            Rect multiSelectRect = new Rect(0, 0, selectorRect.width, selectorRect.height);
            UI.WindowDrawing.DrawSetting.DrawMultiSelectWindow(multiSelectRect, UI.WindowDrawing.DrawSetting.activeMultiSelect, _cachedOnClose);

#if ANDROID
            float titleHeight = Config.S(25f) * 1.30f;
#else
            float titleHeight = Config.S(25f);
#endif
            float dragSafeMargin = Config.ShowMobileButtons ? Config.S(35f) : 0f;
            GUI.DragWindow(new Rect(0, 0, selectorRect.width - dragSafeMargin, titleHeight));

            if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
            }
        }

        private static void DrawGUIControls(int windowID)
        {
            float w = windowRect.width;
            float indent = Config.S(10f);
            Event e = Event.current;
            float y = Config.S(35f);

            Rect headerBgRect = new Rect(0, 0, w, y - indent);
            GUI.Box(headerBgRect, Translator.Translate("GUI Configuration"), Magnetar_Default.SettingsWindow);

            // --- 1. Language Row ---
            string currentLangName = "English";
            if (LanguageSetting?.SelectedValues != null && LanguageSetting.SelectedValues.Count > 0)
            {
                int selectedId = LanguageSetting.SelectedValues.First();
                if (LanguageSetting.Options.ContainsKey(selectedId))
                    currentLangName = LanguageSetting.Options[selectedId];
            }

            if (Config.Language != currentLangName)
            {
                Config.Language = currentLangName;
                Translator.LoadTranslations();
                Translator.DumpMissingStrings();

                if (ModuleManager.Modules != null)
                {
                    foreach (var mod in ModuleManager.Modules)
                        mod.OnLanguageChanged();
                }

                HUDManager.OnLanguageChange();
                Magnetar_Client.NEF.NEFData.OnLanguageChanged();
            }

            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight), $"Language: <color=yellow>{currentLangName}</color>", Magnetar_Default.SettingDescriptionStyle);
            Rect langBtnRect = new Rect(w * 0.5f, y, w * 0.45f, elementHeight);

            if (langBtnRect.Contains(e.mousePosition)) GUI.backgroundColor = Magnetar_Default.AccentColor;
            if (e.type == EventType.MouseDown && e.button == 0 && langBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                OpenSubSelector(LanguageSetting);
            }
            GUI.Box(langBtnRect, "Change", Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;
            y += elementHeight + Config.S(10f);

            // --- 2. Theme Row ---
            string currentTheme = Magnetar_Default.CurrentThemeName;
            if (ThemeSetting?.SelectedValues != null && ThemeSetting.SelectedValues.Count > 0)
            {
                int selThemeId = ThemeSetting.SelectedValues.First();
                if (ThemeSetting.Options.ContainsKey(selThemeId))
                {
                    currentTheme = ThemeSetting.Options[selThemeId];
                }
            }

            if (Config.Theme != currentTheme)
            {
                Magnetar_Default.ApplyTheme(currentTheme);
            }

            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight), $"Theme: <color=yellow>{currentTheme}</color>", Magnetar_Default.SettingDescriptionStyle);
            Rect themeBtnRect = new Rect(w * 0.5f, y, w * 0.45f, elementHeight);

            if (themeBtnRect.Contains(e.mousePosition)) GUI.backgroundColor = Magnetar_Default.AccentColor;
            if (e.type == EventType.MouseDown && e.button == 0 && themeBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                RefreshThemeOptions();
                OpenSubSelector(ThemeSetting);
            }
            GUI.Box(themeBtnRect, "Change", Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;
            y += elementHeight + Config.S(10f);

            // --- 3. GUI Scale Row ---
            if (ScaleSetting != null)
            {
                if (UI.WindowDrawing.DrawSetting.activeSliderId != ScaleSetting.GetHashCode() &&
                    Mathf.Abs(ScaleSetting.Value - Config.GUIScale) > 0.001f)
                {
                    ScaleSetting.Value = Config.GUIScale;
                }
                UI.WindowDrawing.DrawSetting.HandleNumericSetting(ScaleSetting, ref y, w, true);
                y += elementHeight + Config.S(10f);
            }

            // --- 4. Element Scale Row ---
            if (ElementScaleSetting != null)
            {
                if (UI.WindowDrawing.DrawSetting.activeSliderId != ElementScaleSetting.GetHashCode() &&
                    Mathf.Abs(ElementScaleSetting.Value - Config.ElementScale) > 0.001f)
                {
                    ElementScaleSetting.Value = Config.ElementScale;
                }
                UI.WindowDrawing.DrawSetting.HandleNumericSetting(ElementScaleSetting, ref y, w, true);
                y += elementHeight + Config.S(10f);
            }

            // --- 5. Floating Icon Toggle Row ---
            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight), Translator.Translate("Floating Icon"), Magnetar_Default.SettingDescriptionStyle);
            Rect floatIconRect = new Rect(w * 0.5f, y, w * 0.45f, elementHeight);

            if (floatIconRect.Contains(e.mousePosition)) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(floatIconRect,
                Config.ShowFloatingIcon ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Config.ShowFloatingIcon ? Magnetar_Default.ModuleOn : Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            if (floatIconRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                Config.SetFloatingIcon(!Config.ShowFloatingIcon);
                e.Use();
            }
            y += elementHeight + Config.S(10f);

            // --- 6. Mobile Buttons Toggle Row ---
            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight), Translator.Translate("Mobile Close Buttons"), Magnetar_Default.SettingDescriptionStyle);
            Rect mobileBtnRect = new Rect(w * 0.5f, y, w * 0.45f, elementHeight);

            if (mobileBtnRect.Contains(e.mousePosition)) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(mobileBtnRect,
                Config.ShowMobileButtons ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Config.ShowMobileButtons ? Magnetar_Default.ModuleOn : Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            if (mobileBtnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                Config.ShowMobileButtons = !Config.ShowMobileButtons;
                e.Use();
            }
            y += elementHeight + Config.S(10f);

            if (UI.WindowDrawing.DrawSetting.OnPostDraw != null)
            {
                UI.WindowDrawing.DrawSetting.OnPostDraw.Invoke();
                UI.WindowDrawing.DrawSetting.OnPostDraw = null;
            }

            windowRect.height = y;
            GUI.DragWindow(new Rect(0, 0, w, Config.S(25f)));

            Rect _windowRect = new Rect(0, 0, w, y);
            if (_windowRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
            }
        }

        private static void OpenSubSelector(MultiSelectSetting setting)
        {
            UI.WindowDrawing.DrawSetting.activeMultiSelect = setting;
            UI.WindowDrawing.DrawSetting.multiSelectSearchQuery = "";
            UI.WindowDrawing.DrawSetting.manualScrollY = 0f;

            float targetW = Config.S(BaseSelectorWidth);
            float targetH = Mathf.Min(Config.S(BaseSelectorHeight), Config.WindowHeight * 0.8f);
            selectorRect = new Rect((Config.WindowWidth - targetW) / 2f, (Config.WindowHeight - targetH) / 2f, targetW, targetH);

            isSelectingSubWindow = true;
        }
    }
}