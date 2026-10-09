using Magnetar_Client.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI;

public static class AnimationHandler
{
    public static float FadeProgress { get; private set; }
    public static float FadeSpeed { get; set; } = 5.0f;

    public static float DimAlpha { get; private set; }
    public static float DimFadeSpeed { get; set; } = 6.0f;

    public static float DefaultWindowSpeed { get; set; } = 8.0f;

    static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - Mathf.Pow(inv, 2f);
    }

    public static float CurrentEasedAlpha => EaseOut(FadeProgress);
    public static float CurrentEasedDimAlpha => EaseOut(DimAlpha);

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
                return eased ? EaseOut(state.CurrentAlpha) : state.CurrentAlpha;
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
    /// Set or register a new alpha animation key 
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


    internal static void UpdateTransition()
    {
        float dt = Time.unscaledDeltaTime;

        float target = Config.showgui ? 1f : 0f;
        FadeProgress = Mathf.MoveTowards(FadeProgress, target, dt * FadeSpeed);

        float dimTarget = (Config.dimBg && (Config.showgui || Core.HUDManager.forceShow)) ? 1f : 0f;
        DimAlpha = Mathf.MoveTowards(DimAlpha, dimTarget, dt * DimFadeSpeed);
        

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

    /// <summary>
    /// Instantly complete the current fade transition
    /// </summary>
    public static void SnapToVisible()      
    {
        FadeProgress = 1f;
        if (Config.dimBg) DimAlpha = 1f;
    }

    public static bool ShouldRenderGUI => Config.showgui || FadeProgress > 0.001f;

    /// <summary>
    /// Smoothly transitions a specific view inside a group toward a target alpha (0.0 to 1.0).
    /// </summary>
    public static void FadeView(string group, string viewKey, float targetAlpha, float? speed = null)
    {
        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(viewKey)) return;

        float animSpeed = speed ?? DefaultWindowSpeed;

        if (!_scopedViews.TryGetValue(group, out var groupViews))
        {
            groupViews = new Dictionary<string, ViewState>(StringComparer.OrdinalIgnoreCase);
            _scopedViews[group] = groupViews;
        }

        if (!groupViews.TryGetValue(viewKey, out var state))
        {
            state = new ViewState
            {
                CurrentAlpha = 0f,
                TargetAlpha = Mathf.Clamp01(targetAlpha),
                Speed = animSpeed
            };
            groupViews[viewKey] = state;
        }
        else
        {
            state.TargetAlpha = Mathf.Clamp01(targetAlpha);
            if (speed.HasValue) state.Speed = speed.Value;
        }
    }

    /// <summary>
    /// Executes a render action with blended tab alpha if the tab view is active or transitioning.
    /// Automatically handles GUI.color preservation and restoration.
    /// </summary>
    /// <param name="tab">The target tab to check.</param>
    /// <param name="renderAction">The render delegate to execute.</param>
    /// <param name="threshold">Minimum visible alpha threshold.</param>
    public static void RenderWithTabAlpha(TabType tab, Action renderAction, float threshold = 0.001f)
    {
        if (tab == null || renderAction == null) return;

        float tabAlpha = GetViewAlpha(TabType.AnimationGroup, tab.Name);
        if (tabAlpha <= threshold) return;

        Color prevColor = GUI.color;
        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * tabAlpha);

        try
        {
            renderAction();
        }
        finally
        {
            GUI.color = prevColor;
        }
    }

}