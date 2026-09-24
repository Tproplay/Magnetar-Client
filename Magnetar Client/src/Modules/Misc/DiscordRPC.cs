#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
using Magnetar_Client.UI.Setting;
using DiscordRPC;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Utils.Maths;
using Magnetar_Client.Core;
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
    public override bool enableInVanillaMode { get; set; } = true;

    // Mod Data
    public static DiscordRPC instance;

    public FloatSetting SwitchSpeed;

    #region Lines

    public ListStringSetting InGameLine1;
    public ListStringSetting InGameLine2;

    public SelectSetting InGame_Randomizer_mode;

    #endregion

    public enum Status
    {
        InGame, Menu, Transition, Selecting, Big_Garden,
        Magnetar_GUI, InAlamanc
    }

    public static Status status = Status.Menu;

    public List<string> Line1Cycle = new();
    public List<string> Line2Cycle = new();

    private DiscordRpcClient client;
    private Timestamps elapsedTimer;

    private float rotationTimer = 0f;
    private int index1 = 0;
    private int index2 = 0;
    private float dataRefreshTimer = 0f;

    public DiscordRPC()
    {
        instance = this;

        CreateCategory("General");

        SwitchSpeed = new FloatSetting("Switch Speed (s)", 2f, 30f, 5f, 3, 0);
        AddSettings(SwitchSpeed);

        EndCategory();

        CreateCategory("In Game",true);

        InGameLine1 = new ListStringSetting("Line 1",
            new List<string>
            {
                "Magnetar Client v{Magnetar_Version}",
                "Playing: {Level_Name}",
            },
            15, In_Game_AutoCompleteArgs
            );

        InGameLine2 = new ListStringSetting("Line 2",
            new List<string>
            {
                "Sun: {Sun} | Money: {Money}",
                "Wave: {Current_Wave}/{Max_Wave}",
                "Plants: {number_of_plants} | Zombies: { number_of_zombies }",
            },
            15, In_Game_AutoCompleteArgs
            );

        AddSettings(InGameLine1, InGameLine2);

        InGame_Randomizer_mode = new SelectSetting("Iteration Mode", 0)
        {
            Options = new Dictionary<int, string>
            {
                { 0, "Sequential" },
                { 1, "Random" }
            }
        };

        AddSettings(InGame_Randomizer_mode);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        InGame_Randomizer_mode.CustomNames = InGame_Randomizer_mode.Options
            .ToDictionary(kvp => kvp.Key, kvp => Translator.Translate(kvp.Value));
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

        // Re-parse the live variables every 1 second
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

        GameStatus Gamestatus = GameAPP.theGameStatus;

        if (Gamestatus == GameStatus.InGame && !BoardInstanceIsNull) // In Game
        {
            status = Status.InGame; 
        }

        else if (InMainMenu && (Config.showgui || HUDManager.forceShow)) // Menu & Magnetar GUI
        {
            status = Status.Magnetar_GUI;
            Line1Cycle.Add(Translator.Translate("Browsing Magnetar's GUI"));
        }

        else if (InMainMenu) // Menu
        {
            status = Status.Menu;
            Line1Cycle.Add(Translator.Translate("Looking at the Main Menu"));
        }
        else if (InAlmanac)
        {
            status = Status.InAlamanc;
            Line1Cycle.Add(Translator.Translate("Checking out the almanac"));
        }

        else if (Gamestatus == GameStatus.InInterlude) // In Transition
        {
            status = Status.Transition;
            Line1Cycle.Add(Translator.Translate("Started a level"));
        }

        else if (Gamestatus == GameStatus.Selecting) // Picking seeds
        {
            status = Status.Selecting;
            Line1Cycle.Add(Translator.Translate("Picking Seeds"));
        }

        else if (Gamestatus == GameStatus.BigGarden)
        {
            status = Status.Big_Garden;
            Line1Cycle.Add(Translator.Translate("Roaming in the Garden"));
            Line2Cycle.Add(Translator.Translate("Growing Seeds"));
            Line2Cycle.Add(Translator.Translate("Waterning plants"));
        }

        switch (status)
        {
            case Status.InGame:
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
                    break;
                }
        }

    }

    public static List<string> In_Game_AutoCompleteArgs = new()
    {
        "Magnetar_Version","Game_Version",
        "Level_Name","Sun","Money","Current_Wave","Max_Wave",
        "number_of_plants","number_of_zombies",
        "movers_left"
    };

    private string In_Game_FormatString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        string result = input;

        result = result.Replace("{Magnetar_Version}",
            Magnetar_Info.Version);
        result = result.Replace("{Game_Version}", Application.version);
        result = result.Replace("{Level_Name}", GetLevelName());
        result = result.Replace("{Sun}", FormatInternational(BoardInstance.theSun));
        result = result.Replace("{Money}", FormatInternational(BoardInstance.theMoney));
        result = result.Replace("{Current_Wave}", BoardInstance.theWave.ToString());
        result = result.Replace("{Max_Wave}", BoardInstance.theMaxWave.ToString());
        result = result.Replace("{number_of_plants}", plantList.Count.ToString());
        result = result.Replace("{number_of_zombies}", zombieList.Count.ToString());
        result = result.Replace("{movers_left}", BoardInstance.mowerArray.Count.ToString());
        return result;
    }

    private static readonly System.Random _random = new();

    private void RotateIndices()
    {
        if (InGame_Randomizer_mode.Value == 0) // Sequential
        {
            // Line 1
            if (Line1Cycle != null && Line1Cycle.Count > 0)
                index1 = (index1 + 1) % Line1Cycle.Count;
            else index1 = 0;

            // Line 2
            if (Line2Cycle != null && Line2Cycle.Count > 0)
                index2 = (index2 + 1) % Line2Cycle.Count;
            else index2 = 0;
        }
        else if (InGame_Randomizer_mode.Value == 1) // Random
        {
            //Line 1
            if (Line1Cycle != null && Line1Cycle.Count > 0)
                index1 = _random.Next(Line1Cycle.Count);
            else index1 = 0;

            // Line 2
            if (Line2Cycle != null && Line2Cycle.Count > 0)
                index2 = _random.Next(Line2Cycle.Count);
            else index2 = 0;
        }
    }

    private void UpdatePresence()
    {
        if (client == null || !client.IsInitialized) return;

        if (Line1Cycle == null || Line1Cycle.Count == 0 || index1 >= Line1Cycle.Count)
            index1 = 0;
        if (Line2Cycle == null || Line2Cycle.Count == 0 || index2 >= Line2Cycle.Count)
            index2 = 0; 

        string currentLine1 = Line1Cycle.Count > 0 ? Line1Cycle[index1] : "Made By Tproplay";
        string currentLine2 = Line2Cycle.Count > 0 ? Line2Cycle[index2] : $"Pvz Fusion v{Application.version}";

        client.SetPresence(new RichPresence()
        {
            Details = currentLine1,
            State = currentLine2,
            Timestamps = elapsedTimer
        });
    }
}
#endif