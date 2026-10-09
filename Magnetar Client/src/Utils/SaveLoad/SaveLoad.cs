using Magnetar_Client.Api;
using Magnetar_Client.Core;
using Magnetar_Client.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public static class SaveLoad
{
    private static readonly Dictionary<string, ISaveHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private static float _lastSavedTime;
    private static readonly object _fileLock = new();

    #region Handler Registration API

    /// <summary>
    /// Registers a custom save section handler. Use this in your managers and addons.
    /// </summary>
    public static void RegisterHandler(ISaveHandler handler)
    {
        if (handler == null || string.IsNullOrWhiteSpace(handler.SectionKey)) return;
        _handlers[handler.SectionKey] = handler;
    }

    /// <summary>
    /// Removes a previously registered save handler.
    /// </summary>
    public static void UnregisterHandler(string sectionKey)
    {
        if (string.IsNullOrWhiteSpace(sectionKey)) return;
        _handlers.Remove(sectionKey);
    }

    /// <summary>
    /// Quick helper to register a single property or configuration value as a save entry.
    /// </summary>
    public static void RegisterEntry<T>(string key, Func<T> getter, Action<T> setter, T defaultValue = default)
    {
        RegisterHandler(new ValueSaveEntry<T>(key, getter, setter, defaultValue));
    }

    /// <summary>
    /// Tells all registered handlers to reset their state to default.
    /// </summary>
    public static void ResetAll()
    {
        foreach (var handler in _handlers.Values)
        {
            try
            {
                handler.ResetToDefault();
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"[SaveLoad] Error resetting section '{handler.SectionKey}': {ex.Message}");
            }
        }
    }

    #endregion

    #region Core Save & Load

    public static void Save(bool force = false)
    {
        if (!force)
        {
            if (_lastSavedTime == 0f)
            {
                _lastSavedTime = Time.realtimeSinceStartup;
            }
            else if (_lastSavedTime + Config.MinTimeBetweenSaves >= Time.realtimeSinceStartup)
            {
                return;
            }
        }

        _lastSavedTime = Time.realtimeSinceStartup;

        // 1. Collect sections from all registered handlers
        var rootData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var handler in _handlers.Values)
        {
            try
            {
                object sectionData = handler.ExportData();
                if (sectionData != null)
                {
                    rootData[handler.SectionKey] = sectionData;
                }
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"[SaveLoad] Error exporting '{handler.SectionKey}': {ex.Message}");
            }
        }

        // 2. Write Profile JSON to disk
        string savePath = PathsManager.GetProfilePath(Config.CurrentProfile);
        string directory = Path.GetDirectoryName(savePath);

        lock (_fileLock)
        {
            try
            {
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                string json = JsonConvert.SerializeObject(rootData, Formatting.Indented);
                File.WriteAllText(savePath, json);
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"[SaveLoad] Failed writing save file '{savePath}': {ex.Message}");
            }
        }

        // 3. Optional hook for disk asset overrides (e.g. textures)
        TextureLoader.SaveTextureOverrides();

        if (!force) AutoSaveLogger.Msg("Saved profile data.");
    }

    public static void Load()
    {
        string loadPath = PathsManager.GetProfilePath(Config.CurrentProfile);

        lock (_fileLock)
        {
            if (!File.Exists(loadPath))
            {
                AutoSaveLogger.Msg($"No profile found at '{loadPath}'. Initializing defaults.");
                ResetAll();
                Save(true);
                return;
            }

            try
            {
                string json = File.ReadAllText(loadPath);
                JObject rootToken = JObject.Parse(json);

                foreach (var handler in _handlers.Values)
                {
                    try
                    {
                        // Check if data is encapsulated under SectionKey, fallback to rootToken for legacy files
                        JToken sectionToken = rootToken[handler.SectionKey] ?? rootToken;
                        handler.ImportData(sectionToken);
                    }
                    catch (Exception ex)
                    {
                        AutoSaveLogger.Error($"[SaveLoad] Error loading section '{handler.SectionKey}': {ex.Message}");
                    }
                }

                AutoSaveLogger.Msg($"Loaded Profile '{Config.CurrentProfile}'");
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"[SaveLoad] Failed reading save file '{loadPath}': {ex.Message}");
            }

            TextureLoader.LoadTextureOverrides();
        }
    }

    #endregion

    #region Generic Simple Entry Adapter

    private class ValueSaveEntry<T> : ISaveHandler
    {
        public string SectionKey { get; }
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private readonly T _default;

        public ValueSaveEntry(string key, Func<T> getter, Action<T> setter, T defaultValue)
        {
            SectionKey = key;
            _getter = getter;
            _setter = setter;
            _default = defaultValue;
        }

        public object ExportData() => _getter();

        public void ImportData(JToken token)
        {
            if (token == null)
            {
                _setter(_default);
                return;
            }

            try
            {
                // In legacy profiles, token might be the root object containing SectionKey as a property
                JToken target = (token is JObject obj && obj.TryGetValue(SectionKey, out var prop)) ? prop : token;
                _setter(target.ToObject<T>());
            }
            catch
            {
                _setter(_default);
            }
        }

        public void ResetToDefault() => _setter(_default);
    }

    #endregion
}