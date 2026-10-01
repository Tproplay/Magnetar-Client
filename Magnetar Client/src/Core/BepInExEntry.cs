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
        Main.Initialize("com.tproplay.magnetar");

        ClassInjector.RegisterTypeInIl2Cpp<MagnetarUnityHook>();
        AddComponent<MagnetarUnityHook>();
    }
}

public class MagnetarUnityHook : MonoBehaviour
{
    public MagnetarUnityHook(IntPtr ptr) : base(ptr) { }

    private void Update() => Main.Instance?.OnUpdate();
    private void OnGUI() => Main.Instance?.OnGUI();
    private void OnApplicationQuit() => Main.Instance?.OnApplicationQuit();
}
#endif