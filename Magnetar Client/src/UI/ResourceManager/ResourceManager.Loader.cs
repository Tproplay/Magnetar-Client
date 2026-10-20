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

public static partial class ResourceManager
{
    private static readonly Dictionary<string, Sprite> _spriteMemoryCache = new();
    private static readonly Dictionary<string, Texture2D> _rawTexMemoryCache = new();
    private static bool _hasScannedMemory;

    public static Dictionary<int, string> PlantTextureOverrides = new();
    public static Dictionary<int, string> ZombieTextureOverrides = new();

    private static void RefreshMemoryCache()
    {
        _spriteMemoryCache.Clear();
        _rawTexMemoryCache.Clear();

        var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < allSprites.Count; i++)
        {
            Sprite s = allSprites[i];
            if (s != null && !string.IsNullOrEmpty(s.name))
            {
                if (!_spriteMemoryCache.ContainsKey(s.name))
                    _spriteMemoryCache[s.name] = s;
            }
        }

        var allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
        for (int i = 0; i < allTextures.Count; i++)
        {
            Texture2D t = allTextures[i];
            if (t != null && !string.IsNullOrEmpty(t.name))
            {
                if (!_rawTexMemoryCache.ContainsKey(t.name))
                    _rawTexMemoryCache[t.name] = t;
            }
        }
        _hasScannedMemory = true;
    }

    public static Texture2D GetTexture(string textureName)
    {
        if (string.IsNullOrEmpty(textureName)) return null;

        if (_textureCache.TryGetValue(textureName, out Texture2D cachedTex))
            return cachedTex;

        Texture2D directDiskTex = LoadTexture(textureName);
        if (directDiskTex != null)
        {
            _textureCache[textureName] = directDiskTex;
            return directDiskTex;
        }

        bool isExplicitPath = textureName.Contains("/");

        if (isExplicitPath)
        {
            string cleanPath = textureName.ToLower();
            int targetIndex = 0;
            bool useSubAsset = false;

            if (cleanPath.EndsWith("]"))
            {
                int openBracket = cleanPath.LastIndexOf('[');
                if (openBracket != -1)
                {
                    string idxStr = cleanPath.Substring(openBracket + 1, cleanPath.Length - openBracket - 2);
                    if (int.TryParse(idxStr, out int parsedIdx))
                    {
                        targetIndex = parsedIdx;
                        useSubAsset = true;
                        cleanPath = cleanPath.Substring(0, openBracket);
                    }
                }
            }

            Texture2D diskCleanTex = LoadTexture(cleanPath);
            if (diskCleanTex != null)
            {
                _textureCache[textureName] = diskCleanTex;
                return diskCleanTex;
            }

            string resPath = cleanPath.Replace(".png", "").Replace(".jpg", "");

            if (useSubAsset)
            {
                var subAssets = Resources.LoadAll<Sprite>(resPath);
                if (subAssets != null && subAssets.Length > 0)
                {
                    int safeIndex = (targetIndex >= 0 && targetIndex < subAssets.Length) ? targetIndex : 0;
                    Texture2D isolatedTex = CreateReadableCroppedTexture(subAssets[safeIndex]);
                    if (isolatedTex != null)
                    {
                        _textureCache[textureName] = isolatedTex;
                        return isolatedTex;
                    }
                }
            }
            else
            {
                Sprite resSprite = Resources.Load<Sprite>(resPath);
                if (resSprite != null)
                {
                    Texture2D isolatedTex = CreateReadableCroppedTexture(resSprite);
                    if (isolatedTex != null)
                    {
                        _textureCache[textureName] = isolatedTex;
                        return isolatedTex;
                    }
                }
            }

            _textureCache[textureName] = null;
            return null;
        }

        if (!_hasScannedMemory) RefreshMemoryCache();

        if (_spriteMemoryCache.TryGetValue(textureName, out Sprite sprite))
        {
            if (sprite != null)
            {
                Texture2D isolatedTex = CreateReadableCroppedTexture(sprite);
                if (isolatedTex != null)
                {
                    _textureCache[textureName] = isolatedTex;
                    return isolatedTex;
                }
            }
            else
            {
                _spriteMemoryCache.Remove(textureName);
            }
        }

        if (_rawTexMemoryCache.TryGetValue(textureName, out Texture2D tex))
        {
            if (tex != null)
            {
                _textureCache[textureName] = tex;
                return tex;
            }
            else
            {
                _rawTexMemoryCache.Remove(textureName);
            }
        }

        _textureCache[textureName] = null;
        return null;
    }
    public static void SaveTextureOverrides()
    {
        string texturePath = PathsManager.TextureDataPath;
        try
        {
            var texData = new SaveLoadData.TextureSaveData
            {
                PlantTextureOverrides = PlantTextureOverrides ?? new(),
                ZombieTextureOverrides = ZombieTextureOverrides ?? new()
            };
            string dir = Path.GetDirectoryName(texturePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(texturePath, Newtonsoft.Json.JsonConvert.SerializeObject(texData, Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception ex)
        {
            Magnetar_Logger.AutoSaveLogger.Error($"Failed to save TextureData: {ex.Message}");
        }
    }

    public static void LoadTextureOverrides()
    {
        string texturePath = PathsManager.TextureDataPath;
        if (File.Exists(texturePath))
        {
            try
            {
                string json = File.ReadAllText(texturePath);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveLoadData.TextureSaveData>(json);
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