using Magnetar_Client.UI.Setting;
#if MELONLOADER || RELEASE_MELON
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class LevelSetup : Module
{
    public override string Name { get; set; } = "Level Setup";
    public override string Description { get; set; } = "Allows you to manually trigger board's setup functions.";
    public override string SearchHints { get; set; } = "levelsetup boardsetup setupfunctions triggerboardsetup " +
        "setupmanager levelinit boardinit initializetrigger levelinitializer manualsetup setupcustomizer " +
        "boardconfigurator levelsync setupoverride boardhandler initializegame levelconfig startuplevel " +
        "setupdebug boardreset levelreset levelbuilder initboard boardloader customsetup runsetup";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    public static LevelSetup instance;

    // General
    public BoolSetting OverrideOnEnter = new("Override On Enter", false);
    public StringSetting LevelName = new("Custom Level Name", "");

    private string _lastSyncedLevelName = "";
    private bool _userModified = false;

    // Extra
    public ButtonSetting SetGraves;
    public ButtonSetting SetFreezedPlants;
    public ButtonSetting SpawnGraveZombies;

    public LevelSetup()
    {
        instance = this;

        CreateCategory("General");

        LevelName.OnValueChanged += (newVal) =>
        {
            _userModified = !string.Equals(newVal, _lastSyncedLevelName);
            if (Active)
            {
                ApplyCustomLevelName(newVal);
            }
        };

        GameData.Actions.OnInGameUIInstantiated += (inGameUI) =>
        {
            string currentGameLevel = GameData.GetLevelName();

            if (Active && OverrideOnEnter.Value && !string.IsNullOrWhiteSpace(LevelName.Value))
            {
                // Capture authentic game name as reset baseline before overriding
                if (!string.IsNullOrWhiteSpace(currentGameLevel) && currentGameLevel != "Unknown")
                {
                    _lastSyncedLevelName = currentGameLevel;
                    LevelName.DefaultValue = currentGameLevel;
                }
                _userModified = true;
                ApplyCustomLevelName(LevelName.Value);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(currentGameLevel) && currentGameLevel != "Unknown")
                {
                    SyncFromGame(currentGameLevel);
                    _userModified = false;
                }
            }
        };

        AddSettings(OverrideOnEnter, LevelName);
        EndCategory();

        CreateCategory("Extra");

        SetGraves = new ButtonSetting("Set Graves", SetGravesNow);
        SetFreezedPlants = new ButtonSetting("Set Freezed Plants", SetFreezedPlantsNow);
        SpawnGraveZombies = new ButtonSetting("Spawn Grave Zombies", SpawnGraveZombiesNow);

        AddSettings(SetGraves, SetFreezedPlants, SpawnGraveZombies);
        EndCategory();

        OnDisable();
    }

    private void SyncFromGame(string name)
    {
        _lastSyncedLevelName = name;
        LevelName.SetDirect(name);
    }

    public override void OnSettingOpened()
    {
        if (!GameData.BoardInstanceIsNull && !_userModified)
        {
            string currentGameLevel = GameData.GetLevelName();
            if (!string.IsNullOrWhiteSpace(currentGameLevel) && currentGameLevel != "Unknown" && currentGameLevel != LevelName.Value)
            {
                SyncFromGame(currentGameLevel);
            }
        }
    }

    public override void OnSettingUpdate()
    {
        if (GameData.BoardInstanceIsNull) return;

        if (LevelName.IsFocused)
        {
            _userModified = !string.Equals(LevelName.Value, _lastSyncedLevelName);
            if (Active)
            {
                ApplyCustomLevelName(LevelName.Value);
            }
        }
        else if (!_userModified)
        {
            string currentGameLevel = GameData.GetLevelName();
            if (!string.IsNullOrWhiteSpace(currentGameLevel) && currentGameLevel != "Unknown" && currentGameLevel != LevelName.Value)
            {
                SyncFromGame(currentGameLevel);
            }
        }
    }

    public void ApplyCustomLevelName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        // 1. Standard InGameUI
        var ui = InGameUI.Instance;
        if (ui != null)
        {
            try
            {
                ui.SetLevelName(name);
            }
            catch { }

            // LevelName1, LevelName2, and LevelName3 (including drop-shadow children)
            TextMeshProUGUI[] targets = { ui.LevelName1, ui.LevelName2, ui.LevelName3 };
            foreach (var t in targets)
            {
                if (t != null)
                {
                    t.text = name;
                    var childTexts = t.GetComponentsInChildren<TextMeshProUGUI>(true);
                    if (childTexts != null)
                    {
                        foreach (var ct in childTexts)
                        {
                            if (ct != null) ct.text = name;
                        }
                    }
                }
            }

            // Update all texts tracked in levelNameText list
            if (ui.levelNameText != null)
            {
                foreach (var t in ui.levelNameText)
                {
                    if (t != null) t.text = name;
                }
            }
        }

        // 2. IZE / EVE UI (IZBottomMenu)
        if (IZBottomMenu.Instance != null)
        {
            try
            {
                IZBottomMenu.Instance.ChangeString(name);
            }
            catch { }

            if (IZBottomMenu.Instance.levelName != null)
            {
                foreach (var t in IZBottomMenu.Instance.levelName)
                {
                    if (t != null) t.text = name;
                }
            }
        }

        if (!string.IsNullOrEmpty(GameAPP.theIZLevelName))
        {
            GameAPP.theIZLevelName = name;
        }
    }

    public void SetGravesNow()
    {
        if (!Active) return;
        if (GameData.BoardInstanceIsNull) return;
        GameData.BoardInstance.SetGrave();
    }

    public void SpawnGraveZombiesNow()
    {
        if (!Active) return;
        if (GameData.BoardInstanceIsNull) return;

        foreach (var item in GameData.BoardInstance.griditemArray)
        {
            if (item is Grave grave)
            {
                grave.SetZombie();
            }
        }
    }

    public void SetFreezedPlantsNow()
    {
        if (!Active) return;
        if (GameData.BoardInstanceIsNull) return;
        GameData.BoardInstance.SetFreezedPlant();
    }

    public override void OnDisable()
    {
        OverrideOnEnter.IsDisabled = true;
        LevelName.IsDisabled = true;
        SetGraves.IsDisabled = true;
        SetFreezedPlants.IsDisabled = true;
        SpawnGraveZombies.IsDisabled = true;
    }

    public override void OnEnable()
    {
        OverrideOnEnter.IsDisabled = false;
        LevelName.IsDisabled = false;
        SetGraves.IsDisabled = false;
        SetFreezedPlants.IsDisabled = false;
        SpawnGraveZombies.IsDisabled = false;

        if (!GameData.BoardInstanceIsNull)
        {
            if (_userModified && !string.IsNullOrWhiteSpace(LevelName.Value))
            {
                ApplyCustomLevelName(LevelName.Value);
            }
            else
            {
                string currentGameLevel = GameData.GetLevelName();
                if (!string.IsNullOrWhiteSpace(currentGameLevel) && currentGameLevel != "Unknown")
                {
                    SyncFromGame(currentGameLevel);
                }
            }
        }
    }
}
