using Il2CppSystem.Threading.Tasks;
using Magnetar_Client.Core;
using UnityEngine;

namespace Magnetar_Client;

public static class ClientConfig
{
    // Preference Manager
    public static KeyCode MenuOpenKey => PreferenceManager.ModMenuKeyEntry != null ? PreferenceManager.ModMenuKeyEntry.Value : KeyCode.RightShift;

    // GUI
    public static bool showgui;
    public static bool dimBg;
    public static bool InGUI => showgui || HUDManager.forceShow;
    public static TabType CurrentTab = TabType.MODULES;


    public static float GUIScale => GUIManager.ScaleSetting.Value;
    public static float ElementScale => GUIManager.ElementScaleSetting.Value;

    // GUI Settings
    public static float RainbowSpeed = 0.07f;
    public const float NativeWidth = 1920;
    public const float NativeHeight = 1080;

    // GUI Width
    private const float BaseModuleWindowWidth = 200f;
    public static float ModuleWindowWidth => GUIManager.S(BaseModuleWindowWidth);

    private const float BaseElementHeight = 22f;
    public static float elementHeight => GUIManager.S(BaseElementHeight);

    private const float BaseIndent = 8f;
    public static float indent => GUIManager.S(BaseIndent);

    private const float BaseSpacing = 6f;
    public static float spacing => GUIManager.S(BaseSpacing);

    private const float BaseSelectButtonWidth = 70f;
    public static float selectButtonWidth => GUIManager.S(BaseSelectButtonWidth);


    private const float _baseSettingWidth = 200f;
    public static float SettingWidth => GUIManager.S(_baseSettingWidth);

    /// <summary>
    /// Resizes a Rect to the given width/height while keeping its current
    /// center point fixed. Used for draggable windows so they grow/shrink
    /// around wherever the user last dragged them when GUIScale changes,
    /// instead of always resetting to a default position.
    /// </summary>
    public static void RescaleAroundCenter(ref Rect rect, float newWidth, float newHeight)
    {
        float centerX = rect.x + rect.width / 2f;
        float centerY = rect.y + rect.height / 2f;

        rect.width = newWidth;
        rect.height = newHeight;
        rect.x = centerX - newWidth / 2f;
        rect.y = centerY - newHeight / 2f;
    }
}
