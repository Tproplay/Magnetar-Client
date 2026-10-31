
using System;
using System.Reflection;
using UnityEngine;

#if MELONLOADER || MELON_LOCAL
using Il2CppTMPro;
using MelonLoader;
#elif BEPINEX || BEPINEX_LOCAL
using TMPro;
#endif

namespace MagnetarAIAddon;

public static class TranslationHelper
{
    private static bool _checked = false;
    private static bool _isTranslatorAvailable = false;

    private static Type _stringStoreType;
    private static Type _fontStoreType;
    private static Type _utilsType;

    private static MethodInfo _translateTextMethod;
    private static MethodInfo _loadTMPFontMethod;
    private static PropertyInfo _languageProp;

    public static bool IsTranslatorAvailable
    {
        get
        {
            if (!_checked) EnsureInitialized();
            return _isTranslatorAvailable;
        }
    }

    private static void EnsureInitialized()
    {
        _checked = true;
        try
        {
            Assembly translatorAssembly = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name.Equals("PvZ_Fusion_Translator", StringComparison.OrdinalIgnoreCase))
                {
                    translatorAssembly = asm;
                    break;
                }
            }

            if (translatorAssembly == null)
            {
                _isTranslatorAvailable = false;
                return;
            }

            _stringStoreType = translatorAssembly.GetType("PvZ_Fusion_Translator.AssetStore.StringStore");
            _fontStoreType = translatorAssembly.GetType("PvZ_Fusion_Translator.AssetStore.FontStore");
            _utilsType = translatorAssembly.GetType("PvZ_Fusion_Translator.Utils");

            if (_stringStoreType != null)
            {
                // StringStore.TranslateText(string originalText, bool isLog = false)
                _translateTextMethod = _stringStoreType.GetMethod("TranslateText",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(bool) },
                    null);
            }

            if (_fontStoreType != null)
            {
                // FontStore.LoadTMPFont(string language)
                _loadTMPFontMethod = _fontStoreType.GetMethod("LoadTMPFont",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);
            }

            if (_utilsType != null)
            {
                _languageProp = _utilsType.GetProperty("Language", BindingFlags.Public | BindingFlags.Static);
            }

            _isTranslatorAvailable = true;
            Magnetar_Client.Utils.Magnetar_Logger.DebugLogger.Msg("[Assist Bot] PvZ Fusion Translator linked successfully.");
        }
        catch (Exception ex)
        {
            _isTranslatorAvailable = false;
            Magnetar_Client.Utils.Magnetar_Logger.DebugLogger.Warning($"[Assist Bot] Translator not present or failed to link: {ex.Message}");
        }
    }

    public static TMP_FontAsset TranslatorFont
    {
        get
        {
            if (!IsTranslatorAvailable || _loadTMPFontMethod == null) return null;
            try
            {
                string lang = _languageProp != null ? _languageProp.GetValue(null)?.ToString() : "English";
                return _loadTMPFontMethod.Invoke(null, new object[] { lang }) as TMP_FontAsset;
            }
            catch
            {
                return null;
            }
        }
    }

    public static string GetTranslatedTitle(string originalChinese)
    {
        if (string.IsNullOrEmpty(originalChinese)) return "";
        if (!IsTranslatorAvailable || _translateTextMethod == null) return originalChinese;

        try
        {
            // Let StringStore handle exact dictionary + regex pattern matching
            string translated = _translateTextMethod.Invoke(null, new object[] { originalChinese, false }) as string;
            if (!string.IsNullOrEmpty(translated))
                return translated;
        }
        catch { }

        return originalChinese;
    }
}