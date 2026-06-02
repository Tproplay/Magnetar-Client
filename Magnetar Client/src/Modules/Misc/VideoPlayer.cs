using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Il2Cpp;
using Newtonsoft.Json;
using System.Linq;
using Magnetar_Client.Utils;
using Magnetar_Client.Game;

namespace Magnetar_Client.Modules
{
    // Currently Not for Public Use
#if ANDROID
    public class VideoPlayer : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Video Player";
        public override string Description { get; set; } = "Renders a video using plants on the lawn";
        public override string SearchHints { get; set; } = "";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;


        // Mod Data

        public static VideoPlayer instance;

        public MultiSelectSetting Color0Plant;
        public MultiSelectSetting Color1Plant;

        public IntSetting FpsSetting;

        private List<List<List<int>>> frames; // List of every 2D frame
        private int currentFrame = 0;
        private float timer = 0f;

        public IntSetting ColumnSetting;
        public IntSetting RowSetting;

        private int[,] gridState;
        private Plant[,] gridPlants;

        public VideoPlayer() 
        { 
            instance = this;

            ColumnSetting = new IntSetting("Columns", 1, 96, 6);

            Settings.Add(ColumnSetting);

            RowSetting = new IntSetting("Rows", 1, 96, 6);

            var plantNameOverriden = Translator.TranslateEnum(typeof(PlantType));

            foreach (var plant in plantNameOverriden)
            {
                plantNameOverriden[plant.Key] = $"{plant.Value} ({plant.Key})";
            }

            Color0Plant = new MultiSelectSetting("Color 0", typeof(PlantType))
            {
                MaxSelection = 1,
                SelectedValues = new HashSet<int>
                {
                    (int)PlantType.DoomFume
                },
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = plantNameOverriden
            };

            Color1Plant = new MultiSelectSetting("Color 1", typeof(PlantType))
            {
                MaxSelection = 1,
                SelectedValues = new HashSet<int>
                {
                    (int)PlantType.GarlicUmbrella
                },
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = plantNameOverriden
            };

            FpsSetting = new IntSetting("Frame Rate", 1, 60, 30);

            Settings.Add(Color0Plant);
            Settings.Add(Color1Plant);
            Settings.Add(FpsSetting);

        }

        public override void OnEnable()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "Mods", "bad_apple_frames.json");

            if (!File.Exists(path))
            {
                Magnetar_Logger.DebugLogger.Msg("[Bad Apple] ERROR: bad_apple_frames.json not found in Mods folder!");
                Active = false;
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                frames = JsonConvert.DeserializeObject<List<List<List<int>>>>(json);
                Magnetar_Logger.DebugLogger.Msg($"[Bad Apple] Successfully loaded {frames.Count} frames!");
            }
            catch (Exception ex)
            {
                Magnetar_Logger.DebugLogger.Msg($"[Bad Apple] JSON Parsing Error: {ex.Message}");
                Active = false;
                return;
            }

            currentFrame = 0;
            timer = 0f;

            gridState = new int[RowSetting.Value, ColumnSetting.Value];
            gridPlants = new Plant[RowSetting.Value, ColumnSetting.Value];

            for (int col = 0; col < ColumnSetting.Value; col++)
            {
                for (int row = 0; row < RowSetting.Value; row++)
                {
                    gridState[col, row] = -1;
                    gridPlants[col, row] = null;
                }
            }
        }

        public override void OnUpdateActive()
        {
            if (frames == null || frames.Count == 0) return;
            if (AppData.BoardInstanceIsNull) return;

            timer += Time.deltaTime;
            float frameDuration = 1f / FpsSetting.Value;

            while (timer >= frameDuration)
            {
                timer -= frameDuration;
                RenderFrame(currentFrame);

                currentFrame++;

                // Loop the video when it ends
                if (currentFrame >= frames.Count)
                {
                    currentFrame = 0;
                }
            }
        }

        private void RenderFrame(int frameIndex)
        {
            var frameData = frames[frameIndex];

            for (int row = 0; row < frameData.Count; row++)
            {
                for (int col = 0; col < frameData[row].Count; col++)
                {
                    if (col >= ColumnSetting.Value || row >= RowSetting.Value) continue;

                    int pixel = frameData[row][col];
                    if (gridState[col, row] == pixel) continue;
                    gridPlants[col, row]?.Die();
                    gridPlants[col, row] = null;

                    PlantType plant = (pixel == 0) ? (PlantType)Color0Plant.SelectedValues.First()
                        : (PlantType)Color1Plant.SelectedValues.First();

                    try 
                    {
                        gridPlants[col, row] = CreatePlant.Instance.SetPlant(col, row, plant);
                    }

                    catch (Exception e)
                    {
                        MelonLoader.MelonLogger.Error($"[Bad Apple] Error Occured: {e}");
                    }

                    gridState[col, row] = pixel;
                }
            }
        }

        public override void OnDisable()
        {
            for (int col = 0; col < ColumnSetting.Value; col++)
            {
                for (int row = 0; row < RowSetting.Value; row++)
                {
                    gridPlants[col, row]?.Die();
                    gridState[col, row] = -1;
                    gridPlants[col, row] = null;
                }
            }
            frames = null;
        }

        
    }
#endif
}