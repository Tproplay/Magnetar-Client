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
        public override string Description { get; set; } = "Shows rolling status updates on Discord.";
        public override string SearchHints { get; set; } = "discordrpc discordrichpresence discordpresence discordactivity " +
            "discordstatus discordintegration rpcstatus richpresence discordconnect discordlink discordinfo discorddisplay " +
            "rpcpresence discordrp discordstat discordlive discordsync discordgame discordapi discrodrpc discordrcp discordrps " +
            "discordrich discordpresance discordpresense discordconection discordintigration rpcbot rpcclient rpcactive";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data
        public static DiscordRPC instance;

        public FloatSetting SwitchSpeed;

        public StringSetting Line1_1;
        public StringSetting Line1_2;
        public StringSetting Line1_3;
        public StringSetting Line1_4;

        public StringSetting Line2_1;
        public StringSetting Line2_2;
        public StringSetting Line2_3;
        public StringSetting Line2_4;

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
            Line1_1 = new StringSetting("Line1 Message 1", "Magnetar Client v{Magnetar_Version}", AutoCompleteArgs);
            Line1_2 = new StringSetting("Line1 Message 2", "Playing: {Level_Name}", AutoCompleteArgs);
            Line1_3 = new StringSetting("Line1 Message 3", "", AutoCompleteArgs);
            Line1_4 = new StringSetting("Line1 Message 4", "", AutoCompleteArgs);

            Line2_1 = new StringSetting("Line2 Message 1", "Sun: {Sun} | Money: {Money}", AutoCompleteArgs);
            Line2_2 = new StringSetting("Line2 Message 2", "Wave: {Current_Wave}/{Max_Wave}", AutoCompleteArgs);
            Line2_3 = new StringSetting("Line2 Message 3", "", AutoCompleteArgs);
            Line2_4 = new StringSetting("Line2 Message 4", "", AutoCompleteArgs);

            AddSettings(Line1_1, Line1_2, Line1_3, Line1_4);
            AddSettings(Line2_1, Line2_2, Line2_3, Line2_4);
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

            GameStatus status = GameAPP.theGameStatus;

            if (status == GameStatus.InGame && !BoardInstanceIsNull)
            {
                if (!string.IsNullOrWhiteSpace(Line1_1.Value)) Line1Cycle.Add(FormatString(Line1_1.Value));
                if (!string.IsNullOrWhiteSpace(Line1_2.Value)) Line1Cycle.Add(FormatString(Line1_2.Value));
                if (!string.IsNullOrWhiteSpace(Line1_3.Value)) Line1Cycle.Add(FormatString(Line1_1.Value));
                if (!string.IsNullOrWhiteSpace(Line1_4.Value)) Line1Cycle.Add(FormatString(Line1_2.Value));

                if (!string.IsNullOrWhiteSpace(Line2_1.Value)) Line2Cycle.Add(FormatString(Line2_1.Value));
                if (!string.IsNullOrWhiteSpace(Line2_2.Value)) Line2Cycle.Add(FormatString(Line2_2.Value));
                if (!string.IsNullOrWhiteSpace(Line2_3.Value)) Line2Cycle.Add(FormatString(Line2_3.Value));
                if (!string.IsNullOrWhiteSpace(Line2_4.Value)) Line2Cycle.Add(FormatString(Line2_4.Value));
            }

            else if ((status == GameStatus.InGame) || (status == GameStatus.OutGame) && BoardInstanceIsNull)
            {

            }

            else if ((status == GameStatus.InInterlude) && BoardInstanceIsNull)
            {

            }

            else if (status == GameStatus.Selecting)
            {
                Line1Cycle.Add("Picking Seeds");
            }

                UpdatePresence();
        }

        public static List<string> AutoCompleteArgs = new List<string>
        {
            "Magnetar_Version","Level_Name","Sun","Money","Current_Wave","Max_Wave"
        };

        private string FormatString(string input)
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