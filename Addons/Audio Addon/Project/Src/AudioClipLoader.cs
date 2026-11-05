using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppInterop.Runtime;
using Magnetar_Client;
using Magnetar_Client.Api;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace AudioAddon;

public class BundledTrack
{
    public AssetBundle Bundle { get; set; }
    public string BundleFilePath { get; set; }
    public string BundleFileName { get; set; }
    public string AssetName { get; set; }
    public string CleanName { get; set; }
    public string DisplayName { get; set; }
    public bool IsDefaultSoundtrack { get; set; }
}

/// <summary>
/// Handles audio clip loading and caching across both AssetBundles (native streaming, ~0 RAM)
/// and loose files (AudioImportLib / SherkeyXD.BepInEx-Sounds-Loader for user custom files).
/// Searches all AssetBundles in Custom Audios/Music for music, and Custom Audios/Sfx for sound effects.
/// </summary>
public static class AudioClipLoader
{
    private static readonly Dictionary<string, AudioClip> _clipCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, AssetBundle> _loadedBundles = new(StringComparer.OrdinalIgnoreCase);

    public static readonly Dictionary<string, BundledTrack> MusicBundledTracks = new(StringComparer.OrdinalIgnoreCase);
    public static readonly Dictionary<string, BundledTrack> SfxBundledTracks = new(StringComparer.OrdinalIgnoreCase);

