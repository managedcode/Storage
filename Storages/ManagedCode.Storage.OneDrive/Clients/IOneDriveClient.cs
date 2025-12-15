using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph.Models;

namespace ManagedCode.Storage.OneDrive.Clients;

public interface IOneDriveClient
{
    Task EnsureRootAsync(string driveId, string rootPath, bool createIfNotExists, CancellationToken cancellationToken);

    Task<DriveItem> UploadAsync(string driveId, string path, Stream content, string? contentType, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string driveId, string path, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string driveId, string path, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string driveId, string path, CancellationToken cancellationToken);

    Task<DriveItem?> GetMetadataAsync(string driveId, string path, CancellationToken cancellationToken);

    IAsyncEnumerable<DriveItem> ListAsync(string driveId, string? directory, CancellationToken cancellationToken);
}
