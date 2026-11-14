using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Newtonsoft.Json;
using Magnetar_Client.Api;
using Magnetar_Client.UI.Setting;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class ChestPreset : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Chest Preset";
        public override string Description { get; set; } = "Allows you to spawn a Pumpkin Chest with a preset of plants inside, or drop seed cards containing custom inventories.";
        public override string SearchHints { get; set; } = "chestpreset plantchest pumpkinchest presetchest chestspawner chestconfig chestgenerator" +
            " presetspawner chestmanager presetloader chestlooter plantpreset chestcustomizer rewardchest presetitems chestbuilder" +
            " chestinventory chestspawn chestrewards chestcontent presetmanager";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data
        public static ChestPreset instance;

        #region Settings

        // Target Selection
        public SelectSetting TargetMode;
        public GameObjectSetting<Pumpiner> TargetPumpiner;

        // Preset Management
        public SelectSetting<string> PresetSelect;
        public StringSetting NewPresetName;
        public ButtonSetting SaveButton;
        public ButtonSetting LoadButton;
        public ButtonSetting DeleteButton;
        public ButtonSetting RefreshButton;
        public ButtonSetting OpenFolderButton;

        // Pending presets queued for dropped Pumpkin Chest cards
        private static readonly Queue<PresetData> _pendingCardPresets = new();

        // Load Options
        public SelectSetting LoadMode;

        #endregion

        #region Preset Data Schema

        public class PresetItem
        {
            [JsonProperty("plantId")]
            public int PlantId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("count")]
            public int Count { get; set; }
        }

        public class PresetData
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("items")]
            public List<PresetItem> Items { get; set; } = new();
        }

        #endregion

        public static string PresetsDir => PathsManager.ChestPresetsDir;

        public ChestPreset()
        {
            instance = this;

            // 1. Target Selection Category
            CreateCategory("Target Chest");

            TargetMode = new SelectSetting("Target Mode", new string[]
            {
                "Selected Pumpkin Chest",
                "Global Ender Chest",
                "All Placed Pumpkin Chests"
            }, 0);

            TargetPumpiner = new GameObjectSetting<Pumpiner>("Target Pumpkin Chest", allowDerivatives: true);

            AddSettings(TargetMode, TargetPumpiner);
            EndCategory();

            // 2. Preset Management Category
            CreateCategory("Save");

            NewPresetName = new StringSetting("Preset Name", "MyPreset");
            SaveButton = new ButtonSetting("Save Current Chest", OnSaveClicked, "Save");

            AddSettings(NewPresetName, SaveButton);
            EndCategory();
            CreateCategory("Load");

            PresetSelect = new SelectSetting<string>("Selected Preset", "");
            LoadMode = new SelectSetting("Load Mode", new string[]
            {
                "Replace",
                "Merge / Add"
            }, 0);
            DeleteButton = new ButtonSetting("Delete Selected Preset", OnDeleteClicked, "Delete");
            RefreshButton = new ButtonSetting("Refresh Presets", OnRefreshClicked, "Refresh");
            OpenFolderButton = new ButtonSetting("Open Presets Folder", OnOpenFolderClicked, "Open");
            LoadButton = new ButtonSetting("Spawn Preset Card", OnLoadClicked, "Drop Card");

            AddSettings(PresetSelect, LoadButton, DeleteButton, RefreshButton, OpenFolderButton);
            EndCategory();

            // Clear pending drop card presets on level transition or death
            GameData.Actions.OnBoardDestroyed += () => _pendingCardPresets.Clear();
            GameData.Actions.OnBoardDied += () => _pendingCardPresets.Clear();


            // Initialize sample presets if empty and populate dropdown
            EnsureDefaultPresets();
            RefreshPresetList();
        }

        #region Preset Storage Operations

        private void EnsureDefaultPresets()
        {
            try
            {
                string dir = PresetsDir;
                if (!Directory.Exists(dir)) return;

                string[] existing = Directory.GetFiles(dir, "*.json");
                if (existing.Length > 0) return;

                // 1. All-Stars Defense Preset
                var allStars = new PresetData
                {
                    Name = "All-Stars Defense",
                    Description = "High-tier heavy artillery & support plants",
                    Items = new List<PresetItem>
                    {
                        new() { PlantId = (int)PlantType.SuperSnowGatling, Name = "SuperSnowGatling", Count = 64 },
                        new() { PlantId = (int)PlantType.CobCannon, Name = "CobCannon", Count = 32 },
                        new() { PlantId = (int)PlantType.DoomShroom, Name = "DoomShroom", Count = 32 },
                        new() { PlantId = (int)PlantType.TwinFlower, Name = "TwinFlower", Count = 64 },
                        new() { PlantId = (int)PlantType.TallNut, Name = "TallNut", Count = 64 },
                        new() { PlantId = (int)PlantType.TorchWood, Name = "TorchWood", Count = 32 },
                        new() { PlantId = (int)PlantType.Pumpiner, Name = "Pumpiner", Count = 10 },
                        new() { PlantId = (int)PlantType.EndPumpiner, Name = "EndPumpiner", Count = 5 }
                    }
                };
                File.WriteAllText(Path.Combine(dir, "All_Stars_Defense.json"), JsonConvert.SerializeObject(allStars, Formatting.Indented));

                // 2. Starter Kit Preset
                var starterKit = new PresetData
                {
                    Name = "Starter Kit",
                    Description = "Early game staple fusions & defensive plants",
                    Items = new List<PresetItem>
                    {
                        new() { PlantId = (int)PlantType.Peashooter, Name = "Peashooter", Count = 64 },
                        new() { PlantId = (int)PlantType.SunFlower, Name = "SunFlower", Count = 64 },
                        new() { PlantId = (int)PlantType.WallNut, Name = "WallNut", Count = 64 },
                        new() { PlantId = (int)PlantType.CherryBomb, Name = "CherryBomb", Count = 16 },
                        new() { PlantId = (int)PlantType.PotatoMine, Name = "PotatoMine", Count = 32 }
                    }
                };
                File.WriteAllText(Path.Combine(dir, "Starter_Kit.json"), JsonConvert.SerializeObject(starterKit, Formatting.Indented));

                // 3. Shroom Army Preset
                var shroomArmy = new PresetData
                {
                    Name = "Mushroom Brigade",
                    Description = "Nocturnal rapid-deploy shroom assortment",
                    Items = new List<PresetItem>
                    {
                        new() { PlantId = (int)PlantType.SmallPuff, Name = "SmallPuff", Count = 64 },
                        new() { PlantId = (int)PlantType.SunShroom, Name = "SunShroom", Count = 64 },
                        new() { PlantId = (int)PlantType.FumeShroom, Name = "FumeShroom", Count = 64 },
                        new() { PlantId = (int)PlantType.ScaredyShroom, Name = "ScaredyShroom", Count = 64 },
                        new() { PlantId = (int)PlantType.IceShroom, Name = "IceShroom", Count = 32 },
                        new() { PlantId = (int)PlantType.DoomShroom, Name = "DoomShroom", Count = 32 }
                    }
                };
                File.WriteAllText(Path.Combine(dir, "Mushroom_Brigade.json"), JsonConvert.SerializeObject(shroomArmy, Formatting.Indented));
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"[ChestPreset] Error creating sample presets: {ex.Message}");
            }
        }

        public void RefreshPresetList()
        {
            try
            {
                string dir = PresetsDir;
                if (!Directory.Exists(dir)) return;

                string[] files = Directory.GetFiles(dir, "*.json");
                PresetSelect.Options.Clear();

                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    PresetSelect.Options[fileName] = fileName;
                }

                if (PresetSelect.Options.Count > 0)
                {
                    if (string.IsNullOrEmpty(PresetSelect.Value) || !PresetSelect.Options.ContainsKey(PresetSelect.Value))
                    {
                        PresetSelect.Value = PresetSelect.Options.Keys.First();
                    }
                }
                else
                {
                    PresetSelect.Value = "";
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"[ChestPreset] Failed to refresh preset list: {ex.Message}");
            }
        }

        #endregion

        #region Chest Access Helpers

        /// <summary>
        /// Retrieves or creates the global Ender Chest dictionary from Board.Instance.somethings["endChest"].
        /// </summary>
        public static Il2CppSystem.Collections.Generic.Dictionary<PlantType, int> GetOrCreateEndChestDict()
        {
            Board board = Board.Instance;
            if (board == null) return null;

            if (board.somethings == null)
            {
                board.somethings = new Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object>();
            }

            if (!board.somethings.ContainsKey("endChest"))
            {
                var newDict = new Il2CppSystem.Collections.Generic.Dictionary<PlantType, int>();
                board.somethings["endChest"] = newDict;
                return newDict;
            }

            var raw = board.somethings["endChest"];
            return raw?.Cast<Il2CppSystem.Collections.Generic.Dictionary<PlantType, int>>();
        }

        /// <summary>
        /// Resolves the target PumpinerPlants dictionary based on the currently selected TargetMode.
        /// </summary>
        private Il2CppSystem.Collections.Generic.Dictionary<PlantType, int> GetTargetDictionary(out Pumpiner singlePumpiner)
        {
            singlePumpiner = null;

            int mode = TargetMode?.Value ?? 0;
            if (mode == 1) // Global Ender Chest
            {
                return GetOrCreateEndChestDict();
            }

            // Mode 0: Selected Pumpkin Chest
            Pumpiner target = TargetPumpiner?.Value ?? UnityEngine.Object.FindObjectOfType<Pumpiner>();

            if (target != null)
            {
                singlePumpiner = target;
                if (target.PumpinerPlants == null)
                {
                    target.PumpinerPlants = new Il2CppSystem.Collections.Generic.Dictionary<PlantType, int>();
                }
                return target.PumpinerPlants;
            }

            // Mode 2: All Placed Pumpkin Chests fallback
            return GetOrCreateEndChestDict();
        }

        #endregion

        #region UI Button Handlers

        private void OnSaveClicked()
        {
            try
            {
                var dict = GetTargetDictionary(out Pumpiner target);
                if (dict == null || dict.Count == 0)
                {
                    DebugLogger.Msg("[ChestPreset] Target Pumpkin Chest is empty or not found! Place plants into it first.");
                    return;
                }

                string rawName = string.IsNullOrWhiteSpace(NewPresetName?.Value) ? "MyPreset" : NewPresetName.Value.Trim();
                string safeName = string.Join("_", rawName.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(PresetsDir, $"{safeName}.json");

                var preset = new PresetData
                {
                    Name = rawName,
                    Description = $"Exported on {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                };

                var enumerator = dict.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var kvp = enumerator.Current;
                    preset.Items.Add(new PresetItem
                    {
                        PlantId = (int)kvp.Key,
                        Name = kvp.Key.ToString(),
                        Count = kvp.Value
                    });
                }

                string json = JsonConvert.SerializeObject(preset, Formatting.Indented);
                File.WriteAllText(filePath, json);

                RefreshPresetList();
                PresetSelect.Value = safeName;

                DebugLogger.Msg($"[ChestPreset] Successfully saved preset '{safeName}' ({preset.Items.Count} plant types)!");
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"[ChestPreset] Failed to save preset: {ex.Message}");
            }
        }

        private void OnLoadClicked()
        {
            try
            {
                string selectedName = PresetSelect?.Value;
                if (string.IsNullOrEmpty(selectedName))
                {
                    DebugLogger.Msg("[ChestPreset] No preset selected to load!");
                    return;
                }

                string filePath = Path.Combine(PresetsDir, $"{selectedName}.json");
                if (!File.Exists(filePath))
                {
                    DebugLogger.Msg($"[ChestPreset] Preset file does not exist: {filePath}");
                    return;
                }

                string json = File.ReadAllText(filePath);
                var preset = JsonConvert.DeserializeObject<PresetData>(json);
                if (preset == null || preset.Items == null || preset.Items.Count == 0)
                {
                    DebugLogger.Msg("[ChestPreset] Preset is empty or corrupted!");
                    return;
                }

                if (Board.Instance == null)
                {
                    DebugLogger.Msg("[ChestPreset] Active game board not found! Start a level first.");
                    return;
                }

                // Determine seed card type to drop based on TargetMode
                PlantType seedType = (TargetMode?.Value == 1) ? PlantType.EndPumpiner : PlantType.Pumpiner;

                // Determine drop position in lawn bounds
                Vector2 dropPos = Mouse.Instance != null && Camera.main != null
                    ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
                    : Vector2.zero;

                dropPos.x = Mathf.Clamp(dropPos.x, -5f, 5f);
                dropPos.y = Mathf.Clamp(dropPos.y, -3.5f, 2.5f);

                var card = Lawnf.SetDroppedCard(dropPos, seedType, 0);
                if (card != null)
                {
                    _pendingCardPresets.Enqueue(preset);
                    DebugLogger.Msg($"[ChestPreset] Dropped {seedType} seed card with preset '{preset.Name}' at ({dropPos.x:0.#}, {dropPos.y:0.#})! Plant it to spawn the chest.");
                }
                else
                {
                    DebugLogger.Msg("[ChestPreset] Failed to spawn dropped card on lawn.");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"[ChestPreset] Failed to drop preset card: {ex.Message}");
            }
        }

        private void OnApplyClicked()
        {
            try
            {
                string selectedName = PresetSelect?.Value;
                if (string.IsNullOrEmpty(selectedName))
                {
                    DebugLogger.Msg("[ChestPreset] No preset selected to apply!");
                    return;
                }

                string filePath = Path.Combine(PresetsDir, $"{selectedName}.json");
                if (!File.Exists(filePath))
                {
                    DebugLogger.Msg($"[ChestPreset] Preset file does not exist: {filePath}");
                    return;
                }

                string json = File.ReadAllText(filePath);
                var preset = JsonConvert.DeserializeObject<PresetData>(json);
                if (preset == null || preset.Items == null || preset.Items.Count == 0)
                {
                    DebugLogger.Msg("[ChestPreset] Preset is empty or corrupted!");
                    return;
                }

                int mode = TargetMode?.Value ?? 0;
                bool isMerge = (LoadMode?.Value == 1);
                bool allowHugeStack = (NoChestStorageLimit.instance?.Active == true && NoChestStorageLimit.instance.BypassStackLimit?.Value == true);
                bool allowHugeSlots = (NoChestStorageLimit.instance?.Active == true && NoChestStorageLimit.instance.BypassSlotLimit?.Value == true);

                if (mode == 2) // All Placed Pumpkin Chests
                {
                    var allPumpkins = UnityEngine.Object.FindObjectsOfType<Pumpiner>();
                    if (allPumpkins != null)
                    {
                        foreach (var p in allPumpkins)
                        {
                            ApplyPresetToPumpkin(p, preset, isMerge, allowHugeStack, allowHugeSlots);
                        }
                    }

                    // Also update global endChest
                    var endDict = GetOrCreateEndChestDict();
                    if (endDict != null)
                    {
                        ApplyPresetToDict(endDict, preset, isMerge, allowHugeStack, allowHugeSlots);
                    }

                    DebugLogger.Msg($"[ChestPreset] Applied preset '{preset.Name}' across ALL placed Pumpkin Chests and Ender Chest!");
                }
                else if (mode == 1) // Global Ender Chest Only
                {
                    var endDict = GetOrCreateEndChestDict();
                    if (endDict == null)
                    {
                        DebugLogger.Msg("[ChestPreset] Active game board not found! Start a level first.");
                        return;
                    }

                    ApplyPresetToDict(endDict, preset, isMerge, allowHugeStack, allowHugeSlots);

                    // Notify any placed Ender Pumpkins to update visual text counters
                    var endPumpkins = UnityEngine.Object.FindObjectsOfType<EndPumpiner>();
                    if (endPumpkins != null)
                    {
                        foreach (var ep in endPumpkins)
                        {
                            ep.OnRemove();
                        }
                    }

                    DebugLogger.Msg($"[ChestPreset] Applied preset '{preset.Name}' into Global Ender Chest!");
                }
                else // Mode 0: Selected Pumpkin Chest
                {
                    Pumpiner target = TargetPumpiner?.Value ?? UnityEngine.Object.FindObjectOfType<Pumpiner>();

                    if (target == null)
                    {
                        DebugLogger.Msg("[ChestPreset] No target Pumpkin Chest found! Use 'Pick' or place a Pumpkin Chest on the lawn.");
                        return;
                    }

                    ApplyPresetToPumpkin(target, preset, isMerge, allowHugeStack, allowHugeSlots);
                    DebugLogger.Msg($"[ChestPreset] Applied preset '{preset.Name}' into target Pumpkin Chest!");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"[ChestPreset] Failed to apply preset: {ex.Message}");
            }
        }

        private void OnDeleteClicked()
        {
            try
            {
                string selectedName = PresetSelect?.Value;
                if (string.IsNullOrEmpty(selectedName)) return;

                string filePath = Path.Combine(PresetsDir, $"{selectedName}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    DebugLogger.Msg($"[ChestPreset] Deleted preset: {selectedName}");
                    RefreshPresetList();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"[ChestPreset] Failed to delete preset: {ex.Message}");
            }
        }

        private void OnRefreshClicked()
        {
            RefreshPresetList();
            DebugLogger.Msg("[ChestPreset] Preset list refreshed.");
        }

        private void OnOpenFolderClicked()
        {
            try
            {
                string dir = PresetsDir;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"[ChestPreset] Failed to open folder: {ex.Message}");
            }
        }

        #endregion

        #region Application Logic

        private static void ApplyPresetToDict(
            Il2CppSystem.Collections.Generic.Dictionary<PlantType, int> dict,
            PresetData preset,
            bool isMerge,
            bool allowHugeStack,
            bool allowHugeSlots)
        {
            if (dict == null || preset == null || preset.Items == null) return;

            if (!isMerge)
            {
                dict.Clear();
            }

            foreach (var item in preset.Items)
            {
                PlantType plantType = (PlantType)item.PlantId;
                if (!Enum.IsDefined(typeof(PlantType), plantType) || plantType == PlantType.Nothing)
                {
                    if (Enum.TryParse<PlantType>(item.Name, out var parsedType) && parsedType != PlantType.Nothing)
                    {
                        plantType = parsedType;
                    }
                    else
                    {
                        continue; // Skip invalid plant ID
                    }
                }

                int count = item.Count;
                if (count < 1) count = 1;
                if (!allowHugeStack && count > 64) count = 64;

                if (!allowHugeSlots && dict.Count >= 54 && !dict.ContainsKey(plantType))
                {
                    continue; // Respect vanilla 54 grid limit
                }

                if (dict.ContainsKey(plantType))
                {
                    dict[plantType] = isMerge ? (dict[plantType] + count) : count;
                }
                else
                {
                    dict.Add(plantType, count);
                }
            }
        }

        private static void ApplyPresetToPumpkin(
            Pumpiner pumpiner,
            PresetData preset,
            bool isMerge,
            bool allowHugeStack,
            bool allowHugeSlots)
        {
            if (pumpiner == null || preset == null) return;

            if (pumpiner.PumpinerPlants == null)
            {
                pumpiner.PumpinerPlants = new Il2CppSystem.Collections.Generic.Dictionary<PlantType, int>();
            }

            ApplyPresetToDict(pumpiner.PumpinerPlants, preset, isMerge, allowHugeStack, allowHugeSlots);

            // Synchronize contains list
            if (pumpiner.contains == null)
            {
                pumpiner.contains = new Il2CppSystem.Collections.Generic.List<PlantType>();
            }

            if (!isMerge)
            {
                pumpiner.contains.Clear();
            }

            var enumerator = pumpiner.PumpinerPlants.GetEnumerator();
            while (enumerator.MoveNext())
            {
                PlantType pt = enumerator.Current.Key;
                if (!pumpiner.contains.Contains(pt))
                {
                    pumpiner.contains.Add(pt);
                }
            }

            // Refresh text / counter
            if (pumpiner is EndPumpiner ep)
            {
                ep.OnRemove();
            }
            else
            {
                pumpiner.UpdateText();
            }
        }

        #endregion

        #region Harmony Patches

        /// <summary>
        /// Injects queued preset plant inventories into newly planted Pumpkin Chests.
        /// </summary>
        [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.SetPlant))]
        public static class CreatePlant_SetPlant_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(PlantType theSeedType, Plant __result)
            {
                if (__result == null) return;

                if (theSeedType == PlantType.Pumpiner || theSeedType == PlantType.EndPumpiner)
                {
                    if (_pendingCardPresets.Count > 0)
                    {
                        var preset = _pendingCardPresets.Dequeue();
                        var pumpiner = __result.TryCast<Pumpiner>();
                        if (pumpiner != null)
                        {
                            ApplyPresetToPumpkin(
                                pumpiner,
                                preset,
                                isMerge: false,
                                allowHugeStack: NoChestStorageLimit.instance?.Active == true && NoChestStorageLimit.instance.BypassStackLimit?.Value == true,
                                allowHugeSlots: NoChestStorageLimit.instance?.Active == true && NoChestStorageLimit.instance.BypassSlotLimit?.Value == true);
                            DebugLogger.Msg($"[ChestPreset] Injected preset '{preset.Name}' into newly planted {theSeedType} chest!");
                        }
                    }
                }
            }
        }

        #endregion
    }
}
