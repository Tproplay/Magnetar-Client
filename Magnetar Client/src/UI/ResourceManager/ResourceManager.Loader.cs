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
    public static Dictionary<int, string> BulletTextureOverrides = new();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Internal Json deserialization format
    /// </summary>
    public class TextureSaveData
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Dictionary<int, string> PlantTextureOverrides = new();
        public Dictionary<int, string> ZombieTextureOverrides = new();
        public Dictionary<int, string> BulletTextureOverrides = new();
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
                    BulletTextureOverrides = data.BulletTextureOverrides ?? new();
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

    /// <summary>
    /// Gets the texture of bullet from <see cref="ResourcesManager.bulletPrefabs"/>
    /// </summary>
    public static Texture2D GetBulletTexture(int bulletId)
    {
        string cacheKey = $"game_bullet_prefab_{bulletId}";
        if (_textureCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            return cached;

        try
        {
            var resManager = GameAPP.resourcesManager;
            if (resManager != null && resManager.bulletPrefabs != null)
            {
                BulletType type = (BulletType)bulletId;
                if (resManager.bulletPrefabs.TryGetValue(type, out GameObject bulletObj) && bulletObj != null)
                {
                    Sprite sprite = ExtractBestBulletSprite(bulletObj);

                    // If no sprite found directly in prefab asset, check for animated bullet
                    if (sprite == null)
                    {
                        try
                        {
                            GameObject temp = UnityEngine.Object.Instantiate(bulletObj);
                            try
                            {
                                temp.hideFlags = HideFlags.HideAndDontSave;
                                var anim = temp.GetComponentInChildren<Animator>(true);
                                if (anim != null)
                                {
                                    anim.Update(0f);
                                }
                                sprite = ExtractBestBulletSprite(temp);
                            }
                            finally
                            {
                                UnityEngine.Object.DestroyImmediate(temp);
                            }
                        }
                        catch { }
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

                    // Fallback to particle or mesh renderer material texture
                    Renderer[] otherRenderers = bulletObj.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in otherRenderers)
                    {
                        if (r == null || r is SpriteRenderer) continue;
                        if (r.gameObject.name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (r.sharedMaterial != null && r.sharedMaterial.mainTexture is Texture2D matTex)
                        {
                            if (matTex.name != null && matTex.name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                            _textureCache[cacheKey] = matTex;
                            return matTex;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Magnetar_Logger.GUILogger.Error($"Error loading bullet texture {bulletId}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Gets the texture of bullet from <see cref="ResourcesManager.bulletPrefabs"/>
    /// </summary>
    public static Texture2D GetBulletTexture(BulletType bulletType) => GetBulletTexture((int)bulletType);

    /// <summary>
    /// Extracts the most prominent visual Sprite from a bullet prefab hierarchy,
    /// intelligently skipping shadow objects, particle emitters, and sub-components.
    /// </summary>
    private static Sprite ExtractBestBulletSprite(GameObject root)
    {
        if (root == null) return null;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        Sprite bestSprite = null;
        float bestScore = -1f;

        Transform shadowTransform = null;
        try
        {
            Bullet bulletComp = root.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                shadowTransform = bulletComp.shadow;
            }
        }
        catch { }

        foreach (var sr in renderers)
        {
            if (sr == null || sr.sprite == null) continue;

            // Exclude shadow transforms and shadow game objects
            if (shadowTransform != null && (sr.transform == shadowTransform || sr.transform.IsChildOf(shadowTransform)))
                continue;

            string goName = sr.gameObject.name;
            if (goName.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            if (sr.sprite.name != null && sr.sprite.name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            // Score heuristic based on area, hierarchy position, and naming
            float score = sr.sprite.rect.width * sr.sprite.rect.height;

            if (sr.gameObject == root)
                score += 5000f;

            string lowerName = goName.ToLowerInvariant();
            if (lowerName.Contains("bullet") || lowerName.Contains("body") || lowerName.Contains("main"))
                score += 10000f;
            else if (lowerName.Contains("head") || lowerName.Contains("render"))
                score += 3000f;

            score += sr.sortingOrder * 10f;

            if (lowerName.Contains("particle") || lowerName.Contains("glow") || lowerName.Contains("trail") || lowerName.Contains("effect"))
                score *= 0.1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestSprite = sr.sprite;
            }
        }

        if (bestSprite != null)
            return bestSprite;

        // UI Image fallback
        var images = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in images)
        {
            if (img != null && img.sprite != null && img.gameObject.name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return img.sprite;
            }
        }

        return null;
    }

    private static Texture2D CreateReadableCroppedTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;

        // Use textureRect when sprite is packed/atlassed; fallback to rect
        Rect targetRect = sprite.rect;
        if (sprite.packed && sprite.textureRect.width > 0 && sprite.textureRect.height > 0)
        {
            targetRect = sprite.textureRect;
        }

        int x = Mathf.Clamp(Mathf.RoundToInt(targetRect.x), 0, sprite.texture.width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(targetRect.y), 0, sprite.texture.height - 1);
        int width = Mathf.Clamp(Mathf.RoundToInt(targetRect.width), 1, sprite.texture.width - x);
        int height = Mathf.Clamp(Mathf.RoundToInt(targetRect.height), 1, sprite.texture.height - y);

        // Entire texture fast-path: return original texture directly without any conversion or blit!
        if (width == sprite.texture.width && height == sprite.texture.height && x == 0 && y == 0)
        {
            return sprite.texture;
        }

        bool isLinear = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        // Fast-path for readable textures on CPU
        if (sprite.texture.isReadable)
        {
            try
            {
                Color[] pixels = sprite.texture.GetPixels(x, y, width, height);
                Texture2D readableTex = new Texture2D(width, height, TextureFormat.RGBA32, false, isLinear);
                readableTex.SetPixels(pixels);
                readableTex.Apply();
                return readableTex;
            }
            catch { }
        }

        // GPU blit fallback for unreadable/atlassed textures
        RenderTexture tmp = RenderTexture.GetTemporary(
            sprite.texture.width,
            sprite.texture.height,
            0,
            RenderTextureFormat.Default,
            isLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);

        RenderTexture previous = RenderTexture.active;
        try
        {
            Graphics.Blit(sprite.texture, tmp);
            RenderTexture.active = tmp;

            Texture2D cropped = new Texture2D(width, height, TextureFormat.RGBA32, false, isLinear);
            cropped.ReadPixels(new Rect(x, y, width, height), 0, 0);
            cropped.Apply();
            return cropped;
        }
        catch (Exception ex)
        {
            Magnetar_Logger.GUILogger.Error($"Error in CreateReadableCroppedTexture: {ex.Message}");
            return null;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
        }
    }

    /// <summary>
    /// Gets whether entity textures have been pre-warmed.
    /// </summary>
    public static bool EntitiesWarmedUp { get; private set; }
    private static bool _isWarmingUp;

    /// <summary>
    /// Warms up and pre-caches all Plant, Zombie, and Bullet textures ahead of time.
    /// If invoked during an OnGUI pass, draws them offscreen to warm up the IMGUI render pipeline.
    /// </summary>
    public static void WarmUpAllEntityTextures()
    {
        if (EntitiesWarmedUp || _isWarmingUp) return;

        try
        {
            var resManager = GameAPP.resourcesManager;
            if (resManager == null) return;

            _isWarmingUp = true;

            // Plants
            foreach (PlantType pt in Enum.GetValues(typeof(PlantType)))
            {
                int id = (int)pt;
                if (id < 0 || Modules.Module.Banned.PlantTypeBanned.Contains(id)) continue;
                Texture2D tex = GetPlantTexture(id);
                WarmUpDraw(tex);
            }

            // Zombies
            foreach (ZombieType zt in Enum.GetValues(typeof(ZombieType)))
            {
                int id = (int)zt;
                if (id < 0 || Modules.Module.Banned.ZombieTypeBanned.Contains(id)) continue;
                Texture2D tex = GetZombieTexture(id);
                WarmUpDraw(tex);
            }

            // Bullets
            foreach (BulletType bt in Enum.GetValues(typeof(BulletType)))
            {
                int id = (int)bt;
                if (id < 0) continue;
                Texture2D tex = GetBulletTexture(id);
                WarmUpDraw(tex);
            }

            EntitiesWarmedUp = true;
        }
        catch (Exception ex)
        {
            Magnetar_Logger.GUILogger.Error($"Error during WarmUpAllEntityTextures: {ex.Message}");
        }
        finally
        {
            _isWarmingUp = false;
        }
    }

    private static void WarmUpDraw(Texture2D tex)
    {
        if (tex == null) return;
        try
        {
            GUIStyle style = WindowDrawing.DrawSetting.GetCachedEntityStyle(tex);
            if (Event.current != null && style != null)
            {
                GUI.Box(new Rect(-10000, -10000, 1, 1), GUIContent.none, style);
            }
        }
        catch { }
    }
}
