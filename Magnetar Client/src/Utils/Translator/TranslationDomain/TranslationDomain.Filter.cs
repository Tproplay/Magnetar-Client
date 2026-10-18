using System;
using System.Collections.Generic;

namespace Magnetar_Client.Utils;

public partial class TranslationDomain
{
    /// <summary>
    /// Adds a normalized relative path to the blacklist.
    /// Any matching file or directory subtree will be excluded from translation processing.
    /// </summary>
    /// <param name="relativePath">The relative path to blacklist.</param>
    public void Blacklist(string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
            BlacklistedPaths.Add(NormalizePath(relativePath));
    }

    /// <summary>
    /// Adds multiple relative paths to the blacklist.
    /// </summary>
    /// <param name="relativePaths">A collection of relative paths to exclude.</param>
    public void Blacklist(IEnumerable<string> relativePaths)
    {
        if (relativePaths == null) return;
        foreach (var path in relativePaths) Blacklist(path);
    }

    /// <summary>
    /// Adds a normalized relative path to the whitelist.
    /// When any whitelist rules exist, only paths matching the whitelist can be processed.
    /// </summary>
    /// <param name="relativePath">The relative path to whitelist.</param>
    public void Whitelist(string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
            WhitelistedPaths.Add(NormalizePath(relativePath));
    }

    /// <summary>
    /// Adds multiple relative paths to the whitelist.
    /// </summary>
    /// <param name="relativePaths">A collection of relative paths to include.</param>
    public void Whitelist(IEnumerable<string> relativePaths)
    {
        if (relativePaths == null) return;
        foreach (var path in relativePaths) Whitelist(path);
    }

    /// <summary>
    /// Determines whether the specified relative path is blacklisted directly or is located inside a blacklisted folder.
    /// </summary>
    /// <param name="relativePath">The relative path to check.</param>
    /// <returns><see langword="true"/> if the path is explicitly blacklisted or under a blacklisted directory; otherwise, <see langword="false"/>.</returns>
    public bool IsBlacklisted(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || BlacklistedPaths.Count == 0) return false;
        string normalized = NormalizePath(relativePath);

        foreach (var blacklisted in BlacklistedPaths)
        {
            if (string.IsNullOrWhiteSpace(blacklisted)) continue;
            string normB = NormalizePath(blacklisted);
            if (string.Equals(normalized, normB, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(normB + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines whether the specified relative path matches a whitelisted path directly or resides within a whitelisted directory subtree.
    /// </summary>
    /// <param name="relativePath">The relative path to check.</param>
    /// <returns><see langword="true"/> if the path matches or resides in a whitelisted path; otherwise, <see langword="false"/>.</returns>
    public bool IsWhitelisted(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || WhitelistedPaths.Count == 0) return false;
        string normalized = NormalizePath(relativePath);

        foreach (var whitelisted in WhitelistedPaths)
        {
            if (string.IsNullOrWhiteSpace(whitelisted)) continue;
            string normW = NormalizePath(whitelisted);
            if (string.Equals(normalized, normW, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(normW + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Evaluates whether a normalized relative path falls within any configured source directory.
    /// Defaults to <see langword="true"/> if no specific sources are configured.
    /// </summary>
    /// <param name="normalizedRelPath">The pre-normalized relative path to test.</param>
    /// <returns><see langword="true"/> if <see cref="Sources"/> is empty or contains the path; otherwise, <see langword="false"/>.</returns>
    public bool IsPathInSources(string normalizedRelPath)
    {
        if (Sources.Count == 0) return true;

        foreach (var source in Sources)
        {
            string normSource = NormalizePath(source);
            if (string.Equals(normalizedRelPath, normSource, StringComparison.OrdinalIgnoreCase) ||
                normalizedRelPath.StartsWith(normSource + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a relative path is permitted for processing by evaluating all filter layers in order:
    /// <list type="number">
    /// <item><description>Returns <see langword="false"/> if null or whitespace.</description></item>
    /// <item><description>Returns <see langword="false"/> if matched by any blacklist rule.</description></item>
    /// <item><description>Returns <see langword="false"/> if a whitelist is configured and the path is not whitelisted.</description></item>
    /// <item><description>Returns <see langword="false"/> if sources are defined and the path does not reside within any source.</description></item>
    /// </list>
    /// </summary>
    /// <param name="relativePath">The target relative file or folder path.</param>
    /// <returns><see langword="true"/> if the path satisfies all filtering rules; otherwise, <see langword="false"/>.</returns>
    public bool IsAllowed(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return false;
        if (IsBlacklisted(relativePath)) return false;
        if (WhitelistedPaths.Count > 0 && !IsWhitelisted(relativePath)) return false;
        if (Sources.Count > 0 && !IsPathInSources(relativePath)) return false;
        return true;
    }
}