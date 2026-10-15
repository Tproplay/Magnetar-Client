using Il2CppSystem.Threading.Tasks;
using Magnetar_Client.Core;
using Magnetar_Client.Utils;
using UnityEngine;

namespace Magnetar_Client;

public static class ClientConfig
{
    // Preference Manager
    /// <summary>
    /// The key used to open the Menu (default Right-shift)
    /// </summary>
    public static KeyCode MenuOpenKey => PreferenceManager.ModMenuKeyEntry != null ? PreferenceManager.ModMenuKeyEntry.Value : KeyCode.RightShift;

    // GUI
    /// <summary>
    /// True if the menu is open.
    /// </summary>
    /// <remarks>Use ClientConfig.InGUI in case you want to know if the user is in the Menu.</remarks>
    public static bool showgui;
    /// <summary>
    /// Show Dim Background?
    /// </summary>
    public static bool dimBg;
    /// <summary>
    /// True if the user is in Client's gui menu.
    /// </summary>
    public static bool InGUI => showgui || HUDManager.forceShow;
    /// <summary>
    /// The Current Tab that is opened.
    /// </summary>
    public static TabType CurrentTab { get; set; } = TabType.MODULES;

    /// <summary>
    /// The Current GUI Scale.
    /// </summary>
    /// <remarks>Use this to scale your UI.</remarks>
    public static float GUIScale => GUIManager.ScaleSetting.Value;
    /// <summary>
    /// The Current HUD Element Scale.
    /// </summary>
    public static float ElementScale => GUIManager.ElementScaleSetting.Value;
    /// <summary>
    /// The current active profile.
    /// </summary>
    public static string CurrentProfile => ProfileManager.CurrentProfile;

    // GUI Settings
    public static float RainbowSpeed { get; set; } = 0.07f;
    /// <summary>
    /// The Native width for which this client was made. Other screen resoulutions are handled using Matrix4x4.
    /// </summary>
    public const float NativeWidth = 1920;
    /// <summary>
    /// The Native height for which this client was made. Other screen resoulutions are handled using Matrix4x4.
    /// </summary>
    public const float NativeHeight = 1080;

    // GUI Width

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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
