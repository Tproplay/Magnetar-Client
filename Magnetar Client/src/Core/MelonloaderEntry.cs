#if MELONLOADER || RELEASE_MELON
using Magnetar_Client.Core;
using MelonLoader;

[assembly: MelonInfo(typeof(Magnetar_Client.Core.MelonLoaderEntry), MagnetarInfo.ModName, MagnetarInfo.Version, MagnetarInfo.Developer)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]

namespace Magnetar_Client.Core;

public class MelonLoaderEntry : MelonMod
{
    public override void OnInitializeMelon()
    {
        Main.Initialize();
    }

    public override void OnUpdate() => Main.Instance?.OnUpdate();
    public override void OnGUI() => Main.Instance?.OnGUI();
    public override void OnApplicationQuit() => Main.Instance?.OnApplicationQuit();
}
#endif