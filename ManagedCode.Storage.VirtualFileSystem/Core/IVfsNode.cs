using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.VirtualFileSystem.Core;

namespace ManagedCode.Storage.VirtualFileSystem.Core;

/// <summary>
/// Base interface for virtual file system nodes
/// </summary>
public interface IVfsNode
{
    /// <summary>
    /// Gets the path of this node
    /// </summary>
    VfsPath Path { get; }

    /// <summary>
    /// Gets the name of this node
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type of this node
    /// </summary>
    VfsEntryType Type { get; }

    /// <summary>
    /// Gets when this node was created
    /// </summary>
    DateTimeOffset CreatedOn { get; }

    /// <summary>
    /// Gets when this node was last modified
    /// </summary>
    DateTimeOffset LastModified { get; }

    /// <summary>
    /// Checks if this node exists
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the entry exists</returns>
    ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the node information from storage
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the parent directory of this node
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The parent directory</returns>
    ValueTask<IVirtualDirectory> GetParentAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Type of virtual file system entry
/// </summary>
public enum VfsEntryType
{
    /// <summary>
    /// A file entry
    /// </summary>
    File,

    /// <summary>
    /// A directory entry
    /// </summary>
    Directory
}

/// <summary>
/// Progress information for copy operations
/// </summary>
public class CopyProgress
{
    /// <summary>
    /// Total number of bytes to copy
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Number of bytes copied so far
    /// </summary>
    public long CopiedBytes { get; set; }

    /// <summary>
    /// Total number of files to copy
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Number of files copied so far
    /// </summary>
    public int CopiedFiles { get; set; }

    /// <summary>
    /// Current file being copied
    /// </summary>
    public string? CurrentFile { get; set; }

    /// <summary>
    /// Percentage completed (0-100)
    /// </summary>
    public double PercentageComplete => TotalBytes > 0 ? (double)CopiedBytes / TotalBytes * 100 : 0;
}

/// <summary>
/// Result of a delete directory operation
/// </summary>
public class DeleteDirectoryResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of files deleted
    /// </summary>
    public int FilesDeleted { get; set; }

    /// <summary>
    /// Number of directories deleted
    /// </summary>
    public int DirectoriesDeleted { get; set; }

    /// <summary>
    /// List of errors encountered during deletion
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
