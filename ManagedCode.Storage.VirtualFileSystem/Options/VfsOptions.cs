using System;
using System.Collections.Generic;

namespace ManagedCode.Storage.VirtualFileSystem.Options;

/// <summary>
/// Configuration options for Virtual File System
/// </summary>
public class VfsOptions
{
    /// <summary>
    /// Default container name for blob storage
    /// </summary>
    public string DefaultContainer { get; set; } = "vfs";

    /// <summary>
    /// Strategy for handling directories
    /// </summary>
    public DirectoryStrategy DirectoryStrategy { get; set; } = DirectoryStrategy.Virtual;

    /// <summary>
    /// Enable metadata caching for performance
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// Cache time-to-live for metadata
    /// </summary>
    public TimeSpan CacheTTL { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of cache entries
    /// </summary>
    public int MaxCacheEntries { get; set; } = 10000;

    /// <summary>
    /// Default page size for directory listings
    /// </summary>
    public int DefaultPageSize { get; set; } = 100;

    /// <summary>
    /// Maximum concurrent operations
    /// </summary>
    public int MaxConcurrency { get; set; } = 100;

    /// <summary>
    /// Threshold for multipart upload (bytes)
    /// </summary>
    public long MultipartThreshold { get; set; } = 104857600; // 100MB
}

/// <summary>
/// Options for write operations with concurrency control
/// </summary>
public class WriteOptions
{
    /// <summary>
    /// Expected ETag for optimistic concurrency control
    /// </summary>
    public string? ExpectedETag { get; set; }

    /// <summary>
    /// Whether to overwrite if the file exists
    /// </summary>
    public bool Overwrite { get; set; } = true;

    /// <summary>
    /// Content type to set on the blob
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Custom metadata to add to the blob
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Streaming options for large files
/// </summary>
public class StreamOptions
{
    /// <summary>
    /// Buffer size for streaming operations (default: 81920 bytes)
    /// </summary>
    public int BufferSize { get; set; } = 81920;

    /// <summary>
    /// Range start for partial reads
    /// </summary>
    public long? RangeStart { get; set; }

    /// <summary>
    /// Range end for partial reads
    /// </summary>
    public long? RangeEnd { get; set; }

    /// <summary>
    /// Use async I/O for better performance
    /// </summary>
    public bool UseAsyncIO { get; set; } = true;
}

/// <summary>
/// Options for listing directory contents
/// </summary>
public class ListOptions
{
    /// <summary>
    /// Search pattern for filtering entries
    /// </summary>
    public SearchPattern? Pattern { get; set; }

    /// <summary>
    /// Whether to list recursively
    /// </summary>
    public bool Recursive { get; set; } = false;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 100;

    /// <summary>
    /// Include files in the results
    /// </summary>
    public bool IncludeFiles { get; set; } = true;

    /// <summary>
    /// Include directories in the results
    /// </summary>
    public bool IncludeDirectories { get; set; } = true;
}

/// <summary>
/// Options for move operations
/// </summary>
public class MoveOptions
{
    /// <summary>
    /// Whether to overwrite the destination if it exists
    /// </summary>
    public bool Overwrite { get; set; } = false;

    /// <summary>
    /// Whether to preserve metadata during the move
    /// </summary>
    public bool PreserveMetadata { get; set; } = true;
}

/// <summary>
/// Options for copy operations
/// </summary>
public class CopyOptions
{
    /// <summary>
    /// Whether to overwrite the destination if it exists
    /// </summary>
    public bool Overwrite { get; set; } = false;

    /// <summary>
    /// Whether to preserve metadata during the copy
    /// </summary>
    public bool PreserveMetadata { get; set; } = true;

    /// <summary>
    /// Whether to copy recursively for directories
    /// </summary>
    public bool Recursive { get; set; } = true;
}

/// <summary>
/// Options for creating files
/// </summary>
public class CreateFileOptions
{
    /// <summary>
    /// Content type to set on the file
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Initial metadata for the file
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Whether to overwrite if the file already exists
    /// </summary>
    public bool Overwrite { get; set; } = false;
}

/// <summary>
/// Strategy for handling empty directories
/// </summary>
public enum DirectoryStrategy
{
    /// <summary>
    /// Directories exist only if they contain files (virtual)
    /// </summary>
    Virtual,

    /// <summary>
    /// Create zero-byte blob with trailing slash for empty directories
    /// </summary>
    ZeroByteMarker,

    /// <summary>
    /// Create .keep file like git for empty directories
    /// </summary>
    DotKeepFile
}

/// <summary>
/// Search pattern for filtering entries
/// </summary>
public class SearchPattern
{
    /// <summary>
    /// Initializes a new instance of SearchPattern
    /// </summary>
    /// <param name="pattern">The pattern string (supports * and ? wildcards)</param>
    public SearchPattern(string pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    }

    /// <summary>
    /// The pattern string
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Whether the pattern is case sensitive
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Checks if a name matches this pattern
    /// </summary>
    /// <param name="name">The name to check</param>
    /// <returns>True if the name matches the pattern</returns>
    public bool IsMatch(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return IsWildcardMatch(Pattern, name, comparison);
    }

    private static bool IsWildcardMatch(string pattern, string input, StringComparison comparison)
    {
        int patternIndex = 0;
        int inputIndex = 0;
        int starIndex = -1;
        int match = 0;

        while (inputIndex < input.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' ||
                string.Equals(pattern[patternIndex].ToString(), input[inputIndex].ToString(), comparison)))
            {
                patternIndex++;
                inputIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex;
                match = inputIndex;
                patternIndex++;
            }
            else if (starIndex != -1)
            {
                patternIndex = starIndex + 1;
                match++;
                inputIndex = match;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }
}