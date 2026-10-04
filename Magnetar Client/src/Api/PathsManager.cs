using Magnetar_Client.Utils;
using MelonLoader.Utils;
using System.IO;

namespace Magnetar_Client.Api;

internal static class PathsManager
{
    static string _cachedModsDir;
    internal static string ModsDir
    {
        get
        {
            if (!string.IsNullOrEmpty(_cachedModsDir) && Directory.Exists(_cachedModsDir))
            {
                return _cachedModsDir;
            }

            string targetDir = null;

#if ANDROID
            string mobilePlugins = "/storage/emulated/0/PVZRH_Launcher/com.LanPiaoPiao.PlantsVsZombiesRH/BepInEx/plugins";

            try
            {
                if (Directory.Exists("/storage/emulated/0/PVZRH_Launcher/com.LanPiaoPiao.PlantsVsZombiesRH/BepInEx"))
                {
                    targetDir = mobilePlugins;
                }
            }
            catch { }

            if (string.IsNullOrEmpty(targetDir))
            {
                try
                {
                    if (!string.IsNullOrEmpty(Paths.PluginPath)) targetDir = Paths.PluginPath;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(targetDir))
            {
                targetDir = Path.Combine(Application.persistentDataPath, "Magnetar", "Plugins");
            }
#elif MELONLOADER || RELEASE_MELON
            targetDir = MelonEnvironment.ModsDirectory;
#elif BEPINEX || RELEASE_BEPINEX
            targetDir = Paths.PluginPath;
#else
            targetDir = Path.Combine(Application.persistentDataPath, "Magnetar", "Plugins");
#endif

            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                _cachedModsDir = targetDir;
            }
            catch
            {
                _cachedModsDir = ProfileManager.ConfigDir;
                return _cachedModsDir;
            }

            return targetDir;
        }
    }

    internal static string AddonsDir => Path.Combine(ModsDir, "Magnetar Addon");

}
