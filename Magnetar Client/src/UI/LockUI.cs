#if MELONLOADER || RELEASE_MELON
#endif
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using UnityEngine.UI;

namespace Magnetar_Client.UI;

/// <summary>
/// Helper to disable game's input while client's gui is open
/// </summary>
public static class LockUI
{
    /// <summary>
    /// Block Input.GetKeyDown
    /// </summary>
    public static bool BlockKeys { get; set; }

    [HarmonyPatch(typeof(Input))]
    static class BlockKeysPatch
    {
        [HarmonyPatch("GetKeyDown", new[] { typeof(KeyCode) })]
        [HarmonyPrefix]
        static bool Prefix(KeyCode key, ref bool __result)
        {
            if (BlockKeys)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Disables Canvas Click detection
    /// </summary>
    public static bool BlockCanvas 
    {
        get => _blockCanvas;
        set
        {
            _blockCanvas = value;
            if (GameAPP.canvas != null)
                LockCanvas(GameAPP.canvas.GetComponent<Canvas>(), value);

            if (GameAPP.canvasUp != null)
                LockCanvas(GameAPP.canvasUp.GetComponent<Canvas>(), value);
        }
    }
    static bool _blockCanvas = false;


    static void LockCanvas(Canvas canvas, bool locked)
    {
        if (canvas == null) return;

        CanvasGroup group = canvas.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        group.blocksRaycasts = !locked;
        group.interactable = !locked;

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = !locked;
        }
    }

}