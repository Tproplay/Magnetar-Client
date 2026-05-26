using MelonLoader;

namespace Magnetar_Client.Utils
{
    public static class Magnetar_Logger
    {
        public static MelonLogger.Instance TranslatorLogger;
        public static MelonLogger.Instance DebugLogger;
        public static MelonLogger.Instance AutoSaveLogger;
        public static void Init()
        {
            TranslatorLogger = new MelonLogger.Instance("Magnetar Translator");
            DebugLogger = new MelonLogger.Instance("Magnetar Dubugger");
            AutoSaveLogger = new MelonLogger.Instance("Magnetar AutoSave");
        }
    }
}
