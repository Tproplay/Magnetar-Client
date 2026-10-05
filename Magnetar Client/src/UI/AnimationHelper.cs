using UnityEngine;

namespace Magnetar_Client.UI;

public static class UIAnimationHelper
{
    public static float FadeProgress { get; private set; } = 0f;
    public static float FadeSpeed { get; set; } = 5.0f; // Adjusted for a smoother, natural cadence (~200ms)

    // Sub-window transition progress (0.0: Fully Switched/Settled)
    public static float SubWindowAlpha { get; private set; } = 1f;
    private static float _targetSubAlpha = 1f;

    /// <summary>
    /// Cubic ease-out calculation: 1 - (1 - t)^3
    /// Starts fast and glides smoothly into final opacity without a sudden snap.
    /// </summary>
    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - (inv * inv * inv);
    }

    /// <summary>
    /// Evaluated alpha for menu windows and backgrounds using non-linear easing.
    /// </summary>
    public static float CurrentEasedAlpha => EaseOutCubic(FadeProgress);

    public static void UpdateTransition()
    {
        float target = Config.showgui ? 1f : 0f;
        FadeProgress = Mathf.MoveTowards(FadeProgress, target, Time.unscaledDeltaTime * FadeSpeed);

        // Sub-modal fade transition
        SubWindowAlpha = Mathf.MoveTowards(SubWindowAlpha, _targetSubAlpha, Time.unscaledDeltaTime * 7.5f);
    }

    public static void TriggerSubWindowTransition()
    {
        SubWindowAlpha = 0.25f; // Soft dip instead of an empty black flash
        _targetSubAlpha = 1f;
    }

    public static bool ShouldRenderGUI => Config.showgui || FadeProgress > 0.001f;

    /// <summary>
    /// Instantly forces full visibility to prevent single-frame blackouts 
    /// during internal state handoffs (such as exiting HUD edit layout mode).
    /// </summary>
    public static void SnapToVisible()
    {
        FadeProgress = 1f;
        SubWindowAlpha = 1f;
        _targetSubAlpha = 1f;
    }
}