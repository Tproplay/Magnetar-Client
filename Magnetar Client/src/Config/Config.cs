
namespace Magnetar_Client
{
    public enum TabType
    {
        MODULES,
        HUD
    }

    public static class Config
    {
        public readonly static float WindowWidth = 1920;
        public readonly static float WindowHeight = 1080;

        public static bool showgui = true;
        public static bool dimBg = false;
        public static TabType CurrentTab = TabType.MODULES;


        public readonly static float ModuleWindowWidth = 200f;
        public readonly static float elementHeight = 22;
        public readonly static float indent = 10;
        public readonly static float spacing = 6;

        public readonly static float selectButtonWidth = 70;
    }
}
