using Magnetar_Client.Api;
using Magnetar_Client.UI.WindowDrawing;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.UI;

/// <summary>
/// Manages GUI lifecycle and condition guards for Escape and Tab events.
/// </summary>
public static class SafeToCloseManager
{
    // Custom condition guards: return false to block GUI closure
    private static readonly List<Func<bool>> _closeGuards = new();

    // Custom Escape handlers: return true if Escape was consumed (LIFO priority)
    private static readonly List<Func<bool>> _escapeInterceptors = new();

    // Custom Tab condition guards: return false to block Tab cycling across GUI tabs
    private static readonly List<Func<bool>> _tabGuards = new();

    #region Escape Management

    /// <summary>
    /// Registers a condition guard. If the callback returns false, the GUI will not close on Escape.
    /// </summary>
    public static void RegisterGuard(Func<bool> guard)
    {
        if (guard != null && !_closeGuards.Contains(guard))
        {
            _closeGuards.Add(guard);
        }
    }

    /// <summary>
    /// Unregisters an active close guard.
    /// </summary>
    public static void UnregisterGuard(Func<bool> guard)
    {
        if (guard != null)
        {
            _closeGuards.Remove(guard);
        }
    }

    /// <summary>
    /// Registers an Escape interceptor callback (e.g. dismissing an addon modal window).
    /// Returning true consumes the keypress so the parent GUI remains open.
    /// </summary>
    public static void RegisterInterceptor(Func<bool> interceptor)
    {
        if (interceptor != null && !_escapeInterceptors.Contains(interceptor))
        {
            _escapeInterceptors.Add(interceptor);
        }
    }

    /// <summary>
    /// Unregisters an Escape interceptor.
    /// </summary>
    public static void UnregisterInterceptor(Func<bool> interceptor)
    {
        if (interceptor != null)
        {
            _escapeInterceptors.Remove(interceptor);
        }
    }

    /// <summary>
    /// Evaluates all delegates and ICloseHandler services.
    /// Returns true only if every handler and guard allows closing.
    /// </summary>
    public static bool CanClose()
    {
        // Global Guard: Never allow GUI to close if a field is active or just consumed Escape
        if (DrawSetting.IsFocused || (Time.unscaledTime - DrawSetting.LastEscapeTime < 0.15f))
        {
            return false;
        }

        // 1. Evaluate registered ICloseHandler services
        var handlers = ServiceRegistry.CloseHandlers;
        for (int i = 0; i < handlers.Count; i++)
        {
            try
            {
                if (!handlers[i].CanClose())
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[SafeToCloseManager] Error evaluating CanClose on '{handlers[i].Name}': {ex}");
                return false;
            }
        }

        // 2. Evaluate functional delegates
        for (int i = 0; i < _closeGuards.Count; i++)
        {
            try
            {
                if (!_closeGuards[i]())
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[SafeToCloseManager] Error evaluating guard: {ex}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Dispatches Escape event to delegates and ICloseHandler services in reverse order (LIFO).
    /// Returns true if any handler intercepted the keypress.
    /// </summary>
    public static bool TryInterceptEscape()
    {
        if (DrawSetting.IsFocused || (Time.unscaledTime - DrawSetting.LastEscapeTime < 0.15f))
        {
            DrawSetting.ResetInputBind();
            return true;
        }

        // 1. Check functional interceptor delegates first (LIFO)
        for (int i = _escapeInterceptors.Count - 1; i >= 0; i--)
        {
            try
            {
                if (_escapeInterceptors[i]())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[SafeToCloseManager] Error running Escape interceptor: {ex}");
            }
        }

        // 2. Check ICloseHandler services in reverse priority
        var handlers = ServiceRegistry.CloseHandlers;
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            try
            {
                if (handlers[i].OnEscapePressed())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[SafeToCloseManager] Error running OnEscapePressed on '{handlers[i].Name}': {ex}");
            }
        }

        return false;
    }

    #endregion

    #region Tab Management

    /// <summary>
    /// Registers a Tab condition guard. If the callback returns false, Tab cycling across GUI tabs is blocked.
    /// </summary>
    public static void RegisterTabGuard(Func<bool> guard)
    {
        if (guard != null && !_tabGuards.Contains(guard))
        {
            _tabGuards.Add(guard);
        }
    }

    /// <summary>
    /// Unregisters an active Tab condition guard.
    /// </summary>
    public static void UnregisterTabGuard(Func<bool> guard)
    {
        if (guard != null)
        {
            _tabGuards.Remove(guard);
        }
    }

    /// <summary>
    /// Evaluates all registered Tab condition guards.
    /// Returns true only if every registered guard allows Tab cycling.
    /// </summary>
    public static bool CanSwitchTab()
    {
        for (int i = 0; i < _tabGuards.Count; i++)
        {
            try
            {
                if (!_tabGuards[i]())
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[SafeToCloseManager] Error evaluating tab guard: {ex}");
                return false;
            }
        }

        return true;
    }

    #endregion
}
