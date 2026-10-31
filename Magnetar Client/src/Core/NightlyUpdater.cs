#if NIGHTLY
using Magnetar_Client.Api;
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Core;

/// <summary>
/// Checks for a newer nightly build on GitHub at startup and hot-swaps
/// Magnetar Data files (Resources, Themes, FxData.json, TextureData.json).
/// Also stages the new DLL as 'Magnetar Client.dll.update' for the launcher.
/// </summary>
internal static class NightlyUpdater
{
    // ── Config ────────────────────────────────────────────────────────────────

    private const string Repo = "Tproplay/Magnetar-Client";
    private const string ReleaseTag = "nightly";

    // Direct public release asset URLs (no authentication required)
    private const string BuildInfoUrl = $"https://github.com/{Repo}/releases/download/{ReleaseTag}/nightly-build.txt";
    private const string ReleaseApiUrl = $"https://api.github.com/repos/{Repo}/releases/tags/{ReleaseTag}";
    private const string DownloadUrl   = $"https://github.com/{Repo}/releases/download/{ReleaseTag}/MagnetarClient-MelonLoader.zip";

    // ── Entry Point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Call once during <see cref="Main.InitializeCore"/> to kick off the
    /// background update check. Only runs in Nightly builds.
    /// </summary>
    internal static void CheckAsync()
    {
        CoroutineManager.Start(RunCheck());
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────

    private static IEnumerator RunCheck()
    {
        // Small delay so the rest of the client finishes loading first
        yield return new WaitForSecondsRealtime(3f);

        DebugLogger.Msg($"[NightlyUpdater] Checking for nightly updates (v{MagnetarInfo.Version}, build {MagnetarInfo.BuildNumber})...");

        int remoteBuild = -1;
        bool checkDone = false;
        Exception checkError = null;

        var tCheck = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                using var client = MakeClient();

                // Method 1: Fetch static build number file (direct CDN download, zero rate limits)
                try
                {
                    string text = await client.GetStringAsync(BuildInfoUrl);
                    if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text.Trim(), out int b))
                    {
                        remoteBuild = b;
                        return;
                    }
                }
                catch { }

                // Method 2: Public GitHub Release API fallback
                try
                {
                    string json = await client.GetStringAsync(ReleaseApiUrl);
                    var match = Regex.Match(json, @"Build:?\s*(\d+)", RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int b))
                    {
                        remoteBuild = b;
                        return;
                    }
                }
                catch (Exception ex) { checkError = ex; }
            }
            catch (Exception ex) { checkError = ex; }
            finally { checkDone = true; }
        });

        while (!checkDone)
            yield return null;

        if (remoteBuild <= 0)
        {
            if (checkError != null)
                DebugLogger.Warning($"[NightlyUpdater] Could not check nightly build: {checkError.Message}");
            yield break;
        }

        // Compare internal build numbers
        if (remoteBuild <= MagnetarInfo.BuildNumber)
        {
            DebugLogger.Msg($"[NightlyUpdater] Up to date (Current build: {MagnetarInfo.BuildNumber}, Latest build: {remoteBuild}). No update needed.");
            yield break;
        }

        DebugLogger.Msg($"[NightlyUpdater] Newer nightly available: Build {remoteBuild} (current: Build {MagnetarInfo.BuildNumber}). Downloading...");

        // Download zip
        bool downloadDone = false;
        Exception downloadError = null;
        string tempZipPath = Path.Combine(Path.GetTempPath(), $"magnetar_nightly_b{remoteBuild}.zip");

        var tDownload = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                using var client = MakeClient();
                byte[] data = await client.GetByteArrayAsync(DownloadUrl);
                await File.WriteAllBytesAsync(tempZipPath, data);
            }
            catch (Exception ex) { downloadError = ex; }
            finally { downloadDone = true; }
        });

        while (!downloadDone)
            yield return null;

        if (downloadError != null)
        {
            DebugLogger.Warning($"[NightlyUpdater] Download failed: {downloadError.Message}");
            yield break;
        }

        // Extract package
        try
        {
            string modsDir = PathsManager.ModsDir;

            using var zip = ZipFile.OpenRead(tempZipPath);
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    continue;

                string entryName = entry.FullName.Replace('\\', '/');

                // DLL staged for next launch
                if (entryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    string updatePath = Path.Combine(modsDir, "Magnetar Client.dll.update");
                    entry.ExtractToFile(updatePath, overwrite: true);
                    DebugLogger.Msg("[NightlyUpdater] New DLL staged as 'Magnetar Client.dll.update'. It will be applied automatically on next launch.");
                    continue;
                }

                // Data files: strip leading "Mods/" if present
                string relativePath = entryName;
                if (relativePath.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase))
                    relativePath = relativePath.Substring("Mods/".Length);

                string destPath = Path.Combine(modsDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string destDir = Path.GetDirectoryName(destPath);

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                entry.ExtractToFile(destPath, overwrite: true);
            }

            DebugLogger.Msg($"[NightlyUpdater] Data files updated to Build {remoteBuild}! Changes take effect on next launch.");
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[NightlyUpdater] Extraction failed: {ex.Message}");
        }
        finally
        {
            try { File.Delete(tempZipPath); } catch { }
        }
    }

    private static HttpClient MakeClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("MagnetarClient", MagnetarInfo.Version));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.Timeout = TimeSpan.FromSeconds(45);
        return client;
    }
}
#endif
