using System;
using System.Collections;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using MelonLoader;
#elif BEPINEX || RELEASE_BEPINEX
using Il2CppInterop.Runtime.Injection;
using BepInEx.Unity.IL2CPP.Utils.Collections;
#endif

namespace Magnetar_Client;

public interface ICoroutineHandle
{
    bool IsRunning { get; }
    void Stop();
}

public static class CoroutineManager
{
#if BEPINEX || RELEASE_BEPINEX
    private static CoroutineRunner _runner;
    private static bool _typeRegistered = false;

    private class CoroutineRunner : MonoBehaviour
    {
        public CoroutineRunner(IntPtr ptr) : base(ptr) { }
    }

    private static CoroutineRunner Runner
    {
        get
        {
            if (_runner == null)
            {
                if (!_typeRegistered)
                {
                    ClassInjector.RegisterTypeInIl2Cpp<CoroutineRunner>();
                    _typeRegistered = true;
                }

                GameObject runnerObj = new("Magnetar_CoroutineRunner");
                UnityEngine.Object.DontDestroyOnLoad(runnerObj);
                runnerObj.hideFlags = HideFlags.HideAndDontSave;

                _runner = runnerObj.AddComponent<CoroutineRunner>();
            }
            return _runner;
        }
    }

    private class BepInExCoroutineHandle : ICoroutineHandle
    {
        private Coroutine _coroutine;
        private readonly CoroutineRunner _owner;

        public bool IsRunning => _coroutine != null;

        public BepInExCoroutineHandle(CoroutineRunner owner, Coroutine coroutine)
        {
            _owner = owner;
            _coroutine = coroutine;
        }

        public void Stop()
        {
            if (_coroutine != null && _owner != null)
            {
                _owner.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
    }
#elif MELONLOADER || RELEASE_MELON
    private class MelonCoroutineHandle : ICoroutineHandle
    {
        private object _melonToken;

        public bool IsRunning => _melonToken != null;

        public MelonCoroutineHandle(object melonToken)
        {
            _melonToken = melonToken;
        }

        public void Stop()
        {
            if (_melonToken != null)
            {
                MelonCoroutines.Stop(_melonToken);
                _melonToken = null;
            }
        }
    }
#endif

    public static ICoroutineHandle Start(IEnumerator routine)
    {
        if (routine == null) throw new ArgumentNullException(nameof(routine));

#if MELONLOADER || RELEASE_MELON
        object token = MelonCoroutines.Start(routine);
        return new MelonCoroutineHandle(token);

#elif BEPINEX || RELEASE_BEPINEX
        Coroutine coroutine = Runner.StartCoroutine(routine.WrapToIl2Cpp());
        return new BepInExCoroutineHandle(Runner, coroutine);

#else
        throw new PlatformNotSupportedException("Neither MELONLOADER nor BEPINEX compilation symbols are set.");
#endif
    }

    public static void Stop(ICoroutineHandle handle)
    {
        handle?.Stop();
    }
}