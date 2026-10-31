namespace Magnetar_Client.Core;

public static class MagnetarInfo
{
    public const string ModName = "Magnetar Client";
    public const string BaseVersion = "4.0.3";
#if NIGHTLY
    public const string NightlyBuild = "1";
    public const string Version = BaseVersion + "." + NightlyBuild;
    public const bool IsNightly = true;
#else
    public const string Version = BaseVersion;
    public const bool IsNightly = false;
#endif
    public const string Developer = "Tproplay";
    public const string HarmonyId = "com.tproplay.magnetar";
}
