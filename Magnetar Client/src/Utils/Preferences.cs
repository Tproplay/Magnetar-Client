using MelonLoader;

namespace Magnetar_Client;

public static class Preferences
{
#if MELONLOADER || RELEASE_MELON
    public static MelonPreferences_Category MagnetarCategory;
    public static MelonPreferences_Entry<bool> ShowFloatingIconEntry;
    public static MelonPreferences_Entry<bool> ShowMobileButtonsEntry;
#if MELONLOADER || RELEASE_MELON
    public static MelonPreferences_Entry<string> prefCurrentProfile;
#elif BEPINEX || RELEASE_BEPINEX
    public static ConfigEntry<string> prefCurrentProfile;
#endif
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
    public static ConfigFile BepInExConfig;
    public static BepInEx.Configuration.ConfigEntry<bool> ShowFloatingIconEntry;
    public static BepInEx.Configuration.ConfigEntry<bool> ShowMobileButtonsEntry;
#endif
    public static void InitializePreferences()
    {
        Api.Actions.OnEarlyInitializePreferences?.Invoke();

#if MELONLOADER || RELEASE_MELON
        Preferences.MagnetarCategory = MelonPreferences.CreateCategory("Magnetar Client", "Magnetar Client");

        Preferences.ShowFloatingIconEntry = Preferences.MagnetarCategory.CreateEntry<bool>("ShowFloatingIcon",
            false,
            "Show Floating Icon", "Display floating draggable menu button.");

        if (Preferences.ShowFloatingIconEntry != null)
        {
            Config.ShowFloatingIcon = Preferences.ShowFloatingIconEntry.Value;
        }

        Preferences.ShowMobileButtonsEntry = Preferences.MagnetarCategory.CreateEntry<bool>("ShowMobileButtons",
            false,
            "Show Mobile Buttons", "Display top-right close buttons on popup windows.");

        if (Preferences.ShowMobileButtonsEntry != null)
        {
            Config.ShowMobileButtons = Preferences.ShowMobileButtonsEntry.Value;
        }
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
        try
        {
            string configDir = ProfileManager.ConfigDir;
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            string configFilePath = Path.Combine(configDir, "Magnetar_Client.cfg");
            
            Preferences.BepInExConfig = new ConfigFile(configFilePath, true);
            
            Preferences.ShowFloatingIconEntry = Preferences.BepInExConfig.Bind<bool>("UI", "ShowFloatingIcon",
#if ANDROID
                true,
#else
                false,
#endif
                "Display floating draggable menu button.");

            if (Preferences.ShowFloatingIconEntry != null)
            {
                Config.ShowFloatingIcon = Preferences.ShowFloatingIconEntry.Value;
            }

            Preferences.ShowMobileButtonsEntry = Preferences.BepInExConfig.Bind<bool>("UI", "ShowMobileButtons",
#if ANDROID
                true,
#else
                false,
#endif
                "Display top-right close buttons on popup windows.");

            if (Preferences.ShowMobileButtonsEntry != null)
            {
                Config.ShowMobileButtons = Preferences.ShowMobileButtonsEntry.Value;
            }
        }
        catch (Exception ex)
        {
            AutoSaveLogger.Error($"Failed to initialize BepInEx ConfigFile: {ex.Message}");
        }
#endif
        

#if MELONLOADER || RELEASE_MELON
        prefCurrentProfile = Preferences.MagnetarCategory.CreateEntry("CurrentProfile", Config.DefaultProfile, "Active Profile");
#elif BEPINEX || RELEASE_BEPINEX
        try
        {
            if (Preferences.BepInExConfig != null)
            {
                prefCurrentProfile = Preferences.BepInExConfig.Bind("ProfileManager", "CurrentProfile", DefaultProfile, "Active Profile");
            }
        }
        catch (Exception ex)
        {
            AutoSaveLogger.Error($"Failed to bind BepInEx preference: {ex.Message}");
        }
#endif
        Api.Actions.OnLateInitializePreferences?.Invoke();
    }

}
