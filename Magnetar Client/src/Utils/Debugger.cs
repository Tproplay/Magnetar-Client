using System;

#if MELONLOADER || RELEASE_MELON
using MelonLoader;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx.Logging;
#endif

namespace Magnetar_Client.Utils
{
    public class CustomLogger
    {
        private string _name;

#if MELONLOADER || RELEASE_MELON
        private MelonLogger.Instance _melonLogger;
#elif BEPINEX || RELEASE_BEPINEX
        private ManualLogSource _bepInExLogger;
#endif

        public CustomLogger(string name)
        {
            _name = name;
#if MELONLOADER || RELEASE_MELON
            _melonLogger = new MelonLogger.Instance(name);
#elif BEPINEX || RELEASE_BEPINEX
            _bepInExLogger = Logger.CreateLogSource(name);
#endif
        }

        public void Msg(string message)
        {
#if MELONLOADER || RELEASE_MELON
            _melonLogger.Msg(message);
#elif BEPINEX || RELEASE_BEPINEX
            _bepInExLogger.LogInfo(message);
#endif
        }

        public void Warning(string message)
        {
#if MELONLOADER || RELEASE_MELON
            _melonLogger.Warning(message);
#elif BEPINEX || RELEASE_BEPINEX
            _bepInExLogger.LogWarning(message);
#endif
        }

        public void Error(string message)
        {
#if MELONLOADER || RELEASE_MELON
            _melonLogger.Error(message);
#elif BEPINEX || RELEASE_BEPINEX
            _bepInExLogger.LogError(message);
#endif
        }
    }

    public static class Magnetar_Logger
    {
        public static CustomLogger TranslatorLogger;
        public static CustomLogger DebugLogger;
        public static CustomLogger AutoSaveLogger;

        public static void Init()
        {
            TranslatorLogger = new CustomLogger("Magnetar Translator");
            DebugLogger = new CustomLogger("Magnetar Debugger");
            AutoSaveLogger = new CustomLogger("Magnetar AutoSave");
        }
    }
}