using System;

namespace Magnetar_Client.Api;

/// <summary>
/// Prevents the decorated Module, HudElement, or IClientService from being
/// automatically discovered and registered by AddonManager.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MagnetarDoNotRegisterAttribute : Attribute { }
