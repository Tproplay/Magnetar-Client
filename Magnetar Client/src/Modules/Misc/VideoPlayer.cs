#if false
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    // ---------------------------------------------------------------------
    // Async loading / incremental spawning state
    // ---------------------------------------------------------------------
    private enum LoadState { Idle, Loading, Spawning, Playing }

    /// <summary>Max milliseconds per game frame spent spawning plants during setup.</summary>
    private const float SpawnBudgetMs = 4f;

    private LoadState loadState = LoadState.Idle;
    private Task<LoadResult> loadTask;
    private CancellationTokenSource loadCts;

    // Settings snapshot taken when spawning starts, so mid-setup UI changes can't desync the grid
    private int spawnCols;
    private int spawnRows;
    private bool spawnPuff;
    private List<int> spawnColors;
    private Dictionary<int, PlantType> spawnTypes;
    private long spawnIndex;
    private string loadedFileName;

    /// <summary>Plain-data result produced on the worker thread (no Unity objects allowed in here).</summary>
    private sealed class LoadResult
    {
        public string FileName;
        public List<List<List<int>>> Frames;
        public List<int> Colors = new();   // distinct color channels, ascending
        public int MaxColorIndex;
        public string Error;
        public bool Cancelled;
    }

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
        Offset = new Vector2Setting("Offset", new Vector2(0, 0));

        StartPos = new Vector2Setting("Start X", new Vector2(0, 0));

        AddSettings(Offset, StartPos);

        EndCategory();
    }

    private List<Setting> CreateColorSectionTemplate(int index)
    {

        var plantSetting = new MultiSelectSetting("Plant", typeof(PlantType))
        {
            MaxSelection = 1,
            Blacklist = Banned.PlantTypeBanned,
            CustomNames = TranslateEnum<PlantType>(),
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

    public override void OnLanguageChanged()
    {
        if (ColorSection != null)
        {
            foreach (var section in ColorSection.Sections)
            {
                var plantSetting = section.Find<MultiSelectSetting>("Plant");
                if (plantSetting != null)
                {
                    plantSetting.CustomNames = TranslateEnum<PlantType>();
                }
            }
        }
    }

    public override void OnEnable()
    {
        // Make sure nothing from a previous run (or an unfinished load) is still alive
        Teardown();

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

        // Read + parse + scan on a worker thread. Only plain data crosses back to the main thread.
        loadedFileName = selectedFileName;
        loadCts = new CancellationTokenSource();
        CancellationToken token = loadCts.Token;
        loadState = LoadState.Loading;
        DebugLogger.Msg($"[Video Player] Loading '{selectedFileName}' in the background...");
        loadTask = Task.Run(() => LoadVideoFile(filePath, selectedFileName, token), token);
    }

    /// <summary>
    /// Runs on a worker thread. Must not touch Unity objects, settings, or the logger.
    /// </summary>
    private static LoadResult LoadVideoFile(string filePath, string fileName, CancellationToken token)
    {
        var result = new LoadResult { FileName = fileName };
        try
        {
            // Stream straight from disk instead of building one giant string first
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                result.Frames = new JsonSerializer().Deserialize<List<List<List<int>>>>(jsonReader);
            }

            token.ThrowIfCancellationRequested();

            if (result.Frames == null || result.Frames.Count == 0)
            {
                result.Error = "Video file has no frame data!";
                return result;
            }

            // Collect all distinct color channel indices used in the video
            var unique = new HashSet<int>();
            foreach (var frame in result.Frames)
            {
                token.ThrowIfCancellationRequested();
                if (frame == null) continue;
                foreach (var row in frame)
                {
                    if (row == null) continue;
                    foreach (int pixel in row)
                    {
                        unique.Add(pixel);
                    }
                }
            }

            if (unique.Count > 0 && unique.Min() < 0)
            {
                result.Error = $"File '{fileName}' contains a negative color value ({unique.Min()}), which is invalid.";
                return result;
            }

            result.Colors = unique.OrderBy(c => c).ToList();
            result.MaxColorIndex = result.Colors.Count > 0 ? result.Colors[result.Colors.Count - 1] : 0;
        }
        catch (OperationCanceledException)
        {
            result.Cancelled = true;
        }
        catch (Exception ex)
        {
            result.Error = $"JSON Parsing Error: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Main thread: checks whether the background load finished and, if so, validates it against the
    /// current settings and starts the incremental spawn phase.
    /// </summary>
    private void PollLoad()
    {
        if (loadTask == null || !loadTask.IsCompleted) return;

        Task<LoadResult> task = loadTask;
        loadTask = null;

        if (task.IsCanceled)
        {
            Teardown();
            return;
        }

        if (task.IsFaulted)
        {
            Fail($"Failed to load video: {task.Exception?.GetBaseException().Message}");
            return;
        }

        LoadResult result = task.Result;
        if (result.Cancelled)
        {
            Teardown();
            return;
        }

        if (result.Error != null)
        {
            Fail(result.Error);
            return;
        }

        BeginSpawn(result);
    }

    private void BeginSpawn(LoadResult result)
    {
        int maxColorIndex = result.MaxColorIndex;
        int activeMaxAllowed = MaxColorsSetting.Value;

        // Ensure file does not require channels beyond the configured Max Colors
        if (maxColorIndex >= activeMaxAllowed)
        {
            Fail($"File '{result.FileName}' uses color channel {maxColorIndex}, " +
                 $"which exceeds the Max Colors setting ({activeMaxAllowed})! Increase 'Max Colors' first.");
            return;
        }

        // Verify every required color channel exists as a section and has a plant selected
        List<string> missingSectionErrors = new();
        List<string> unassignedColorErrors = new();
        Dictionary<int, PlantType> resolvedTypes = new();

        foreach (int colorIdx in result.Colors)
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
            Fail($"Missing sections for required channels: {string.Join(", ", missingSectionErrors)}. " +
                 $"Please add more sections to the Colors setting.");
            return;
        }

        if (unassignedColorErrors.Count > 0)
        {
            Fail($"Cannot start animation! The following color channels have no plant selected: " +
                 $"{string.Join(", ", unassignedColorErrors)}.");
            return;
        }

        if (AppData.BoardInstanceIsNull)
        {
            Fail("Board went away while the video was loading.");
            return;
        }

        frames = result.Frames;
        DebugLogger.Msg($"[Video Player] Successfully loaded {frames.Count} frames from '{result.FileName}'");

        currentFrame = 0;
        timer = 0f;

        spawnCols = ColumnSetting.Value;
        spawnRows = RowSetting.Value;
        spawnPuff = PuffMode.Value;
        spawnColors = result.Colors;
        spawnTypes = resolvedTypes;
        spawnIndex = 0;

        gridState = new int[spawnCols, spawnRows];
        for (int col = 0; col < spawnCols; col++)
        {
            for (int row = 0; row < spawnRows; row++)
            {
                gridState[col, row] = -1;
            }
        }

        // Allocate jagged array sized to highest color index + 1
        colorGrids = new Plant[maxColorIndex + 1][,];
        foreach (int colorIdx in spawnColors)
        {
            colorGrids[colorIdx] = new Plant[spawnCols, spawnRows];
        }

        loadState = LoadState.Spawning;
        long total = (long)spawnCols * spawnRows * spawnColors.Count;
        DebugLogger.Msg($"[Video Player] Spawning {total} plants over multiple frames...");
    }

    /// <summary>
    /// Spawns plants in small batches (Unity objects can only be created on the main thread),
    /// spending at most SpawnBudgetMs per frame so the game doesn't hitch.
    /// </summary>
    private void StepSpawn()
    {
        int colorCount = spawnColors.Count;
        long total = (long)spawnCols * spawnRows * colorCount;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (spawnIndex < total)
        {
            long cell = spawnIndex / colorCount;
            int colorIdx = spawnColors[(int)(spawnIndex % colorCount)];
            int col = (int)(cell / spawnRows);
            int row = (int)(cell % spawnRows);
            spawnIndex++;

            try
            {
                Plant p = SpawnPlant(col, row, spawnTypes[colorIdx]);
                if (spawnPuff)
                {
                    SetupVideoPixel(p, col, row);
                }
                colorGrids[colorIdx][col, row] = p;
            }
            catch (Exception e)
            {
                DebugLogger.Error($"[Video Player] Error spawning Color {colorIdx} at ({col},{row}): {e.Message}");
            }

            if (sw.Elapsed.TotalMilliseconds >= SpawnBudgetMs) break;
        }

        if (spawnIndex >= total)
        {
            spawnColors = null;
            spawnTypes = null;
            currentFrame = 0;
            timer = 0f;
            loadState = LoadState.Playing;
            DebugLogger.Msg($"[Video Player] Setup complete, starting playback of '{loadedFileName}'.");
        }
    }

    private void Fail(string message)
    {
        DebugLogger.Warning($"[Video Player] {message}");
        Teardown();
        Active = false;
    }

    public override void OnUpdateActive()
    {
        if (loadState == LoadState.Idle) return;

        if (AppData.BoardInstanceIsNull)
        {
            DebugLogger.Warning("[Video Player] Board Not Found, stopping playback.");
            OnDisable();
            Active = false;
            return;
        }

        switch (loadState)
        {
            case LoadState.Loading:
                PollLoad();
                return;

            case LoadState.Spawning:
                StepSpawn();
                return;

            case LoadState.Playing:
                break;

            default:
                return;
        }

        if (frames == null || frames.Count == 0) return;

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
        // Use the grid size that was actually spawned, not the live setting values
        int maxCol = gridState.GetLength(0);
        int maxRow = gridState.GetLength(1);

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

        if (spawnPuff)
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
        Teardown();
    }

    /// <summary>
    /// Cancels any in-flight load, kills every spawned plant (even a half-finished spawn) and resets state.
    /// Safe to call repeatedly.
    /// </summary>
    private void Teardown()
    {
        try { loadCts?.Cancel(); } catch { }
        loadCts = null;
        loadTask = null;
        loadState = LoadState.Idle;

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
        spawnColors = null;
        spawnTypes = null;
        spawnIndex = 0;
    }
}
#endif