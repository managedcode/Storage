using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.VirtualFileSystem.Options;

namespace ManagedCode.Storage.VirtualFileSystem.Core;

/// <summary>
/// Represents a directory in the virtual filesystem
/// </summary>
public interface IVirtualDirectory : IVfsNode
{
    /// <summary>
    /// Lists files in this directory with pagination and pattern matching
    /// </summary>
    /// <param name="pattern">Search pattern for filtering</param>
    /// <param name="recursive">Whether to search recursively</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of files</returns>
    IAsyncEnumerable<IVirtualFile> GetFilesAsync(
        SearchPattern? pattern = null,
        bool recursive = false,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists subdirectories with pagination
    /// </summary>
    /// <param name="pattern">Search pattern for filtering</param>
    /// <param name="recursive">Whether to search recursively</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of directories</returns>
    IAsyncEnumerable<IVirtualDirectory> GetDirectoriesAsync(
        SearchPattern? pattern = null,
        bool recursive = false,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all entries (files and directories) in this directory
    /// </summary>
    /// <param name="pattern">Search pattern for filtering</param>
    /// <param name="recursive">Whether to search recursively</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of entries</returns>
    IAsyncEnumerable<IVfsNode> GetEntriesAsync(
        SearchPattern? pattern = null,
        bool recursive = false,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a file in this directory
    /// </summary>
    /// <param name="name">File name</param>
    /// <param name="options">File creation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created file</returns>
    ValueTask<IVirtualFile> CreateFileAsync(
        string name,
        CreateFileOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a subdirectory
    /// </summary>
    /// <param name="name">Directory name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created directory</returns>
    ValueTask<IVirtualDirectory> CreateDirectoryAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for this directory
    /// </summary>
    /// <param name="recursive">Whether to calculate recursively</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Directory statistics</returns>
    Task<DirectoryStats> GetStatsAsync(
        bool recursive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes this directory
    /// </summary>
    /// <param name="recursive">Whether to delete recursively</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete operation result</returns>
    Task<DeleteDirectoryResult> DeleteAsync(
        bool recursive = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for a directory
/// </summary>
public class DirectoryStats
{
    /// <summary>
    /// Number of files in the directory
    /// </summary>
    public int FileCount { get; init; }

    /// <summary>
    /// Number of subdirectories
    /// </summary>
    public int DirectoryCount { get; init; }

    /// <summary>
    /// Total size of all files in bytes
    /// </summary>
    public long TotalSize { get; init; }

    /// <summary>
    /// File count by extension
    /// </summary>
    public Dictionary<string, int> FilesByExtension { get; init; } = new();

    /// <summary>
    /// The largest file in the directory
    /// </summary>
    public IVirtualFile? LargestFile { get; init; }

    /// <summary>
    /// Oldest modification date
    /// </summary>
    public DateTimeOffset? OldestModified { get; init; }

    /// <summary>
    /// Newest modification date
    /// </summary>
    public DateTimeOffset? NewestModified { get; init; }
}