#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
using Magnetar_Client.UI.Setting;
using DiscordRPC;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Magnetar_Client.Utils;

using static Magnetar_Client.GameData;
using static Magnetar_Client.Utils.Maths;
using Magnetar_Client.Core;
using Magnetar_Client.Api;

#if !ANDROID
namespace Magnetar_Client.Modules;

public class DiscordRPC : Module
{
    // Mod Info
    public override string Name { get; set; } = "Discord RPC";
    public override string Description { get; set; } = "Shows rolling Gamestatus updates on Discord.";
    public override string SearchHints { get; set; } = "discordrpc discordrichpresence discordpresence discordactivity " +
        "discordstatus discordintegration rpcstatus richpresence discordconnect discordlink discordinfo discorddisplay " +
        "rpcpresence discordrp discordstat discordlive discordsync discordgame discordapi discrodrpc discordrcp discordrps " +
        "discordrich discordpresance discordpresense discordconection discordintigration rpcbot rpcclient rpcactive";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;
    public override bool EnableInVanillaMode { get; set; } = true;

    // Mod Data
    public static DiscordRPC instance;

    public FloatSetting SwitchSpeed;

    public ListStringSetting InGameLine1;
    public SelectSetting InGame_Randomizer_mode1;
    public ListStringSetting InGameLine2;
    public SelectSetting InGame_Randomizer_mode2;


    public List<string> Line1Cycle = new();
    public List<string> Line2Cycle = new();

    private DiscordRpcClient client;
    private Timestamps elapsedTimer;

    private float rotationTimer;
    private int index1;
    private int index2;
    private float dataRefreshTimer;

    public int TotalPresenceUpdates
    {
        get => GetSaved<int>("TotalUpdates", 0);
        set => SetSaved("TotalUpdates", value);
    }

    public DiscordRPC()
    {
        instance = this;

        // 1. Register module-specific phrases to SavedTranslationData
        RegisterTranslations(Translator.CreateDictionary(
            new string[]
            {
                // status
                "Browsing Magnetar's GUI",
                "Looking at the Main Menu",
                "Checking out the almanac",
                "Viewing plants in the almanac",
                "Viewing zombies in the almanac",
                "Started a level",
                "Picking Seeds",
                "Roaming in the Garden",
                "Growing Seeds",
                "Waterning plants",

                // Iteration order
                "Sequential",
                "Random",

                // Extra
                "Made By Tproplay",
            }
            ));

        // 2. Register module-specific regex translations
        RegisterRegexTranslation(@"^Playing:\s*(.*)$", "Playing: {1}");
        RegisterRegexTranslation(@"^Pvz Fusion v(.*)$", "Pvz Fusion v{1}");

        CreateCategory("General");

        SwitchSpeed = new FloatSetting("Switch Speed (s)", 2f, 30f, 5f, 3, 0);
        AddSettings(SwitchSpeed);

        EndCategory();

        CreateCategory("In Game", true);

        InGameLine1 = new ListStringSetting("Line 1",
            new List<string>
            {
                "Magnetar Client v{Magnetar.Version}",
                "Playing: {Level.Name}",
            },
            15, In_Game_AutoCompleteArgs
            );

        InGame_Randomizer_mode1 = new SelectSetting("Iteration Mode", 0)
        {
            CustomSaveKey = "Iteration Mode 1",
            FallbackSaveKeys = new List<string> { "Iteration Mode" },
            Options = new Dictionary<int, string>
            {
                { 0, "Sequential" },
                { 1, "Random" }
            }
        };

        AddSettings(InGameLine1, InGame_Randomizer_mode1);

        InGameLine2 = new ListStringSetting("Line 2",
            new List<string>
            {
                "Sun: {Level.Sun} | Money: {Level.Money}",
                "Wave: {Level.Current_Wave}/{Level.Max_Wave}",
                "Plants: {Level.Number_Of_Plants} | Zombies: {Level.Number_Of_Zombies}",
            },
            15, In_Game_AutoCompleteArgs
            );

        InGame_Randomizer_mode2 = new SelectSetting("Iteration Mode", 0)
        {
            CustomSaveKey = "Iteration Mode 2",
            FallbackSaveKeys = new List<string> { "Iteration Mode" },
            Options = new Dictionary<int, string>
            {
                { 0, "Sequential" },
                { 1, "Random" }
            }
        };

        AddSettings(InGameLine2, InGame_Randomizer_mode2);

        EndCategory();

        // Legacy Compatibility
        Actions.OnLateInitializeCore += UpdateLegacyStrings;
    }

