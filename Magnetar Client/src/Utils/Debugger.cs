using MelonLoader;

namespace Magnetar_Client.Utils
{
    public static class Magnetar_Logger
    {
        public static MelonLogger.Instance TranslatorLogger;
#if DEBUG
        public static MelonLogger.Instance DebugLogger;
        public static MelonLogger.Instance AutoSaveLogger;
#endif
        public static void Init()
        {
            TranslatorLogger = new MelonLogger.Instance("Magnetar Translator");
#if DEBUG
            DebugLogger = new MelonLogger.Instance("Magnetar Dubugger");
            AutoSaveLogger = new MelonLogger.Instance("Magnetar AutoSaveLogger");
#endif
        }
    }
}
