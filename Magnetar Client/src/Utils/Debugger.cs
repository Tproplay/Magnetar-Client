using System;
using System.Runtime.InteropServices; // Required for native Windows API support
#if MELONLOADER || RELEASE_MELON
using MelonLoader;
using MelonLoader.Logging; 
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
        private ColorARGB _loggerColor; 
#elif BEPINEX || RELEASE_BEPINEX
        private ManualLogSource _bepInExLogger;
        private string _ansiColorPrefix;
        private const string AnsiReset = "\x1b[0m";
#endif

#if MELONLOADER || RELEASE_MELON
        public CustomLogger(string name, ColorARGB melonColor)
        {
            _name = name;
            _melonLogger = new MelonLogger.Instance(name, melonColor);
            _loggerColor = melonColor;
        }
#elif BEPINEX || RELEASE_BEPINEX
        public CustomLogger(string name, string bepInAnsiColor)
        {
            _name = name;
            _bepInExLogger = Logger.CreateLogSource(name);
            _ansiColorPrefix = bepInAnsiColor;
        }
#else
        public CustomLogger(string name)
        {
            _name = name;
        }
#endif

        public void Msg(string message)
        {
#if MELONLOADER || RELEASE_MELON
            _melonLogger.Msg(_loggerColor, message);
#elif BEPINEX || RELEASE_BEPINEX
            _bepInExLogger.LogInfo($"{_ansiColorPrefix}{message}{AnsiReset}");
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
        public static CustomLogger DebugModeLogger;

        // --- Native Windows Console API bindings to enable ANSI processing ---
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        public static void Init()
        {
#if BEPINEX || RELEASE_BEPINEX
            // Try to force enable ANSI virtual terminal escape sequences on Windows Console host
            try
            {
                IntPtr stdOutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (GetConsoleMode(stdOutHandle, out uint consoleMode))
                {
                    consoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                    SetConsoleMode(stdOutHandle, consoleMode);
                }
            }
            catch { /* Fail-silent fallback if running outside of standard Win32 environment */ }
#endif

#if MELONLOADER || RELEASE_MELON
            TranslatorLogger = new CustomLogger("Magnetar Translator", ColorARGB.Magenta);
            DebugLogger = new CustomLogger("Magnetar Debugger", ColorARGB.Cyan);
            AutoSaveLogger = new CustomLogger("Magnetar AutoSave", ColorARGB.Lime);
            DebugModeLogger = new CustomLogger("Debug Mode", ColorARGB.Red);
#elif BEPINEX || RELEASE_BEPINEX
            TranslatorLogger = new CustomLogger("Magnetar Translator", "\x1b[35m");
            DebugLogger = new CustomLogger("Magnetar Debugger", "\x1b[36m");
            AutoSaveLogger = new CustomLogger("Magnetar AutoSave", "\x1b[32m");
            DebugModeLogger = new CustomLogger("Debug Mode", "\x1b[31m"); // Fixed the incorrect "\e" pattern to standard "\x1b"
#endif
        }
    }
}