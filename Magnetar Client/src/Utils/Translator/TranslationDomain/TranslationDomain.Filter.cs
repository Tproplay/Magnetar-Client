using System;
using System.Collections.Generic;

namespace Magnetar_Client.Utils;

public partial class TranslationDomain
{
    #region Filtering (Sources, Blacklist & Whitelist)

    public void Blacklist(string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
            BlacklistedPaths.Add(NormalizePath(relativePath));
    }

    public void Blacklist(IEnumerable<string> relativePaths)
    {
        if (relativePaths == null) return;
        foreach (var path in relativePaths) Blacklist(path);
    }

    public void Whitelist(string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
            WhitelistedPaths.Add(NormalizePath(relativePath));
    }

    public void Whitelist(IEnumerable<string> relativePaths)
    {
        if (relativePaths == null) return;
        foreach (var path in relativePaths) Whitelist(path);
    }

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

    public bool IsAllowed(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return false;
        if (IsBlacklisted(relativePath)) return false;
        if (WhitelistedPaths.Count > 0 && !IsWhitelisted(relativePath)) return false;
        if (Sources.Count > 0 && !IsPathInSources(relativePath)) return false;
        return true;
    }

    #endregion
}