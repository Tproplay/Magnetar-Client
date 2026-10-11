using System;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if BEPINEX || RELEASE_BEPINEX || ANDROID
using Il2CppInterop.Runtime;
#endif

namespace Magnetar_Client.UI;

public static partial class ResourceManager
{
    private static Font DefaultFont;

    public static Font LoadFont(string assetName = "Magnetar_font")
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

    public static void InitFont()
    {
        if (GUI.skin != null && DefaultFont != null && GUI.skin.font == DefaultFont) return;

        DefaultFont = LoadFont("Magnetar_font");

        if (DefaultFont != null)
        {
            if (GUI.skin != null)
            {
                GUI.skin.font = DefaultFont;
                GUI.skin.box.font = DefaultFont;
                GUI.skin.label.font = DefaultFont;
                GUI.skin.button.font = DefaultFont;
                GUI.skin.textField.font = DefaultFont;
                GUI.skin.textArea.font = DefaultFont;
                GUI.skin.toggle.font = DefaultFont;
                GUI.skin.window.font = DefaultFont;
            }
        }
        else
        {
            GUILogger.Warning("Custom font not found in Resources directory.");
        }
    }
}