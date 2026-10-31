using Magnetar_Client.Utils;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if MELONLOADER || BEPINEX
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
#endif

#if MELONLOADER || MELON_LOCAL
using Il2Cpp;
#elif BEPINEX || BEPINEX_LOCAL

#endif

namespace MagnetarAIAddon;

/// <summary>
/// Game Data fed to the ML model for making predictions.
/// </summary>
public class BuffDecisionData
{
    /// <summary>
    /// Used to group options of the same menu
    /// </summary>
    [LoadColumn(0)] public float GroupId { get; set; }
    /// <summary>
    /// The difficulty on which the level was played
    /// </summary>
    [LoadColumn(1)] public float Difficulty { get; set; }
    /// <summary>
    /// The number of plants the player had owned
    /// </summary>
    [LoadColumn(2)] public float CurrentPlantCount { get; set; }
    /// <summary>
    /// Maximum number of plants allowed
    /// </summary>
    [LoadColumn(3)] public float MaxPlantCount { get; set; }
    /// <summary>
    /// Plants player has picked
    /// </summary>
    [LoadColumn(4)] public string OwnedPlants { get; set; } = "0";
    /// <summary>
    /// The plant for which the modifier is for
    /// </summary>
    [LoadColumn(5)] public float PlantTypeId { get; set; }
    /// <summary>
    /// The quality of the modifier
    /// </summary>
    [LoadColumn(6)] public float Quality { get; set; }
    /// <summary>
    /// Number of times the modifier has been already picked
    /// </summary>
    [LoadColumn(7)] public float TimesModifierPicked { get; set; }
    /// <summary>
    /// Title of the modifier (in chinese)
    /// </summary>
    [LoadColumn(8)] public string CandidateTitle { get; set; } = string.Empty;
    /// <summary>
    /// How many options are in the menu
    /// </summary>
    [LoadColumn(9)] public float PoolSize { get; set; }
    /// <summary>
    /// Max quality in all other options
    /// </summary>
    [LoadColumn(10)] public float CompetitorMaxQuality { get; set; }
    /// <summary>
    /// Avg quality in all other options
    /// </summary>
    [LoadColumn(11)] public float CompetitorAvgQuality { get; set; }
    /// <summary>
    /// The continuous cumulative wave across all boss regions
    /// </summary>
    [LoadColumn(12)] public float Wave { get; set; }
    /// <summary>
    /// What Zombie Spawn loadout is it?
    /// </summary>
    [LoadColumn(13)] public float ZombieLoadout { get; set; }

    /// <summary>
    /// Was this buff picked by the player
    /// </summary>
    [LoadColumn(14), ColumnName("Label")] public bool WasChosen { get; set; }
}

/// <summary>
/// Format in which the ML outputs the prediction.
/// </summary>
public class BuffScorePrediction
{
    [ColumnName("PredictedLabel")] public bool PredictedLabel { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}

public static class RunProgressionTracker
{
    private static int _currentRegionOffset = 0;
    private static int _lastObservedWave = -1;

    public static void ResetRun()
    {
        _currentRegionOffset = 0;
        _lastObservedWave = -1;
    }

    public static float GetContinuousWave()
    {
        if (Board.Instance == null) return 0f;

        int rawWave = Board.Instance.theWave;

        // Boss transition detected: wave dropped significantly (e.g. from >= 90 back to <= 5)
        if (_lastObservedWave >= 90 && rawWave < 10)
        {
            _currentRegionOffset += 100;
        }

        _lastObservedWave = rawWave;
        return (float)(_currentRegionOffset + rawWave);
    }
}

public static class AIService
{
#if MELONLOADER || BEPINEX
    public const string ServerUrl = "https://magnetar-client.onrender.com/aiaddon";
    private const string TelemetryEndpoint = $"{ServerUrl}/api/telemetry/session";
    private const string ModelDownloadEndpoint = $"{ServerUrl}/api/model/download";
    private const string TelemetryNotifyEndpoint = $"{ServerUrl}/api/telemetry/notify";

