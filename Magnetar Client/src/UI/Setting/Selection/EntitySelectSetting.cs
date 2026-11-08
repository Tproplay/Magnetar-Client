using System;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.Modules;
using Magnetar_Client.Utils;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A multi-select setting specialized for in-game entities (Plants, Zombies, Bullets, or Custom)
/// featuring a textured block-grid GUI pattern.
/// </summary>
public class EntitySelectSetting : MultiSelectSetting
{
    public const Magnetar_Client.UI.Setting.EntityType Plant = Magnetar_Client.UI.Setting.EntityType.Plant;
    public const Magnetar_Client.UI.Setting.EntityType Zombie = Magnetar_Client.UI.Setting.EntityType.Zombie;
    public const Magnetar_Client.UI.Setting.EntityType Bullet = Magnetar_Client.UI.Setting.EntityType.Bullet;
    public const Magnetar_Client.UI.Setting.EntityType Custom = Magnetar_Client.UI.Setting.EntityType.Custom;

    /// <summary>
    /// Gets the entity type (Plant, Zombie, Bullet, or Custom) configured for this setting, or <c>null</c> if custom-configured.
    /// </summary>
    public EntityType? EntityType { get; }

    /// <summary>
    /// Gets or sets a custom texture provider function mapping an entity ID to its preview <see cref="Texture2D"/>.
    /// </summary>
    public Func<int, Texture2D> TextureProvider { get; set; }

    public EntitySelectSetting(string name, EntityType entityType) : base(name)
    {
        EntityType = entityType;
        InitializeDefaults();
    }

