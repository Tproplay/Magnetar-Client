namespace Magnetar_Client.Core;

public static class MagnetarInfo
{
    public const string ModName = "Magnetar Client";
    public const string Version = "4.1";
    public const string SupportedGameVersion = "4.1";
#if NIGHTLY
    public const int BuildNumber = 1;
#endif
    public const PlatformType Platform =
#if ANDROID
        PlatformType.Android;
#elif MELONLOADER || RELEASE_MELON
        PlatformType.Melonloader;
#elif RELEASE_BEPINEX || BEPINEX
        PlatformType.BepInEx;
#endif
    public const string Developer = "Tproplay";
    public const string HarmonyId = "com.tproplay.magnetar";

    public enum PlatformType
    {
        Melonloader, BepInEx, Android
    }
}
