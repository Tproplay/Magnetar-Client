using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime;
using Magnetar_Client;
using Magnetar_Client.Api;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace AudioAddon;

/// <summary>
/// Handles audio clip loading and caching across both AssetBundles (native streaming, ~0 RAM)
/// and loose files (AudioImportLib / SherkeyXD.BepInEx-Sounds-Loader for user custom files).
/// </summary>
public static class AudioClipLoader
{
    private static readonly Dictionary<string, AudioClip> _clipCache = new(StringComparer.OrdinalIgnoreCase);

    public static AssetBundle MusicBundle { get; private set; }
    public static readonly Dictionary<string, string> BundleAssetMap = new(StringComparer.OrdinalIgnoreCase);

    public static string BundlePath => Path.Combine(PathsManager.AddonsDir, "Custom Audios", "MusicAB");

    /// <summary>
    /// Loads the music AssetBundle directly from Custom Audios/MusicAB.
    /// </summary>
    public static bool EnsureMusicBundleLoaded()
    {
        if (MusicBundle != null) return true;

        string bundlePath = BundlePath;
        if (!File.Exists(bundlePath)) return false;

        try
        {
            MusicBundle = AssetBundle.LoadFromFile(bundlePath);
            if (MusicBundle != null)
            {
                BundleAssetMap.Clear();
                var assetNames = MusicBundle.GetAllAssetNames();
                foreach (var asset in assetNames)
                {
                    string cleanName = Path.GetFileNameWithoutExtension(asset);

                    // Preserve canonical PascalCase from MusicType enum if matching
                    foreach (var val in Enum.GetValues(typeof(MusicType)))
                    {
                        string enumName = val.ToString();
                        if (cleanName.Equals(enumName, StringComparison.OrdinalIgnoreCase))
                        {
                            cleanName = enumName;
                            break;
                        }
                    }

                    BundleAssetMap[cleanName] = asset;
                }
                MagnetarApi.LogMsg($"[Audio Addon] Loaded music AssetBundle from '{bundlePath}' with {BundleAssetMap.Count} track(s).");
                return true;
            }
        }
        catch (Exception ex)
        {
            MagnetarApi.LogError($"[Audio Addon] Failed to load AssetBundle from '{bundlePath}': {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Checks whether an audio track exists either in the AssetBundle or as a loose file.
    /// </summary>
    public static bool HasTrack(string trackKey)
    {
        if (string.IsNullOrEmpty(trackKey)) return false;
        if (BundleAssetMap.ContainsKey(trackKey)) return true;
        if (File.Exists(trackKey)) return true;
        return false;
    }

    /// <summary>
    /// Returns true if the clip is already cached in memory.
    /// </summary>
    public static bool IsLoaded(string sourceKey)
    {
        return !string.IsNullOrEmpty(sourceKey) && _clipCache.ContainsKey(sourceKey);
    }

    /// <summary>
    /// Gets a cached clip if already loaded.
    /// </summary>
    public static AudioClip GetCachedClip(string sourceKey)
    {
        if (string.IsNullOrEmpty(sourceKey)) return null;
        _clipCache.TryGetValue(sourceKey, out var clip);
        return clip;
    }

    /// <summary>
    /// Synchronously loads an AudioClip from the AssetBundle (streaming) or from a loose file.
    /// </summary>
    public static AudioClip LoadAudioClip(string sourceKey)
    {
        if (string.IsNullOrEmpty(sourceKey)) return null;

        if (_clipCache.TryGetValue(sourceKey, out var cached) && cached != null)
        {
            return cached;
        }

        AudioClip clip = null;

        // 1. Check if track is inside the AssetBundle (Streams natively with ~0 RAM)
        if (MusicBundle != null && BundleAssetMap.TryGetValue(sourceKey, out var bundleAssetName))
        {
            try
            {
                var assetObj = MusicBundle.LoadAsset(bundleAssetName, Il2CppType.Of<AudioClip>());
                clip = assetObj != null ? assetObj.TryCast<AudioClip>() : null;
            }
            catch (Exception ex)
            {
                MagnetarApi.LogError($"[Audio Addon] Failed to load '{bundleAssetName}' from AssetBundle: {ex.Message}");
            }
        }

        // 2. Fallback: Loose file on disk (User added custom file)
        if (clip == null && File.Exists(sourceKey))
        {
#if MELONLOADER || RELEASE_MELON
            try
            {
                clip = AudioImportLib.API.LoadAudioClip(sourceKey, true);
            }
            catch (Exception ex)
            {
                MagnetarApi.LogError($"[Audio Addon] AudioImportLib failed to load '{sourceKey}': {ex.Message}");
            }
#elif BEPINEX || RELEASE_BEPINEX
            try
            {
                clip = BepInEx_Sounds_Loader.AudioFactory.Instance.LoadClip(sourceKey);
            }
            catch (Exception ex)
            {
                MagnetarApi.LogError($"[Audio Addon] AudioFactory failed to load '{sourceKey}': {ex.Message}");
            }
#endif
        }

        if (clip != null)
        {
            _clipCache[sourceKey] = clip;
        }

        return clip;
    }

    /// <summary>
    /// Asynchronously loads an AudioClip across frames without freezing the game.
    /// </summary>
    public static ICoroutineHandle LoadAudioClipAsync(string sourceKey, Action<AudioClip> onLoaded)
    {
        if (string.IsNullOrEmpty(sourceKey))
        {
            onLoaded?.Invoke(null);
            return null;
        }

        if (_clipCache.TryGetValue(sourceKey, out var cached) && cached != null)
        {
            onLoaded?.Invoke(cached);
            return null;
        }

        return CoroutineManager.Start(LoadAsyncRoutine(sourceKey, onLoaded));
    }

    private static IEnumerator LoadAsyncRoutine(string sourceKey, Action<AudioClip> onLoaded)
    {
        yield return null;

        var clip = LoadAudioClip(sourceKey);
        onLoaded?.Invoke(clip);
    }

    /// <summary>
    /// Sequentially preloads audio tracks over frames without freezing the game.
    /// </summary>
    public static ICoroutineHandle PreloadAudiosAsync(IEnumerable<string> sourceKeys, Action<string, AudioClip> onTrackLoaded, Action onCompleted = null)
    {
        return CoroutineManager.Start(PreloadRoutine(sourceKeys, onTrackLoaded, onCompleted));
    }

    private static IEnumerator PreloadRoutine(IEnumerable<string> sourceKeys, Action<string, AudioClip> onTrackLoaded, Action onCompleted)
    {
        foreach (var key in sourceKeys)
        {
            if (string.IsNullOrEmpty(key)) continue;

            if (_clipCache.TryGetValue(key, out var cached) && cached != null)
            {
                onTrackLoaded?.Invoke(key, cached);
                continue;
            }

            yield return null; // Yield a frame before next asset

            var clip = LoadAudioClip(key);
            if (clip != null)
            {
                onTrackLoaded?.Invoke(key, clip);
            }
        }

        onCompleted?.Invoke();
    }

    /// <summary>
    /// Evicts clips from memory that are no longer actively playing, reclaiming RAM.
    /// </summary>
    public static void EvictUnusedClips(HashSet<string> activeKeys)
    {
        var keysToRemove = new List<string>();
        foreach (var kvp in _clipCache)
        {
            if (activeKeys == null || !activeKeys.Contains(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            if (_clipCache.TryGetValue(key, out var clip))
            {
                _clipCache.Remove(key);
                // If it was a loose decompressed file (not bundled), destroy it to free memory
                if (!BundleAssetMap.ContainsKey(key) && clip != null)
                {
                    try
                    {
                        UnityEngine.Object.Destroy(clip);
                    }
                    catch { }
                }
            }
        }
    }

    public static void ClearCache()
    {
        _clipCache.Clear();
    }
}
