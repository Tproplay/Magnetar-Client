using UnityEngine;
using UnityEngine.UI;
using Il2Cpp;

namespace Magnetar_Client.UI;

public static class LockUI
{
    public static void LockGameCanvas(bool locked)
    {
        if (GameAPP.canvas != null)
            LockCanvas(GameAPP.canvas.GetComponent<Canvas>(), locked);

        if (GameAPP.canvasUp != null)
            LockCanvas(GameAPP.canvasUp.GetComponent<Canvas>(), locked);
    }

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