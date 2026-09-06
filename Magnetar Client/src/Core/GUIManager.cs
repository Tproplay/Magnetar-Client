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

        static float width = 500;
        public static float elementHeight = 25f;

        public static Rect windowRect = new Rect((Config.WindowWidth - width) / 2, (Config.WindowHeight - 300) / 2, width, 300);

        static float selectorWidth = 500;
        static float selectorHeight = 800;
        public static Rect selectorRect = new Rect((Config.WindowWidth - selectorWidth) / 2, (Config.WindowHeight - selectorHeight) / 2, selectorWidth, selectorHeight);

        public static void Init()
        {
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
                return;
            }

            var languages = System.IO.Directory.GetDirectories(path);

            var i = 0;
            foreach (var language in languages)
            {
                TranslatorLogger.Msg("Found Language: " + language);
                string _language = Path.GetFileName(language);

                LanguageSetting.AddOption(i, _language);
                // Default to english
                if (_language == "English") LanguageSetting.SelectedValues.Add(i);
                i++;
            }
#endif
        }

        public static void Render()
        {
            Event e = Event.current;

            #region Handle Escape
            // Close the Language Selector Window
            if (isSelectingLanguage && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                isSelectingLanguage = false;
                e.Use();
                return;
            }
            #endregion

            if (isSelectingLanguage)
            {
                selectorRect = GUI.Window(
                    4001,
                    selectorRect,
                    (GUI.WindowFunction)DrawLanguageSelector,
                    "",
                    Magnetar_Default.ModuleWindow
                );
            }
            else
            {
                windowRect = GUI.Window(
                    4000,
                    windowRect,
                    (GUI.WindowFunction)DrawGUIControls,
                    "",
                    Magnetar_Default.ModuleWindow
                );
            }
        }

        private static void DrawLanguageSelector(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, selectorRect.width, 25));
            Event e = Event.current;

            float startY = 35 + elementHeight + 10;
            Rect multiSelectRect = new Rect(0, startY, selectorRect.width, selectorRect.height);

            UI.WindowDrawing.DrawSetting.DrawMultiSelectWindow(multiSelectRect, UI.WindowDrawing.DrawSetting.activeMultiSelect);

            if (multiSelectRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
                e = null;
            }
        }

        private static void DrawGUIControls(int windowID)
        {
            float w = windowRect.width;
            float indent = 10;
            Event e = Event.current;
            float y = 35;

            Rect headerBgRect = new Rect(0, 0, w, y-indent);
            GUI.Box(headerBgRect, Translator.Translate("GUI Configuration"), Magnetar_Default.SettingsWindow);

            

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

                Magnetar_Client.Utils.Magnetar_Logger.TranslatorLogger.Msg($"Language changed to {Config.Language}. Reloading translations...");
                Magnetar_Client.Utils.Translator.LoadTranslations();
                Magnetar_Client.Utils.Translator.DumpMissingStrings();
                

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

            GUI.Label(new Rect(indent, y, w * 0.45f, elementHeight), $"Language: <color=yellow>{currentLangName}</color>",Magnetar_Default.SettingDescriptionStyle);

            Rect selectBtnRect = new Rect(w * 0.5f, y, w * 0.45f, elementHeight);

            if (selectBtnRect.Contains(e.mousePosition))
                GUI.backgroundColor = Magnetar_Default.AccentColor;

            if (e.type == EventType.MouseDown && e.button == 0 && selectBtnRect.Contains(e.mousePosition))
            {
                e.Use();
                UI.WindowDrawing.DrawSetting.activeMultiSelect = LanguageSetting;
                UI.WindowDrawing.DrawSetting.multiSelectSearchQuery = "";
                UI.WindowDrawing.DrawSetting.manualScrollY = 0f;

                // Re-center the selector window
                selectorRect.x = (Config.WindowWidth - selectorWidth) / 2;
                selectorRect.y = (Config.WindowHeight - selectorHeight) / 2;

                isSelectingLanguage = true;
            }

            GUI.Box(selectBtnRect, "Change", Magnetar_Default.SettingOff);
            GUI.backgroundColor = Color.white;

            y += elementHeight + 10;

            windowRect.height = y;

            GUI.DragWindow(new Rect(0, 0, w, 25));

            Rect _windowRect = new Rect(0, 0, w, y);
            if (_windowRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
                e = null;
            }
        }
    }
}