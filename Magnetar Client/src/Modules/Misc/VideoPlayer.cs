using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Magnetar_Client.Api;
using Magnetar_Client.Game;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using Newtonsoft.Json;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class VideoPlayer : Module
{
    public override string Name { get; set; } = "Video Player";
    public override string Description { get; set; } = "Renders a video using plants on the lawn\n" +
        "Plant Anywhere is recommended to be turned On.";
    public override string SearchHints { get; set; } = "videoplayer playvideo plantvideo plantrender lawnvideo " +
        "videorenderer plantdisplay videoplayer lawnplayer screenplay videoplayer mod plantscreen videoprojection" +
        " videoplayerscreen videoonlawn lawnmovie plantmovie playermod visualvideo videoprojector plantframe" +
        " animateplants videoplayback bad apple";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

    public static VideoPlayer instance;

    // File Selection Setting
    public MultiSelectSetting VideoFileSetting;

    // Playback Settings
    public IntSetting FpsSetting;
    public IntSetting ColumnSetting;
    public IntSetting RowSetting;
    public BoolSetting PuffMode;

    // Color Configuration
    public const int AbsoluteMaxColors = 20;
    public IntSetting MaxColorsSetting;
    public SectionSetting ColorSection;

    // Offset Settings
    public Vector2Setting Offset;
    public Vector2Setting StartPos;

    private List<List<List<int>>> frames;
    private int currentFrame = 0;
    private float timer = 0f;

    private int[,] gridState;
    // Indexed by [colorIndex][column, row]
    private Plant[][,] colorGrids;

    private readonly Dictionary<int, string> _discoveredFiles = new();

    public VideoPlayer()
    {
        instance = this;

        CreateCategory("Video Source");
        ScanVideoFiles();
        VideoFileSetting = new MultiSelectSetting("Select Video")
        {
            Options = _discoveredFiles,
            MaxSelection = 1,
            DisplayAlphabetically = true
        };

        if (_discoveredFiles.Count > 0)
        {
            VideoFileSetting.Select(_discoveredFiles.Keys.First());
        }

        AddSettings(VideoFileSetting);
        EndCategory();

        CreateCategory("Playback");
        FpsSetting = new IntSetting("Frame Rate", 1, 60, 30);
        AddSettings(FpsSetting);

        ColumnSetting = new IntSetting("Columns", 1, 96, 24);
        AddSettings(ColumnSetting);

        RowSetting = new IntSetting("Rows", 1, 96, 12);
        AddSettings(RowSetting);

        PuffMode = new BoolSetting("Puff Mode", false);
        AddSettings(PuffMode);
        EndCategory();

        CreateCategory("Colors & Entities");
        MaxColorsSetting = new IntSetting("Max Colors", 1, AbsoluteMaxColors, 5);
        AddSettings(MaxColorsSetting);

        ColorSection = new SectionSetting("Colors", CreateColorSectionTemplate, defaultSections: 5, maxSections: MaxColorsSetting.Value);

        ColorSection.OnSectionsChanged = () =>
        {
            for (int i = 0; i < ColorSection.Sections.Count; i++)
            {
                ColorSection.Sections[i].Title = $"Color #{i}";
            }
        };
        ColorSection.OnSectionsChanged.Invoke();

        MaxColorsSetting.OnValueChanged = newMax =>
        {
            ColorSection.MaxSections = newMax;
            while (ColorSection.Sections.Count > newMax)
            {
                ColorSection.RemoveSection(ColorSection.Sections.Count - 1);
            }
        };

        AddSettings(ColorSection);
        EndCategory();

        CreateCategory("Placement & Offsets");
        Offset = new Vector2Setting("Offset", new Vector2(0,0));

        StartPos = new Vector2Setting("Start X", new Vector2(0,0));

        AddSettings(Offset, StartPos);

        EndCategory();
    }

    private List<Setting> CreateColorSectionTemplate(int index)
    {
        var bannedPlants = new HashSet<int>
        {
            (int)PlantType.Nothing,
            257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 246, 247
        };

        var plantSetting = new MultiSelectSetting("Plant", typeof(PlantType))
        {
            MaxSelection = 1,
            Blacklist = bannedPlants,
            CustomNames = GetFormattedPlantNames()
        };

        return new List<Setting> { plantSetting };
    }

    private void ScanVideoFiles()
    {
        _discoveredFiles.Clear();
        string videoDir = Path.Combine(PathsManager.DataDir, "Video Player");

        if (!Directory.Exists(videoDir))
        {
            Directory.CreateDirectory(videoDir);
        }

        string[] files = Directory.GetFiles(videoDir, "*.json", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
            _discoveredFiles[i] = Path.GetFileName(files[i]);
        }
    }

    private Dictionary<int, string> GetFormattedPlantNames()
    {
        var translated = Translator.TranslateEnum(typeof(PlantType));
        var formatted = new Dictionary<int, string>();
        foreach (var kvp in translated)
        {
            formatted[kvp.Key] = $"{kvp.Value} ({kvp.Key})";
        }
        return formatted;
    }

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        var names = GetFormattedPlantNames();
        if (ColorSection != null)
        {
            foreach (var section in ColorSection.Sections)
            {
                var plantSetting = section.Find<MultiSelectSetting>("Plant");
                if (plantSetting != null)
                {
                    plantSetting.CustomNames = names;
                }
            }
        }
    }

    public override void OnEnable()
    {
        if (AppData.BoardInstanceIsNull)
        {
            DebugLogger.Warning("[Video Player] No Board Instance Found!");
            Active = false;
            return;
        }

        ScanVideoFiles();
        if (_discoveredFiles.Count == 0 || VideoFileSetting.SelectedValues.Count == 0)
        {
            DebugLogger.Warning("[Video Player] No video .json files found in Data/Video Player or none selected!");
            Active = false;
            return;
        }

        int selectedId = VideoFileSetting.SelectedValues.First();
        if (!_discoveredFiles.TryGetValue(selectedId, out string selectedFileName))
        {
            DebugLogger.Warning("[Video Player] Selected video file index is invalid!");
            Active = false;
            return;
        }

        string filePath = Path.Combine(PathsManager.DataDir, "Video Player", selectedFileName);
        if (!File.Exists(filePath))
        {
            DebugLogger.Warning($"[Video Player] File not found: '{filePath}'");
            Active = false;
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            frames = JsonConvert.DeserializeObject<List<List<List<int>>>>(json);
            DebugLogger.Msg($"[Video Player] Successfully loaded {frames.Count} frames from '{selectedFileName}'");
        }
        catch (Exception ex)
        {
            DebugLogger.Warning($"[Video Player] JSON Parsing Error: {ex.Message}");
            Active = false;
            return;
        }

        if (frames == null || frames.Count == 0)
        {
            DebugLogger.Warning("[Video Player] Video file has no frame data!");
            Active = false;
            return;
        }

        // Collect all distinct color channel indices used in the video
        HashSet<int> uniqueColorsInFile = new();
        foreach (var frame in frames)
        {
            foreach (var row in frame)
            {
                foreach (int pixel in row)
                {
                    uniqueColorsInFile.Add(pixel);
                }
            }
        }

        int maxColorIndex = uniqueColorsInFile.Count > 0 ? uniqueColorsInFile.Max() : 0;
        int activeMaxAllowed = MaxColorsSetting.Value;

        // Ensure file does not require channels beyond the configured Max Colors
        if (maxColorIndex >= activeMaxAllowed)
        {
            DebugLogger.Warning($"[Video Player] File '{selectedFileName}' uses color channel {maxColorIndex}, " +
                               $"which exceeds the Max Colors setting ({activeMaxAllowed})! Increase 'Max Colors' first.");
            Active = false;
            return;
        }

        // Verify every required color channel exists as a section and has a plant selected
        List<string> missingSectionErrors = new();
        List<string> unassignedColorErrors = new();
        Dictionary<int, PlantType> resolvedTypes = new();

        foreach (int colorIdx in uniqueColorsInFile)
        {
            if (colorIdx >= ColorSection.Sections.Count)
            {
                missingSectionErrors.Add($"Color #{colorIdx}");
                continue;
            }

            var section = ColorSection.Sections[colorIdx];
            var plantSetting = section.Find<MultiSelectSetting>("Plant");

            if (plantSetting == null || plantSetting.SelectedValues.Count == 0)
            {
                unassignedColorErrors.Add($"Color #{colorIdx}");
            }
            else
            {
                resolvedTypes[colorIdx] = (PlantType)plantSetting.SelectedValues.First();
            }
        }

        if (missingSectionErrors.Count > 0)
        {
            DebugLogger.Warning($"[Video Player] Missing sections for required channels: {string.Join(", ", missingSectionErrors)}. " +
                               $"Please add more sections to the Colors setting.");
            Active = false;
            return;
        }

        if (unassignedColorErrors.Count > 0)
        {
            DebugLogger.Warning($"[Video Player] Cannot start animation! The following color channels have no plant selected: " +
                               $"{string.Join(", ", unassignedColorErrors)}.");
            Active = false;
            return;
        }

        currentFrame = 0;
        timer = 0f;

        int cols = ColumnSetting.Value;
        int rows = RowSetting.Value;

        gridState = new int[cols, rows];

        // Allocate jagged array sized to highest color index + 1
        colorGrids = new Plant[maxColorIndex + 1][,];
        foreach (int colorIdx in uniqueColorsInFile)
        {
            colorGrids[colorIdx] = new Plant[cols, rows];
        }

        // Spawn pixel grids
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                gridState[col, row] = -1;

                foreach (int colorIdx in uniqueColorsInFile)
                {
                    try
                    {
                        Plant p = SpawnPlant(col, row, resolvedTypes[colorIdx]);
                        if (PuffMode.Value)
                        {
                            SetupVideoPixel(p, col, row);
                        }
                        colorGrids[colorIdx][col, row] = p;
                    }
                    catch (Exception e)
                    {
                        DebugLogger.Error($"[Video Player] Error spawning Color {colorIdx} at ({col},{row}): {e.Message}");
                    }
                }
            }
        }
    }

    public override void OnUpdateActive()
    {
        if (frames == null || frames.Count == 0) return;

        if (AppData.BoardInstanceIsNull)
        {
            DebugLogger.Warning("[Video Player] Board Not Found, stopping playback.");
            OnDisable();
            Active = false;
            return;
        }

        timer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1, FpsSetting.Value);

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
        int maxCol = ColumnSetting.Value;
        int maxRow = RowSetting.Value;

        for (int row = 0; row < frameData.Count; row++)
        {
            if (row >= maxRow) break;

            for (int col = 0; col < frameData[row].Count; col++)
            {
                if (col >= maxCol) break;

                int pixel = frameData[row][col];
                if (gridState[col, row] == pixel) continue;

                for (int c = 0; c < colorGrids.Length; c++)
                {
                    if (colorGrids[c] != null)
                    {
                        TogglePlantVisibility(colorGrids[c][col, row], pixel == c);
                    }
                }

                gridState[col, row] = pixel;
            }
        }
    }

    private Plant SpawnPlant(int col, int row, PlantType type)
    {
        if (CreatePlant.Instance == null) return null;

        if (PuffMode.Value)
            return CreatePlant.Instance.SetPlant(col / 4, row / 4, type, puffV: new Vector2(col * Offset.Value.x, -row * Offset.Value.y));
        else
            return CreatePlant.Instance.SetPlant(col, row, type);
    }

    private void SetupVideoPixel(Plant plant, int col, int row)
    {
        if (plant == null) return;

        float exactX = StartPos.Value.x + (col * Offset.Value.x);
        float exactY = StartPos.Value.y - (row * Offset.Value.y);
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
        if (colorGrids != null)
        {
            for (int c = 0; c < colorGrids.Length; c++)
            {
                if (colorGrids[c] == null) continue;

                int cols = colorGrids[c].GetLength(0);
                int rows = colorGrids[c].GetLength(1);

                for (int col = 0; col < cols; col++)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        try { colorGrids[c][col, row]?.Die(); } catch { }
                    }
                }
            }
        }

        frames = null;
        colorGrids = null;
        gridState = null;
    }
}