    // Backwards-compatibility map for code referencing BundleAssetMap
    public static readonly Dictionary<string, string> BundleAssetMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if a file is a valid Unity AssetBundle by verifying its binary header signature.
    /// </summary>
    public static bool IsAssetBundle(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;

        string ext = Path.GetExtension(filePath);
        var nonBundleExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".ogg", ".wav", ".flac", ".aiff",
            ".txt", ".json", ".xml", ".meta", ".png", ".jpg", ".jpeg", ".dll", ".manifest"
        };
        if (nonBundleExts.Contains(ext)) return false;

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length < 7) return false;
            byte[] magic = new byte[7];
            int read = fs.Read(magic, 0, 7);
            if (read < 7) return false;

            string sig = System.Text.Encoding.ASCII.GetString(magic);
            return sig == "UnityFS" || sig == "UnityWeb" || sig == "UnityRaw";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Scans for and loads all AssetBundles located strictly in Custom Audios/Music/.
    /// </summary>
    public static void ScanAndLoadMusicBundles(string musicDir)
    {
        if (!Directory.Exists(musicDir)) return;
        ScanMusicDirectory(musicDir);
    }

    private static void ScanMusicDirectory(string dir)
    {
        var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (!IsAssetBundle(file)) continue;

            string bundleFileName = Path.GetFileName(file);
            string bundleBaseName = Path.GetFileNameWithoutExtension(file);
            bool isDefaultSoundtrack = string.Equals(bundleFileName, "musicAB_windows", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(bundleFileName, "musicAB_android", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(bundleBaseName, "musicAB_windows", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(bundleBaseName, "musicAB_android", StringComparison.OrdinalIgnoreCase);

            if (!_loadedBundles.TryGetValue(file, out var bundle) || bundle == null)
            {
                try
                {
                    bundle = AssetBundle.LoadFromFile(file);
                    if (bundle != null)
                    {
                        _loadedBundles[file] = bundle;
                    }
                }
                catch (Exception ex)
                {
                    MagnetarApi.LogError($"[Audio Addon] Failed to load music AssetBundle '{file}': {ex.Message}");
                    continue;
                }
            }

            if (bundle != null)
            {
                var assetNames = bundle.GetAllAssetNames();
                foreach (var asset in assetNames)
                {
                    string cleanName = Path.GetFileNameWithoutExtension(asset);

                    // Match canonical PascalCase from MusicType enum if applicable
                    foreach (var val in Enum.GetValues(typeof(MusicType)))
                    {
                        string enumName = val.ToString();
                        if (cleanName.Equals(enumName, StringComparison.OrdinalIgnoreCase))
                        {
                            cleanName = enumName;
                            break;
                        }
                    }

                    string key = cleanName;
                    // If track already exists from another bundle, disambiguate key
                    if (MusicBundledTracks.ContainsKey(key))
                    {
                        key = $"{cleanName} ({bundleFileName})";
                    }

                    var track = new BundledTrack
                    {
                        Bundle = bundle,
                        BundleFilePath = file,
                        BundleFileName = bundleFileName,
                        AssetName = asset,
                        CleanName = cleanName,
                        DisplayName = isDefaultSoundtrack ? $"[Bundled] {cleanName}" : $"[Bundled: {bundleFileName}] {cleanName}",
                        IsDefaultSoundtrack = isDefaultSoundtrack
                    };

                    MusicBundledTracks[key] = track;
                    BundleAssetMap[cleanName] = asset;
                }

                MagnetarApi.LogMsg($"[Audio Addon] Loaded music AssetBundle '{bundleFileName}' with {assetNames.Length} track(s).");
            }
        }
    }

    /// <summary>
    /// Scans for and loads all AssetBundles located strictly in Custom Audios/Sfx/.
    /// </summary>
    public static void ScanAndLoadSfxBundles(string sfxDir)
    {
        if (!Directory.Exists(sfxDir)) return;
        ScanSfxDirectory(sfxDir);
    }

    private static void ScanSfxDirectory(string dir)
    {
        var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (!IsAssetBundle(file)) continue;

            string bundleFileName = Path.GetFileName(file);

            if (!_loadedBundles.TryGetValue(file, out var bundle) || bundle == null)
            {
                try
                {
                    bundle = AssetBundle.LoadFromFile(file);
                    if (bundle != null)
                    {
                        _loadedBundles[file] = bundle;
                    }
                }
                catch (Exception ex)
                {
                    MagnetarApi.LogError($"[Audio Addon] Failed to load SFX AssetBundle '{file}': {ex.Message}");
                    continue;
                }
            }

            if (bundle != null)
            {
                var assetNames = bundle.GetAllAssetNames();
                foreach (var asset in assetNames)
                {
                    string cleanName = Path.GetFileNameWithoutExtension(asset);

                    // Match canonical PascalCase from SoundType enum if applicable
                    foreach (var val in Enum.GetValues(typeof(SoundType)))
                    {
                        string enumName = val.ToString();
                        if (cleanName.Equals(enumName, StringComparison.OrdinalIgnoreCase))
                        {
                            cleanName = enumName;
                            break;
                        }
                    }

                    string key = $"bundled:sfx:{bundleFileName}:{cleanName}";
                    SfxBundledTracks[key] = new BundledTrack
                    {
                        Bundle = bundle,
                        BundleFilePath = file,
                        BundleFileName = bundleFileName,
                        AssetName = asset,
                        CleanName = cleanName,
                        DisplayName = $"[Bundled: {bundleFileName}] {cleanName}"
                    };
                }

                MagnetarApi.LogMsg($"[Audio Addon] Loaded SFX AssetBundle '{bundleFileName}' with {assetNames.Length} sound(s).");
            }
        }
    }

    /// <summary>
    /// Checks whether an audio track exists either in an AssetBundle or as a loose file.
    /// </summary>
    public static bool HasTrack(string trackKey)
    {
        if (string.IsNullOrEmpty(trackKey)) return false;
        if (MusicBundledTracks.ContainsKey(trackKey)) return true;
        if (SfxBundledTracks.ContainsKey(trackKey)) return true;
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
    /// Synchronously loads an AudioClip from an AssetBundle (streaming) or from a loose file.
    /// </summary>
    public static AudioClip LoadAudioClip(string sourceKey)
    {
        if (string.IsNullOrEmpty(sourceKey)) return null;

        if (_clipCache.TryGetValue(sourceKey, out var cached) && cached != null)
        {
            return cached;
        }

        AudioClip clip = null;

        // 1. Check Music AssetBundles
        BundledTrack musicTrack = null;
        if (MusicBundledTracks.TryGetValue(sourceKey, out var directMusic))
        {
            musicTrack = directMusic;
        }
        else
        {
            musicTrack = MusicBundledTracks.Values.FirstOrDefault(t => t.CleanName.Equals(sourceKey, StringComparison.OrdinalIgnoreCase));
        }

        if (musicTrack != null && musicTrack.Bundle != null)
        {
            try
            {
                var assetObj = musicTrack.Bundle.LoadAsset(musicTrack.AssetName, Il2CppType.Of<AudioClip>());
                clip = assetObj != null ? assetObj.TryCast<AudioClip>() : null;
            }
            catch (Exception ex)
            {
                MagnetarApi.LogError($"[Audio Addon] Failed to load music '{musicTrack.AssetName}' from AssetBundle: {ex.Message}");
            }
        }

        // 2. Check SFX AssetBundles
        if (clip == null)
        {
            BundledTrack sfxTrack = null;
            if (SfxBundledTracks.TryGetValue(sourceKey, out var directSfx))
            {
                sfxTrack = directSfx;
            }
            else
            {
                sfxTrack = SfxBundledTracks.Values.FirstOrDefault(t => t.CleanName.Equals(sourceKey, StringComparison.OrdinalIgnoreCase));
            }

            if (sfxTrack != null && sfxTrack.Bundle != null)
            {
                try
                {
                    var assetObj = sfxTrack.Bundle.LoadAsset(sfxTrack.AssetName, Il2CppType.Of<AudioClip>());
                    clip = assetObj != null ? assetObj.TryCast<AudioClip>() : null;
                }
                catch (Exception ex)
                {
                    MagnetarApi.LogError($"[Audio Addon] Failed to load SFX '{sfxTrack.AssetName}' from AssetBundle: {ex.Message}");
                }
            }
        }

        // 3. Fallback: Loose file on disk (User added custom file)
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
                // Only destroy loose decompressed audio files; never destroy streaming bundle clips
                bool isBundled = MusicBundledTracks.ContainsKey(key) || SfxBundledTracks.ContainsKey(key) || BundleAssetMap.ContainsKey(key);
                if (!isBundled && clip != null)
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
