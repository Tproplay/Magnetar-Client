namespace Magnetar_Client
{
    public enum TabType
    {
        MODULES,
        HUD,
        GUI,
        NEF
    }

    public static class Magnetar_Info
    {
        public const string ModName = "Magnetar Client";
        public const string Version = "3.7.2";
        public const string Developer = "Tproplay";
    }

    public static class Config
    {
        public readonly static float WindowWidth = 1920;
        public readonly static float WindowHeight = 1080;

        public static bool showgui = true;
        public static bool dimBg = false;
        public static TabType CurrentTab = TabType.MODULES;

        public static float MinTimeBetweenSaves = 120;

        public readonly static float ModuleWindowWidth = 200f;
        public readonly static float elementHeight = 22;
        public readonly static float indent = 10;
        public readonly static float spacing = 6;

        public readonly static float selectButtonWidth = 70;

        public static string Language = "English";

        public static float SettingWidth = 260;

        public static class ModuleManager
        {
            public static float SettingsWidth = 630f;
            public static float PopupSpeed = 10f;

            // Search Window
            public static float SearchAnimationSpeed = 15f;
            public static float SearchWidthMultiplier = 1.5f;

            // Settings Window
            public static float MaxSettingsWindowHeightPct = 0.8f;
            public static float SettingsScrollLerpSpeed = 15f;
            public static float ScrollSensitivity = 25f;

            // Multi-Select Window
            public static float MultiSelectWindowWidth = 500f;
            public static float MultiSelectWindowHeight = 800f;
        }

        public static class SettingsInput
        {
            // Numeric Sliders
            public static float NumericInputWidth = 75f;
            public static float SliderScrollStep = 0.04f;
            public static float SliderHeight = 8f;
            public static int SliderThumbFontSize = 40;

            // Multi-Select Window
            public static float MultiSelectRowHeight = 22f;
            public static float MultiSelectHeaderHeight = 65f;

            // Dropdowns (Select Setting)
            public static float DropdownRowHeight = 22f;
            public static int DropdownMaxVisibleRows = 6;
            public static float DropdownScrollSensitivity = 15f;

            // Text Fields & Autocomplete
            public static int TextFieldUndoLimit = 200;
            public static float AutocompleteRowHeight = 22f;
            public static float AutocompleteMaxHeight = 150f;
            public static float AutocompleteScrollSensitivity = 15f;
        }
    }
}