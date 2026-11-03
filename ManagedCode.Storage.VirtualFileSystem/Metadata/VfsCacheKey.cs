using System.Runtime.CompilerServices;
using ManagedCode.Storage.VirtualFileSystem.Core;
using Microsoft.Extensions.Caching.Memory;

namespace ManagedCode.Storage.VirtualFileSystem.Metadata;

internal enum VfsCacheKind
{
    FileExists,
    DirectoryExists,
    Metadata,
    CustomMetadata
}

internal static class VfsCacheKey
{
    private static readonly ConditionalWeakTable<IVirtualFileSystem, object> Scopes = new();

    internal static object For(IVirtualFileSystem vfs, VfsCacheKind kind, VfsPath path) =>
        (Scopes.GetValue(vfs, static _ => new object()), kind, path.Value);

    internal static void InvalidateFile(IMemoryCache cache, IVirtualFileSystem vfs, VfsPath path)
    {
        if (!vfs.Options.EnableCache)
            return;

        cache.Remove(For(vfs, VfsCacheKind.FileExists, path));
        cache.Remove(For(vfs, VfsCacheKind.Metadata, path));
        cache.Remove(For(vfs, VfsCacheKind.CustomMetadata, path));

        var parent = path.GetParent();
        while (true)
        {
            cache.Remove(For(vfs, VfsCacheKind.DirectoryExists, parent));
            if (parent.IsRoot)
                break;

            parent = parent.GetParent();
        }
    }
}
