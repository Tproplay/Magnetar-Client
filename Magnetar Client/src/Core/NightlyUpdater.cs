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
/// Checks for a newer nightly build on GitHub Actions at startup and hot-swaps
/// Magnetar Data files (Resources, Themes, FxData.json, TextureData.json).
/// The DLL itself cannot be replaced while the game is running; a reminder is
/// shown in the log instead.
/// </summary>
internal static class NightlyUpdater
{
    // ── Config ────────────────────────────────────────────────────────────────

    /// <summary>GitHub repo in "owner/repo" format.</summary>
    private const string Repo = "Tproplay/Magnetar-Client";

    /// <summary>Name of the GitHub Actions workflow file.</summary>
    private const string WorkflowFile = "nightly.yml";

    /// <summary>Only branches with this name are considered nightly builds.</summary>
    private const string NightlyBranch = "master";

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

        DebugLogger.Msg($"[NightlyUpdater] Checking for nightly updates (current: v{MagnetarInfo.Version})...");

        string runId = null;
        string artifactId = null;
        string artifactName = null;

        // ── Step 1: find latest successful workflow run on master ─────────────
        bool step1Done = false;
        Exception step1Error = null;

        var t1 = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                using var client = MakeClient();
                // GET /repos/{owner}/{repo}/actions/workflows/{workflow}/runs
                string url = $"https://api.github.com/repos/{Repo}/actions/workflows/{WorkflowFile}/runs"
                           + $"?branch={NightlyBranch}&status=success&per_page=1";

                string json = await client.GetStringAsync(url);

