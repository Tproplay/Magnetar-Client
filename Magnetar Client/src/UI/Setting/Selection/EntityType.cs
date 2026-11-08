namespace Magnetar_Client.UI.Setting;

/// <summary>
/// Specifies the type of in-game entities supported by <see cref="EntitySelectSetting"/>.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// Plant entities backed by game <c>PlantType</c>.
    /// </summary>
    Plant,

    /// <summary>
    /// Zombie entities backed by game <c>ZombieType</c>.
    /// </summary>
    Zombie,

    /// <summary>
    /// Bullet entities backed by game <c>BulletType</c>.
    /// </summary>
    Bullet,

    /// <summary>
    /// Custom entity type with user-provided options and textures.
    /// </summary>
    Custom
}
