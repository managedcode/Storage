using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ManagedCode.Storage.GoogleDrive.Clients;

public class GoogleDriveClient : IGoogleDriveClient
{
    private readonly DriveService _driveService;

    public GoogleDriveClient(DriveService driveService)
    {
        _driveService = driveService ?? throw new ArgumentNullException(nameof(driveService));
    }

    public Task EnsureRootAsync(string rootFolderId, bool createIfNotExists, CancellationToken cancellationToken)
    {
        // Google Drive root exists by default when using "root". Additional folder tree is created on demand in UploadAsync.
        return Task.CompletedTask;
    }

    public async Task<DriveFile> UploadAsync(string rootFolderId, string path, Stream content, string? contentType, CancellationToken cancellationToken)
    {
        var (parentId, fileName) = await EnsureParentFolderAsync(rootFolderId, path, cancellationToken);

        var fileMetadata = new DriveFile
        {
            Name = fileName,
            Parents = new List<string> { parentId }
        };

        var request = _driveService.Files.Create(fileMetadata, content, contentType ?? "application/octet-stream");
        request.Fields = "id,name,parents,createdTime,modifiedTime,md5Checksum,size";
        return await request.UploadAsync(cancellationToken).ContinueWith(async _ => await _driveService.Files.Get(request.ResponseBody.Id).ExecuteAsync(cancellationToken)).Unwrap();
    }

    public async Task<Stream> DownloadAsync(string rootFolderId, string path, CancellationToken cancellationToken)
    {
        var file = await FindFileByPathAsync(rootFolderId, path, cancellationToken) ?? throw new FileNotFoundException(path);
        var stream = new MemoryStream();
        await _driveService.Files.Get(file.Id).DownloadAsync(stream, cancellationToken);
        stream.Position = 0;
        return stream;
    }

    public async Task<bool> DeleteAsync(string rootFolderId, string path, CancellationToken cancellationToken)
    {
        var file = await FindFileByPathAsync(rootFolderId, path, cancellationToken);
        if (file == null)
        {
            return false;
        }

        await _driveService.Files.Delete(file.Id).ExecuteAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(string rootFolderId, string path, CancellationToken cancellationToken)
    {
        return await FindFileByPathAsync(rootFolderId, path, cancellationToken) != null;
    }

    public Task<DriveFile?> GetMetadataAsync(string rootFolderId, string path, CancellationToken cancellationToken)
    {
        return FindFileByPathAsync(rootFolderId, path, cancellationToken);
    }

    public async IAsyncEnumerable<DriveFile> ListAsync(string rootFolderId, string? directory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var parentId = string.IsNullOrWhiteSpace(directory)
            ? rootFolderId
            : await EnsureFolderPathAsync(rootFolderId, directory!, false, cancellationToken) ?? rootFolderId;

        var request = _driveService.Files.List();
        request.Q = $"'{parentId}' in parents and trashed=false";
        request.Fields = "files(id,name,parents,createdTime,modifiedTime,md5Checksum,size,mimeType)";

        do
        {
            var response = await request.ExecuteAsync(cancellationToken);
            foreach (var file in response.Files ?? Enumerable.Empty<DriveFile>())
            {
                yield return file;
            }

            request.PageToken = response.NextPageToken;
        } while (!string.IsNullOrEmpty(request.PageToken) && !cancellationToken.IsCancellationRequested);
    }

    private async Task<(string ParentId, string Name)> EnsureParentFolderAsync(string rootFolderId, string fullPath, CancellationToken cancellationToken)
    {
        var normalizedPath = fullPath.Replace("\\", "/").Trim('/');
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return (rootFolderId, Guid.NewGuid().ToString("N"));
        }

        var parentPath = string.Join('/', segments.Take(segments.Length - 1));
        var parentId = await EnsureFolderPathAsync(rootFolderId, parentPath, true, cancellationToken) ?? rootFolderId;
        return (parentId, segments.Last());
    }

    private async Task<string?> EnsureFolderPathAsync(string rootFolderId, string path, bool createIfMissing, CancellationToken cancellationToken)
    {
        var currentId = rootFolderId;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var folder = await FindChildAsync(currentId, segment, cancellationToken);
            if (folder == null)
            {
                if (!createIfMissing)
                {
                    return null;
                }

                var metadata = new DriveFile { Name = segment, MimeType = "application/vnd.google-apps.folder", Parents = new List<string> { currentId } };
                folder = await _driveService.Files.Create(metadata).ExecuteAsync(cancellationToken);
            }

            currentId = folder.Id;
        }

        return currentId;
    }

    private async Task<DriveFile?> FindChildAsync(string parentId, string name, CancellationToken cancellationToken)
    {
        var request = _driveService.Files.List();
        request.Q = $"'{parentId}' in parents and name='{name}' and trashed=false";
        request.Fields = "files(id,name,parents,createdTime,modifiedTime,md5Checksum,size,mimeType)";
        var response = await request.ExecuteAsync(cancellationToken);
        return response.Files?.FirstOrDefault();
    }

    private async Task<DriveFile?> FindFileByPathAsync(string rootFolderId, string path, CancellationToken cancellationToken)
    {
        var normalizedPath = path.Replace("\\", "/").Trim('/');
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        var parentPath = string.Join('/', segments.Take(segments.Length - 1));
        var fileName = segments.Last();
        var parentId = await EnsureFolderPathAsync(rootFolderId, parentPath, false, cancellationToken);
        if (parentId == null)
        {
            return null;
        }

        return await FindChildAsync(parentId, fileName, cancellationToken);
    }
}
