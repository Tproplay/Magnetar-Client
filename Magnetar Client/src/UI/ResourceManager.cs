using Il2CppSystem.IO;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;
using File = System.IO.File;
using Path = System.IO.Path;

#if BEPINEX || RELEASE_BEPINEX || ANDROID
using Il2CppInterop.Runtime;
#endif

namespace Magnetar_Client.UI;

public static class ResourceManager
{
    private static readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Font> _fontCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Finds and loads the client AssetBundle from ResourceDirectory or legacy ModsDir locations.
    /// </summary>
    public static AssetBundle LoadClientBundle()
    {
        foreach (var b in AssetBundle.GetAllLoadedAssetBundles().ToArray())
        {
            if (b != null && string.Equals(b.name, Path.GetFileName(MagnetarUIABPath), StringComparison.OrdinalIgnoreCase))
                return b;
        }

        if (File.Exists(MagnetarUIABPath))
        {
            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(MagnetarUIABPath);
                if (bundle != null)
                {
                    GUILogger.Msg($"Successfully loaded AssetBundle from '{MagnetarUIABPath}'");
                    return bundle;
                }
            }
            catch (Exception ex)
            {
                GUILogger.Error($"Failed loading AssetBundle at '{MagnetarUIABPath}': {ex.Message}");
            }
        }

        return null;
    }

    #region Font Loading via AssetBundle
    /// <summary>
    /// Loads a pre-compiled Font asset directly from the AssetBundle.
    /// </summary>
    public static Font LoadFont(string assetName = "Magnetar_font")
    {
        if (string.IsNullOrWhiteSpace(assetName)) return null;

        if (_fontCache.TryGetValue(assetName, out var cached) && cached != null)
            return cached;

        AssetBundle bundle = LoadClientBundle();
        if (bundle != null)
        {
            try
            {
                string cleanName = Path.GetFileNameWithoutExtension(assetName);
                string[] possibleNames = new[] { assetName, cleanName, "Magnetar_font", "font" };

                foreach (string name in possibleNames)
                {
#if MELONLOADER || RELEASE_MELON
                    Font font = bundle.LoadAsset<Font>(name);
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
                    var raw = bundle.LoadAsset(name, Il2CppType.Of<Font>());
                    Font font = raw != null ? raw.TryCast<Font>() : null;
#endif
                    if (font != null)
                    {
                        font.hideFlags = HideFlags.DontSave;
                        _fontCache[assetName] = font;
                        _fontCache[cleanName] = font;
                        GUILogger.Msg($"Loaded Font '{name}' from AssetBundle.");
                        return font;
                    }
                }
            }
            catch (Exception ex)
            {
                GUILogger.Error($"Error loading Font '{assetName}' from AssetBundle: {ex.Message}");
            }
        }

        // fallback: borrow any existing loaded font in the game if the bundle is missing
        try
        {
            var loaded = Resources.FindObjectsOfTypeAll<Font>();
            for (int i = 0; i < loaded.Count; i++)
            {
                var f = loaded[i];
                if (f != null && f.material != null && !string.IsNullOrEmpty(f.name))
                {
                    _fontCache[assetName] = f;
                    return f;
                }
            }
        }
        catch { }

        return null;
    }
    #endregion

    #region Texture & Sprite Loading
    /// <summary>
    /// Loads a Texture2D from the AssetBundle, falling back to direct disk loading if present.
    /// </summary>
    public static Texture2D LoadTexture(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName)) return null;

        if (_textureCache.TryGetValue(assetName, out var cached) && cached != null)
            return cached;

        string cleanName = Path.GetFileNameWithoutExtension(assetName);

        // 1. Try loading from AssetBundle
        AssetBundle bundle = LoadClientBundle();
        if (bundle != null)
        {
            try
            {
                string[] targets = new[] { assetName, cleanName, $"assets/assets/{assetName}", $"assets/assets/{cleanName}.png" };
                foreach (string name in targets)
                {
#if MELONLOADER || RELEASE_MELON
                    Texture2D tex = bundle.LoadAsset<Texture2D>(name);
                    if (tex != null)
                    {
                        _textureCache[assetName] = tex;
                        return tex;
                    }

                    Sprite spr = bundle.LoadAsset<Sprite>(name);
                    if (spr != null && spr.texture != null)
                    {
                        _textureCache[assetName] = spr.texture;
                        return spr.texture;
                    }
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
                    var rawTex = bundle.LoadAsset(name, Il2CppType.Of<Texture2D>());
                    if (rawTex != null)
                    {
                        Texture2D tex = rawTex.TryCast<Texture2D>();
                        if (tex != null)
                        {
                            _textureCache[assetName] = tex;
                            return tex;
                        }
                    }

                    var rawSpr = bundle.LoadAsset(name, Il2CppType.Of<Sprite>());
                    if (rawSpr != null)
                    {
                        Sprite spr = rawSpr.TryCast<Sprite>();
                        if (spr != null && spr.texture != null)
                        {
                            _textureCache[assetName] = spr.texture;
                            return spr.texture;
                        }
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                GUILogger.Error($"Error loading Texture '{assetName}' from AssetBundle: {ex.Message}");
            }
        }

        // 2. Loose disk fallback (e.g., custom user PNG drops in Resources folder)
        string diskPath = ResolveDiskPath(assetName);
        if (diskPath != null && File.Exists(diskPath))
        {
            try
            {
                byte[] rawBytes = File.ReadAllBytes(diskPath);
                Texture2D diskTex = new(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };

                if (ImageConversion.LoadImage(diskTex, rawBytes))
                {
                    _textureCache[assetName] = diskTex;
                    return diskTex;
                }
            }
            catch (Exception ex)
            {
                GUILogger.Error($"Failed loading loose texture from '{diskPath}': {ex.Message}");
            }
        }

        return null;
    }

    public static Texture2D LoadLogoTexture()
    {
        return LoadTexture("magnetar_logo")
            ?? LoadTexture("Magnetar_logo.png")
            ?? LoadTexture("logo");
    }

    private static string ResolveDiskPath(string relativePath)
    {
        string direct = Path.Combine(ResouceDataDir, relativePath);
        if (File.Exists(direct)) return direct;

        string withExt = direct.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? direct : direct + ".png";
        if (File.Exists(withExt)) return withExt;

        return null;
    }
    #endregion

    public static void ClearCache()
    {
        _textureCache.Clear();
        _spriteCache.Clear();
        _fontCache.Clear();
    }
}