    public EntitySelectSetting(string name, EntityType entityType, IEnumerable<int> defaultSelections) : base(name)
    {
        EntityType = entityType;
        InitializeDefaults();
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                if (Options.ContainsKey(val) && !IsBlacklisted(val, Options[val]))
                {
                    SelectedValues.Add(val);
                }
            }
            SetCurrentAsDefault();
        }
    }

    public EntitySelectSetting(string name, EntityType entityType, IEnumerable<PlantType> defaultSelections) : base(name)
    {
        EntityType = entityType;
        InitializeDefaults();
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                int id = (int)val;
                if (Options.ContainsKey(id) && !IsBlacklisted(id, Options[id]))
                {
                    SelectedValues.Add(id);
                }
            }
            SetCurrentAsDefault();
        }
    }

    public EntitySelectSetting(string name, EntityType entityType, IEnumerable<ZombieType> defaultSelections) : base(name)
    {
        EntityType = entityType;
        InitializeDefaults();
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                int id = (int)val;
                if (Options.ContainsKey(id) && !IsBlacklisted(id, Options[id]))
                {
                    SelectedValues.Add(id);
                }
            }
            SetCurrentAsDefault();
        }
    }

    public EntitySelectSetting(string name, EntityType entityType, IEnumerable<BulletType> defaultSelections) : base(name)
    {
        EntityType = entityType;
        InitializeDefaults();
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                int id = (int)val;
                if (Options.ContainsKey(id) && !IsBlacklisted(id, Options[id]))
                {
                    SelectedValues.Add(id);
                }
            }
            SetCurrentAsDefault();
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntitySelectSetting"/> with a custom texture provider and no preset <see cref="EntityType"/>.
    /// </summary>
    public EntitySelectSetting(string name, Func<int, Texture2D> textureProvider, IEnumerable<int> defaultSelections = null) : base(name)
    {
        EntityType = null;
        TextureProvider = textureProvider;
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                if (Options.ContainsKey(val) && !IsBlacklisted(val, Options[val]))
                {
                    SelectedValues.Add(val);
                }
            }
            SetCurrentAsDefault();
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntitySelectSetting"/> with an enum type, custom texture provider, and no preset <see cref="EntityType"/>.
    /// </summary>
    public EntitySelectSetting(string name, Type enumType, Func<int, Texture2D> textureProvider, IEnumerable<int> defaultSelections = null) : base(name, enumType)
    {
        EntityType = null;
        TextureProvider = textureProvider;
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                if (Options.ContainsKey(val) && !IsBlacklisted(val, Options[val]))
                {
                    SelectedValues.Add(val);
                }
            }
            SetCurrentAsDefault();
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntitySelectSetting"/> with explicit options, custom texture provider, and no preset <see cref="EntityType"/>.
    /// </summary>
    public EntitySelectSetting(string name, Dictionary<int, string> options, Func<int, Texture2D> textureProvider, IEnumerable<int> defaultSelections = null) : base(name, options)
    {
        EntityType = null;
        TextureProvider = textureProvider;
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                if (Options.ContainsKey(val) && !IsBlacklisted(val, Options[val]))
                {
                    SelectedValues.Add(val);
                }
            }
            SetCurrentAsDefault();
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntitySelectSetting"/> with a dictionary of textures mapping ID to <see cref="Texture2D"/>, and no preset <see cref="EntityType"/>.
    /// </summary>
    public EntitySelectSetting(string name, Dictionary<int, Texture2D> textures, IEnumerable<int> defaultSelections = null) : base(name)
    {
        EntityType = null;
        if (textures != null)
        {
            foreach (var kvp in textures)
            {
                Options[kvp.Key] = kvp.Key.ToString();
            }
            TextureProvider = id => textures.TryGetValue(id, out var tex) ? tex : null;
        }
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                if (Options.ContainsKey(val) && !IsBlacklisted(val, Options[val]))
                {
                    SelectedValues.Add(val);
                }
            }
            SetCurrentAsDefault();
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntitySelectSetting"/> with options and a dictionary of textures, and no preset <see cref="EntityType"/>.
    /// </summary>
    public EntitySelectSetting(string name, Dictionary<int, string> options, Dictionary<int, Texture2D> textures, IEnumerable<int> defaultSelections = null) : base(name, options)
    {
        EntityType = null;
        if (textures != null)
        {
            TextureProvider = id => textures.TryGetValue(id, out var tex) ? tex : null;
        }
        if (defaultSelections != null)
        {
            foreach (var val in defaultSelections)
            {
                if (Options.ContainsKey(val) && !IsBlacklisted(val, Options[val]))
                {
                    SelectedValues.Add(val);
                }
            }
            SetCurrentAsDefault();
        }
    }

    private void InitializeDefaults()
    {
        if (!EntityType.HasValue) return;

        if (EntityType.Value == Plant)
        {
            TextureProvider = ResourceManager.GetPlantTexture;
            CustomNames = Translator.TranslateEnum(typeof(PlantType));
            foreach (PlantType pt in Enum.GetValues(typeof(PlantType)))
            {
                int id = (int)pt;
                if (Module.Banned.PlantTypeBanned.Contains(id) || id < 0) continue;
                Options[id] = pt.ToString();
            }
            Blacklist = new HashSet<int>(Module.Banned.PlantTypeBanned);
        }
        else if (EntityType.Value == Zombie)
        {
            TextureProvider = ResourceManager.GetZombieTexture;
            CustomNames = Translator.TranslateEnum(typeof(ZombieType));
            foreach (ZombieType zt in Enum.GetValues(typeof(ZombieType)))
            {
                int id = (int)zt;
                if (Module.Banned.ZombieTypeBanned.Contains(id) || id < 0) continue;
                Options[id] = zt.ToString();
            }
            Blacklist = new HashSet<int>(Module.Banned.ZombieTypeBanned);
        }
        else if (EntityType.Value == Bullet)
        {
            TextureProvider = ResourceManager.GetBulletTexture;
            CustomNames = Translator.TranslateEnum(typeof(BulletType));
            foreach (BulletType bt in Enum.GetValues(typeof(BulletType)))
            {
                int id = (int)bt;
                if (id < 0) continue;
                Options[id] = bt.ToString();
            }
        }
    }

    /// <summary>
    /// Gets the preview texture for the specified entity ID.
    /// </summary>
    public Texture2D GetTexture(int id)
    {
        return TextureProvider != null ? TextureProvider(id) : null;
    }

    // Strongly-typed PlantType helpers
    public bool IsSelected(PlantType plant) => IsSelected((int)plant);
    public void Select(PlantType plant) => Select((int)plant);
    public void Deselect(PlantType plant) => Deselect((int)plant);
    public void Toggle(PlantType plant) => Toggle((int)plant);

    // Strongly-typed ZombieType helpers
    public bool IsSelected(ZombieType zombie) => IsSelected((int)zombie);
    public void Select(ZombieType zombie) => Select((int)zombie);
    public void Deselect(ZombieType zombie) => Deselect((int)zombie);
    public void Toggle(ZombieType zombie) => Toggle((int)zombie);

    // Strongly-typed BulletType helpers
    public bool IsSelected(BulletType bullet) => IsSelected((int)bullet);
    public void Select(BulletType bullet) => Select((int)bullet);
    public void Deselect(BulletType bullet) => Deselect((int)bullet);
    public void Toggle(BulletType bullet) => Toggle((int)bullet);
}
