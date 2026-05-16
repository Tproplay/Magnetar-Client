using DiscordRPC;
using Il2Cpp;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class DiscordRPC : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Discord RPC";
        public override string Description { get; set; } = "Shows rolling status updates on Discord.";
        public override string SearchHints { get; set; } = "discordrpc discordrichpresence discordpresence discordactivity " +
            "discordstatus discordintegration rpcstatus richpresence discordconnect discordlink discordinfo discorddisplay " +
            "rpcpresence discordrp discordstat discordlive discordsync discordgame discordapi discrodrpc discordrcp discordrps " +
            "discordrich discordpresance discordpresense discordconection discordintigration rpcbot rpcclient rpcactive";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data

        public static DiscordRPC instance;

        public FloatSetting SwitchSpeed = new FloatSetting("Switch Speed (s)", 1f, 30f, 5f);

        public List<string> Line1Cycle = new List<string> ();
        public List<string> Line2Cycle = new List<string> ();

        private DiscordRpcClient client;
        private Timestamps elapsedTimer;

        private float rotationTimer = 0f;
        private int index1 = 0;
        private int index2 = 0;

        public DiscordRPC()
        {
            instance = this;

            AddSettings(SwitchSpeed);
        }

        // Mod Logic
        public override void OnEnable()
        {
            elapsedTimer = Timestamps.Now;
            client = new DiscordRpcClient("1500852523764813928");
            client.Initialize();
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

        private float dataRefreshTimer = 0f;

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

            

            switch (GameAPP.theGameStatus)
            {
                case (GameStatus.OutGame):
                case (GameStatus.InGame):
                case (GameStatus.Selecting):
                case (GameStatus.InInterlude):
                    {
                        // Not In a Level

                        if (BoardInstanceIsNull)
                        {
                            Line1Cycle.Add("Looking at the Main Menu");
                            Line1Cycle.Add("Selecting a level to play");
                            return;
                        }


                        // In a Level

                        GameStatus status = GameAPP.theGameStatus;

                        // Display the Level Name
                        string levelName = GetLevelName();
                        
                        if (levelName.Length > 18) Line1Cycle.Add(levelName);
                        else Line1Cycle.Add($"Playing: {Translator.Translate(levelName)}");

                        // Selecting Seeds
                        if (status == GameStatus.Selecting)
                        {
                            Line2Cycle.Add("Selecting Seeds");
                            
                            break;
                        }

                        // In a transition (e.g, level start -> seed selection phase)
                        if (status == GameStatus.InInterlude)
                        {
                            Line2Cycle.Add("Started the level"); break;
                        }

                        // In the game
                        Line2Cycle.Add($"Sun : {board.theSun}");

                        if (board.boardTag.isEndless)
                            Line2Cycle.Add($"Round : {board.theCurrentSurvivalRound}");

                        Line2Cycle.Add($"Wave: {board.theWave}/{board.theMaxWave}");
                        Line2Cycle.Add($"Plants:{plantList.Count} | Zombies:{zombieList.Count}");
                        break;
                    
                        
                    }

                case (GameStatus.BigGarden):
                    {
                        if (BoardInstanceIsNull)
                        {
                            Line1Cycle.Add("Looking at the Main Menu");
                            Line1Cycle.Add("Selecting a level to play");
                            return;
                        }

                        Line1Cycle.Add("Roaming in BigGarden");
                        Line1Cycle.Add("Growing Plants");
                        
                        break;
                    }
                
            }
        }

        private void RotateIndices()
        {
            // Increment and wrap around using the Modulo (%) operator
            if (Line1Cycle.Count > 0) index1 = (index1 + 1) % Line1Cycle.Count;
            if (Line2Cycle.Count > 0) index2 = (index2 + 1) % Line2Cycle.Count;
        }

        private void UpdatePresence()
        {
            if (client == null || !client.IsInitialized) return;

            // Fetch current items based on the indices
            string currentLine1 = Line1Cycle.Count > 0 ? Line1Cycle[index1] : "Made By Tproplay";
            string currentLine2 = Line2Cycle.Count > 0 ? Line2Cycle[index2] : $"Pvz Fusion v{Application.version}";

            client.SetPresence(new RichPresence()
            {
                Details = currentLine1, // Line 1
                State = currentLine2,   // Line 2
                Timestamps = elapsedTimer
            });
        }
    }
}
