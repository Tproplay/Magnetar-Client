using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Magnetar_Client;
using Magnetar_Client.Api;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace AudioAddon;

public class CustomSfx : Module
{
    // Mod Info
    public override string Name { get; set; } = "Custom Sfx";
    public override string Description { get; set; } = "Allows you to change game's sound effects.";
    public override string SearchHints { get; set; } = "customsfx sfxchanger soundeffectchanger audiosfx customsoundeffects" +
        " sfxmod soundchanger sfxpack audioinjector soundsfxchanger sfxplayer noiseselector customaudio soundsfxmod sfxmanager" +
        " soundpack audioaddon effectschanger sfxsubstitute soundreplacer";
    public override string Author { get; set; } = "Tproplay";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Addon;
    public override bool EnableInVanillaMode { get; set; } = true;

    // Mod Data
    public static CustomSfx Instance { get; private set; }

    public static string SfxDir => GetSfxDir();

    public SectionSetting SfxOverride;

    private readonly Dictionary<SoundType, AudioClip> _vanillaSounds = new();
    private ICoroutineHandle _sfxPreloadHandle;

    public CustomSfx()
    {
        Instance = this;

        CreateCategory("General");

        SfxOverride = new SectionSetting("SFX Overrides", (index) =>
        {
            var audios = GetSfxOptions();
            var enabledSetting = new BoolSetting("Enabled", true);
            var soundSetting = new MultiSelectSetting("Sound", typeof(SoundType))
            {
                MaxSelection = 1,
                CustomNames = TranslateEnum(typeof(SoundType))
            };
            var replacementSetting = new MultiSelectSetting<string>("Replacement", audios, GetSfxOptionDisplayName)
            {
                MaxSelection = 1
            };

            enabledSetting.OnValueChanged = (_) => ApplyAllOverridesAsync();
            soundSetting.OnSelectionChanged = (_, _) =>
            {
                UpdateSectionTitles();
                ApplyAllOverridesAsync();
            };
            replacementSetting.OnSelectionChanged = (_, _) =>
            {
                UpdateSectionTitles();
                ApplyAllOverridesAsync();
            };

            return new List<Setting>
            {
                enabledSetting,
                soundSetting,
                replacementSetting
            };
        }, defaultSections: 0);

        SfxOverride.OnSectionsChanged = () =>
        {
            UpdateSectionTitles();
            ApplyAllOverridesAsync();
        };

        AddSettings(SfxOverride);
        EndCategory();
    }

