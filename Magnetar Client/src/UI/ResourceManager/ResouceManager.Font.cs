using System;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

#if BEPINEX || RELEASE_BEPINEX || ANDROID
using Il2CppInterop.Runtime;
#endif

namespace Magnetar_Client.UI;

public static partial class ResourceManager
{
    private static Font _defaultFont;
    private static TMP_FontAsset _defaultFontAsset;
    private static bool _fontInitialized;

    /// <summary>
    /// The default client font loaded from the Magnetar UI AssetBundle or Resources.
    /// </summary>
    public static Font DefaultFont
    {
        get
        {
            if (!_fontInitialized)
            {
                InitFont();
            }
            return _defaultFont;
        }
        internal set
        {
            _defaultFont = value;
            _fontInitialized = value != null;
        }
    }

    /// <summary>
    /// TextMeshPro font asset generated from <see cref="DefaultFont"/>.
    /// </summary>
    public static TMP_FontAsset DefaultFontAsset
    {
        get
        {
            if (_defaultFontAsset != null) return _defaultFontAsset;

            Font font = DefaultFont;
            if (font != null)
            {
                try
                {
                    _defaultFontAsset = TMP_FontAsset.CreateFontAsset(font);
                    if (_defaultFontAsset != null)
                    {
                        _defaultFontAsset.hideFlags = HideFlags.DontSave;
                    }
                }
                catch (Exception ex)
                {
                    GUILogger.Error($"Failed creating TMP_FontAsset from DefaultFont: {ex.Message}");
                }
            }

            return _defaultFontAsset;
        }
    }

    static Font LoadFont(string assetName = "Magnetar_font")
    {
        if (string.IsNullOrWhiteSpace(assetName)) return null;

        if (_fontCache.TryGetValue(assetName, out var cached) && cached != null)
            return cached;

        AssetBundle bundle = LoadClientBundle();
        if (bundle != null)
        {
            try
            {
                string cleanName = Path.GetFileNameWithoutExtension(assetName);
                string[] possibleNames = new[] { assetName, cleanName, "Magnetar_font", "font" };

                foreach (string name in possibleNames)
                {
#if MELONLOADER || RELEASE_MELON
                    Font font = bundle.LoadAsset<Font>(name);
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
                    var raw = bundle.LoadAsset(name, Il2CppType.Of<Font>());
                    Font font = raw != null ? raw.TryCast<Font>() : null;
#endif
                    if (font != null)
                    {
                        font.hideFlags = HideFlags.DontSave;
                        _fontCache[assetName] = font;
                        _fontCache[cleanName] = font;
                        GUILogger.Msg($"Loaded Font '{name}' from AssetBundle.");
                        return font;
                    }
                }
            }
            catch (Exception ex)
            {
                GUILogger.Error($"Error loading Font '{assetName}' from AssetBundle: {ex.Message}");
            }
        }

        try
        {
            var loaded = Resources.FindObjectsOfTypeAll<Font>();
            for (int i = 0; i < loaded.Count; i++)
            {
                var f = loaded[i];
                if (f != null && f.material != null && !string.IsNullOrEmpty(f.name))
                {
                    _fontCache[assetName] = f;
                    return f;
                }
            }
        }
        catch { }

        return null;
    }

    internal static void InitFont()
    {
        if (!_fontInitialized)
        {
            _fontInitialized = true;
            _defaultFont = LoadFont("Magnetar_font");

            if (_defaultFont == null)
            {
                GUILogger.Warning("Custom font not found in Resources directory.");
            }
        }

        if (GUI.skin != null && _defaultFont != null && GUI.skin.font != _defaultFont)
        {
            GUI.skin.font = _defaultFont;
            GUI.skin.box.font = _defaultFont;
            GUI.skin.label.font = _defaultFont;
            GUI.skin.button.font = _defaultFont;
            GUI.skin.textField.font = _defaultFont;
            GUI.skin.textArea.font = _defaultFont;
            GUI.skin.toggle.font = _defaultFont;
            GUI.skin.window.font = _defaultFont;
        }
    }
}