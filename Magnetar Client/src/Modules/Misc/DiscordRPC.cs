using DiscordRPC;
using Il2Cpp;
using Magnetar_Client.Utils;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.Modules
{
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

        // Mod Data
        public static DiscordRPC instance;

        public FloatSetting SwitchSpeed;

        #region Lines

        public StringSetting InGame_Line1_1;
        public StringSetting InGame_Line1_2;
        public StringSetting InGame_Line1_3;
        public StringSetting InGame_Line1_4;

        public StringSetting InGame_Line2_1;
        public StringSetting InGame_Line2_2;
        public StringSetting InGame_Line2_3;
        public StringSetting InGame_Line2_4;

        public StringSetting Menu_Line1_1;
        public StringSetting Menu_Line1_2;
        public StringSetting Menu_Line1_3;
        public StringSetting Menu_Line1_4;

        public StringSetting Menu_Line2_1;
        public StringSetting Menu_Line2_2;
        public StringSetting Menu_Line2_3;
        public StringSetting Menu_Line2_4;

        #endregion

        public enum Status
        {
            InGame, Menu, Transition
        }

        public static Status status = Status.Menu;

        public List<string> Line1Cycle = new List<string>();
        public List<string> Line2Cycle = new List<string>();

        private DiscordRpcClient client;
        private Timestamps elapsedTimer;

        private float rotationTimer = 0f;
        private int index1 = 0;
        private int index2 = 0;
        private float dataRefreshTimer = 0f;

        public DiscordRPC()
        {
            instance = this;

            SwitchSpeed = new FloatSetting("Switch Speed (s)", 1f, 30f, 5f);
            AddSettings(SwitchSpeed);

            CreateCategory("In Game",true);
            InGame_Line1_1 = new StringSetting("Line1 Message 1", "Magnetar Client v{Magnetar_Version}", In_Game_AutoCompleteArgs);
            InGame_Line1_2 = new StringSetting("Line1 Message 2", "Playing: {Level_Name}", In_Game_AutoCompleteArgs);
            InGame_Line1_3 = new StringSetting("Line1 Message 3", "", In_Game_AutoCompleteArgs);
            InGame_Line1_4 = new StringSetting("Line1 Message 4", "", In_Game_AutoCompleteArgs);

            InGame_Line2_1 = new StringSetting("Line2 Message 1", "Sun: {Sun} | Money: {Money}", In_Game_AutoCompleteArgs);
            InGame_Line2_2 = new StringSetting("Line2 Message 2", "Wave: {Current_Wave}/{Max_Wave}", In_Game_AutoCompleteArgs);
            InGame_Line2_3 = new StringSetting("Line2 Message 3", "", In_Game_AutoCompleteArgs);
            InGame_Line2_4 = new StringSetting("Line2 Message 4", "", In_Game_AutoCompleteArgs);

            AddSettings(InGame_Line1_1, InGame_Line1_2, InGame_Line1_3, InGame_Line1_4);
            AddSettings(InGame_Line2_1, InGame_Line2_2, InGame_Line2_3, InGame_Line2_4);
            EndCategory();
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

            else if ((Gamestatus == GameStatus.InGame) || (Gamestatus == GameStatus.OutGame) && BoardInstanceIsNull)
            {
                status = Status.Menu;
            }

            else if (Gamestatus == GameStatus.InInterlude)
            {
                status = Status.Transition;
            }

            else if (Gamestatus == GameStatus.Selecting)
            {
                Line1Cycle.Add("Picking Seeds");
            }

            switch (status)
            {
                case Status.InGame:
                    {
                        if (!string.IsNullOrWhiteSpace(InGame_Line1_1.Value)) Line1Cycle.Add(In_Game_FormatString(InGame_Line1_1.Value));
                        if (!string.IsNullOrWhiteSpace(InGame_Line1_2.Value)) Line1Cycle.Add(In_Game_FormatString(InGame_Line1_2.Value));
                        if (!string.IsNullOrWhiteSpace(InGame_Line1_3.Value)) Line1Cycle.Add(In_Game_FormatString(InGame_Line1_3.Value));
                        if (!string.IsNullOrWhiteSpace(InGame_Line1_4.Value)) Line1Cycle.Add(In_Game_FormatString(InGame_Line1_4.Value));

                        if (!string.IsNullOrWhiteSpace(InGame_Line2_1.Value)) Line2Cycle.Add(In_Game_FormatString(InGame_Line2_1.Value));
                        if (!string.IsNullOrWhiteSpace(InGame_Line2_2.Value)) Line2Cycle.Add(In_Game_FormatString(InGame_Line2_2.Value));
                        if (!string.IsNullOrWhiteSpace(InGame_Line2_3.Value)) Line2Cycle.Add(In_Game_FormatString(InGame_Line2_3.Value));
                        if (!string.IsNullOrWhiteSpace(InGame_Line2_4.Value)) Line2Cycle.Add(In_Game_FormatString(InGame_Line2_4.Value));
                        break;
                    }
            }


            UpdatePresence();
        }

        public static List<string> In_Game_AutoCompleteArgs = new List<string>
        {
            "Magnetar_Version","Level_Name","Sun","Money","Current_Wave","Max_Wave"
        };

        private string In_Game_FormatString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            string result = input;

            result = result.Replace("{Magnetar_Version}", 
                System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttribute<MelonInfoAttribute>().Version);
            result = result.Replace("{Level_Name}", GetLevelName());
            result = result.Replace("{Sun}", FormatInternational(board.theSun));
            result = result.Replace("{Money}", FormatInternational(board.theMoney));
            result = result.Replace("{Current_Wave}", board.theWave.ToString());
            result = result.Replace("{Max_Wave}", board.theMaxWave.ToString());
            return result;
        }

        private void RotateIndices()
        {
            if (Line1Cycle.Count > 0) index1 = (index1 + 1) % Line1Cycle.Count;
            if (Line2Cycle.Count > 0) index2 = (index2 + 1) % Line2Cycle.Count;
        }

        private void UpdatePresence()
        {
            if (client == null || !client.IsInitialized) return;

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
}