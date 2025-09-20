using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.VirtualFileSystem.Metadata;

/// <summary>
/// Interface for managing metadata on blob storage providers
/// </summary>
public interface IMetadataManager
{
    /// <summary>
    /// Sets VFS metadata on a blob
    /// </summary>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="metadata">VFS metadata to set</param>
    /// <param name="customMetadata">Additional custom metadata</param>
    /// <param name="expectedETag">Expected ETag for concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SetVfsMetadataAsync(
        string blobName,
        VfsMetadata metadata,
        IDictionary<string, string>? customMetadata = null,
        string? expectedETag = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets VFS metadata from a blob
    /// </summary>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VFS metadata or null if not found</returns>
    Task<VfsMetadata?> GetVfsMetadataAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets custom metadata from a blob
    /// </summary>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Custom metadata dictionary</returns>
    Task<IReadOnlyDictionary<string, string>> GetCustomMetadataAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists and gets its basic information
    /// </summary>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blob metadata or null if not found</returns>
    Task<BlobMetadata?> GetBlobInfoAsync(
        string blobName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// VFS-specific metadata for files and directories
/// </summary>
public class VfsMetadata
{
    /// <summary>
    /// VFS metadata version for compatibility
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// When the entry was created
    /// </summary>
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the entry was last modified
    /// </summary>
    public DateTimeOffset Modified { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// VFS entry attributes
    /// </summary>
    public VfsAttributes Attributes { get; set; } = VfsAttributes.None;

    /// <summary>
    /// Custom metadata specific to this entry
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
}

/// <summary>
/// VFS file/directory attributes
/// </summary>
[Flags]
public enum VfsAttributes
{
    /// <summary>
    /// No special attributes
    /// </summary>
    None = 0,

    /// <summary>
    /// Hidden entry
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// System entry
    /// </summary>
    System = 2,

    /// <summary>
    /// Read-only entry
    /// </summary>
    ReadOnly = 4,

    /// <summary>
    /// Archive entry
    /// </summary>
    Archive = 8,

    /// <summary>
    /// Temporary entry
    /// </summary>
    Temporary = 16,

    /// <summary>
    /// Compressed entry
    /// </summary>
    Compressed = 32
}

/// <summary>
/// Cache entry for metadata
/// </summary>
internal class MetadataCacheEntry
{
    public VfsMetadata Metadata { get; set; } = null!;
    public IReadOnlyDictionary<string, string> CustomMetadata { get; set; } = new Dictionary<string, string>();
    public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ETag { get; set; }
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public BlobMetadata? BlobMetadata { get; set; }
}

/// <summary>
/// Base implementation for metadata managers
/// </summary>
public abstract class BaseMetadataManager : IMetadataManager
{
    protected const string VFS_VERSION_KEY = "vfs-version";
    protected const string VFS_CREATED_KEY = "vfs-created";
    protected const string VFS_MODIFIED_KEY = "vfs-modified";
    protected const string VFS_ATTRIBUTES_KEY = "vfs-attributes";
    protected const string VFS_CUSTOM_PREFIX = "vfs-";

    protected abstract string MetadataPrefix { get; }

    public abstract Task SetVfsMetadataAsync(
        string blobName,
        VfsMetadata metadata,
        IDictionary<string, string>? customMetadata = null,
        string? expectedETag = null,
        CancellationToken cancellationToken = default);

    public abstract Task<VfsMetadata?> GetVfsMetadataAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyDictionary<string, string>> GetCustomMetadataAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    public abstract Task<BlobMetadata?> GetBlobInfoAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds metadata dictionary for storage
    /// </summary>
    protected Dictionary<string, string> BuildMetadataDictionary(
        VfsMetadata metadata,
        IDictionary<string, string>? customMetadata = null)
    {
        var dict = new Dictionary<string, string>
        {
            [$"{MetadataPrefix}{VFS_VERSION_KEY}"] = metadata.Version,
            [$"{MetadataPrefix}{VFS_CREATED_KEY}"] = metadata.Created.ToString("O"),
            [$"{MetadataPrefix}{VFS_MODIFIED_KEY}"] = metadata.Modified.ToString("O"),
            [$"{MetadataPrefix}{VFS_ATTRIBUTES_KEY}"] = ((int)metadata.Attributes).ToString()
        };

        // Add VFS custom metadata
        foreach (var kvp in metadata.CustomMetadata)
        {
            dict[$"{MetadataPrefix}{VFS_CUSTOM_PREFIX}{kvp.Key}"] = kvp.Value;
        }

        // Add additional custom metadata
        if (customMetadata != null)
        {
            foreach (var kvp in customMetadata)
            {
                if (!kvp.Key.StartsWith(MetadataPrefix))
                {
                    dict[$"{MetadataPrefix}{kvp.Key}"] = kvp.Value;
                }
                else
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }
        }

        return dict;
    }

    /// <summary>
    /// Parses VFS metadata from storage metadata
    /// </summary>
    protected VfsMetadata? ParseVfsMetadata(IDictionary<string, string> storageMetadata)
    {
        var versionKey = $"{MetadataPrefix}{VFS_VERSION_KEY}";
        if (!storageMetadata.TryGetValue(versionKey, out var version))
            return null; // Not VFS metadata

        var metadata = new VfsMetadata { Version = version };

        // Parse created date
        var createdKey = $"{MetadataPrefix}{VFS_CREATED_KEY}";
        if (storageMetadata.TryGetValue(createdKey, out var createdStr) &&
            DateTimeOffset.TryParse(createdStr, out var created))
        {
            metadata.Created = created;
        }

        // Parse modified date
        var modifiedKey = $"{MetadataPrefix}{VFS_MODIFIED_KEY}";
        if (storageMetadata.TryGetValue(modifiedKey, out var modifiedStr) &&
            DateTimeOffset.TryParse(modifiedStr, out var modified))
        {
            metadata.Modified = modified;
        }

        // Parse attributes
        var attributesKey = $"{MetadataPrefix}{VFS_ATTRIBUTES_KEY}";
        if (storageMetadata.TryGetValue(attributesKey, out var attributesStr) &&
            int.TryParse(attributesStr, out var attributes))
        {
            metadata.Attributes = (VfsAttributes)attributes;
        }

        // Parse custom metadata
        var customPrefix = $"{MetadataPrefix}{VFS_CUSTOM_PREFIX}";
        foreach (var kvp in storageMetadata)
        {
            if (kvp.Key.StartsWith(customPrefix))
            {
                var customKey = kvp.Key[customPrefix.Length..];
                metadata.CustomMetadata[customKey] = kvp.Value;
            }
        }

        return metadata;
    }

    /// <summary>
    /// Extracts custom metadata (non-VFS) from storage metadata
    /// </summary>
    protected Dictionary<string, string> ExtractCustomMetadata(IDictionary<string, string> storageMetadata)
    {
        var result = new Dictionary<string, string>();

        foreach (var kvp in storageMetadata)
        {
            if (kvp.Key.StartsWith(MetadataPrefix))
            {
                // Skip VFS system metadata
                if (kvp.Key.EndsWith(VFS_VERSION_KEY) ||
                    kvp.Key.EndsWith(VFS_CREATED_KEY) ||
                    kvp.Key.EndsWith(VFS_MODIFIED_KEY) ||
                    kvp.Key.EndsWith(VFS_ATTRIBUTES_KEY) ||
                    kvp.Key.Contains($"{VFS_CUSTOM_PREFIX}"))
                {
                    continue;
                }

                // Include other custom metadata
                var key = kvp.Key[MetadataPrefix.Length..];
                result[key] = kvp.Value;
            }
        }

        return result;
    }
}