    private static string GetSfxDir()
    {
        string dir = Path.Combine(PathsManager.AddonsDir, "Custom Audios", "Sfx");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    /// <summary>
    /// Discovers all available SFX replacement options, including bundled AssetBundle tracks and loose files.
    /// </summary>
    public static List<string> GetSfxOptions()
    {
        string dir = SfxDir;
        if (!Directory.Exists(dir)) return new List<string>();

        // 1. Scan and load all AssetBundles in Custom Audios/Sfx/
        AudioClipLoader.ScanAndLoadSfxBundles(dir);

        var options = new List<string>();

        // 2. Add bundled SFX options
        foreach (var kvp in AudioClipLoader.SfxBundledTracks)
        {
            options.Add(kvp.Key);
        }

        // 3. Add loose audio files (.mp3, .ogg, .wav)
        var supportedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp3", ".ogg", ".wav" };
        var files = Directory.GetFiles(dir)
            .Where(f => supportedExts.Contains(Path.GetExtension(f)))
            .ToList();
        options.AddRange(files);

        return options;
    }

    public static string GetSfxOptionDisplayName(string optionKey)
    {
        if (AudioClipLoader.SfxBundledTracks.TryGetValue(optionKey, out var bundled))
        {
            return bundled.DisplayName;
        }
        return Path.GetFileName(optionKey);
    }

    private void UpdateSectionTitles()
    {
        for (int i = 0; i < SfxOverride.Sections.Count; i++)
        {
            var section = SfxOverride.Sections[i];
            if (section == null) continue;

            var soundSet = section.Find<MultiSelectSetting>("Sound");
            var repSet = section.Find<MultiSelectSetting<string>>("Replacement");

            string soundName = (soundSet != null && soundSet.SelectedValues.Count > 0)
                ? ((SoundType)soundSet.SelectedValues.First()).ToString()
                : "None";

            string repName = (repSet != null && repSet.SelectedValues.Count > 0)
                ? GetSfxOptionDisplayName(repSet.SelectedValues.First())
                : "None";

            section.Title = (soundName != "None" && repName != "None")
                ? $"{soundName} -> {repName}"
                : $"SFX Override #{i + 1}";
        }
    }

    public override void OnLanguageChanged()
    {
        for (int i = 0; i < SfxOverride.Sections.Count; i++)
        {
            var soundSet = SfxOverride.Sections[i].Find<MultiSelectSetting>("Sound");
            if (soundSet != null)
            {
                soundSet.CustomNames = TranslateEnum(typeof(SoundType));
            }
        }
    }

    // Mod Logic & Interception

    public override void OnEnable()
    {
        ApplyAllOverridesAsync();
    }

    public override void OnDisable()
    {
        _sfxPreloadHandle?.Stop();
        _sfxPreloadHandle = null;
        RestoreVanillaSounds();
    }

    public void OnSoundManagerLoadedSound(SoundManager soundMgr)
    {
        if (soundMgr == null || soundMgr.sounds == null) return;

        SnapshotVanillaSounds(soundMgr);

        if (Active)
        {
            ApplyAllOverridesAsync(soundMgr);
        }
    }

    private void SnapshotVanillaSounds(SoundManager soundMgr)
    {
        if (soundMgr == null || soundMgr.sounds == null) return;

        foreach (var val in Enum.GetValues(typeof(SoundType)))
        {
            var sType = (SoundType)val;
            if (soundMgr.sounds.ContainsKey(sType) && !_vanillaSounds.ContainsKey(sType))
            {
                _vanillaSounds[sType] = soundMgr.sounds[sType];
            }
        }
    }

    /// <summary>
    /// Preloads and applies all configured SFX overrides asynchronously across frames.
    /// </summary>
    public void ApplyAllOverridesAsync(SoundManager soundMgr = null)
    {
        soundMgr ??= GameAPP.soundManager;
        if (soundMgr == null || soundMgr.sounds == null) return;

        SnapshotVanillaSounds(soundMgr);
        RestoreVanillaSounds(soundMgr);

        if (!Active) return;

        var filesToPreload = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targetMappings = new List<(SoundType soundType, string filePath)>();

        for (int i = 0; i < SfxOverride.Sections.Count; i++)
        {
            var section = SfxOverride.Sections[i];
            if (section == null) continue;

            var enabled = section.Find<BoolSetting>("Enabled");
            if (enabled != null && !enabled.Value) continue;

            var soundSet = section.Find<MultiSelectSetting>("Sound");
            var repSet = section.Find<MultiSelectSetting<string>>("Replacement");

            if (soundSet == null || repSet == null) continue;
            if (soundSet.SelectedValues.Count == 0 || repSet.SelectedValues.Count == 0) continue;

            SoundType soundType = (SoundType)soundSet.SelectedValues.First();
            string selectedPath = repSet.SelectedValues.First();

            // If it's a loose relative file path on disk, resolve it to SfxDir
            if (!AudioClipLoader.SfxBundledTracks.ContainsKey(selectedPath) && !File.Exists(selectedPath))
            {
                string fullPath = Path.Combine(SfxDir, selectedPath);
                if (File.Exists(fullPath))
                {
                    selectedPath = fullPath;
                }
            }

            filesToPreload.Add(selectedPath);
            targetMappings.Add((soundType, selectedPath));
        }

        _sfxPreloadHandle?.Stop();
        _sfxPreloadHandle = AudioClipLoader.PreloadAudiosAsync(filesToPreload, (loadedPath, clip) =>
        {
            if (clip == null || !Active || soundMgr.sounds == null) return;

            foreach (var (soundType, path) in targetMappings)
            {
                if (string.Equals(path, loadedPath, StringComparison.OrdinalIgnoreCase))
                {
                    soundMgr.sounds[soundType] = clip;
                }
            }
        });
    }

    public static AudioClip GetVanillaSound(SoundType sound)
    {
        if (Instance != null && Instance._vanillaSounds.TryGetValue(sound, out var clip) && clip != null)
        {
            return clip;
        }

        try
        {
            string soundName = Enum.GetName(typeof(SoundType), sound) ?? sound.ToString();
            var resClip = Resources.Load<AudioClip>("Audio/Sound/" + soundName);
            if (resClip != null)
            {
                if (Instance != null) Instance._vanillaSounds[sound] = resClip;
                return resClip;
            }
        }
        catch { }

        return null;
    }

    public void RestoreVanillaSounds(SoundManager soundMgr = null)
    {
        soundMgr ??= GameAPP.soundManager;
        if (soundMgr == null || soundMgr.sounds == null) return;

        foreach (var val in Enum.GetValues(typeof(SoundType)))
        {
            var sType = (SoundType)val;
            var clip = GetVanillaSound(sType);
            if (clip != null)
            {
                soundMgr.sounds[sType] = clip;
            }
        }
    }

    [HarmonyPatch(typeof(SoundManager))]
    public static class SoundManagerPatch
    {
        [HarmonyPatch(nameof(SoundManager.LoadSound))]
        [HarmonyPostfix]
        public static void LoadSoundPostfix(SoundManager __instance)
        {
            try
            {
                Instance?.OnSoundManagerLoadedSound(__instance);
            }
            catch (Exception ex)
            {
                MagnetarApi.LogError($"[Custom Sfx] Failed in LoadSoundPostfix: {ex.Message}");
            }
        }
    }
}
