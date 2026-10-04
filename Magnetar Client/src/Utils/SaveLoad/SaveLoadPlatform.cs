using System;
using System.IO;
using static Magnetar_Client.Api.MagnetarApi;

#if MELONLOADER || RELEASE_MELON
using MelonLoader;
using MelonLoader.Utils;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
using BepInEx.Configuration;
#endif

namespace Magnetar_Client.Utils;

public static class SaveLoadPlatform
{
    public static string ProfilesDir => Path.Combine(ConfigDir, "Magnetar Profiles");

    public static string GetSafeProfilePath(string profileName)
    {
        try
        {
            string path = GetProfilePath(profileName);
            if (!string.IsNullOrEmpty(path)) return path;
        }
        catch { }

        if (!Directory.Exists(ProfilesDir)) Directory.CreateDirectory(ProfilesDir);
        return Path.Combine(ProfilesDir, $"{profileName}.json");
    }

    public static string GetSafeTexturePath()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
        return Path.Combine(DataDir, "TextureData.json");
    }
}