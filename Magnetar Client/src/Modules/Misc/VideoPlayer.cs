using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Magnetar_Client.Utils;
using Magnetar_Client.Game;
using static Magnetar_Client.Utils.Magnetar_Logger;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
#if ANDROID
    public class VideoPlayer : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Video Player";
        public override string Description { get; set; } = "Renders a video using plants on the lawn\n" +
            "Plant Anywhere is recommended to be turned On.";
        public override string SearchHints { get; set; } = "videoplayer playvideo plantvideo plantrender lawnvideo " +
            "videorenderer plantdisplay videoplayer lawnplayer screenplay videoplayer mod plantscreen videoprojection" +
            " videoplayerscreen videoonlawn lawnmovie plantmovie playermod visualvideo videoprojector plantframe" +
            " animateplants videoplayback bad apple";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod Data

        public static VideoPlayer instance;

        public MultiSelectSetting Color0Plant;
        public MultiSelectSetting Color1Plant;
        public MultiSelectSetting Color2Plant;
        public MultiSelectSetting Color3Plant;
        public MultiSelectSetting Color4Plant;
        public IntSetting FpsSetting;
        public IntSetting ColumnSetting;
        public IntSetting RowSetting;
        public BoolSetting PuffMode;

        public FloatSetting OffsetX;
        public FloatSetting OffsetY;
        public FloatSetting StartX;
        public FloatSetting StartY;

        private List<List<List<int>>> frames;
        private int currentFrame = 0;
        private float timer = 0f;

        private int[,] gridState;
        private Plant[,] color0Grid;
        private Plant[,] color1Grid;
        private Plant[,] color2Grid;
        private Plant[,] color3Grid;
        private Plant[,] color4Grid;
        public VideoPlayer()
        {
            instance = this;

            FpsSetting = new IntSetting("Frame Rate", 1, 60, 30);

            Settings.Add(FpsSetting);

            ColumnSetting = new IntSetting("Columns", 1, 96, 24);
            Settings.Add(ColumnSetting);

            RowSetting = new IntSetting("Rows", 1, 96, 12);
            Settings.Add(RowSetting);

            #region Colors
            var plantNameOverriden = Translator.TranslateEnum(typeof(PlantType));
            foreach (var plant in plantNameOverriden)
            {
                plantNameOverriden[plant.Key] = $"{plant.Value} ({plant.Key})";
            }

            var BannedPlants = new HashSet<int> { (int)PlantType.Nothing, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 246, 247 };

            Color0Plant = new MultiSelectSetting("Color 0", typeof(PlantType))
            {
                MaxSelection = 1,
                Blacklist = BannedPlants,
                CustomNames = plantNameOverriden
            };

            Color1Plant = new MultiSelectSetting("Color 1", typeof(PlantType))
            {
                MaxSelection = 1,
                Blacklist = BannedPlants,
                CustomNames = plantNameOverriden
            };

            Color2Plant = new MultiSelectSetting("Color 2", typeof(PlantType))
            {
                MaxSelection = 1,
                Blacklist = BannedPlants,
                CustomNames = plantNameOverriden
            };

            Color3Plant = new MultiSelectSetting("Color 3", typeof(PlantType))
            {
                MaxSelection = 1,
                Blacklist = BannedPlants,
                CustomNames = plantNameOverriden
            };

            Color4Plant = new MultiSelectSetting("Color 4", typeof(PlantType))
            {
                MaxSelection = 1,
                Blacklist = BannedPlants,
                CustomNames = plantNameOverriden
            };
            Settings.Add(Color0Plant);
            Settings.Add(Color1Plant);
            Settings.Add(Color2Plant);
            Settings.Add(Color3Plant);
            Settings.Add(Color4Plant);

            #endregion

            PuffMode = new BoolSetting("Puff Mode", false);
            Settings.Add(PuffMode);

            OffsetX = new FloatSetting("OffsetX", 0f, 2f, 0.5f, 2);
            Settings.Add(OffsetX);

            OffsetY = new FloatSetting("OffsetY", 0f, 2f, 0.5f, 2);
            Settings.Add(OffsetY);

            StartX = new FloatSetting("Start X", -100f, 100f, -5f, 2);
            Settings.Add(StartX);

            StartY = new FloatSetting("Start Y", -100f, 100f, 5f, 2);
            Settings.Add(StartY);
        }

        public override void OnEnable()
        {
            if (AppData.BoardInstanceIsNull)
            {
                DebugLogger.Warning("[Video Player] No Board Instance Found!");
                Active = false;
                return; 
            }

            string path = Path.Combine(Environment.CurrentDirectory, "Mods", "bad_apple_frames.json");

            if (!File.Exists(path))
            {
                DebugLogger.Msg("[Video Player] ERROR: bad_apple_frames.json not found in Mods folder!");
                Active = false;
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                frames = JsonConvert.DeserializeObject<List<List<List<int>>>>(json);
                DebugLogger.Msg($"[Video Player] Successfully loaded {frames.Count} frames!");
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"[Video Player] JSON Parsing Error: {ex.Message}");
                Active = false;
                return;
            }

            currentFrame = 0;
            timer = 0f;

            gridState = new int[ColumnSetting.Value, RowSetting.Value];
            color0Grid = new Plant[ColumnSetting.Value, RowSetting.Value];
            color1Grid = new Plant[ColumnSetting.Value, RowSetting.Value];
            color2Grid = new Plant[ColumnSetting.Value, RowSetting.Value];
            color3Grid = new Plant[ColumnSetting.Value, RowSetting.Value];
            color4Grid = new Plant[ColumnSetting.Value, RowSetting.Value];

            PlantType p0Type = (PlantType)Color0Plant.SelectedValues.First();
            PlantType p1Type = (PlantType)Color1Plant.SelectedValues.First();
            PlantType p2Type = (PlantType)Color1Plant.SelectedValues.First();
            PlantType p3Type = (PlantType)Color1Plant.SelectedValues.First();
            PlantType p4Type = (PlantType)Color1Plant.SelectedValues.First();

            for (int col = 0; col < ColumnSetting.Value; col++)
            {
                for (int row = 0; row < RowSetting.Value; row++)
                {
                    gridState[col, row] = -1;

                    try
                    {
                        Plant p0 = SpawnPlant(col, row, p0Type);
                        Plant p1 = SpawnPlant(col, row, p1Type);
                        Plant p2 = SpawnPlant(col, row, p2Type);
                        Plant p3 = SpawnPlant(col, row, p3Type);
                        Plant p4 = SpawnPlant(col, row, p4Type);

                        if (PuffMode.Value)
                        {
                            SetupVideoPixel(p0, col, row);
                            SetupVideoPixel(p1, col, row);
                            SetupVideoPixel(p2, col, row);
                            SetupVideoPixel(p3, col, row);
                            SetupVideoPixel(p4, col, row);
                        }

                        color0Grid[col, row] = p0;
                        color1Grid[col, row] = p1;
                        color2Grid[col, row] = p2;
                        color3Grid[col, row] = p3;
                        color4Grid[col, row] = p4;
                    }
                    catch (Exception e)
                    {
                        DebugLogger.Error($"[Video Player] Error Spawn: {e}");
                    }
                }
            }
        }

        public override void OnUpdateActive()
        {
            if (frames == null || frames.Count == 0) return;
            if (AppData.BoardInstanceIsNull)
            {
                DebugLogger.Warning("[Video Player] Board Not Found, quitting");
                OnDisable();
                Active = false;
                return;
            }
            timer += Time.deltaTime;
            float frameDuration = 1f / FpsSetting.Value;

            while (timer >= frameDuration)
            {
                timer -= frameDuration;
                RenderFrame(currentFrame);

                currentFrame++;
                if (currentFrame >= frames.Count) currentFrame = 0;
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

                    TogglePlantVisibility(color0Grid[col, row], pixel == 0);
                    TogglePlantVisibility(color1Grid[col, row], pixel == 1);
                    TogglePlantVisibility(color2Grid[col, row], pixel == 2);
                    TogglePlantVisibility(color3Grid[col, row], pixel == 3);
                    TogglePlantVisibility(color4Grid[col, row], pixel == 4);

                    gridState[col, row] = pixel;
                }
            }
        }

        private Plant SpawnPlant(int col, int row, PlantType type)
        {
            if (PuffMode.Value)
                return CreatePlant.Instance.SetPlant(col / 4, row / 4, type, puffV: new Vector2(col * OffsetX.Value, -row * OffsetY.Value));
            else
                return CreatePlant.Instance.SetPlant(col, row, type);
        }

        private void SetupVideoPixel(Plant plant, int col, int row)
        {
            if (plant == null) return;

            float exactX = StartX.Value + (col * OffsetX.Value);
            float exactY = StartY.Value - (row * OffsetY.Value);
            float exactZ = plant.transform.position.z;

            plant.transform.position = new Vector3(exactX, exactY, exactZ);

            TogglePlantVisibility(plant, false);
        }

        private void TogglePlantVisibility(Plant plant, bool isVisible)
        {
            if (plant == null) return;

            var renderers = plant.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.enabled = isVisible;
            }
        }

        public override void OnDisable()
        {
            for (int col = 0; col < ColumnSetting.Value; col++)
            {
                for (int row = 0; row < RowSetting.Value; row++)
                {
                    color0Grid[col, row]?.Die();
                    color1Grid[col, row]?.Die();
                    color2Grid[col, row]?.Die();
                    color3Grid[col, row]?.Die();
                    color4Grid[col, row]?.Die();
                }
            }
            frames = null;
            color0Grid = null;
            color1Grid = null;
            color2Grid = null;
            color3Grid = null;
            color4Grid = null;
            gridState = null;
        }
    }
#endif
}