using System;
using System.IO;
using System.Linq;

namespace ManagedCode.Storage.Cartograph;

internal static class CartographPath
{
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith('/') || normalized.Contains(':') || normalized.Contains('\0') ||
            normalized.Split('/').Any(part => part.Length == 0 || part is "." or ".."))
        {
            throw new ArgumentException("Use a relative catalog path without empty or traversal segments.", nameof(path));
        }

        return normalized;
    }

    public static string DirectoryPrefix(string? directory)
    {
        if (string.IsNullOrEmpty(directory))
            return string.Empty;

        return Normalize(directory.TrimEnd('/', '\\')) + '/';
    }

    public static void ValidateCatalogPath(string path)
    {
        if (Normalize(path) != path)
            throw new InvalidDataException("Catalog paths must use forward slashes.");
    }

    public static string ResolveFileSystemPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath) ?? throw new IOException("The path has no filesystem root.");
        var resolved = root;
        foreach (var component in fullPath[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            resolved = Path.Combine(resolved, component);
            FileSystemInfo info = System.IO.Directory.Exists(resolved) ? new DirectoryInfo(resolved) : new FileInfo(resolved);
            if (info.LinkTarget is not null)
            {
                var target = info.ResolveLinkTarget(returnFinalTarget: true) ?? throw new IOException("Cannot resolve symbolic link.");
                resolved = ResolveFileSystemPath(target.FullName);
            }
        }

        return resolved;
    }
}
