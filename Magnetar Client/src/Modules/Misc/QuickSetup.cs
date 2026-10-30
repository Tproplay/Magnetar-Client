using Magnetar_Client.UI.Setting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;

using static Magnetar_Client.GameData;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Api.PathsManager;
using UnityEngine;
using Magnetar_Client.Utils;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class QuickSetup : Module
{
    #region Module Info

    public override string Name { get; set; } = "Quick setup";
    public override string Description { get; set; } = "Save/load a custom setup loadout.";
    public override string SearchHints { get; set; } =
        "quicksetup loadout setupmanager savebuild loadbuild " +
        "setup presetmanager quickload quicksave plantsetup configuration buildmanager setuploader loadouts " +
        "quick-setup customsetup setupsaver presetload";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

    #endregion

    #region Settings

    // Save category
    public StringSetting NameOfSave;
    public ButtonSetting SaveCurrentSetup;
    public ButtonSetting CopyCurrentCode;

    // Load category
    public BoolSetting ClearBeforeLoad;
    public MultiSelectSetting SelectedLoadSetup;
    public ButtonSetting LoadSetup;
    public ButtonSetting LoadFromClipBoard;
    public ButtonSetting DeleteSelectedSetup;

    #endregion

    #region State

    public static QuickSetup instance;

    private Dictionary<string, string> _savedSetups = new(StringComparer.OrdinalIgnoreCase);

    ICoroutineHandle _copyCoroutine;
    ICoroutineHandle _pasteCoroutine;

    #endregion

    #region Constructor

    public QuickSetup()
    {
        instance = this;

        RegisterTranslations(Translator.CreateDictionary(new string[]
        {
            // Save
            "Copy To Clipboard",
            "Copied!",

            // Load
            "Load From Clipboard",
            "Loaded!",
        }));

        CreateCategory("Save");

        NameOfSave = new StringSetting("Name", "");
        SaveCurrentSetup = new ButtonSetting("Save Current Setup", SaveCurrentSetupCallBack, "Save");
        CopyCurrentCode = new ButtonSetting("Copy Current Setup Code", CopySetupCodeToClipBoard, "Copy To Clipboard")
        {
            SaveButtonTranslation = false
        };

        AddSettings(
            NameOfSave,
            SaveCurrentSetup,
            CopyCurrentCode
        );

        EndCategory();

        CreateCategory("Load");

        ClearBeforeLoad = new BoolSetting("Clear Lawn Before Load", true);
        SelectedLoadSetup = new MultiSelectSetting("Setup") { MaxSelection = 1 };
        LoadSetup = new ButtonSetting("Load Selected Setup", LoadSelectedSetupCallBack, "Load");
        LoadFromClipBoard = new ButtonSetting("Load Setup From Clipboard", LoadSetupFromClipBoard, "Load From Clipboard")
        {
            SaveButtonTranslation = false
        };
        DeleteSelectedSetup = new ButtonSetting("Delete Selected Setup", DeleteSelectedSetupCallBack, "<color=red>Delete</color>");

        AddSettings(
            ClearBeforeLoad,
            SelectedLoadSetup,
            LoadSetup,
            LoadFromClipBoard,
            DeleteSelectedSetup
        );

        EndCategory();

        LoadSetupsFromDisk();
    }

    #endregion

    #region Save

    private void CopySetupCodeToClipBoard()
    {
        string saveName = string.IsNullOrEmpty(NameOfSave.Value.Trim()) ? "Untitled" : NameOfSave.Value.Trim();
        string code = GenerateSetupCode();
        GUIUtility.systemCopyBuffer = $"{saveName}::{code}";

        _copyCoroutine?.Stop();
        _copyCoroutine = CoroutineManager.Start(FlashButtonText(
            CopyCurrentCode,
            Translate("Copied!"),
            Translate("Copy To Clipboard"),
            1f,
            () => _copyCoroutine = null));
    }

    public void SaveCurrentSetupCallBack()
    {
        if (PlantList == null || PlantList.Count == 0)
        {
            DebugLogger.Warning("[QuickSetup] No plants found on the lawn to save.");
            return;
        }

        string saveName = NameOfSave.Value?.Trim();
        if (string.IsNullOrEmpty(saveName))
            saveName = $"Setup_{_savedSetups.Count + 1}";

        string code = GenerateSetupCode();
        if (string.IsNullOrEmpty(code))
        {
            DebugLogger.Error("[QuickSetup] Failed to encode current BoardInstance setup.");
            return;
        }

        _savedSetups[saveName] = code;
        SaveSetupsToDisk();
        RefreshLoadOptions();

        NameOfSave.Value = "";
        DebugLogger.Msg($"[QuickSetup] Saved '{saveName}' ({PlantList.Count} plants).");
    }

    #endregion

    #region Load

    public void LoadSelectedSetupCallBack()
    {
        if (!Active)
        {
            DebugLogger.Warning("[QuickSetup] Module is disabled. Turn the module ON to load setups.");
            return;
        }

        if (BoardInstanceIsNull)
        {
            DebugLogger.Warning("[QuickSetup] Board instance is Null.");
            return;
        }

        string selectedName = GetSelectedSetupName();
        if (string.IsNullOrEmpty(selectedName))
        {
            DebugLogger.Warning("[QuickSetup] No setup selected.");
            return;
        }

        if (!_savedSetups.TryGetValue(selectedName, out string code) || string.IsNullOrEmpty(code))
        {
            DebugLogger.Warning($"[QuickSetup] Setup '{selectedName}' contains no data.");
            return;
        }

        int placed = ApplySetupCode(code);
        DebugLogger.Msg($"[QuickSetup] Loaded '{selectedName}' ({placed} plants placed).");
    }

    public void LoadSetupFromClipBoard()
    {
        if (!Active)
        {
            DebugLogger.Warning("[QuickSetup] Module is disabled. Turn the module ON to load setups.");
            return;
        }

        if (BoardInstanceIsNull)
        {
            DebugLogger.Warning("[QuickSetup] Board instance is Null.");
            return;
        }

        string clipboard = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrWhiteSpace(clipboard))
        {
            DebugLogger.Warning("[QuickSetup] Clipboard is empty.");
            return;
        }

        // Expected format: "Name::code"  or just "code" (bare code without name prefix)
        string code;
        string pastedName;

        int sep = clipboard.IndexOf("::", StringComparison.Ordinal);
        if (sep >= 0)
        {
            pastedName = clipboard[..sep].Trim();
            code = clipboard[(sep + 2)..].Trim();
        }
        else
        {
            pastedName = "Clipboard";
            code = clipboard.Trim();
        }

        if (string.IsNullOrEmpty(code))
        {
            DebugLogger.Warning("[QuickSetup] Clipboard does not contain a valid setup code.");
            return;
        }

        int placed = ApplySetupCode(code);
        if (placed == 0)
        {
            DebugLogger.Warning("[QuickSetup] No plants could be decoded from the clipboard code.");
            return;
        }

        DebugLogger.Msg($"[QuickSetup] Loaded '{pastedName}' from clipboard ({placed} plants placed).");

        _pasteCoroutine?.Stop();
        _pasteCoroutine = CoroutineManager.Start(FlashButtonText(
            LoadFromClipBoard,
            Translate("Loaded!"),
            Translate("Load From Clipboard"),
            1f,
            () => _pasteCoroutine = null));
    }

    #endregion

    #region Delete

    public void DeleteSelectedSetupCallBack()
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
            SelectedLoadSetup.SelectedValues.Clear();
            DebugLogger.Msg($"[QuickSetup] Deleted '{selectedName}'.");
        }
    }

    #endregion

    #region Code Generation & Application

    public static string GenerateSetupCode()
    {
        if (PlantList == null || PlantList.Count == 0) return string.Empty;

        StringBuilder sb = new(PlantList.Count * 6);

        for (int i = 0; i < PlantList.Count; i++)
        {
            var plant = PlantList[i];
            if (plant == null) continue;

            // Encoding: (plantType * 10000) + (col * 100) + row
            long encoded = ((int)plant.thePlantType * 10000L) + (plant.thePlantColumn * 100) + plant.thePlantRow;

            if (sb.Length > 0) sb.Append('-');
            sb.Append(ToBase62(encoded));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Decodes a setup code and spawns all plants. Optionally clears the lawn first.
    /// </summary>
    /// <returns>The number of plants successfully placed.</returns>
    private int ApplySetupCode(string code)
    {
        if (ClearBeforeLoad.Value && PlantList != null)
        {
            var snapshot = PlantList.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i]?.Die();
        }

        int placed = 0;
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
                placed++;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[QuickSetup] Token decode failure on '{tokens[i]}': {ex.Message}");
            }
        }

        return placed;
    }

    private static void SpawnPlant(int col, int row, int plantType)
        => CreatePlant.Instance.SetPlant(col, row, (PlantType)plantType);

    #endregion

    #region Disk I/O

    private void LoadSetupsFromDisk()
    {
        try
        {
            if (File.Exists(SavedSetupPlantPath))
            {
                string json = File.ReadAllText(SavedSetupPlantPath);
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
            File.WriteAllText(SavedSetupPlantPath, JsonConvert.SerializeObject(_savedSetups, Formatting.Indented));
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[QuickSetup] Failed to save setups to disk: {ex.Message}");
        }
    }

    #endregion

    #region UI Helpers

    private void RefreshLoadOptions()
    {
        SelectedLoadSetup.Options.Clear();
        int id = 0;
        foreach (var kvp in _savedSetups)
            SelectedLoadSetup.AddOption(id++, kvp.Key);
    }

    private string GetSelectedSetupName()
    {
        if (SelectedLoadSetup.SelectedValues?.Count > 0)
        {
            int selectedId = SelectedLoadSetup.SelectedValues.First();
            if (SelectedLoadSetup.Options.TryGetValue(selectedId, out string name))
                return name;
        }
        return null;
    }

    /// <summary>
    /// Temporarily overrides a button's text, then restores it after <paramref name="duration"/> seconds.
    /// </summary>
    private static IEnumerator FlashButtonText(ButtonSetting button, string flash, string restore, float duration, Action onDone = null)
    {
        button.CustomButtonText = flash;
        yield return new WaitForSeconds(duration);
        button.CustomButtonText = restore;
        onDone?.Invoke();
    }

    #endregion

    #region Base62 Encoding

    public static string ToBase62(long value)
    {
        if (value == 0) return "0";
        const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        char[] buffer = new char[16];
        int i = 0;

        while (value > 0)
        {
            buffer[i++] = alphabet[(int)(value % alphabet.Length)];
            value /= alphabet.Length;
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

    #endregion
}