using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ManagedCode.Storage.GoogleDrive.Clients;

public interface IGoogleDriveClient
{
    Task EnsureRootAsync(string rootFolderId, bool createIfNotExists, CancellationToken cancellationToken);

    Task<DriveFile> UploadAsync(string rootFolderId, string path, Stream content, string? contentType, bool supportsAllDrives, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string rootFolderId, string path, bool supportsAllDrives, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string rootFolderId, string path, bool supportsAllDrives, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string rootFolderId, string path, bool supportsAllDrives, CancellationToken cancellationToken);

    Task<DriveFile?> GetMetadataAsync(string rootFolderId, string path, bool supportsAllDrives, CancellationToken cancellationToken);

    IAsyncEnumerable<DriveFile> ListAsync(string rootFolderId, string? directory, bool supportsAllDrives, CancellationToken cancellationToken);
}
