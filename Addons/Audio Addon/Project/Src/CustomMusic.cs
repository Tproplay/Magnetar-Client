using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HarmonyLib;
using Magnetar_Client;
using Magnetar_Client.Api;
using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace AudioAddon;

public class CustomMusic : Module
{
    // Mod Info
    public override string Name { get; set; } = "Custom Music";
    public override string Description { get; set; } = "Allows you to change ingame's music.";
    public override string SearchHints { get; set; } = "custommusic musicchanger soundtrackchanger audiomod customsoundtrack" +
        " backgroundmusic bgmchanger musicplayer soundchanger soundtracks audioinjector musicmod songswitcher audioextender " +
        "musicswitcher audioextension songmanager trackchanger audiopack musicmanager soundpack customaudio";
    public override string Author { get; set; } = "Tproplay";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Addon;
    public override bool EnableInVanillaMode { get; set; } = true;

    // Mod Data
    public static CustomMusic Instance { get; private set; }

    public static MusicType CurrentPlayingTrack = MusicType.MainMenu;

    public readonly Dictionary<MusicType, SelectSetting> TrackSettings = new();

    private readonly List<string> _audioFiles = new();
    private static readonly Dictionary<MusicType, AudioClip> _vanillaMusics = new();
    private readonly Dictionary<int, string> _optionsDict = new();

    private ICoroutineHandle _activeLoadHandle;
    private static bool _isDownloading = false;
    private static bool _isInitializingBundles = false;
    private static bool _hasLocalBundles = false;
    private static MusicType? _pendingPlayTrack = null;

    public static string AudioDir => GetAudioDir();

    public CustomMusic()
    {
        Instance = this;

        _hasLocalBundles = HasLocalBundles();
        _isInitializingBundles = _hasLocalBundles;

        // 1. Initial lightweight scan (loose custom files only - completes in < 1ms, zero startup freeze)
        ScanAudioFiles(scanBundlesSynchronously: false);

        // 2. Dynamically categorize and create settings for all in-game music tracks
        BuildDynamicTrackSettings();

        // 3. Asynchronously load AssetBundles in background and check for missing downloads
        CoroutineManager.Start(InitializeBundlesRoutine());
    }