    private static readonly Dictionary<string, string> legacyAutoComplete = new(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Magnetar_Version", "Magnetar.Version" },
        { "Game_Version", "Game.Version" },
        { "Level_Name", "Level.Name" },
        { "Sun", "Level.Sun" },
        { "Money", "Level.Money" },
        { "Current_Wave", "Level.Current_Wave" },
        { "Max_Wave", "Level.Max_Wave" },
        { "number_of_plants", "Level.Number_Of_Plants" },
        { "number_of_zombies", "Level.Number_Of_Zombies" },
        { "movers_left", "Level.Movers_Left" }
    };

    private void UpdateLegacyStrings()
    {
        UpdateListSetting(InGameLine1);
        UpdateListSetting(InGameLine2);
    }

    private void UpdateListSetting(ListStringSetting setting)
    {
        if (setting?.Values == null) return;

        bool modified = false;
        for (int i = 0; i < setting.Values.Count; i++)
        {
            string line = setting.Values[i];
            if (string.IsNullOrEmpty(line)) continue;

            string updatedLine = ReplaceLegacyKeys(line);
            if (updatedLine != line)
            {
                setting.Values[i] = updatedLine;
                modified = true;
            }
        }

        if (modified)
        {
            setting.OnValueChanged?.Invoke(setting.Values);
        }
    }

    private static string ReplaceLegacyKeys(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains("{")) return input;

