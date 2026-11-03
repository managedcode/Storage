using System;
using System.IO;

namespace ManagedCode.Storage.FileSystem;

internal static class StoragePath
{
    internal static string Resolve(string rootPath, string relativePath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        var fullPath = Path.GetFullPath(relativePath, root);
        if (string.Equals(fullPath, root, StringComparison.Ordinal))
            return fullPath;

        var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("The path is outside the storage root.");

        RejectLinks(root, fullPath[prefix.Length..]);
        return fullPath;
    }

    private static void RejectLinks(string root, string relativePath)
    {
        var current = root;
        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new UnauthorizedAccessException("Storage paths cannot traverse symbolic links or reparse points.");
            }
            catch (IOException ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                return;
            }
        }
    }
}