    private static bool HasLocalBundles()
    {
        try
        {
            string dir = AudioDir;
            if (!Directory.Exists(dir)) return false;
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                if (AudioClipLoader.IsAssetBundle(f)) return true;
            }
        }
        catch { }
        return false;
    }

    private IEnumerator InitializeBundlesRoutine()
    {
        _isInitializingBundles = _hasLocalBundles;
        try
        {
            string creditsPath = Path.Combine(PathsManager.AddonsDir, "Custom Audios", "Credits.txt");
            string targetBundleName = MagnetarInfo.Platform == MagnetarInfo.PlatformType.Android ? "musicAB_android" : "musicAB_windows";
            string targetPath = Path.Combine(AudioDir, targetBundleName);

            // 1. Asynchronously load all existing AssetBundles in Custom Audios/Music/ across frames
            yield return AudioClipLoader.ScanAndLoadMusicBundlesRoutine(AudioDir);

            bool hasDefaultBundle = AudioClipLoader.MusicBundledTracks.Values.Any(t => t.IsDefaultSoundtrack);
            bool bundleExists = hasDefaultBundle || (File.Exists(targetPath) && AudioClipLoader.IsAssetBundle(targetPath));
            bool creditsExist = File.Exists(creditsPath);

            // Refresh settings with loaded bundle tracks
            if (AudioClipLoader.MusicBundledTracks.Count > 0)
            {
                RefreshTrackSettingsAfterBundleLoaded();
            }

            // 2. If default bundle or credits are missing, download in background
            if (!bundleExists || !creditsExist)
            {
                yield return DownloadMissingFilesRoutine(targetBundleName, targetPath, !bundleExists, creditsPath, !creditsExist);
            }
        }
        finally
        {
            _isInitializingBundles = false;

            if (_pendingPlayTrack.HasValue && Active)
            {
                var trackToPlay = _pendingPlayTrack.Value;
                _pendingPlayTrack = null;
                CurrentPlayingTrack = trackToPlay;
                ApplyCurrentTrackOverride();
            }
        }
    }

    private IEnumerator DownloadMissingFilesRoutine(
        string targetBundleName,
        string targetPath,
        bool needBundleDownload,
        string creditsPath,
        bool needCreditsDownload)
    {
        if (_isDownloading) yield break;
        _isDownloading = true;

        if (needBundleDownload)
        {
            MagnetarApi.LogMsg($"[Audio Addon] Default soundtrack '{targetBundleName}' not found in Custom Audios/Music/. Downloading in background...");
        }
        if (needCreditsDownload)
        {
            MagnetarApi.LogMsg("[Audio Addon] Credits.txt not found in Custom Audios/. Downloading in background...");
        }

        bool downloadDone = false;
        Exception bundleError = null;
        Exception creditsError = null;
        string tempBundlePath = targetPath + ".download.tmp";
        string tempCreditsPath = creditsPath + ".download.tmp";

        _ = Task.Run(async () =>
        {
            try
            {
                using var handler = new HttpClientHandler { AllowAutoRedirect = true };
                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromMinutes(10)
                };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Magnetar-AudioAddon");

                // 1. Download Credits.txt if needed
                if (needCreditsDownload)
                {
                    try
                    {
                        string[] creditsUrls = new[]
                        {
                            "https://raw.githubusercontent.com/Tproplay/Magnetar-Client/master/Addons/Audio%20Addon/Project/Custom%20Audios/Credits.txt",
                            "https://raw.githubusercontent.com/Tproplay/Magnetar-Client/devlop/Addons/Audio%20Addon/Project/Custom%20Audios/Credits.txt",
                            "https://github.com/Tproplay/Magnetar-Client/raw/master/Addons/Audio%20Addon/Project/Custom%20Audios/Credits.txt"
                        };

                        HttpResponseMessage creditsResp = null;
                        foreach (var url in creditsUrls)
                        {
                            try
                            {
                                var req = new HttpRequestMessage(HttpMethod.Get, url);
                                var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                                if (resp.IsSuccessStatusCode)
                                {
                                    creditsResp = resp;
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (creditsResp != null)
                        {
                            string creditsDir = Path.GetDirectoryName(creditsPath);
                            if (!string.IsNullOrEmpty(creditsDir) && !Directory.Exists(creditsDir))
                            {
                                Directory.CreateDirectory(creditsDir);
                            }

                            using (var stream = await creditsResp.Content.ReadAsStreamAsync())
                            using (var fs = new FileStream(tempCreditsPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                            {
                                await stream.CopyToAsync(fs);
                            }

                            if (File.Exists(creditsPath))
                            {
                                File.Delete(creditsPath);
                            }
                            File.Move(tempCreditsPath, creditsPath);
                        }
                        else
                        {
                            creditsError = new Exception("Unable to reach GitHub raw download servers for Credits.txt.");
                        }
                    }
                    catch (Exception ex)
                    {
                        creditsError = ex;
                        try { if (File.Exists(tempCreditsPath)) File.Delete(tempCreditsPath); } catch { }
                    }
                }

                // 2. Download AssetBundle if needed
                if (needBundleDownload)
                {
                    try
                    {
                        string[] bundleUrls = new[]
                        {
                            $"https://raw.githubusercontent.com/Tproplay/Magnetar-Client/master/Addons/Audio%20Addon/Project/Custom%20Audios/{targetBundleName}",
                            $"https://raw.githubusercontent.com/Tproplay/Magnetar-Client/devlop/Addons/Audio%20Addon/Project/Custom%20Audios/{targetBundleName}",
                            $"https://github.com/Tproplay/Magnetar-Client/raw/master/Addons/Audio%20Addon/Project/Custom%20Audios/{targetBundleName}"
                        };

                        HttpResponseMessage bundleResp = null;
                        foreach (var url in bundleUrls)
                        {
                            try
                            {
                                var req = new HttpRequestMessage(HttpMethod.Get, url);
                                var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                                if (resp.IsSuccessStatusCode)
                                {
                                    bundleResp = resp;
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (bundleResp == null)
                        {
                            throw new Exception("Unable to reach GitHub raw download servers for AssetBundle.");
                        }

                        string bundleDir = Path.GetDirectoryName(targetPath);
                        if (!string.IsNullOrEmpty(bundleDir) && !Directory.Exists(bundleDir))
                        {
                            Directory.CreateDirectory(bundleDir);
                        }

                        using (var stream = await bundleResp.Content.ReadAsStreamAsync())
                        using (var fs = new FileStream(tempBundlePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
                        {
                            await stream.CopyToAsync(fs);
                        }

                        if (!AudioClipLoader.IsAssetBundle(tempBundlePath))
                        {
                            try { File.Delete(tempBundlePath); } catch { }
                            throw new Exception("Downloaded file is corrupt or not a valid Unity AssetBundle.");
                        }

                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }
                        File.Move(tempBundlePath, targetPath);
                    }
                    catch (Exception ex)
                    {
                        bundleError = ex;
                        try { if (File.Exists(tempBundlePath)) File.Delete(tempBundlePath); } catch { }
                    }
                }
            }
            finally
            {
                downloadDone = true;
            }
        });

        while (!downloadDone)
        {
            yield return null;
        }

        _isDownloading = false;

        if (needCreditsDownload)
        {
            if (creditsError != null)
            {
                MagnetarApi.LogWarning($"[Audio Addon] Could not download Credits.txt: {creditsError.Message}");
            }
            else
            {
                MagnetarApi.LogMsg("[Audio Addon] Successfully downloaded Credits.txt.");
            }
        }

        if (needBundleDownload)
        {
            if (bundleError != null)
            {
                MagnetarApi.LogWarning($"[Audio Addon] Could not download '{targetBundleName}': {bundleError.Message}");
                yield break;
            }

            MagnetarApi.LogMsg($"[Audio Addon] Successfully downloaded '{targetBundleName}' ({new FileInfo(targetPath).Length / 1048576f:F1} MB). Loading soundtrack...");
            yield return AudioClipLoader.ScanAndLoadMusicBundlesRoutine(AudioDir);
            RefreshTrackSettingsAfterBundleLoaded();
        }
    }

    public void RefreshTrackSettingsAfterBundleLoaded()
    {
        ScanAudioFiles(scanBundlesSynchronously: false);

        foreach (var value in Enum.GetValues(typeof(MusicType)))
        {
            var track = (MusicType)value;
            string trackName = track.ToString();

            if (TrackSettings.TryGetValue(track, out var setting))
            {
                setting.Options = new Dictionary<int, string>(_optionsDict);

                // If setting was at vanilla default, preselect the newly downloaded bundle track
                if (setting.Value == 0)
                {
                    for (int i = 0; i < _audioFiles.Count; i++)
                    {
                        string item = _audioFiles[i];
                        if (AudioClipLoader.MusicBundledTracks.TryGetValue(item, out var bTrack) &&
                            bTrack.IsDefaultSoundtrack &&
                            bTrack.CleanName.Equals(trackName, StringComparison.OrdinalIgnoreCase))
                        {
                            setting.Value = i + 1;
                            break;
                        }
                    }
                }
            }
        }

        if (Active)
        {
            if (_pendingPlayTrack.HasValue)
            {
                CurrentPlayingTrack = _pendingPlayTrack.Value;
                _pendingPlayTrack = null;
            }
            ApplyCurrentTrackOverride();
        }
    }

    private static string GetAudioDir()
    {
        string dir = Path.Combine(PathsManager.AddonsDir, "Custom Audios", "Music");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    private void ScanAudioFiles(bool scanBundlesSynchronously = false)
    {
        _audioFiles.Clear();
        _optionsDict.Clear();
        _optionsDict[0] = "Default (Vanilla)";

        string dir = AudioDir;
        if (!Directory.Exists(dir)) return;

        // 1. Scan and load all AssetBundles in Custom Audios/Music/ only if explicitly requested
        if (scanBundlesSynchronously)
        {
            AudioClipLoader.ScanAndLoadMusicBundles(dir);
        }

        int optionIndex = 1;

        // 2. Discover Bundled tracks (streams from disk with ~0 RAM)
        if (AudioClipLoader.MusicBundledTracks.Count > 0)
        {
            foreach (var kvp in AudioClipLoader.MusicBundledTracks)
            {
                _audioFiles.Add(kvp.Key);
                _optionsDict[optionIndex] = kvp.Value.DisplayName;
                optionIndex++;
            }
        }

        // 3. Discover loose audio files (.mp3, .ogg, .wav)
        var supportedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp3", ".ogg", ".wav" };
        var files = Directory.GetFiles(dir);

        foreach (var file in files)
        {
            string ext = Path.GetExtension(file);
            if (supportedExts.Contains(ext))
            {
                _audioFiles.Add(file);
                string fileName = Path.GetFileName(file);
                _optionsDict[optionIndex] = AudioClipLoader.MusicBundledTracks.Count > 0 ? $"[Custom] {fileName}" : fileName;
                optionIndex++;
            }
        }
    }

    private void BuildDynamicTrackSettings()
    {
        var categoryBuckets = new Dictionary<string, List<(MusicType track, SelectSetting setting)>>
        {
            { "Adventure Stages", new() },
            { "Dynamic Drums", new() },
            { "Bosses & Combat", new() },
            { "Menu & Mini-games", new() },
            { "Other Tracks", new() }
        };

        foreach (var value in Enum.GetValues(typeof(MusicType)))
        {
            var track = (MusicType)value;
            string trackName = track.ToString();

            // Find default match:
            int defaultIndex = 0;

            // 1. If musicAB_android or musicAB_windows is present, apply its track as the default setting
            for (int i = 0; i < _audioFiles.Count; i++)
            {
                string item = _audioFiles[i];
                if (AudioClipLoader.MusicBundledTracks.TryGetValue(item, out var bTrack) &&
                    bTrack.IsDefaultSoundtrack &&
                    bTrack.CleanName.Equals(trackName, StringComparison.OrdinalIgnoreCase))
                {
                    defaultIndex = i + 1;
                    break;
                }
            }

            // 2. Fallback: Loose custom file matching trackName
            if (defaultIndex == 0)
            {
                for (int i = 0; i < _audioFiles.Count; i++)
                {
                    string item = _audioFiles[i];
                    if (File.Exists(item) && Path.GetFileNameWithoutExtension(item).Equals(trackName, StringComparison.OrdinalIgnoreCase))
                    {
                        defaultIndex = i + 1;
                        break;
                    }
                }
            }

            // 3. Fallback: Any other bundled track matching trackName
            if (defaultIndex == 0)
            {
                for (int i = 0; i < _audioFiles.Count; i++)
                {
                    string item = _audioFiles[i];
                    if (AudioClipLoader.MusicBundledTracks.TryGetValue(item, out var bTrack) &&
                        bTrack.CleanName.Equals(trackName, StringComparison.OrdinalIgnoreCase))
                    {
                        defaultIndex = i + 1;
                        break;
                    }
                    else if (AudioClipLoader.BundleAssetMap.ContainsKey(item) && item.Equals(trackName, StringComparison.OrdinalIgnoreCase))
                    {
                        defaultIndex = i + 1;
                        break;
                    }
                }
            }

            var setting = new SelectSetting(trackName, defaultIndex)
            {
                Options = new Dictionary<int, string>(_optionsDict),
                OnSelectionChanged = (newVal) =>
                {
                    ApplyTrackOverride(track, newVal);
                }
            };

            TrackSettings[track] = setting;

            string targetCat = GetCategoryForTrack(trackName);
            if (!categoryBuckets.ContainsKey(targetCat))
            {
                categoryBuckets[targetCat] = new();
            }
            categoryBuckets[targetCat].Add((track, setting));
        }

        // Render populated categories
        foreach (var kvp in categoryBuckets)
        {
            if (kvp.Value.Count == 0) continue;

            CreateCategory(kvp.Key);
            foreach (var (_, setting) in kvp.Value)
            {
                AddSettings(setting);
            }
            EndCategory();
        }
    }

    private static string GetCategoryForTrack(string trackName)
    {
        if (trackName.EndsWith("_drum", StringComparison.OrdinalIgnoreCase) ||
            trackName.Contains("drum", StringComparison.OrdinalIgnoreCase))
        {
            return "Dynamic Drums";
        }

        if (trackName.Contains("Boss", StringComparison.OrdinalIgnoreCase) ||
            trackName.Contains("Battle", StringComparison.OrdinalIgnoreCase))
        {
            return "Bosses & Combat";
        }

        if (trackName.Equals("MainMenu", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("SelectCard", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("Garden", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("IZ", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("Loon", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("Roof_pre", StringComparison.OrdinalIgnoreCase))
        {
            return "Menu & Mini-games";
        }

        if (trackName.Equals("Day", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("Night", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("Pool", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("Fog", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("Roof", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("Snow", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("RoofPool", StringComparison.OrdinalIgnoreCase) ||
            trackName.Equals("NightSnow", StringComparison.OrdinalIgnoreCase))
        {
            return "Adventure Stages";
        }

        return "Other Tracks";
    }

    // Mod Logic & Interception

    public static AudioClip GetVanillaMusic(MusicType track)
    {
        if (_vanillaMusics.TryGetValue(track, out var clip) && clip != null)
        {
            return clip;
        }

        try
        {
            string trackName = Enum.GetName(typeof(MusicType), track) ?? track.ToString();
            var resClip = Resources.Load<AudioClip>("Audio/Music/" + trackName);
            if (resClip != null)
            {
                _vanillaMusics[track] = resClip;
                return resClip;
            }
        }
        catch (Exception ex)
        {
            MagnetarApi.LogError($"[Audio Addon] Failed to load vanilla music for '{track}': {ex.Message}");
        }

        return null;
    }

    public static MusicType ResolveActiveMusicType()
    {
        try
        {
            if (GameAPP.canvas != null)
            {
                if (GameAPP.canvas.Find("MainMenu(Clone)") != null ||
                    GameAPP.canvas.Find("MainMenu_travel(Clone)") != null)
                {
                    return MusicType.MainMenu;
                }
            }
        }
        catch { }

        return CurrentPlayingTrack;
    }

    public override void OnEnable()
    {
        CurrentPlayingTrack = ResolveActiveMusicType();
        ApplyCurrentTrackOverride();
    }

    public override void OnDisable()
    {
        _activeLoadHandle?.Stop();
        _activeLoadHandle = null;
        RestoreVanillaMusics();
    }

    public void OnSoundManagerLoadedMusic(SoundManager soundMgr)
    {
        if (soundMgr == null || soundMgr.musics == null) return;

        SnapshotVanillaMusics(soundMgr);

        if (Active)
        {
            ApplyCurrentTrackOverride(soundMgr);
        }
    }

    private void SnapshotVanillaMusics(SoundManager soundMgr)
    {
        if (soundMgr == null || soundMgr.musics == null) return;

        foreach (var value in Enum.GetValues(typeof(MusicType)))
        {
            var track = (MusicType)value;
            if (!_vanillaMusics.ContainsKey(track) && soundMgr.musics.ContainsKey(track))
            {
                var clip = soundMgr.musics[track];
                if (clip != null)
                {
                    _vanillaMusics[track] = clip;
                }
            }
        }
    }

    public void ApplyTrackOverride(MusicType track, int optionIndex)
    {
        var soundMgr = GameAPP.soundManager;
        if (soundMgr == null || soundMgr.musics == null) return;

        SnapshotVanillaMusics(soundMgr);

        if (!Active || optionIndex <= 0 || optionIndex - 1 >= _audioFiles.Count)
        {
            var vanilla = GetVanillaMusic(track);
            if (vanilla != null)
            {
                soundMgr.musics[track] = vanilla;

                if (GameAPP.music != null && CurrentPlayingTrack == track)
                {
                    GameAPP.music.clip = vanilla;
                    GameAPP.music.time = 0f;
                    GameAPP.music.Play();
                }
            }
            return;
        }

        string sourceKey = _audioFiles[optionIndex - 1];

        // Check if clip is already cached in memory
        var cached = AudioClipLoader.GetCachedClip(sourceKey);
        if (cached != null)
        {
            OnTrackClipLoaded(track, cached);
            return;
        }

        // Load asynchronously over a frame to avoid freezing the UI/game
        AudioClipLoader.LoadAudioClipAsync(sourceKey, (clip) =>
        {
            if (clip != null && Active)
            {
                OnTrackClipLoaded(track, clip);
            }
        });
    }

    public void OnTrackClipLoaded(MusicType track, AudioClip clip)
    {
        if (clip == null) return;

        var soundMgr = GameAPP.soundManager;
        if (soundMgr != null && soundMgr.musics != null)
        {
            soundMgr.musics[track] = clip;
        }

        // If this track is currently active or playing, transition to the custom track immediately
        if (GameAPP.music != null)
        {
            bool isCurrent = (CurrentPlayingTrack == track);
            var vanilla = GetVanillaMusic(track);
            bool isPlayingVanilla = vanilla != null && GameAPP.music.clip == vanilla;

            if (isCurrent || isPlayingVanilla)
            {
                GameAPP.music.clip = clip;
                GameAPP.music.Play();
            }
        }

        // If drum track, update musicDrum
        if (GameAPP.musicDrum != null)
        {
            bool isDrumPlayingVanilla = false;
            var vDrum = GetVanillaMusic(track);
            if (vDrum != null && GameAPP.musicDrum.clip == vDrum)
            {
                isDrumPlayingVanilla = true;
            }

            if (isDrumPlayingVanilla || track.ToString().EndsWith("_drum", StringComparison.OrdinalIgnoreCase))
            {
                GameAPP.musicDrum.clip = clip;
            }
        }
    }

    /// <summary>
    /// Loads and applies the override for the active track and evicts unused clips to keep RAM usage low.
    /// </summary>
    public void ApplyCurrentTrackOverride(SoundManager soundMgr = null)
    {
        soundMgr ??= GameAPP.soundManager;
        if (soundMgr == null || soundMgr.musics == null || !Active) return;

        SnapshotVanillaMusics(soundMgr);

        var track = CurrentPlayingTrack;
        EvictUnusedTracks(track);

        if (!TrackSettings.TryGetValue(track, out var setting) || setting.Value <= 0)
        {
            var vanilla = GetVanillaMusic(track);
            if (vanilla != null)
            {
                soundMgr.musics[track] = vanilla;
                if (GameAPP.music != null && (CurrentPlayingTrack == track || GameAPP.music.clip == null || !GameAPP.music.isPlaying))
                {
                    GameAPP.music.clip = vanilla;
                    GameAPP.music.time = 0f;
                    GameAPP.music.Play();
                }
            }
            return;
        }

        int idx = setting.Value - 1;
        if (idx >= 0 && idx < _audioFiles.Count)
        {
            string sourceKey = _audioFiles[idx];
            var cached = AudioClipLoader.GetCachedClip(sourceKey);
            if (cached != null)
            {
                OnTrackClipLoaded(track, cached);
            }
            else
            {
                _activeLoadHandle?.Stop();
                _activeLoadHandle = AudioClipLoader.LoadAudioClipAsync(sourceKey, (clip) =>
                {
                    if (clip != null && Active)
                    {
                        OnTrackClipLoaded(track, clip);
                    }
                });
            }
        }
    }

    /// <summary>
    /// Reclaims RAM by evicting loose audio tracks that are not currently active or drum-paired.
    /// </summary>
    public void EvictUnusedTracks(MusicType currentTrack)
    {
        var soundMgr = GameAPP.soundManager;
        var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Retain current track key
        if (TrackSettings.TryGetValue(currentTrack, out var currSetting) && currSetting.Value > 0)
        {
            int idx = currSetting.Value - 1;
            if (idx >= 0 && idx < _audioFiles.Count)
            {
                activeKeys.Add(_audioFiles[idx]);
            }
        }

        // Retain dynamic drum pair key if exists
        MusicType drumTrack = MusicType.MainMenu;
        bool hasDrum = false;
        if (soundMgr != null && soundMgr.drumPairs != null && soundMgr.drumPairs.TryGetValue(currentTrack, out var dId))
        {
            drumTrack = dId;
            hasDrum = true;
        }
        else if (Enum.TryParse<MusicType>(currentTrack.ToString() + "_drum", true, out var parsedDrum))
        {
            drumTrack = parsedDrum;
            hasDrum = true;
        }

        if (hasDrum && TrackSettings.TryGetValue(drumTrack, out var drumSetting) && drumSetting.Value > 0)
        {
            int idx = drumSetting.Value - 1;
            if (idx >= 0 && idx < _audioFiles.Count)
            {
                activeKeys.Add(_audioFiles[idx]);
            }
        }

        // Evict unused loose clips from RAM
        AudioClipLoader.EvictUnusedClips(activeKeys);

        // Restore vanilla references for evicted tracks in soundMgr.musics so no destroyed objects are kept
        if (soundMgr != null && soundMgr.musics != null)
        {
            foreach (var kvp in TrackSettings)
            {
                var t = kvp.Key;
                if (t == currentTrack || (hasDrum && t == drumTrack)) continue;

                int optIdx = kvp.Value.Value;
                if (optIdx > 0 && optIdx - 1 < _audioFiles.Count)
                {
                    string key = _audioFiles[optIdx - 1];
                    if (!AudioClipLoader.IsLoaded(key))
                    {
                        var vanilla = GetVanillaMusic(t);
                        if (vanilla != null)
                        {
                            soundMgr.musics[t] = vanilla;
                        }
                    }
                }
            }
        }
    }

    public void RestoreVanillaMusics(SoundManager soundMgr = null)
    {
        soundMgr ??= GameAPP.soundManager;

        // 1. Restore all entries in soundMgr.musics back to original Resources clips
        foreach (var value in Enum.GetValues(typeof(MusicType)))
        {
            var track = (MusicType)value;
            var vanillaClip = GetVanillaMusic(track);
            if (vanillaClip != null && soundMgr != null && soundMgr.musics != null)
            {
                soundMgr.musics[track] = vanillaClip;
            }
        }

        // 2. Identify the active playing track based on current game state
        CurrentPlayingTrack = ResolveActiveMusicType();

        // 3. Immediately switch base music playback back to default vanilla clip
        var currentVanilla = GetVanillaMusic(CurrentPlayingTrack);
        if (GameAPP.music != null && currentVanilla != null)
        {
            GameAPP.music.clip = currentVanilla;
            GameAPP.music.time = 0f;
            GameAPP.music.Play();
        }

        // 4. Reset or stop dynamic drum track
        MusicType drumTrack = MusicType.MainMenu;
        bool hasDrum = false;
        if (soundMgr != null && soundMgr.drumPairs != null && soundMgr.drumPairs.TryGetValue(CurrentPlayingTrack, out var dId))
        {
            drumTrack = dId;
            hasDrum = true;
        }
        else if (Enum.TryParse<MusicType>(CurrentPlayingTrack.ToString() + "_drum", true, out var parsedDrum))
        {
            drumTrack = parsedDrum;
            hasDrum = true;
        }

        if (GameAPP.musicDrum != null)
        {
            if (hasDrum)
            {
                var drumVanilla = GetVanillaMusic(drumTrack);
                if (drumVanilla != null)
                {
                    GameAPP.musicDrum.clip = drumVanilla;
                    GameAPP.musicDrum.time = GameAPP.music != null ? GameAPP.music.time : 0f;
                    GameAPP.musicDrum.Play();
                }
            }
            else
            {
                GameAPP.musicDrum.Stop();
                GameAPP.musicDrum.clip = null;
            }
        }
    }

    [HarmonyPatch(typeof(SoundManager))]
    public static class SoundManagerPatch
    {
        [HarmonyPatch(nameof(SoundManager.LoadMusic))]
        [HarmonyPostfix]
        public static void LoadMusicPostfix(SoundManager __instance)
        {
            try
            {
                Instance?.OnSoundManagerLoadedMusic(__instance);
            }
            catch (Exception ex)
            {
                MagnetarApi.LogError($"[Audio Addon] Failed in LoadMusicPostfix: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameAPP))]
    public static class GameAppPatch
    {
        [HarmonyPatch(nameof(GameAPP.PlayMusic), new Type[] { typeof(MusicType) })]
        [HarmonyPrefix]
        public static bool PlayMusicPrefix(MusicType id)
        {
            try
            {
                CurrentPlayingTrack = id;

                if (Instance == null || !Instance.Active) return true;

                // 1. If local bundles are currently initializing asynchronously in background, defer playback
                if (_isInitializingBundles && _hasLocalBundles)
                {
                    _pendingPlayTrack = id;
                    if (GameAPP.music != null && GameAPP.music.isPlaying)
                    {
                        GameAPP.music.Stop();
                    }
                    if (GameAPP.musicDrum != null && GameAPP.musicDrum.isPlaying)
                    {
                        GameAPP.musicDrum.Stop();
                    }
                    return false; // Suppress vanilla music playback during startup!
                }

                // 2. Evict unused tracks to keep RAM low
                Instance.EvictUnusedTracks(id);

                // 3. Inspect setting for this track
                if (Instance.TrackSettings.TryGetValue(id, out var setting))
                {
                    // User explicitly chose Vanilla for this track
                    if (setting.Value == 0) return true;

                    int fileIdx = setting.Value - 1;
                    if (fileIdx >= 0 && fileIdx < Instance._audioFiles.Count)
                    {
                        string sourceKey = Instance._audioFiles[fileIdx];
                        var cached = AudioClipLoader.GetCachedClip(sourceKey);
                        if (cached != null)
                        {
                            if (GameAPP.soundManager?.musics != null)
                            {
                                GameAPP.soundManager.musics[id] = cached;
                            }
                            if (GameAPP.music != null)
                            {
                                GameAPP.music.clip = cached;
                                GameAPP.music.time = 0f;
                                GameAPP.music.Play();
                            }
                            return false; // Handled directly with custom clip
                        }
                        else
                        {
                            // Clip needs loading; suppress vanilla playback in the meantime
                            _pendingPlayTrack = id;
                            if (GameAPP.music != null && GameAPP.music.isPlaying)
                            {
                                GameAPP.music.Stop();
                            }

                            AudioClipLoader.LoadAudioClipAsync(sourceKey, (clip) =>
                            {
                                if (clip != null && Instance != null && Instance.Active)
                                {
                                    Instance.OnTrackClipLoaded(id, clip);
                                }
                            });
                            return false; // Suppress vanilla playback while loading clip
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MagnetarApi.LogError($"[Audio Addon] Failed in PlayMusicPrefix: {ex.Message}");
            }

            return true;
        }
    }
}