        return Regex.Replace(input, @"\{\s*([^{}\s]+)\s*\}", match =>
        {
            string key = match.Groups[1].Value;
            if (legacyAutoComplete.TryGetValue(key, out string replacement))
            {
                return $"{{{replacement}}}";
            }
            return match.Value;
        });
    }

    public override void OnLanguageChanged()
    {
        InGame_Randomizer_mode1.CustomNames = InGame_Randomizer_mode1.Options
            .ToDictionary(kvp => kvp.Key, kvp => Translate(kvp.Value));
        InGame_Randomizer_mode2.CustomNames = InGame_Randomizer_mode2.Options
            .ToDictionary(kvp => kvp.Key, kvp => Translate(kvp.Value));
    }

    public override void OnEnable()
    {
        elapsedTimer = Timestamps.Now;
        client = new DiscordRpcClient("1500852523764813928");
        client.Initialize();

        UpdateText();
        UpdatePresence();
    }

    public override void OnDisable()
    {
        if (client != null)
        {
            client.ClearPresence();
            client.Dispose();
        }
    }

    public override void OnUpdateActive()
    {
        rotationTimer += Time.deltaTime;
        dataRefreshTimer += Time.deltaTime;

        if (dataRefreshTimer >= 1.0f)
        {
            UpdateText();
            dataRefreshTimer = 0f;
        }

        if (rotationTimer >= SwitchSpeed.Value)
        {
            rotationTimer = 0f;
            RotateIndices();
            UpdatePresence();
        }
    }

    private void UpdateText()
    {
        Line1Cycle.Clear();
        Line2Cycle.Clear();

        if (StatusCheck.InGamePickingSeeds)
        {
            Line1Cycle.Add(Translate("Picking Seeds"));
        }
        else if (StatusCheck.InGameLevel)
        {
            foreach (var line in InGameLine1.Values)
            {
                if (string.IsNullOrEmpty(line)) continue;
                Line1Cycle.Add(In_Game_FormatString(line));
            }
            foreach (var line in InGameLine2.Values)
            {
                if (string.IsNullOrEmpty(line)) continue;
                Line2Cycle.Add(In_Game_FormatString(line));
            }
        }
        else if (StatusCheck.OnGameEnterAnimation)
        {
            Line1Cycle.Add(Translate("Started a level"));
        }
        else if (StatusCheck.OnMainMenu && ClientConfig.showgui)
        {
            Line1Cycle.Add(Translate("Browsing Magnetar's GUI"));
        }
        else if (StatusCheck.OnMainMenu)
        {
            Line1Cycle.Add(Translate("Looking at the Main Menu"));
        }
        else if (StatusCheck.InAlmanac)
        {
            Line1Cycle.Add(Translate("Checking out the almanac"));
            if (StatusCheck.OnAlmanacPlant) Line1Cycle.Add(Translate("Viewing plants in the almanac"));
            else if (StatusCheck.OnAlmanacZombie) Line1Cycle.Add(Translate("Viewing zombies in the almanac"));
        }
        else if (StatusCheck.InBigZenGarden)
        {
            Line1Cycle.Add(Translate("Roaming in the Big Garden"));
            Line2Cycle.Add(Translate("Growing Seeds"));
            Line2Cycle.Add(Translate("Waterning plants"));
        }
        else if (StatusCheck.InZenGarden)
        {
            Line2Cycle.Add(Translate("Growing Seeds"));
            Line2Cycle.Add(Translate("Waterning plants"));
        }
    }

    public static List<string> In_Game_AutoCompleteArgs = new()
    {
        // Game Info
        "Magnetar.Version","Game.Version",

        // Level
        "Level.Name",
        "Level.Sun","Level.Money",
        "Level.Current_Wave","Level.Max_Wave",
        "Level.Number_Of_Plants","Level.Number_Of_Zombies",
        "Level.Movers_Left"
    };

    private string In_Game_FormatString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        string result = ReplaceLegacyKeys(input);

        // Game Info
        result = result.Replace("{Magnetar.Version}", MagnetarInfo.Version);
        result = result.Replace("{Game.Version}", Application.version);

        // Level
        result = result.Replace("{Level.Name}", GetLevelName());
        result = result.Replace("{Level.Sun}", FormatInternational(BoardInstance.theSun));
        result = result.Replace("{Level.Money}", FormatInternational(BoardInstance.theMoney));
        result = result.Replace("{Level.Current_Wave}", BoardInstance.theWave.ToString());
        result = result.Replace("{Level.Max_Wave}", BoardInstance.theMaxWave.ToString());
        result = result.Replace("{Level.Number_Of_Plants}", PlantList.Count.ToString());
        result = result.Replace("{Level.Number_Of_Zombies}", ZombieList.Count.ToString());
        result = result.Replace("{Level.Movers_Left}", BoardInstance.mowerArray.Count.ToString());

        return result;
    }

    private static readonly System.Random _random = new();

    private void RotateIndices()
    {
        // Line 1 Rotation
        if (Line1Cycle != null && Line1Cycle.Count > 0)
        {
            if (InGame_Randomizer_mode1.Value == 1) // Random
                index1 = _random.Next(Line1Cycle.Count);
            else // Sequential (0)
                index1 = (index1 + 1) % Line1Cycle.Count;
        }
        else
        {
            index1 = 0;
        }

        // Line 2 Rotation
        if (Line2Cycle != null && Line2Cycle.Count > 0)
        {
            if (InGame_Randomizer_mode2.Value == 1) // Random
                index2 = _random.Next(Line2Cycle.Count);
            else // Sequential (0)
                index2 = (index2 + 1) % Line2Cycle.Count;
        }
        else
        {
            index2 = 0;
        }
    }

    private void UpdatePresence()
    {
        if (client == null || !client.IsInitialized) return;

        if (Line1Cycle == null || Line1Cycle.Count == 0 || index1 >= Line1Cycle.Count)
            index1 = 0;
        if (Line2Cycle == null || Line2Cycle.Count == 0 || index2 >= Line2Cycle.Count)
            index2 = 0;

        string defaultLine1 = Translate("Made By Tproplay");
        string defaultLine2 = Translate($"Pvz Fusion v{Application.version}");

        string currentLine1 = Line1Cycle.Count > 0 ? Line1Cycle[index1] : defaultLine1;
        string currentLine2 = Line2Cycle.Count > 0 ? Line2Cycle[index2] : defaultLine2;

        client.SetPresence(new RichPresence()
        {
            Details = currentLine1,
            State = currentLine2,
            Timestamps = elapsedTimer
        });

        TotalPresenceUpdates++;
    }
}
#endif