    private const string GenesisHash = "00000000000000000000000000000000";
    private static string _lastRowHash = GenesisHash;

    private static string GetResolvedModSecret() => SecretsProvider.GetModSecret();

    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(90) };

    private static string SessionsDir => Path.Combine(BaseAddonDir, "Sessions");

    private static string _currentSessionFilePath;
    private static int _sessionGroupCounter = 0;
    private static int _minimumLinesInSession { get; set; } = 120;

    public static bool IsRestarting { get; set; } = false;
    public static string CurrentSessionFilePath => _currentSessionFilePath;
#endif

    private static readonly Dictionary<string, int> _modifierPickCounts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly MLContext _mlContext = new(seed: 42);
    private static PredictionEngine<BuffDecisionData, BuffScorePrediction> _predEngine;
    private static ITransformer _trainedModel;

    private static string BaseAddonDir => Path.Combine(Magnetar_Client.Api.PathsManager.AddonsDir, "ML");
    private static string ModelPath => Path.Combine(BaseAddonDir, "buff_model.zip");

    /// <summary>
    /// Creates base directories and loads the local model.
    /// </summary>
    public static void Init()
    {
        if (!Directory.Exists(BaseAddonDir)) Directory.CreateDirectory(BaseAddonDir);

#if MELONLOADER || BEPINEX
        if (!Directory.Exists(SessionsDir)) Directory.CreateDirectory(SessionsDir);
#endif

        LoadModel();
    }

