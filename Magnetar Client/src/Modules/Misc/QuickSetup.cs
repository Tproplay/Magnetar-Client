using Magnetar_Client.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class QuickSetup : Module
    {
        public override string Name { get; set; } = "Quick setup";
        public override string Description { get; set; } = "Save/load a custom setup loadout.";
        public override string SearchHints { get; set; } = "quicksetup loadout setupmanager savebuild loadbuild " +
            "setup presetmanager quickload quicksave plantsetup configuration buildmanager setuploader loadouts " +
            "quick-setup customsetup setupsaver presetload";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Settings
        public StringSetting NameOfSave;
        public BoolSetting ClearBeforeLoad;
        public MultiSelectSetting LoadSetup;

        public static QuickSetup instance;

        private Dictionary<string, string> _savedSetups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static string SetupsFilePath => Path.Combine(ProfileManager.ConfigDir, "Custom_Setups.json");

        public QuickSetup()
        {
            instance = this;

            CreateCategory("Save");
            NameOfSave = new StringSetting("Name", "");
            AddSettings(
                NameOfSave,
                new ButtonSetting("Save Current Setup", SaveCurrentSetup, "Save")
            );
            EndCategory();

            CreateCategory("Load");
            ClearBeforeLoad = new BoolSetting("Clear Lawn Before Load", true);
            LoadSetup = new MultiSelectSetting("Setup")
            {
                MaxSelection = 1
            };

            AddSettings(
                ClearBeforeLoad,
                LoadSetup,
                new ButtonSetting("Load Selected Setup", LoadSelectedSetup, "Load"),
                new ButtonSetting("Delete Selected Setup", DeleteSelectedSetup, "Delete")
            );
            EndCategory();

            LoadSetupsFromDisk();
        }

        public void SaveCurrentSetup()
        {
            if (plantList == null || plantList.Count == 0)
            {
                DebugLogger.Warning("[QuickSetup] No plants found on the lawn to save.");
                return;
            }

            string saveName = NameOfSave.Value?.Trim();
            if (string.IsNullOrEmpty(saveName))
            {
                saveName = $"Setup_{_savedSetups.Count + 1}";
            }

            string code = GenerateSetupCode();
            if (string.IsNullOrEmpty(code))
            {
                DebugLogger.Error("[QuickSetup] Failed to encode current board setup.");
                return;
            }

            _savedSetups[saveName] = code;
            SaveSetupsToDisk();
            RefreshLoadOptions();

            NameOfSave.Value = "";
            DebugLogger.Msg($"[QuickSetup] Saved '{saveName}' ({plantList.Count} plants).");
        }

        public void LoadSelectedSetup()
        {
            if (!Active)
            {
                DebugLogger.Warning("[QuickSetup] Module is disabled. Turn the module ON to load setups.");
                return;
            }

            if (BoardInstanceIsNull)
            {
                DebugLogger.Warning("[QuickSetup] Board install is Null");
                return;
            }

            string selectedName = GetSelectedSetupName();
            if (string.IsNullOrEmpty(selectedName))
            {
                DebugLogger.Warning("No setup selected.");
                return;
            }

            if (!_savedSetups.TryGetValue(selectedName, out string code) || string.IsNullOrEmpty(code))
            {
                DebugLogger.Warning($"[QuickSetup] Setup '{selectedName}' contains no data.");
                return;
            }

            if (ClearBeforeLoad.Value && plantList != null)
            {
                // Snapshot to an array to prevent collection modification crashes during iteration
                var currentPlants = plantList.ToArray();
                for (int i = 0; i < currentPlants.Length; i++)
                {
                    if (currentPlants[i] != null)
                    {
                        currentPlants[i].Die();
                    }
                }
            }

            int placedCount = 0;
            string[] tokens = code.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length; i++)
            {
                try
                {
                    long val = FromBase62(tokens[i]);
                    int row = (int)(val % 100);
                    val /= 100;
                    int col = (int)(val % 100);
                    int plantType = (int)(val / 100);

                    SpawnPlant(col, row, plantType);
                    placedCount++;
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[QuickSetup] Token decode failure on '{tokens[i]}': {ex.Message}");
                }
            }

            DebugLogger.Msg($"[QuickSetup] Loaded '{selectedName}' ({placedCount} plants placed).");
        }

        public void DeleteSelectedSetup()
        {
            string selectedName = GetSelectedSetupName();
            if (string.IsNullOrEmpty(selectedName))
            {
                DebugLogger.Warning("[QuickSetup] No setup selected to delete.");
                return;
            }

            if (_savedSetups.Remove(selectedName))
            {
                SaveSetupsToDisk();
                RefreshLoadOptions();
                LoadSetup.SelectedValues.Clear();
                DebugLogger.Msg($"[QuickSetup] Deleted '{selectedName}'.");
            }
        }

        public string GenerateSetupCode()
        {
            if (plantList == null || plantList.Count == 0) return string.Empty;

            StringBuilder sb = new StringBuilder(plantList.Count * 6);

            for (int i = 0; i < plantList.Count; i++)
            {
                var plant = plantList[i];
                if (plant == null) continue;

                // Arithmetic encoding: (plantType * 10000) + (col * 100) + row
                long code = ((long)(int)plant.thePlantType * 10000L) + (plant.thePlantColumn * 100) + plant.thePlantRow;

                if (sb.Length > 0) sb.Append('-');
                sb.Append(ToBase62(code));
            }

            return sb.ToString();
        }

        private void SpawnPlant(int col, int row, int plantType)
        {
            CreatePlant.Instance.SetPlant(col, row, (PlantType)plantType);
        }

        private void LoadSetupsFromDisk()
        {
            try
            {
                string path = SetupsFilePath;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    _savedSetups = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[QuickSetup] Failed to load setups from disk: {ex.Message}");
                _savedSetups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            RefreshLoadOptions();
        }

        private void SaveSetupsToDisk()
        {
            try
            {
                string path = SetupsFilePath;
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(path, JsonConvert.SerializeObject(_savedSetups, Formatting.Indented));
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[QuickSetup] Failed to save setups to disk: {ex.Message}");
            }
        }

        private void RefreshLoadOptions()
        {
            LoadSetup.Options.Clear();
            int id = 0;
            foreach (var kvp in _savedSetups)
            {
                LoadSetup.AddOption(id++, kvp.Key);
            }
        }

        private string GetSelectedSetupName()
        {
            if (LoadSetup.SelectedValues != null && LoadSetup.SelectedValues.Count > 0)
            {
                int selectedId = LoadSetup.SelectedValues.First();
                if (LoadSetup.Options.TryGetValue(selectedId, out string name))
                {
                    return name;
                }
            }
            return null;
        }

        public static string ToBase62(long value)
        {
            if (value == 0) return "0";
            const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            char[] buffer = new char[16];
            int i = 0;

            while (value > 0)
            {
                buffer[i++] = alphabet[(int)(value % 62)];
                value /= 62;
            }

            Array.Reverse(buffer, 0, i);
            return new string(buffer, 0, i);
        }

        public static long FromBase62(string base62String)
        {
            if (string.IsNullOrEmpty(base62String)) return 0;
            long result = 0;

            for (int i = 0; i < base62String.Length; i++)
            {
                char c = base62String[i];
                int val;

                if (c >= '0' && c <= '9') val = c - '0';
                else if (c >= 'a' && c <= 'z') val = c - 'a' + 10;
                else if (c >= 'A' && c <= 'Z') val = c - 'A' + 36;
                else throw new ArgumentException($"Invalid Base62 character: {c}");

                result = (result * 62) + val;
            }

            return result;
        }
    }
}