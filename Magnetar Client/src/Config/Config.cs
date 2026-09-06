#if MELONLOADER || RELEASE_MELON
using MelonLoader;
using MelonLoader.Utils;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx.Configuration;
#endif

using UnityEngine;

namespace Magnetar_Client
{
    public enum TabType
    {
        MODULES,
        HUD,
        GUI,
        NEF,
        PROFILE,
    }

    public static class Magnetar_Info
    {
        public const string ModName = "Magnetar Client";
        public const string Version = "3.9.0";
        public const string Developer = "Tproplay";
    }

    public static class Config
    {
        public static string CurrentProfile = "Default";

        // Native canvas size used by the outer letterbox matrix (main.cs).
        // NOT scaled by GUIScale - it's the fixed reference resolution
        // everything else is authored against.
        public readonly static float WindowWidth = 1920;
        public readonly static float WindowHeight = 1080;

        public static float GUIScale = 1f;
        public static float S(float value) => value * GUIScale;

        public static bool showgui = true;
        public static bool dimBg = false;
        public static TabType CurrentTab = TabType.MODULES;

        public static float MinTimeBetweenSaves = 120;

        // --- Scaled UI sizes ---------------------------------------------
        // These are exposed as their original (unscaled) "base"/1x sizes via
        // the Base* fields, and every public property below returns that
        // base size run through S(), so every consumer (ModuleManager,
        // DrawSetting, etc.) automatically scales with Config.GUIScale
        // without needing to call S() itself.

        private const float BaseModuleWindowWidth = 200f;
        public static float ModuleWindowWidth => S(BaseModuleWindowWidth);

        private const float BaseElementHeight = 22f;
        public static float elementHeight => S(BaseElementHeight);

        private const float BaseIndent = 10f;
        public static float indent => S(BaseIndent);

        private const float BaseSpacing = 6f;
        public static float spacing => S(BaseSpacing);

        private const float BaseSelectButtonWidth = 70f;
        public static float selectButtonWidth => S(BaseSelectButtonWidth);

        public static string Language = "English";

        private static float _baseSettingWidth = 260f;
        public static float SettingWidth
        {
            get => S(_baseSettingWidth);
            set => _baseSettingWidth = value;
        }

        public static class ModuleManager
        {
            private static float _baseSettingsWidth = 630f;
            public static float SettingsWidth
            {
                get => S(_baseSettingsWidth);
                set => _baseSettingsWidth = value;
            }

            public static float PopupSpeed = 10f;

            // Search Window
            public static float SearchAnimationSpeed = 15f;
            public static float SearchWidthMultiplier = 1.5f;

            // Settings Window
            public static float MaxSettingsWindowHeightPct = 0.8f;
            public static float SettingsScrollLerpSpeed = 15f;

            private static float _baseScrollSensitivity = 25f;
            public static float ScrollSensitivity
            {
                get => S(_baseScrollSensitivity);
                set => _baseScrollSensitivity = value;
            }

            // Multi-Select Window
            private static float _baseMultiSelectWindowWidth = 500f;
            public static float MultiSelectWindowWidth
            {
                get => S(_baseMultiSelectWindowWidth);
                set => _baseMultiSelectWindowWidth = value;
            }

            private static float _baseMultiSelectWindowHeight = 800f;
            public static float MultiSelectWindowHeight
            {
                get => S(_baseMultiSelectWindowHeight);
                set => _baseMultiSelectWindowHeight = value;
            }
        }

        public static class SettingsInput
        {
            // Numeric Sliders
            private static float _baseNumericInputWidth = 75f;
            public static float NumericInputWidth
            {
                get => S(_baseNumericInputWidth);
                set => _baseNumericInputWidth = value;
            }

            // Fraction of the slider's own value range moved per scroll tick -
            // not a pixel size, so it does not scale with GUIScale.
            public static float SliderScrollStep = 0.04f;

            private static float _baseSliderHeight = 8f;
            public static float SliderHeight
            {
                get => S(_baseSliderHeight);
                set => _baseSliderHeight = value;
            }

            private static int _baseSliderThumbFontSize = 40;
            public static int SliderThumbFontSize
            {
                get => Mathf.Max(1, Mathf.RoundToInt(S(_baseSliderThumbFontSize)));
                set => _baseSliderThumbFontSize = value;
            }

            // Multi-Select Window
            private static float _baseMultiSelectRowHeight = 22f;
            public static float MultiSelectRowHeight
            {
                get => S(_baseMultiSelectRowHeight);
                set => _baseMultiSelectRowHeight = value;
            }

            private static float _baseMultiSelectHeaderHeight = 65f;
            public static float MultiSelectHeaderHeight
            {
                get => S(_baseMultiSelectHeaderHeight);
                set => _baseMultiSelectHeaderHeight = value;
            }

            // Dropdowns (Select Setting)
            private static float _baseDropdownRowHeight = 22f;
            public static float DropdownRowHeight
            {
                get => S(_baseDropdownRowHeight);
                set => _baseDropdownRowHeight = value;
            }

            // Row count, not a size - does not scale.
            public static int DropdownMaxVisibleRows = 6;

            private static float _baseDropdownScrollSensitivity = 15f;
            public static float DropdownScrollSensitivity
            {
                get => S(_baseDropdownScrollSensitivity);
                set => _baseDropdownScrollSensitivity = value;
            }

            // Text Fields & Autocomplete
            // History depth, not a size - does not scale.
            public static int TextFieldUndoLimit = 200;

            private static float _baseAutocompleteRowHeight = 22f;
            public static float AutocompleteRowHeight
            {
                get => S(_baseAutocompleteRowHeight);
                set => _baseAutocompleteRowHeight = value;
            }

            private static float _baseAutocompleteMaxHeight = 150f;
            public static float AutocompleteMaxHeight
            {
                get => S(_baseAutocompleteMaxHeight);
                set => _baseAutocompleteMaxHeight = value;
            }

            private static float _baseAutocompleteScrollSensitivity = 15f;
            public static float AutocompleteScrollSensitivity
            {
                get => S(_baseAutocompleteScrollSensitivity);
                set => _baseAutocompleteScrollSensitivity = value;
            }
        }

        /// <summary>
        /// Resizes a Rect to the given width/height while keeping its current
        /// center point fixed. Used for draggable windows so they grow/shrink
        /// around wherever the user last dragged them when GUIScale changes,
        /// instead of always resetting to a default position.
        /// </summary>
        public static void RescaleAroundCenter(ref Rect rect, float newWidth, float newHeight)
        {
            float centerX = rect.x + rect.width / 2f;
            float centerY = rect.y + rect.height / 2f;

            rect.width = newWidth;
            rect.height = newHeight;
            rect.x = centerX - newWidth / 2f;
            rect.y = centerY - newHeight / 2f;
        }
    }

    public static class Prefrences
    {
#if MELONLOADER || RELEASE_MELON
        public static MelonPreferences_Category MagnetarCategory;
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
        public static ConfigFile BepInExConfig;
#endif
    }
}