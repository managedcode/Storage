using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.VirtualFileSystem.Options;

namespace ManagedCode.Storage.VirtualFileSystem.Core;

/// <summary>
/// Main virtual filesystem interface providing filesystem abstraction over blob storage
/// </summary>
public interface IVirtualFileSystem : IAsyncDisposable
{
    /// <summary>
    /// Gets the underlying storage provider
    /// </summary>
    IStorage Storage { get; }

    /// <summary>
    /// Gets the container name in blob storage
    /// </summary>
    string ContainerName { get; }

    /// <summary>
    /// Gets the configuration options for this VFS instance
    /// </summary>
    VfsOptions Options { get; }

    // File Operations - ValueTask for cache-friendly operations

    /// <summary>
    /// Gets or creates a file reference (doesn't create actual blob until write)
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Virtual file instance</returns>
    ValueTask<IVirtualFile> GetFileAsync(VfsPath path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists (often cached for performance)
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file exists</returns>
    ValueTask<bool> FileExistsAsync(VfsPath path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file was deleted</returns>
    ValueTask<bool> DeleteFileAsync(VfsPath path, CancellationToken cancellationToken = default);

    // Directory Operations

    /// <summary>
    /// Gets or creates a directory reference (virtual - no actual blob created)
    /// </summary>
    /// <param name="path">Directory path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Virtual directory instance</returns>
    ValueTask<IVirtualDirectory> GetDirectoryAsync(VfsPath path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a directory exists (has any blobs with the path prefix)
    /// </summary>
    /// <param name="path">Directory path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the directory exists</returns>
    ValueTask<bool> DirectoryExistsAsync(VfsPath path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a directory and optionally all its contents
    /// </summary>
    /// <param name="path">Directory path</param>
    /// <param name="recursive">Whether to delete recursively</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete operation result</returns>
    Task<DeleteDirectoryResult> DeleteDirectoryAsync(
        VfsPath path,
        bool recursive = false,
        CancellationToken cancellationToken = default);

    // Common Operations - Task for always-async operations

    /// <summary>
    /// Moves/renames a file or directory
    /// </summary>
    /// <param name="source">Source path</param>
    /// <param name="destination">Destination path</param>
    /// <param name="options">Move options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task MoveAsync(
        VfsPath source,
        VfsPath destination,
        MoveOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file or directory
    /// </summary>
    /// <param name="source">Source path</param>
    /// <param name="destination">Destination path</param>
    /// <param name="options">Copy options</param>
    /// <param name="progress">Progress reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task CopyAsync(
        VfsPath source,
        VfsPath destination,
        CopyOptions? options = null,
        IProgress<CopyProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entry (file or directory) information
    /// </summary>
    /// <param name="path">Entry path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entry information or null if not found</returns>
    ValueTask<IVfsNode?> GetEntryAsync(VfsPath path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists directory contents with pagination
    /// </summary>
    /// <param name="path">Directory path</param>
    /// <param name="options">Listing options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of entries</returns>
    IAsyncEnumerable<IVfsNode> ListAsync(
        VfsPath path,
        ListOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Manager for multiple virtual file system mounts
/// </summary>
public interface IVirtualFileSystemManager : IAsyncDisposable
{
    /// <summary>
    /// Mounts a storage provider at the specified mount point
    /// </summary>
    /// <param name="mountPoint">Mount point path</param>
    /// <param name="storage">Storage provider</param>
    /// <param name="options">VFS options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task MountAsync(
        string mountPoint,
        IStorage storage,
        VfsOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unmounts a storage provider from the specified mount point
    /// </summary>
    /// <param name="mountPoint">Mount point path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UnmountAsync(string mountPoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the VFS instance for a mount point
    /// </summary>
    /// <param name="mountPoint">Mount point path</param>
    /// <returns>VFS instance</returns>
    IVirtualFileSystem GetMount(string mountPoint);

    /// <summary>
    /// Gets all current mounts
    /// </summary>
    /// <returns>Dictionary of mount points and their VFS instances</returns>
    IReadOnlyDictionary<string, IVirtualFileSystem> GetMounts();

    /// <summary>
    /// Resolves a path to a mount point and relative path
    /// </summary>
    /// <param name="path">Full path</param>
    /// <returns>Mount point and relative path</returns>
    (string MountPoint, VfsPath RelativePath) ResolvePath(string path);
}