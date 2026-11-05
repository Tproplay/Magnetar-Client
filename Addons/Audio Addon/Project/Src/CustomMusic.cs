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

    public static string AudioDir => GetAudioDir();

    public CustomMusic()
    {
        Instance = this;

        // Discover custom music files (both AssetBundle streaming tracks and loose files)
        ScanAudioFiles();

        // Dynamically categorize and create settings for all in-game music tracks
        BuildDynamicTrackSettings();

        // If official default AssetBundle was not found, download it in background
        CheckAndDownloadMissingDefaultBundle();
    }

    private void CheckAndDownloadMissingDefaultBundle()
    {
        bool hasDefaultBundle = AudioClipLoader.MusicBundledTracks.Values.Any(t => t.IsDefaultSoundtrack);
        if (hasDefaultBundle) return;

        string targetBundleName = MagnetarInfo.Platform == MagnetarInfo.PlatformType.Android ? "musicAB_android" : "musicAB_windows";
        string targetPath = Path.Combine(AudioDir, targetBundleName);

        if (File.Exists(targetPath) && AudioClipLoader.IsAssetBundle(targetPath))
        {
            AudioClipLoader.ScanAndLoadMusicBundles(AudioDir);
            RefreshTrackSettingsAfterBundleLoaded();
            return;
        }

        CoroutineManager.Start(DownloadDefaultBundleRoutine(targetBundleName, targetPath));
    }

    private IEnumerator DownloadDefaultBundleRoutine(string targetBundleName, string targetPath)
    {
        if (_isDownloading) yield break;
        _isDownloading = true;

        MagnetarApi.LogMsg($"[Audio Addon] Default soundtrack '{targetBundleName}' not found in Custom Audios/Music/. Downloading in background...");

        string[] candidateUrls = new[]
        {
            $"https://raw.githubusercontent.com/Tproplay/Magnetar-Client/master/Addons/Audio%20Addon/Project/Custom%20Audios/{targetBundleName}",
            $"https://raw.githubusercontent.com/Tproplay/Magnetar-Client/devlop/Addons/Audio%20Addon/Project/Custom%20Audios/{targetBundleName}",
            $"https://github.com/Tproplay/Magnetar-Client/raw/master/Addons/Audio%20Addon/Project/Custom%20Audios/{targetBundleName}"
        };

        bool downloadDone = false;
        Exception downloadError = null;
        string tempPath = targetPath + ".download.tmp";

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

                HttpResponseMessage response = null;
                foreach (var url in candidateUrls)
                {
                    try
                    {
                        var req = new HttpRequestMessage(HttpMethod.Get, url);
                        var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                        if (resp.IsSuccessStatusCode)
                        {
                            response = resp;
                            break;
                        }
                    }
                    catch { }
                }

                if (response == null)
                {
                    throw new Exception("Unable to reach GitHub raw download servers.");
                }

                string dir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
                {
                    await stream.CopyToAsync(fs);
                }

                if (!AudioClipLoader.IsAssetBundle(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                    throw new Exception("Downloaded file is corrupt or not a valid Unity AssetBundle.");
                }

                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                File.Move(tempPath, targetPath);
            }
            catch (Exception ex)
            {
                downloadError = ex;
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
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

        if (downloadError != null)
        {
            MagnetarApi.LogWarning($"[Audio Addon] Could not download '{targetBundleName}': {downloadError.Message}");
            yield break;
        }

        MagnetarApi.LogMsg($"[Audio Addon] Successfully downloaded '{targetBundleName}' ({new FileInfo(targetPath).Length / 1048576f:F1} MB). Loading soundtrack...");

        AudioClipLoader.ScanAndLoadMusicBundles(AudioDir);
        RefreshTrackSettingsAfterBundleLoaded();
    }

    public void RefreshTrackSettingsAfterBundleLoaded()
    {
        ScanAudioFiles();

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

    private void ScanAudioFiles()
    {
        _audioFiles.Clear();
        _optionsDict.Clear();
        _optionsDict[0] = "Default (Vanilla)";

        string dir = AudioDir;
        if (!Directory.Exists(dir)) return;

        // 1. Scan and load all AssetBundles in Custom Audios/Music/
        AudioClipLoader.ScanAndLoadMusicBundles(dir);

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

        if (TrackSettings.TryGetValue(track, out var setting) && setting.Value > 0)
        {
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
        public static void PlayMusicPrefix(MusicType id)
        {
            try
            {
                CurrentPlayingTrack = id;

                if (Instance == null || !Instance.Active) return;

                // Evict unused tracks and load current track override
                Instance.EvictUnusedTracks(id);

                if (Instance.TrackSettings.TryGetValue(id, out var setting) && setting.Value > 0)
                {
                    int fileIdx = setting.Value - 1;
                    if (fileIdx >= 0 && fileIdx < Instance._audioFiles.Count)
                    {
                        string sourceKey = Instance._audioFiles[fileIdx];
                        var cached = AudioClipLoader.GetCachedClip(sourceKey);
                        if (cached != null && GameAPP.soundManager?.musics != null)
                        {
                            GameAPP.soundManager.musics[id] = cached;
                        }
                        else if (!AudioClipLoader.IsLoaded(sourceKey))
                        {
                            // Load asynchronously in background so track transition never freezes
                            AudioClipLoader.LoadAudioClipAsync(sourceKey, (clip) =>
                            {
                                if (clip != null && Instance != null && Instance.Active)
                                {
                                    Instance.OnTrackClipLoaded(id, clip);
                                }
                            });
                        }
                    }
                }
            }
            catch { }
        }
    }
}
