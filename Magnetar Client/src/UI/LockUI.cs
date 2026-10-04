#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
using HarmonyLib;
using Magnetar_Client.Core;
using UnityEngine;

namespace Magnetar_Client.UI;

public static class LockUI
{
    [HarmonyPatch(typeof(Input), "GetKeyDown", new[] { typeof(KeyCode) })]
    public static class BlockSKeysPatch
    {
        public static bool BlockKeys;
        public static bool Prefix(KeyCode key, ref bool __result)
        {
            if ((Config.showgui || HUDManager.forceShow) && BlockKeys)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}