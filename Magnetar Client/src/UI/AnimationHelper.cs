using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI;

public static class UIAnimationHelper
{
    public static float FadeProgress { get; private set; } = 0f;
    public static float FadeSpeed { get; set; } = 5.0f;

    public static float DimAlpha { get; private set; } = 0f;
    public static float DimFadeSpeed { get; set; } = 6.0f;

    public static float DefaultWindowSpeed { get; set; } = 8.0f;

    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - (inv * inv * inv);
    }

    public static float CurrentEasedAlpha => EaseOutCubic(FadeProgress);
    public static float CurrentEasedDimAlpha => EaseOutCubic(DimAlpha);

    #region Scoped View Registry System

    private class ViewState
    {
        public float CurrentAlpha;
        public float TargetAlpha;
        public float Speed;
    }

    private static readonly Dictionary<string, Dictionary<string, ViewState>> _scopedViews =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the current alpha for a specific view inside a group.
    /// If no views in the group are active, defaults the first registered view to 1.0f.
    /// </summary>
    public static float GetViewAlpha(string group, string viewKey, bool eased = true)
    {
        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(viewKey)) return 0f;

        if (_scopedViews.TryGetValue(group, out var groupViews))
        {
            if (groupViews.TryGetValue(viewKey, out var state))
            {
                return eased ? EaseOutCubic(state.CurrentAlpha) : state.CurrentAlpha;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Switches the active view strictly within its own group, leaving all other groups/tabs untouched.
    /// </summary>
    public static void SwitchView(string group, string targetViewKey, float? speed = null)
    {
        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(targetViewKey)) return;

        float animSpeed = speed ?? DefaultWindowSpeed;

        if (!_scopedViews.TryGetValue(group, out var groupViews))
        {
            groupViews = new Dictionary<string, ViewState>(StringComparer.OrdinalIgnoreCase);
            _scopedViews[group] = groupViews;
        }

        if (!groupViews.ContainsKey(targetViewKey))
        {
            groupViews[targetViewKey] = new ViewState { CurrentAlpha = 0f, TargetAlpha = 1f, Speed = animSpeed };
        }

        foreach (var kvp in groupViews)
        {
            bool isTarget = string.Equals(kvp.Key, targetViewKey, StringComparison.OrdinalIgnoreCase);
            kvp.Value.TargetAlpha = isTarget ? 1f : 0f;
            kvp.Value.Speed = animSpeed;
        }
    }

    /// <summary>
    /// Primes an initial view state directly without animation.
    /// </summary>
    public static void SetViewImmediate(string group, string viewKey, float alpha)
    {
        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(viewKey)) return;

        if (!_scopedViews.TryGetValue(group, out var groupViews))
        {
            groupViews = new Dictionary<string, ViewState>(StringComparer.OrdinalIgnoreCase);
            _scopedViews[group] = groupViews;
        }

        if (!groupViews.TryGetValue(viewKey, out var state))
        {
            groupViews[viewKey] = new ViewState { CurrentAlpha = alpha, TargetAlpha = alpha, Speed = DefaultWindowSpeed };
        }
        else
        {
            state.CurrentAlpha = alpha;
            state.TargetAlpha = alpha;
        }
    }

    #endregion

    public static void UpdateTransition()
    {
        float target = Config.showgui ? 1f : 0f;
        FadeProgress = Mathf.MoveTowards(FadeProgress, target, Time.unscaledDeltaTime * FadeSpeed);

        float dimTarget = (Config.dimBg && (Config.showgui || Core.HUDManager.forceShow)) ? 1f : 0f;
        DimAlpha = Mathf.MoveTowards(DimAlpha, dimTarget, Time.unscaledDeltaTime * DimFadeSpeed);

        float dt = Time.unscaledDeltaTime;

        foreach (var groupViews in _scopedViews.Values)
        {
            foreach (var state in groupViews.Values)
            {
                if (!Mathf.Approximately(state.CurrentAlpha, state.TargetAlpha))
                {
                    state.CurrentAlpha = Mathf.MoveTowards(state.CurrentAlpha, state.TargetAlpha, dt * state.Speed);
                }
            }
        }
    }

    public static void SnapToVisible()
    {
        FadeProgress = 1f;
        if (Config.dimBg) DimAlpha = 1f;
    }

    public static bool ShouldRenderGUI => Config.showgui || FadeProgress > 0.001f;
}