    /// <summary>
    /// Loads the local model from disk.
    /// </summary>
    public static void LoadModel()
    {
        try
        {
            if (File.Exists(ModelPath))
            {
                _trainedModel = _mlContext.Model.Load(ModelPath, out _);
                _predEngine = _mlContext.Model.CreatePredictionEngine<BuffDecisionData, BuffScorePrediction>(_trainedModel);
                Magnetar_Logger.DebugLogger.Msg("[Assist Bot] Local model loaded into inference engine.");
            }
            else
            {
                Magnetar_Logger.DebugLogger.Warning("[Assist Bot] Local model not found.");
            }
        }
        catch (Exception ex)
        {
            Magnetar_Logger.DebugLogger.Error($"[Assist Bot] Model load error: {ex.Message}");
        }
    }

#if MELONLOADER || BEPINEX
    /// <summary>
    /// Downloads the latest model from the server and refreshes the engine.
    /// </summary>
    public static async Task SyncModelWithServerAsync()
    {
        try
        {
            Magnetar_Logger.DebugLogger.Msg("[Assist Bot] Checking server for updated buff model...");
            var response = await _httpClient.GetAsync(ModelDownloadEndpoint);

            if (response.IsSuccessStatusCode)
            {
                byte[] modelBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(ModelPath, modelBytes);

                string modelName = "buff_model.zip";
                if (response.Headers.TryGetValues("X-Model-Name", out var nameValues))
                {
                    modelName = System.Linq.Enumerable.FirstOrDefault(nameValues) ?? modelName;
                }

                string builtTime = "Unknown";
                if (response.Headers.TryGetValues("X-Model-Built-Utc", out var timeValues))
                {
                    builtTime = System.Linq.Enumerable.FirstOrDefault(timeValues) ?? builtTime;
                }

                double sizeKb = modelBytes.Length / 1024.0;

                Magnetar_Logger.DebugLogger.Msg(
                    $"[Assist Bot] Updated model applied successfully.\n" +
                    $"            -> Name: {modelName}\n" +
                    $"            -> Size: {sizeKb:F2} KB\n" +
                    $"            -> Built: {builtTime}"
                );

                LoadModel();

                
            }
            else
            {
                Magnetar_Logger.DebugLogger.Msg($"[Assist Bot] Server check finished ({response.StatusCode}). Using local model.");
            }
        }
        catch (Exception ex)
        {
            Magnetar_Logger.DebugLogger.Warning($"[Assist Bot] Could not reach server for model sync: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates the local hash chain, strips the proof hashes, and uploads in the 15-column format.
    /// </summary>
    public static async Task<bool> UploadSessionTelemetryAsync(string sessionFilePath)
    {
        if (!File.Exists(sessionFilePath)) return false;

        try
        {
            string[] lines = await File.ReadAllLinesAsync(sessionFilePath);
            if (lines.Length <= 1) return false;

            string secret = GetResolvedModSecret();
            string previousHash = GenesisHash;
            var sanitizedLines = new List<string>(lines.Length)
            {
                // Recreate clean standard 15-column header for server
                "GroupId,Difficulty,CurrentPlantCount,MaxPlantCount,OwnedPlants,PlantTypeId,Quality," +
                "TimesModifierPicked,CandidateTitle,PoolSize,CompetitorMaxQuality,CompetitorAvgQuality,Wave," +
                "ZombieLoadout,Label"
            };

            // Client-side verification: iterate and re-verify each chained row
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                int lastComma = line.LastIndexOf(',');
                if (lastComma == -1)
                {
                    Magnetar_Logger.DebugLogger.Warning($"[Assist Bot] Local session tampered: Malformed row at line {i + 1}.");
                    return false;
                }

                string rowData = line[..lastComma];
                string storedHash = line[(lastComma + 1)..].Trim();

                string expectedHash = ComputeRowHash(secret, previousHash, rowData);

                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expectedHash),
                        Encoding.UTF8.GetBytes(storedHash)))
                {
                    Magnetar_Logger.DebugLogger.Error(
                        $"[Assist Bot] Session integrity violation! Line {i + 1} was modified while the game was running. Upload aborted.");
                    return false;
                }

                previousHash = storedHash;
                sanitizedLines.Add(rowData); // Strips the 16th hash column
            }

            // Convert clean lines to bytes
            string cleanCsvContent = string.Join("\n", sanitizedLines) + "\n";
            byte[] fileBytes = Encoding.UTF8.GetBytes(cleanCsvContent);
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // Compute standard payload HMAC signature
            string signature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] timeBytes = Encoding.UTF8.GetBytes(timestamp);
                hmac.TransformBlock(timeBytes, 0, timeBytes.Length, null, 0);
                hmac.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
                signature = Convert.ToHexString(hmac.Hash!).ToLowerInvariant();
            }

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", Path.GetFileName(sessionFilePath));

            using var request = new HttpRequestMessage(HttpMethod.Post, TelemetryEndpoint);
            request.Headers.Add("X-Mod-Signature", signature);
            request.Headers.Add("X-Mod-Timestamp", timestamp);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                Magnetar_Logger.DebugLogger.Warning($"[AI] Telemetry rejected by server: {response.StatusCode} | {errorBody}");
                return false;
            }

            await _httpClient.PostAsync(TelemetryNotifyEndpoint, null);
            return true;
        }
        catch (Exception ex)
        {
            Magnetar_Logger.DebugLogger.Error($"[Assist Bot] Telemetry upload failed: {ex.Message}");
            return false;
        }
    }
    
    public static void StartNewSession()
    {
        if (!Directory.Exists(BaseAddonDir)) Directory.CreateDirectory(BaseAddonDir);
        if (!Directory.Exists(SessionsDir)) Directory.CreateDirectory(SessionsDir);

        _sessionGroupCounter = 0;
        _modifierPickCounts.Clear();
        RunProgressionTracker.ResetRun();
        _lastRowHash = GenesisHash;
        _currentSessionFilePath = Path.Combine(SessionsDir, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        // Local file keeps RowHash column for continuous tamper detection
        File.WriteAllText(_currentSessionFilePath,
            "GroupId,Difficulty,CurrentPlantCount,MaxPlantCount,OwnedPlants,PlantTypeId,Quality," +
            "TimesModifierPicked,CandidateTitle,PoolSize,CompetitorMaxQuality,CompetitorAvgQuality,Wave," +
            "ZombieLoadout,Label,RowHash\n");
    }

    public static void DiscardCurrentSession()
    {
        try
        {
            if (!string.IsNullOrEmpty(_currentSessionFilePath) && File.Exists(_currentSessionFilePath))
            {
                File.Delete(_currentSessionFilePath);
                Magnetar_Logger.DebugLogger.Msg($"[Assist Bot] Discarded session file: {_currentSessionFilePath}");
            }
        }
        catch (Exception ex)
        {
            Magnetar_Logger.DebugLogger.Error($"[Assist Bot] Discard session error: {ex.Message}");
        }
        _currentSessionFilePath = null;
        ResetModifiersCounts();
    }

    public static bool IsValidCurrentSessionData()
    {
        if (string.IsNullOrEmpty(_currentSessionFilePath) || !File.Exists(_currentSessionFilePath))
            return false;

        try
        {
            return File.ReadAllLines(_currentSessionFilePath).Length > _minimumLinesInSession;
        }
        catch
        {
            return false;
        }
    }

    public static void RecordSession(List<BuffDecisionData> optionsShown, int chosenIndex)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentSessionFilePath)) StartNewSession();
            int currentGroupId = ++_sessionGroupCounter;
            string secret = GetResolvedModSecret();

            using (var sw = File.AppendText(_currentSessionFilePath))
            {
                for (int i = 0; i < optionsShown.Count; i++)
                {
                    var opt = optionsShown[i];
                    bool chosen = (i == chosenIndex);
                    string safeTitle = opt.CandidateTitle.Replace(',', ' ').Trim();
                    string safeOwned = string.IsNullOrEmpty(opt.OwnedPlants) ? "0" : opt.OwnedPlants.Trim();

                    string rowData = $"{currentGroupId},{opt.Difficulty},{opt.CurrentPlantCount},{opt.MaxPlantCount}," +
                        $"{safeOwned},{opt.PlantTypeId},{opt.Quality},{opt.TimesModifierPicked},{safeTitle}," +
                        $"{opt.PoolSize},{opt.CompetitorMaxQuality},{opt.CompetitorAvgQuality},{opt.Wave}," +
                        $"{opt.ZombieLoadout},{chosen}";

                    // Compute rolling hash: HMAC(secret, previousHash + ":" + rowData)
                    string currentHash = ComputeRowHash(secret, _lastRowHash, rowData);
                    _lastRowHash = currentHash;

                    // Append row with hash to local disk
                    sw.WriteLine($"{rowData},{currentHash}");
                }
            }

            if (chosenIndex >= 0 && chosenIndex < optionsShown.Count)
            {
                IncrementModifierCount(optionsShown[chosenIndex].CandidateTitle);
            }
        }
        catch (Exception ex)
        {
            Magnetar_Logger.DebugLogger.Error($"[Assist Bot] Record failed: {ex.Message}");
        }
    }

    private static string ComputeRowHash(string secret, string previousHash, string rawRowData)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(previousHash + ":" + rawRowData);

        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
#endif

    public static void ResetModifiersCounts()
    {
        _modifierPickCounts.Clear();
    }

    public static int GetModifierPickCount(string modifierTitle)
    {
        if (string.IsNullOrEmpty(modifierTitle)) return 0;
        return _modifierPickCounts.TryGetValue(modifierTitle, out int count) ? count : 0;
    }

    public static void IncrementModifierCount(string modifierTitle)
    {
        if (string.IsNullOrEmpty(modifierTitle)) return;
        if (_modifierPickCounts.ContainsKey(modifierTitle))
            _modifierPickCounts[modifierTitle]++;
        else
            _modifierPickCounts[modifierTitle] = 1;
    }

    public static float PredictBuffScore(BuffDecisionData input)
    {
        if (_predEngine == null)
            return Mathf.Clamp01((input.Quality + 1.0f) * 0.2f);

        try
        {
            var pred = _predEngine.Predict(input);
            return Mathf.Max(0f, pred.Probability);
        }
        catch (Exception ex)
        {
            Magnetar_Logger.DebugLogger.Error($"[Assist Bot] Failed to predict: {ex.Message}");
            return 0f;
        }
    }
}