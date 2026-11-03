using System;
using System.IO;

namespace ManagedCode.Storage.Server.ChunkUpload;

/// <summary>
/// Options controlling how chunked uploads are persisted while all parts arrive.
/// </summary>
public class ChunkUploadOptions
{
    /// <summary>
    /// Absolute path where temporary chunk data is persisted. Defaults to <see cref="Path.GetTempPath"/>.
    /// </summary>
    public string TempPath { get; set; } = Path.Combine(Path.GetTempPath(), "managedcode-storage", "chunks");

    /// <summary>
    /// Idle time after which an upload session expires. Append and completion requests clean up expired sessions.
    /// A nonpositive value disables expiration.
    /// </summary>
    public TimeSpan SessionTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum number of concurrent active chunk sessions cached in memory.
    /// </summary>
    public int MaxActiveSessions { get; set; } = 100;
}
