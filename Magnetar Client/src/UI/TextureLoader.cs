using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.Utils;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
#endif

namespace Magnetar_Client.UI;

public static class TextureLoader
{
    private static readonly Dictionary<string, Texture2D> _textureCache = new();

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

        // ====================================================================
        // STAGE 1: DIRECT DISK LOADING VIA RESOURCE MANAGER (NO ASSETBUNDLES)
        // ====================================================================
        Texture2D directDiskTex = ResourceManager.LoadTexture(textureName);
        if (directDiskTex != null)
        {
            _textureCache[textureName] = directDiskTex;
            return directDiskTex;
        }

        bool isExplicitPath = textureName.Contains("/");

        // ====================================================================
        // STAGE 2: EXPLICIT SUB-PATH & SUB-ASSET PARSING
        // ====================================================================
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

            // Check if clean stripped path exists on disk in Resources or its subfolders
            Texture2D diskCleanTex = ResourceManager.LoadTexture(cleanPath);
            if (diskCleanTex != null)
            {
                _textureCache[textureName] = diskCleanTex;
                return diskCleanTex;
            }

            // Fallback check against Unity's built-in game Resources
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

        // ====================================================================
        // STAGE 3: IN-MEMORY GAME OBJECT FALLBACK
        // ====================================================================
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

    private static Texture2D CreateReadableCroppedTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;

        Texture2D sourceTex = sprite.texture;
        Rect textureRect = sprite.textureRect;

        int width = (int)textureRect.width;
        int height = (int)textureRect.height;

        RenderTexture tempRT = RenderTexture.GetTemporary(
            sourceTex.width,
            sourceTex.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default
        );

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = tempRT;

        GL.Clear(false, true, new Color(1f, 1f, 1f, 0f));

        Graphics.Blit(sourceTex, tempRT);

        Texture2D readableCopy = new(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, false);
        readableCopy.ReadPixels(new Rect(0, 0, sourceTex.width, sourceTex.height), 0, 0);
        readableCopy.Apply();

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(tempRT);

        Texture2D readableCroppedTex = new(width, height, TextureFormat.RGBA32, false);

        Color[] pixels = readableCopy.GetPixels((int)textureRect.x, (int)textureRect.y, width, height);

        readableCroppedTex.SetPixels(pixels);
        readableCroppedTex.Apply();

        UnityEngine.Object.Destroy(readableCopy);

        return readableCroppedTex;
    }

    public static Texture2D GetPlantTexture(int plantId)
    {
        if (PlantTextureOverrides.TryGetValue(plantId, out string texturePath))
        {
            Texture2D overrideTex = GetTexture(texturePath);
            if (overrideTex != null) return overrideTex;
        }

        string rawName = ((PlantType)plantId).ToString();
        string enumNameLower = rawName.ToLower();
        string preferredPath = $"plants/{enumNameLower}/{enumNameLower}";

        Texture2D tex = GetTexture(preferredPath);
        string successfulPath = preferredPath;

        if (tex == null)
        {
            tex = GetTexture(rawName);
            successfulPath = rawName;
        }

#if MELONLOADER || BEPINEX
        if (tex == null)
        {
            if (!PlantTextureOverrides.ContainsKey(plantId))
            {
                PlantTextureOverrides[plantId] = preferredPath;
            }
        }
        else
        {
            if (!PlantTextureOverrides.ContainsKey(plantId))
            {
                PlantTextureOverrides[plantId] = successfulPath;
            }
        }
#endif
        return tex;
    }

    public static Texture2D GetZombieTexture(int zombieId)
    {
        if (ZombieTextureOverrides.TryGetValue(zombieId, out string texturePath))
        {
            Texture2D overrideTex = GetTexture(texturePath);
            if (overrideTex != null) return overrideTex;
        }

        string rawName = ((ZombieType)zombieId).ToString();
        string enumNameLower = rawName.ToLower();
        string preferredPath = $"zombies/{enumNameLower}/{enumNameLower}";

        Texture2D tex = GetTexture(preferredPath);
        string successfulPath = preferredPath;

        if (tex == null)
        {
            tex = GetTexture(rawName);
            successfulPath = rawName;
        }

#if MELONLOADER || BEPINEX
        if (tex == null)
        {
            if (!ZombieTextureOverrides.ContainsKey(zombieId))
            {
                ZombieTextureOverrides[zombieId] = preferredPath;
                Utils.SaveLoad.Save();
            }
        }
        else
        {
            if (!ZombieTextureOverrides.ContainsKey(zombieId))
            {
                ZombieTextureOverrides[zombieId] = successfulPath;
                Utils.SaveLoad.Save();
            }
        }
#endif
        return tex;
    }

    public static void ClearCache()
    {
        _textureCache.Clear();
        _spriteMemoryCache.Clear();
        _rawTexMemoryCache.Clear();
        _hasScannedMemory = false;
    }

    public static void SaveTextureOverrides()
    {
        string texturePath = Api.PathsManager.TextureDataPath;
        try
        {
            var texData = new SaveLoadData.TextureSaveData
            {
                PlantTextureOverrides = PlantTextureOverrides ?? new(),
                ZombieTextureOverrides = ZombieTextureOverrides ?? new()
            };
            string dir = System.IO.Path.GetDirectoryName(texturePath);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(texturePath, Newtonsoft.Json.JsonConvert.SerializeObject(texData, Newtonsoft.Json.Formatting.Indented));
        }
        catch (System.Exception ex)
        {
            Magnetar_Logger.AutoSaveLogger.Error($"Failed to save TextureData: {ex.Message}");
        }
    }

    public static void LoadTextureOverrides()
    {
        string texturePath = Api.PathsManager.TextureDataPath;
        if (System.IO.File.Exists(texturePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(texturePath);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveLoadData.TextureSaveData>(json);
                if (data != null)
                {
                    PlantTextureOverrides = data.PlantTextureOverrides ?? new();
                    ZombieTextureOverrides = data.ZombieTextureOverrides ?? new();
                }
            }
            catch (System.Exception ex)
            {
                Magnetar_Logger.AutoSaveLogger.Error($"Failed to load TextureData: {ex.Message}");
            }
        }
    }
}
