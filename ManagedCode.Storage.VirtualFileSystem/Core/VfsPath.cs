using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ManagedCode.Storage.VirtualFileSystem.Core;

/// <summary>
/// Normalized path representation for virtual filesystem
/// </summary>
public readonly struct VfsPath : IEquatable<VfsPath>
{
    private readonly string _normalized;

    /// <summary>
    /// Initializes a new instance of VfsPath with the specified path
    /// </summary>
    /// <param name="path">The path to normalize</param>
    /// <exception cref="ArgumentException">Thrown when path is null or whitespace</exception>
    public VfsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        _normalized = NormalizePath(path);
    }

    /// <summary>
    /// Gets the normalized path value
    /// </summary>
    public string Value => _normalized;

    /// <summary>
    /// Gets a value indicating whether this path represents the root directory
    /// </summary>
    public bool IsRoot => _normalized == "/";

    /// <summary>
    /// Gets a value indicating whether this path represents a directory (no file extension)
    /// </summary>
    public bool IsDirectory => !Path.HasExtension(_normalized);

    /// <summary>
    /// Gets the parent directory path
    /// </summary>
    /// <returns>The parent directory path, or root if this is already root</returns>
    public VfsPath GetParent()
    {
        if (IsRoot) return this;
        var lastSlash = _normalized.LastIndexOf('/');
        return new VfsPath(lastSlash == 0 ? "/" : _normalized[..lastSlash]);
    }

    /// <summary>
    /// Combines this path with a child name
    /// </summary>
    /// <param name="name">The child name to combine</param>
    /// <returns>A new VfsPath representing the combined path</returns>
    public VfsPath Combine(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        return new VfsPath(_normalized == "/" ? "/" + name : _normalized + "/" + name);
    }

    /// <summary>
    /// Gets the file name portion of the path
    /// </summary>
    /// <returns>The file name</returns>
    public string GetFileName() => Path.GetFileName(_normalized);

    /// <summary>
    /// Gets the file name without extension
    /// </summary>
    /// <returns>The file name without extension</returns>
    public string GetFileNameWithoutExtension() => Path.GetFileNameWithoutExtension(_normalized);

    /// <summary>
    /// Gets the file extension
    /// </summary>
    /// <returns>The file extension including the leading dot</returns>
    public string GetExtension() => Path.GetExtension(_normalized);

    /// <summary>
    /// Converts the path to a blob key for storage operations
    /// </summary>
    /// <returns>The blob key (path without leading slash)</returns>
    public string ToBlobKey()
    {
        return _normalized.Length > 1 ? _normalized[1..] : "";
    }

    /// <summary>
    /// Normalize path to canonical form
    /// </summary>
    private static string NormalizePath(string path)
    {
        // Security: Check for null bytes (potential security issue)
        if (path.Contains('\0'))
            throw new ArgumentException("Path contains null bytes", nameof(path));

        // Security: Check for control characters
        if (path.Any(c => char.IsControl(c) && c != '\t' && c != '\r' && c != '\n'))
            throw new ArgumentException("Path contains control characters", nameof(path));

        // 1. Replace backslashes with forward slashes
        path = path.Replace('\\', '/');

        // 2. Collapse multiple slashes
        while (path.Contains("//"))
            path = path.Replace("//", "/");

        // 3. Remove trailing slash except for root
        if (path.Length > 1 && path.EndsWith('/'))
            path = path.TrimEnd('/');

        // 4. Ensure absolute path
        if (!path.StartsWith('/'))
            path = '/' + path;

        // 5. Resolve . and ..
        var segments = new List<string>();
        foreach (var segment in path.Split('/'))
        {
            if (segment == "." || string.IsNullOrEmpty(segment))
                continue;
            if (segment == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);
            }
            else
            {
                segments.Add(segment);
            }
        }

        return "/" + string.Join("/", segments);
    }

    /// <summary>
    /// Implicit conversion from string to VfsPath
    /// </summary>
    public static implicit operator VfsPath(string path) => new(path);

    /// <summary>
    /// Implicit conversion from VfsPath to string
    /// </summary>
    public static implicit operator string(VfsPath path) => path._normalized;

    /// <summary>
    /// Returns the normalized path
    /// </summary>
    public override string ToString() => _normalized;

    /// <summary>
    /// Returns the hash code for this path
    /// </summary>
    public override int GetHashCode() => _normalized.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Determines whether this path equals another VfsPath
    /// </summary>
    public bool Equals(VfsPath other) => _normalized == other._normalized;

    /// <summary>
    /// Determines whether this path equals another object
    /// </summary>
    public override bool Equals(object? obj) => obj is VfsPath other && Equals(other);

    /// <summary>
    /// Equality operator
    /// </summary>
    public static bool operator ==(VfsPath left, VfsPath right) => left.Equals(right);

    /// <summary>
    /// Inequality operator
    /// </summary>
    public static bool operator !=(VfsPath left, VfsPath right) => !left.Equals(right);
}