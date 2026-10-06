using UnityEngine;

namespace Magnetar_Client.UI;

public static class UIAnimationHelper
{
    public static float FadeProgress { get; private set; } = 0f;
    public static float FadeSpeed { get; set; } = 5.0f;

    public static float SubWindowAlpha { get; private set; } = 1f;
    private static float _targetSubAlpha = 1f;

    // Dedicated smooth alpha for background dimming
    public static float DimAlpha { get; private set; } = 0f;
    public static float DimFadeSpeed { get; set; } = 6.0f;

    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - (inv * inv * inv);
    }

    public static float CurrentEasedAlpha => EaseOutCubic(FadeProgress);
    public static float CurrentEasedDimAlpha => EaseOutCubic(DimAlpha);

    public static void UpdateTransition()
    {
        float target = Config.showgui ? 1f : 0f;
        FadeProgress = Mathf.MoveTowards(FadeProgress, target, Time.unscaledDeltaTime * FadeSpeed);

        // Sub-modal fade transition
        SubWindowAlpha = Mathf.MoveTowards(SubWindowAlpha, _targetSubAlpha, Time.unscaledDeltaTime * 7.5f);

        // Target for dimmed background: full opacity if dimBg is on and menu/layout is active
        float dimTarget = (Config.dimBg && (Config.showgui || Core.HUDManager.forceShow)) ? 1f : 0f;
        DimAlpha = Mathf.MoveTowards(DimAlpha, dimTarget, Time.unscaledDeltaTime * DimFadeSpeed);
    }

    public static void TriggerSubWindowTransition()
    {
        SubWindowAlpha = 0.25f;
        _targetSubAlpha = 1f;
    }

    public static void SnapToVisible()
    {
        FadeProgress = 1f;
        SubWindowAlpha = 1f;
        _targetSubAlpha = 1f;
        if (Config.dimBg) DimAlpha = 1f;
    }

    public static bool ShouldRenderGUI => Config.showgui || FadeProgress > 0.001f;
}