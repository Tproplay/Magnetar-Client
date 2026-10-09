#if MELONLOADER || RELEASE_MELON
using Magnetar_Client;
using MelonLoader;

[assembly: MelonInfo(typeof(Magnetar_Client.Core.MelonLoaderEntry), Magnetar_Info.ModName, Magnetar_Info.Version, Magnetar_Info.Developer)]
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