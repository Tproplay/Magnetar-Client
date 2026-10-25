#if BEPINEX || RELEASE_BEPINEX
using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Magnetar_Client;
using Magnetar_Client.Core;

namespace Magnetar_Client.Core;

[BepInPlugin(MagnetarInfo.HarmonyId, MagnetarInfo.ModName, MagnetarInfo.Version)]
public class BepInExEntry : BasePlugin
{
    public override void Load()
    {
        Main.Initialize();

        ClassInjector.RegisterTypeInIl2Cpp<MagnetarUnityHook>();
        AddComponent<MagnetarUnityHook>();
    }
}

public class MagnetarUnityHook : MonoBehaviour
{
    public MagnetarUnityHook(IntPtr ptr) : base(ptr) { }

#pragma warning disable CA1822 // Mark members as static
    private void Update() => Main.Instance?.OnUpdate();
    private void OnGUI() => Main.Instance?.OnGUI();
    private void OnApplicationQuit() => Main.Instance?.OnApplicationQuit();
#pragma warning restore CA1822 // Mark members as static
}
#endif