using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Magnetar_Client.Api;
using Magnetar_Client.Utils;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
#endif

namespace Magnetar_Client.UI;

/// <summary>
/// Manages loading textures and fonts.
/// </summary>
public static partial class ResourceManager
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static Dictionary<int, string> PlantTextureOverrides = new();
    public static Dictionary<int, string> ZombieTextureOverrides = new();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Internal Json deserialization format
    /// </summary>
    public class TextureSaveData
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Dictionary<int, string> PlantTextureOverrides = new();
        public Dictionary<int, string> ZombieTextureOverrides = new();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    internal static void LoadTextureOverrides()
    {
        string texturePath = PathsManager.TextureDataPath;
        if (File.Exists(texturePath))
        {
            try
            {
                string json = File.ReadAllText(texturePath);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TextureSaveData>(json);
                if (data != null)
                {
                    PlantTextureOverrides = data.PlantTextureOverrides ?? new();
                    ZombieTextureOverrides = data.ZombieTextureOverrides ?? new();
                }
            }
            catch (Exception ex)
            {
                Magnetar_Logger.AutoSaveLogger.Error($"Failed to load TextureData: {ex.Message}");
            }
        }
    }
    /// <summary>
    /// Gets the texture of plant from <see cref="ResourcesManager.plantPreviews"/>
    /// </summary>
    public static Texture2D GetPlantTexture(int plantId)
    {
        string cacheKey = $"game_plant_preview_{plantId}";
        if (_textureCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            return cached;

        try
        {
            var resManager = GameAPP.resourcesManager;
            if (resManager != null && resManager.plantPreviews != null)
            {
                PlantType type = (PlantType)plantId;
                if (resManager.plantPreviews.TryGetValue(type, out GameObject previewObj) && previewObj != null)
                {
                    SpriteRenderer sr = previewObj.GetComponent<SpriteRenderer>() ?? previewObj.GetComponentInChildren<SpriteRenderer>();
                    Sprite sprite = sr != null ? sr.sprite : null;

                    if (sprite == null)
                    {
                        var img = previewObj.GetComponent<UnityEngine.UI.Image>() ?? previewObj.GetComponentInChildren<UnityEngine.UI.Image>();
                        if (img != null) sprite = img.sprite;
                    }

                    if (sprite != null)
                    {
                        Texture2D tex = CreateReadableCroppedTexture(sprite);
                        if (tex != null)
                        {
                            _textureCache[cacheKey] = tex;
                            return tex;
                        }
                    }
                }
            }
        }
        catch { }

        return null;
    }
    /// <summary>
    /// Gets the texture of plant from <see cref="ResourcesManager.plantPreviews"/>
    /// </summary>
    public static Texture2D GetPlantTexture(PlantType plantType) => GetPlantTexture((int)plantType);
    /// <summary>
    /// Gets the texture of zombie from <see cref="ResourcesManager.zombieSprites"/>
    /// </summary>
    public static Texture2D GetZombieTexture(int zombieId)
    {
        string cacheKey = $"game_zombie_sprite_{zombieId}";
        if (_textureCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            return cached;

        try
        {
            var resManager = GameAPP.resourcesManager;
            if (resManager != null && resManager.zombieSprites != null)
            {
                ZombieType type = (ZombieType)zombieId;
                if (resManager.zombieSprites.TryGetValue(type, out Sprite zSprite) && zSprite != null)
                {
                    Texture2D tex = CreateReadableCroppedTexture(zSprite);
                    if (tex != null)
                    {
                        _textureCache[cacheKey] = tex;
                        return tex;
                    }
                }
            }
        }
        catch { }

        return null;
    }
    /// <summary>
    /// Gets the texture of zombie from <see cref="ResourcesManager.zombieSprites"/>
    /// </summary>
    public static Texture2D GetZombieTexture(ZombieType zombieType) => GetZombieTexture((int)zombieType);

    private static Texture2D CreateReadableCroppedTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;

        if ((int)sprite.rect.width == sprite.texture.width &&
            (int)sprite.rect.height == sprite.texture.height &&
            sprite.rect.x == 0 && sprite.rect.y == 0)
        {
            return sprite.texture;
        }

        RenderTexture tmp = RenderTexture.GetTemporary(
            sprite.texture.width,
            sprite.texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(sprite.texture, tmp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D cropped = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
        cropped.ReadPixels(new Rect(sprite.rect.x, sprite.rect.y, sprite.rect.width, sprite.rect.height), 0, 0);
        cropped.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);
        return cropped;
    }
}