#if BEPINEX || RELEASE_BEPINEX
using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Magnetar_Client;
using Magnetar_Client.Core;

namespace Magnetar_Client.Core;

[BepInPlugin("com.tproplay.magnetar", Magnetar_Info.ModName, Magnetar_Info.Version)]
public class BepInExEntry : BasePlugin
{
    public override void Load()
    {
        MainCore.Initialize("com.tproplay.magnetar");

        // Register and attach the MonoBehaviour driver to pump Update and OnGUI events
        ClassInjector.RegisterTypeInIl2Cpp<MagnetarUnityHook>();
        AddComponent<MagnetarUnityHook>();
    }
}

public class MagnetarUnityHook : MonoBehaviour
{
    public MagnetarUnityHook(IntPtr ptr) : base(ptr) { }

    private void Update() => MainCore.Instance?.OnUpdate();
    private void OnGUI() => MainCore.Instance?.OnGUI();
    private void OnApplicationQuit() => MainCore.Instance?.OnApplicationQuit();
}
#endif