using Magnetar_Client.Modules;
using Magnetar_Client.UI.Themes;
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

        // Base (unscaled, GUIScale == 1) sizes - actual sizes are derived via
        // Config.S() so this window scales with the rest of the UI.
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

        private static GUI.WindowFunction LangSelectorDelegate => _cachedLangSelector ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((System.Action<int>)DrawLanguageSelector);

        private static GUI.WindowFunction GuiControlsDelegate => _cachedGuiControls ??=
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<GUI.WindowFunction>((System.Action<int>)DrawGUIControls);

        public static void Init()
        {
            // --- 1. Language Setting ---
            LanguageSetting = new MultiSelectSetting("Language")
            {
                MaxSelection = 1,
                Options = new System.Collections.Generic.Dictionary<int, string>(),
                CustomNames = new System.Collections.Generic.Dictionary<int, string>()
            };

#if ANDROID
            // Lock to default English on Android without directory scanning or translation switching
            LanguageSetting.AddOption(0, "English");
            LanguageSetting.SelectedValues.Add(0);
            LanguageSetting.IsDisabled = true;
            TranslatorLogger.Msg("Android detected: Language locked to English.");
#else
            string path = Path.Combine(Magnetar_Client.Core.main.ModsDirectory, "Magnetar Translation");

            if (!System.IO.Directory.Exists(path))
            {
                LanguageSetting.AddOption(0, "English");
                LanguageSetting.SelectedValues.Add(0);
            }
            else
            {
                var languages = System.IO.Directory.GetDirectories(path);

                var i = 0;
                foreach (var language in languages)
                {
                    TranslatorLogger.Msg("Found Language: " + language);
                    string _language = Path.GetFileName(language);

                    LanguageSetting.AddOption(i, _language);
                    if (_language == "English") LanguageSetting.SelectedValues.Add(i);
                    i++;
                }
            }
#endif

            // --- 2. GUI Scale Slider Setting ---
            if (Config.GUIScale <= 0.1f)
            {
                Config.GUIScale = 1.0f;
            }

            ScaleSetting = new FloatSetting("GUI Scale", 0.5f, 2.0f, Config.GUIScale, decimalPlaces: 3, trueMin: 0.25f, trueMax: 3.0f)
            {
                OnValueChanged = (val) =>
                {
                    Config.GUIScale = val;
                    SaveLoad.Save(true);
                }
            };
        }

        public static void Render()
        {
            Event e = Event.current;

            #region Handle Escape
            if (isSelectingLanguage && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                isSelectingLanguage = false;
                e.Use();
                return;
            }
            #endregion

            // Keep both windows sized for the current GUIScale, growing or
            // shrinking around wherever they're currently positioned so an
            // open window doesn't jump when the scale changes mid-session.
            Config.RescaleAroundCenter(ref windowRect, Config.S(BaseWidth), windowRect.height);
            Config.RescaleAroundCenter(ref selectorRect, Config.S(BaseSelectorWidth), Config.S(BaseSelectorHeight));

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
#if !ANDROID
            GUI.DragWindow(new Rect(0, 0, selectorRect.width, Config.S(25f)));
#endif
            Event e = Event.current;

            float startY = Config.S(35f) + elementHeight + Config.S(10f);
            Rect multiSelectRect = new Rect(0, startY, selectorRect.width, selectorRect.height);

            UI.WindowDrawing.DrawSetting.DrawMultiSelectWindow(multiSelectRect, UI.WindowDrawing.DrawSetting.activeMultiSelect);

            if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
#if !ANDROID
                Input.ResetInputAxes();
#endif
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

            if (LanguageSetting != null && LanguageSetting.SelectedValues != null && LanguageSetting.SelectedValues.Count > 0)
            {
                int selectedId = LanguageSetting.SelectedValues.First();

                if (LanguageSetting.Options.ContainsKey(selectedId))
                {
                    currentLangName = LanguageSetting.Options[selectedId];
                }
            }

            if (Config.Language != currentLangName)
            {
                Config.Language = currentLangName;

                TranslatorLogger.Msg($"Language changed to {Config.Language}. Reloading translations...");
                Translator.LoadTranslations();
                Translator.DumpMissingStrings();

                if (ModuleManager.Modules != null)
                {
                    foreach (var mod in ModuleManager.Modules)
                    {
                        mod.OnLanguageChanged();
                    }
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

                selectorRect.x = (Config.WindowWidth - Config.S(BaseSelectorWidth)) / 2;
                selectorRect.y = (Config.WindowHeight - Config.S(BaseSelectorHeight)) / 2;

                isSelectingLanguage = true;
            }

            GUI.Box(selectBtnRect, "Change", Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            y += elementHeight + Config.S(10f);

            // --- 2. GUI Scale Row ---
            if (ScaleSetting != null)
            {
                // Synchronize if loaded from config externally while not actively dragging
                if (UI.WindowDrawing.DrawSetting.activeSliderId != ScaleSetting.GetHashCode() &&
                    Mathf.Abs(ScaleSetting.Value - Config.GUIScale) > 0.001f)
                {
                    ScaleSetting.Value = Config.GUIScale;
                }

                UI.WindowDrawing.DrawSetting.HandleNumericSetting(ScaleSetting, ref y, w, true);
                y += elementHeight + Config.S(10f);
            }

            if (UI.WindowDrawing.DrawSetting.OnPostDraw != null)
            {
                UI.WindowDrawing.DrawSetting.OnPostDraw.Invoke();
                UI.WindowDrawing.DrawSetting.OnPostDraw = null;
            }

            windowRect.height = y;

#if !ANDROID
            GUI.DragWindow(new Rect(0, 0, w, Config.S(25f)));
#endif

            Rect _windowRect = new Rect(0, 0, w, y);
            if (_windowRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
#if !ANDROID
                Input.ResetInputAxes();
#endif
                e.Use();
            }
        }
    }
}