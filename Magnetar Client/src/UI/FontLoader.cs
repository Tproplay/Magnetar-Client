using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
#endif

namespace Magnetar_Client.UI;

public static class FontLoader
{
    private static Font DefaultFont;

    public static void Init()
    {
        if (GUI.skin != null && DefaultFont != null && GUI.skin.font == DefaultFont) return;

        DefaultFont = ResourceManager.LoadFont("Magnetar_font");

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

            GUILogger.Msg("[Texture Loader] Successfully loaded and applied custom font!");
        }
        else
        {
            GUILogger.Warning("[Texture Loader] Custom font not found in Resources directory.");
        }
    }
}