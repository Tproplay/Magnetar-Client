namespace Magnetar_Client;

public static class MagnetarInfo
{
    public const string ModName = "Magnetar Client";
    public const string Version = "4.1.0";
    public const string SupportedGameVersion = "4.1";
#if NIGHTLY
    public const int BuildNumber = 1;
#endif
    public static PlatformType Platform =>
#if ANDROID
            PlatformType.Android;
#elif MELONLOADER || RELEASE_MELON || NIGHTLY_MELON
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