                runId = ExtractJsonValue(json, "\"id\":");
                if (string.IsNullOrEmpty(runId))
                    throw new Exception("No successful nightly run found.");
            }
            catch (Exception ex) { step1Error = ex; }
            finally { step1Done = true; }
        });

        while (!step1Done)
            yield return null;

        if (step1Error != null)
        {
            DebugLogger.Warning($"[NightlyUpdater] Could not fetch workflow runs: {step1Error.Message}");
            yield break;
        }

        // ── Step 2: find the MelonLoader artifact in that run ─────────────────
        bool step2Done = false;
        Exception step2Error = null;

        var t2 = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                using var client = MakeClient();
                string url = $"https://api.github.com/repos/{Repo}/actions/runs/{runId}/artifacts";
                string json = await client.GetStringAsync(url);

                // Find the first artifact whose name contains "MelonLoader"
                int idx = json.IndexOf("MelonLoader", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) throw new Exception("No MelonLoader artifact found in run.");

                // Walk back to the start of that artifact object to read its id and name
                int objStart = json.LastIndexOf('{', idx);
                string fragment = json.Substring(objStart, Math.Min(500, json.Length - objStart));

                artifactId   = ExtractJsonValue(fragment, "\"id\":");
                artifactName = ExtractJsonStringValue(fragment, "\"name\":");
            }
            catch (Exception ex) { step2Error = ex; }
            finally { step2Done = true; }
        });

        while (!step2Done)
            yield return null;

        if (step2Error != null)
        {
            DebugLogger.Warning($"[NightlyUpdater] Could not fetch artifact list: {step2Error.Message}");
            yield break;
        }

        // ── Step 3: compare artifact version with current version ─────────────
        var match = Regex.Match(artifactName ?? string.Empty, @"\b\d+\.\d+\.\d+\.\d+\b");
        if (match.Success && Version.TryParse(match.Value, out var remoteVer) && Version.TryParse(MagnetarInfo.Version, out var localVer))
        {
            if (remoteVer <= localVer)
            {
                DebugLogger.Msg($"[NightlyUpdater] Up to date (current: v{localVer}, latest: v{remoteVer}). No update needed.");
                yield break;
            }
            DebugLogger.Msg($"[NightlyUpdater] Newer nightly build available: v{remoteVer} (current: v{localVer}). Downloading...");
        }
        else if (artifactName != null && artifactName.Contains(MagnetarInfo.Version))
        {
            DebugLogger.Msg($"[NightlyUpdater] Already on latest nightly build (v{MagnetarInfo.Version}). No update needed.");
            yield break;
        }
        else
        {
            DebugLogger.Msg($"[NightlyUpdater] New nightly artifact found: {artifactName}. Downloading...");
        }

        // ── Step 4: download & extract the artifact zip ───────────────────────
        bool step4Done = false;
        Exception step4Error = null;
        string tempZipPath = Path.Combine(Path.GetTempPath(), $"magnetar_nightly_{artifactId}.zip");

        var t4 = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                using var client = MakeClient();
                // GitHub redirects artifact downloads — HttpClient follows automatically
                string url = $"https://api.github.com/repos/{Repo}/actions/artifacts/{artifactId}/zip";

                byte[] data = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(tempZipPath, data);
            }
            catch (Exception ex) { step4Error = ex; }
            finally { step4Done = true; }
        });

        while (!step4Done)
            yield return null;

        if (step4Error != null)
        {
            DebugLogger.Warning($"[NightlyUpdater] Download failed: {step4Error.Message}");
            yield break;
        }

        // ── Step 5: extract — skip the DLL, update data files only ────────────
        try
        {
            string modsDir  = PathsManager.ModsDir;
            bool   dllFound = false;

            using var zip = ZipFile.OpenRead(tempZipPath);
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    continue; // skip directory entries

                string entryName = entry.FullName.Replace('\\', '/');

                // ── DLL: cannot hot-replace — just note it ────────────────────
                if (entryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    dllFound = true;
                    DebugLogger.Warning("[NightlyUpdater] DLL update is available but cannot be applied while the game is running. Please replace 'Magnetar Client.dll' manually after closing the game.");
                    continue;
                }

                // ── Data files: extract relative to Mods directory ────────────
                // The zip layout is: Mods/Magnetar Data/...
                // Strip leading "Mods/" so it lands in modsDir correctly.
                string relativePath = entryName;
                if (relativePath.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase))
                    relativePath = relativePath.Substring("Mods/".Length);

                string destPath = Path.Combine(modsDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string destDir  = Path.GetDirectoryName(destPath);

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                entry.ExtractToFile(destPath, overwrite: true);
            }

            DebugLogger.Msg($"[NightlyUpdater] Data files updated from nightly '{artifactName}'! Changes take effect on next launch.");

            if (!dllFound)
                DebugLogger.Warning("[NightlyUpdater] No DLL found in the artifact — only data files were updated.");
        }
        catch (Exception ex)
        {
            DebugLogger.Error($"[NightlyUpdater] Extraction failed: {ex.Message}");
        }
        finally
        {
            // Clean up temp zip
            try { File.Delete(tempZipPath); } catch { }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HttpClient MakeClient()
    {
        var client = new HttpClient();
        // GitHub API requires a User-Agent header
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("MagnetarClient", MagnetarInfo.Version));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    /// <summary>Extracts the first numeric value after a JSON key, e.g. "id": 123456</summary>
    private static string ExtractJsonValue(string json, string key)
    {
        int keyIdx = json.IndexOf(key, StringComparison.Ordinal);
        if (keyIdx < 0) return null;

        int valueStart = keyIdx + key.Length;
        // Skip whitespace
        while (valueStart < json.Length && (char.IsDigit(json[valueStart]) == false && json[valueStart] != '-'))
        {
            if (valueStart >= json.Length) return null;
            valueStart++;
        }

        int valueEnd = valueStart;
        while (valueEnd < json.Length && (char.IsDigit(json[valueEnd]) || json[valueEnd] == '-'))
            valueEnd++;

        return valueEnd > valueStart ? json.Substring(valueStart, valueEnd - valueStart) : null;
    }

    /// <summary>Extracts the first quoted string value after a JSON key, e.g. "name": "foo"</summary>
    private static string ExtractJsonStringValue(string json, string key)
    {
        int keyIdx = json.IndexOf(key, StringComparison.Ordinal);
        if (keyIdx < 0) return null;

        int quoteOpen = json.IndexOf('"', keyIdx + key.Length);
        if (quoteOpen < 0) return null;

        int valueStart = quoteOpen + 1;
        int quoteClose = json.IndexOf('"', valueStart);
        if (quoteClose < 0) return null;

        return json.Substring(valueStart, quoteClose - valueStart);
    }
}
#endif
