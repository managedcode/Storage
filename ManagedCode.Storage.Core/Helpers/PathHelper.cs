using System;
using System.IO;

namespace ManagedCode.Storage.Core.Helpers;

/// <summary>
/// Helper methods for cross-platform path operations
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Normalizes path separators for the target system
    /// </summary>
    /// <param name="path">Path to normalize</param>
    /// <param name="targetSeparator">Target path separator character</param>
    /// <returns>Normalized path</returns>
    public static string NormalizePath(string? path, char targetSeparator = '/')
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Replace all possible path separators with target separator
        return path.Replace('\\', targetSeparator).Replace('/', targetSeparator);
    }

    /// <summary>
    /// Normalizes path for Unix-like systems (FTP, Linux, etc.) 
    /// Always uses forward slash (/) as separator
    /// </summary>
    /// <param name="path">Path to normalize</param>
    /// <returns>Unix-style path</returns>
    public static string ToUnixPath(string? path)
    {
        return NormalizePath(path, '/');
    }

    /// <summary>
    /// Normalizes path for Windows systems
    /// Always uses backslash (\) as separator  
    /// </summary>
    /// <param name="path">Path to normalize</param>
    /// <returns>Windows-style path</returns>
    public static string ToWindowsPath(string? path)
    {
        return NormalizePath(path, '\\');
    }

    /// <summary>
    /// Gets directory path from file path and normalizes separators
    /// </summary>
    /// <param name="filePath">Full file path</param>
    /// <param name="targetSeparator">Target path separator</param>
    /// <returns>Normalized directory path or empty string</returns>
    public static string GetDirectoryPath(string? filePath, char targetSeparator = '/')
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        var directoryPath = Path.GetDirectoryName(filePath);
        return NormalizePath(directoryPath, targetSeparator);
    }

    /// <summary>
    /// Gets Unix-style directory path from file path
    /// </summary>
    /// <param name="filePath">Full file path</param>
    /// <returns>Unix-style directory path</returns>
    public static string GetUnixDirectoryPath(string? filePath)
    {
        return GetDirectoryPath(filePath, '/');
    }

    /// <summary>
    /// Combines path segments using the specified separator
    /// </summary>
    /// <param name="separator">Path separator to use</param>
    /// <param name="paths">Path segments to combine</param>
    /// <returns>Combined path</returns>
    public static string CombinePaths(char separator, params string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return string.Empty;

        var result = paths[0] ?? string.Empty;

        for (int i = 1; i < paths.Length; i++)
        {
            var path = paths[i];
            if (string.IsNullOrEmpty(path))
                continue;

            // Remove leading separators from current path
            path = path.TrimStart('/', '\\');

            // Ensure result doesn't end with separator (unless it's root)
            if (result.Length > 0 && result[^1] != separator)
                result += separator;

            result += path;
        }

        return NormalizePath(result, separator);
    }

    /// <summary>
    /// Combines path segments using Unix-style separators (/)
    /// </summary>
    /// <param name="paths">Path segments to combine</param>
    /// <returns>Combined Unix-style path</returns>
    public static string CombineUnixPaths(params string[] paths)
    {
        return CombinePaths('/', paths);
    }

    /// <summary>
    /// Combines path segments using Windows-style separators (\)
    /// </summary>
    /// <param name="paths">Path segments to combine</param>
    /// <returns>Combined Windows-style path</returns>
    public static string CombineWindowsPaths(params string[] paths)
    {
        return CombinePaths('\\', paths);
    }

    /// <summary>
    /// Ensures path is relative (doesn't start with separator)
    /// </summary>
    /// <param name="path">Path to make relative</param>
    /// <returns>Relative path</returns>
    public static string EnsureRelativePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        return path.TrimStart('/', '\\');
    }

    /// <summary>
    /// Ensures path is absolute (starts with separator)
    /// </summary>
    /// <param name="path">Path to make absolute</param>
    /// <param name="separator">Path separator to use</param>
    /// <returns>Absolute path</returns>
    public static string EnsureAbsolutePath(string? path, char separator = '/')
    {
        if (string.IsNullOrEmpty(path))
            return separator.ToString();

        var normalizedPath = NormalizePath(path, separator);

        if (normalizedPath[0] != separator)
            normalizedPath = separator + normalizedPath;

        return normalizedPath;
    }

    /// <summary>
    /// Checks if path is absolute (starts with separator or drive letter on Windows)
    /// </summary>
    /// <param name="path">Path to check</param>
    /// <returns>True if path is absolute</returns>
    public static bool IsAbsolutePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Unix-style absolute path
        if (path[0] == '/' || path[0] == '\\')
            return true;

        // Windows-style absolute path (C:\, D:\, etc.)
        if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':')
            return true;

        return false;
    }

    /// <summary>
    /// Removes trailing path separators from path (except for root paths)
    /// </summary>
    /// <param name="path">Path to trim</param>
    /// <returns>Path without trailing separators</returns>
    public static string TrimTrailingSeparators(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.Length <= 1)
            return path ?? string.Empty;

        return path.TrimEnd('/', '\\');
    }

    /// <summary>
    /// Gets the file name from path without directory
    /// </summary>
    /// <param name="path">Full path</param>
    /// <returns>File name only</returns>
    public static string GetFileName(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var normalizedPath = NormalizePath(path);
        var lastSeparatorIndex = normalizedPath.LastIndexOfAny(new[] { '/', '\\' });

        return lastSeparatorIndex >= 0
            ? normalizedPath[(lastSeparatorIndex + 1)..]
            : normalizedPath;
    }
}