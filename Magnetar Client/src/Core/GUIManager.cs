using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
using System;
using System.Linq;
using UnityEngine;
using System.IO;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core
{
    public static class GUIManager
    {
        public static bool isSelectingLanguage = false;

        public static MultiSelectSetting LanguageSetting;
        public static FloatSetting ScaleSetting;
        public static FloatSetting ElementScaleSetting;

        private const float BaseWidth = 500f;
        private const float BaseHeight = 300f;
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

        private static GUI.WindowFunction _cachedLangSelector;
        private static GUI.WindowFunction _cachedGuiControls;
        private static readonly Action _cachedOnClose = OnClose;

        private static GUI.WindowFunction LangSelectorDelegate => _cachedLangSelector ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((System.Action<int>)DrawLanguageSelector);

        private static GUI.WindowFunction GuiControlsDelegate => _cachedGuiControls ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((System.Action<int>)DrawGUIControls);

        public static void OnClose()
        {
            isSelectingLanguage = false;
            UI.WindowDrawing.DrawSetting.activeMultiSelect = null;
        }

        public static void Init()
        {
            LanguageSetting = new MultiSelectSetting("Language")
            {
                MaxSelection = 1,
                Options = new System.Collections.Generic.Dictionary<int, string>(),
                CustomNames = new System.Collections.Generic.Dictionary<int, string>()
            };

            string translationRoot = Path.Combine(SaveLoad.ModsDir, "Magnetar Translation");

            try
            {
                if (!Directory.Exists(translationRoot))
                {
                    Directory.CreateDirectory(translationRoot);
                }
            }
            catch { }

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

            // --- 2. GUI Scale Setting ---
            if (Config.GUIScale <= 0.1f) Config.GUIScale = 1.0f;
            ScaleSetting = new FloatSetting("GUI Scale", 0.5f, 2.0f, Config.GUIScale, decimalPlaces: 2, trueMin: 0.25f, trueMax: 3.0f)
            {
                OnValueChanged = (val) =>
                {
                    Config.GUIScale = val;
                }
            };

            // --- 3. Element Scale Setting ---
            if (Config.ElementScale <= 0.1f) Config.ElementScale = 1.0f;
            ElementScaleSetting = new FloatSetting("Element Scale", 0.5f, 2.0f, Config.ElementScale, decimalPlaces: 2, trueMin: 0.25f, trueMax: 3.0f)
            {
                OnValueChanged = (val) =>
                {
                    Config.ElementScale = val;
                }
            };
        }

        public static void Render()
        {
            Magnetar_Default.Rescale();

            Event e = Event.current;

            #region Handle Escape
            if (isSelectingLanguage && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                OnClose();
                e.Use();
                return;
            }
            #endregion

            Config.RescaleAroundCenter(ref windowRect, Config.S(BaseWidth), windowRect.height);

            float targetSelectorWidth = Config.S(BaseSelectorWidth);
            float maxSelectorHeight = Config.WindowHeight * 0.8f;
            float targetSelectorHeight = Mathf.Min(Config.S(BaseSelectorHeight), maxSelectorHeight);
            Config.RescaleAroundCenter(ref selectorRect, targetSelectorWidth, targetSelectorHeight);

            if (isSelectingLanguage)
            {
                selectorRect = GUI.Window(
                    4001,
                    selectorRect,
                    LangSelectorDelegate,
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

        private static void DrawLanguageSelector(int windowID)
        {
            Event e = Event.current;

            Rect multiSelectRect = new Rect(0, 0, selectorRect.width, selectorRect.height);
            UI.WindowDrawing.DrawSetting.DrawMultiSelectWindow(multiSelectRect, UI.WindowDrawing.DrawSetting.activeMultiSelect, _cachedOnClose);

            float titleHeight = Config.S(34f);
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

            // 1. Language Row
            string currentLangName = "English";
            if (LanguageSetting != null && LanguageSetting.SelectedValues != null && LanguageSetting.SelectedValues.Count > 0)
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
            Rect selectBtnRect = new Rect(w * 0.5f, y, w * 0.45f, elementHeight);

            if (selectBtnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

            if (e.type == EventType.MouseDown && e.button == 0 && selectBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                UI.WindowDrawing.DrawSetting.activeMultiSelect = LanguageSetting;
                UI.WindowDrawing.DrawSetting.multiSelectSearchQuery = "";
                UI.WindowDrawing.DrawSetting.manualScrollY = 0f;

                float targetW = Config.S(BaseSelectorWidth);
                float targetH = Mathf.Min(Config.S(BaseSelectorHeight), Config.WindowHeight * 0.8f);
                selectorRect = new Rect((Config.WindowWidth - targetW) / 2f, (Config.WindowHeight - targetH) / 2f, targetW, targetH);

                isSelectingLanguage = true;
            }

            GUI.Box(selectBtnRect, "Change", Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;
            y += elementHeight + Config.S(10f);

            // 2. GUI Scale Row
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

            // --- 3. Element Scale Row ---
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

            // --- 4. Floating Icon Toggle Row ---
            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight),
                Translator.Translate("Floating Icon"),
                Magnetar_Default.SettingDescriptionStyle);

            Rect floatIconRect = new Rect(w * 0.5f, y, w * 0.45f, elementHeight);
            bool floatIconHover = floatIconRect.Contains(e.mousePosition);

            if (floatIconHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(floatIconRect,
                Config.ShowFloatingIcon ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Config.ShowFloatingIcon ? Magnetar_Default.ModuleOn : Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            if (floatIconHover && e.type == EventType.MouseDown && e.button == 0)
            {
                Config.SetFloatingIcon(!Config.ShowFloatingIcon);
                e.Use();
            }

            y += elementHeight + Config.S(10f);

            // --- 5. Mobile Buttons Toggle Row ---
            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight),
                Translator.Translate("Mobile Close Buttons"),
                Magnetar_Default.SettingDescriptionStyle);

            Rect mobileBtnRect = new Rect(w * 0.5f, y, w * 0.45f, elementHeight);
            bool mobileBtnHover = mobileBtnRect.Contains(e.mousePosition);

            if (mobileBtnHover) GUI.backgroundColor = Magnetar_Default.AccentColor;
            GUI.Box(mobileBtnRect,
                Config.ShowMobileButtons ? Translator.Translate("ON") : Translator.Translate("OFF"),
                Config.ShowMobileButtons ? Magnetar_Default.ModuleOn : Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            if (mobileBtnHover && e.type == EventType.MouseDown && e.button == 0)
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
    }
}