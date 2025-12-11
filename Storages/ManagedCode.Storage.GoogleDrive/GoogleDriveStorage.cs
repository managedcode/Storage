using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.GoogleDrive.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using File = Google.Apis.Drive.v3.Data.File;

namespace ManagedCode.Storage.GoogleDrive;

/// <summary>
/// Google Drive storage implementation.
/// </summary>
public class GoogleDriveStorage : BaseStorage<DriveService, GoogleDriveStorageOptions>, IGoogleDriveStorage
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";
    private readonly ILogger<GoogleDriveStorage>? _logger;
    private string? _containerFolderId;

    public GoogleDriveStorage(GoogleDriveStorageOptions options, ILogger<GoogleDriveStorage>? logger = null)
        : base(options)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the current container folder ID (resolved from options or created).
    /// </summary>
    public string? ContainerFolderId => _containerFolderId;

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_containerFolderId))
            {
                return Result.Succeed();
            }

            var request = StorageClient.Files.Delete(_containerFolderId);
            request.SupportsAllDrives = true;

            await request.ExecuteAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _containerFolderId = null;
            IsContainerCreated = false;
            return Result.Succeed();
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _containerFolderId = null;
            IsContainerCreated = false;
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(
        string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist(cancellationToken);

        var parentFolderId = _containerFolderId;

        // If a directory is specified, find or use it
        if (!string.IsNullOrEmpty(directory))
        {
            var dirId = await FindFolderIdAsync(directory, _containerFolderId, cancellationToken);
            if (dirId != null)
            {
                parentFolderId = dirId;
            }
            else
            {
                yield break; // Directory doesn't exist
            }
        }

        string? pageToken = null;
        do
        {
            var listRequest = StorageClient.Files.List();
            listRequest.Q = $"'{parentFolderId ?? "root"}' in parents and trashed = false and mimeType != '{FolderMimeType}'";
            listRequest.Fields = "nextPageToken, files(id, name, mimeType, size, createdTime, modifiedTime, webViewLink, webContentLink, parents)";
            listRequest.PageToken = pageToken;

            listRequest.SupportsAllDrives = true;
            listRequest.IncludeItemsFromAllDrives = true;

            FileList fileList;
            try
            {
                fileList = await listRequest.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
                yield break;
            }

            if (fileList.Files == null)
            {
                yield break;
            }

            foreach (var file in fileList.Files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                yield return MapToBlobMetadata(file, directory);
            }

            pageToken = fileList.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);

            var fileId = await FindFileIdAsync(fileName, _containerFolderId, cancellationToken);
            if (fileId == null)
            {
                return Result<Stream>.Fail($"File '{fileName}' not found");
            }

            var request = StorageClient.Files.Get(fileId);
            request.SupportsAllDrives = true;

            var stream = new MemoryStream();
            await request.DownloadAsync(stream, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            stream.Position = 0;
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    protected override DriveService CreateStorageClient()
    {
        var credential = GetCredential();

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = StorageOptions.ApplicationName
        });
    }

    protected override Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsContainerCreated && !string.IsNullOrEmpty(_containerFolderId))
            {
                return Task.FromResult(Result.Succeed());
            }

            if (string.IsNullOrEmpty(StorageOptions.FolderId))
            {
                return Task.FromResult(Result.Fail("FolderId is required"));
            }

            _containerFolderId = StorageOptions.FolderId;
            return Task.FromResult(Result.Succeed());
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Task.FromResult(Result.Fail(ex));
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);

            var folderId = await FindFolderIdAsync(directory, _containerFolderId, cancellationToken);
            if (folderId == null)
            {
                return Result.Succeed(); // Directory doesn't exist
            }

            // Try to trash the folder first (works with lower permissions)
            try
            {
                var trashUpdate = new File { Trashed = true };
                var updateRequest = StorageClient.Files.Update(trashUpdate, folderId);
                updateRequest.SupportsAllDrives = true;
                await updateRequest.ExecuteAsync(cancellationToken);
                return Result.Succeed();
            }
            catch (GoogleApiException trashEx)
            {
                _logger?.LogWarning("Trashing folder failed, attempting permanent delete: {Message}", trashEx.Message);
                
                // Fall back to permanent delete
                var deleteRequest = StorageClient.Files.Delete(folderId);
                deleteRequest.SupportsAllDrives = true;
                await deleteRequest.ExecuteAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return Result.Succeed();
            }
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(
        Stream stream,
        UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var parentFolderId = _containerFolderId;

            // Handle directory in the path
            if (!string.IsNullOrEmpty(options.Directory))
            {
                parentFolderId = await GetOrCreateDirectoryPathAsync(options.Directory, _containerFolderId, cancellationToken);
            }

            var fileMetadata = new File
            {
                Name = options.FileName,
                MimeType = options.MimeType,
                Parents = parentFolderId != null ? new List<string> { parentFolderId } : null
            };

            var createRequest = StorageClient.Files.Create(fileMetadata, stream, options.MimeType ?? "application/octet-stream");
            createRequest.Fields = "id, name, mimeType, size, createdTime, modifiedTime, webViewLink, webContentLink, parents";
            createRequest.SupportsAllDrives = true;

            var uploadProgress = await createRequest.UploadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (uploadProgress.Status != UploadStatus.Completed)
            {
                var errorMessage = uploadProgress.Exception?.Message ?? "Upload failed";
                _logger?.LogError("Google Drive upload failed: {Error}", errorMessage);
                return Result<BlobMetadata>.Fail(errorMessage);
            }

            var uploadedFile = createRequest.ResponseBody;
            var metadataOptions = MetadataOptions.FromBaseOptions(options);

            return await GetBlobMetadataInternalAsync(metadataOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(
        LocalFile localFile,
        DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var parentFolderId = _containerFolderId;
            if (!string.IsNullOrEmpty(options.Directory))
            {
                parentFolderId = await FindFolderIdAsync(options.Directory, _containerFolderId, cancellationToken);
            }

            var fileId = await FindFileIdAsync(options.FileName, parentFolderId, cancellationToken);
            if (fileId == null)
            {
                return Result<LocalFile>.Fail($"File '{options.FullPath}' not found");
            }

            var request = StorageClient.Files.Get(fileId);
            request.SupportsAllDrives = true;

            await request.DownloadAsync(localFile.FileStream, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // Get file metadata
            var metadataRequest = StorageClient.Files.Get(fileId);
            metadataRequest.Fields = "id, name, mimeType, size, createdTime, modifiedTime, webViewLink, webContentLink";
            metadataRequest.SupportsAllDrives = true;

            var file = await metadataRequest.ExecuteAsync(cancellationToken);
            localFile.BlobMetadata = MapToBlobMetadata(file, options.Directory);

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<LocalFile>.Fail($"File '{options.FullPath}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(
        DeleteOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var parentFolderId = _containerFolderId;
            if (!string.IsNullOrEmpty(options.Directory))
            {
                parentFolderId = await FindFolderIdAsync(options.Directory, _containerFolderId, cancellationToken);
            }

            var fileId = await FindFileIdAsync(options.FileName, parentFolderId, cancellationToken);
            if (fileId == null)
            {
                return Result<bool>.Succeed(false);
            }

            // Try to trash the file first (works with lower permissions)
            // For Shared Drives, permanent delete requires organizer role
            try
            {
                var trashUpdate = new File { Trashed = true };
                var updateRequest = StorageClient.Files.Update(trashUpdate, fileId);
                updateRequest.SupportsAllDrives = true;
                await updateRequest.ExecuteAsync(cancellationToken);
                return Result<bool>.Succeed(true);
            }
            catch (GoogleApiException trashEx)
            {
                _logger?.LogWarning("Trashing file failed, attempting permanent delete: {Message}", trashEx.Message);
                
                // Fall back to permanent delete
                var deleteRequest = StorageClient.Files.Delete(fileId);
                deleteRequest.SupportsAllDrives = true;
                await deleteRequest.ExecuteAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return Result<bool>.Succeed(true);
            }
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger?.LogWarning("File not found during delete: {Message}", ex.Message);
            return Result<bool>.Succeed(false);
        }
        catch (GoogleApiException ex)
        {
            _logger?.LogError("Google API error during delete: {Status} - {Message}", ex.HttpStatusCode, ex.Message);
            return Result<bool>.Fail(ex);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(
        ExistOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var parentFolderId = _containerFolderId;
            if (!string.IsNullOrEmpty(options.Directory))
            {
                parentFolderId = await FindFolderIdAsync(options.Directory, _containerFolderId, cancellationToken);
                if (parentFolderId == null)
                {
                    return Result<bool>.Succeed(false);
                }
            }

            var fileId = await FindFileIdAsync(options.FileName, parentFolderId, cancellationToken);
            return Result<bool>.Succeed(fileId != null);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(
        MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var parentFolderId = _containerFolderId;
            if (!string.IsNullOrEmpty(options.Directory))
            {
                parentFolderId = await FindFolderIdAsync(options.Directory, _containerFolderId, cancellationToken);
            }

            var fileId = await FindFileIdAsync(options.FileName, parentFolderId, cancellationToken);
            if (fileId == null)
            {
                return Result<BlobMetadata>.Fail($"File '{options.FullPath}' not found");
            }

            var request = StorageClient.Files.Get(fileId);
            request.Fields = "id, name, mimeType, size, createdTime, modifiedTime, webViewLink, webContentLink, parents";
            request.SupportsAllDrives = true;

            var file = await request.ExecuteAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return Result<BlobMetadata>.Succeed(MapToBlobMetadata(file, options.Directory));
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<BlobMetadata>.Fail($"File '{options.FullPath}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override Task<Result> SetLegalHoldInternalAsync(
        bool hasLegalHold,
        LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        // Google Drive doesn't have a direct equivalent to legal hold
        // This could be implemented using file restrictions or labels
        _logger?.LogWarning("Legal hold is not directly supported in Google Drive");
        return Task.FromResult(Result.Fail("Legal hold is not supported in Google Drive"));
    }

    protected override Task<Result<bool>> HasLegalHoldInternalAsync(
        LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        // Google Drive doesn't have a direct equivalent to legal hold
        _logger?.LogWarning("Legal hold is not directly supported in Google Drive");
        return Task.FromResult(Result<bool>.Fail("Legal hold is not supported in Google Drive"));
    }

    #region Private Helper Methods

    private ICredential GetCredential()
    {
        if (StorageOptions.Credential != null)
        {
            return StorageOptions.Credential;
        }

        GoogleCredential googleCredential;

        if (!string.IsNullOrEmpty(StorageOptions.ServiceAccountJson))
        {
            googleCredential = GoogleCredential.FromJson(StorageOptions.ServiceAccountJson);
        }
        else if (!string.IsNullOrEmpty(StorageOptions.ServiceAccountJsonPath))
        {
            googleCredential = GoogleCredential.FromFile(StorageOptions.ServiceAccountJsonPath);
        }
        else
        {
            throw new InvalidOperationException(
                "No credentials provided. Set Credential, ServiceAccountJson, or ServiceAccountJsonPath in options.");
        }

        return googleCredential.CreateScoped(DriveService.Scope.Drive);
    }

    private async Task<string?> FindFileIdAsync(string fileName, string? parentFolderId, CancellationToken cancellationToken)
    {
        var listRequest = StorageClient.Files.List();
        var parentQuery = parentFolderId != null ? $"'{parentFolderId}' in parents" : "'root' in parents";
        listRequest.Q = $"{parentQuery} and name = '{EscapeQueryString(fileName)}' and trashed = false and mimeType != '{FolderMimeType}'";
        listRequest.Fields = "files(id)";
        listRequest.SupportsAllDrives = true;
        listRequest.IncludeItemsFromAllDrives = true;

        var result = await listRequest.ExecuteAsync(cancellationToken);
        return result.Files?.FirstOrDefault()?.Id;
    }

    private async Task<string?> FindFolderIdAsync(string folderPath, string? parentFolderId, CancellationToken cancellationToken)
    {
        var parts = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var currentParentId = parentFolderId;

        foreach (var part in parts)
        {
            var listRequest = StorageClient.Files.List();
            var parentQuery = currentParentId != null ? $"'{currentParentId}' in parents" : "'root' in parents";
            listRequest.Q = $"{parentQuery} and name = '{EscapeQueryString(part)}' and mimeType = '{FolderMimeType}' and trashed = false";
            listRequest.Fields = "files(id)";
            listRequest.SupportsAllDrives = true;
            listRequest.IncludeItemsFromAllDrives = true;

            var result = await listRequest.ExecuteAsync(cancellationToken);
            var folder = result.Files?.FirstOrDefault();

            if (folder == null)
            {
                return null;
            }

            currentParentId = folder.Id;
        }

        return currentParentId;
    }

    private async Task<string> CreateFolderAsync(string folderName, string? parentFolderId, CancellationToken cancellationToken)
    {
        var fileMetadata = new File
        {
            Name = folderName,
            MimeType = FolderMimeType,
            Parents = parentFolderId != null ? new List<string> { parentFolderId } : null
        };

        var request = StorageClient.Files.Create(fileMetadata);
        request.Fields = "id";
        request.SupportsAllDrives = true;

        var folder = await request.ExecuteAsync(cancellationToken);
        return folder.Id;
    }

    private async Task<string?> GetOrCreateDirectoryPathAsync(string directoryPath, string? parentFolderId, CancellationToken cancellationToken)
    {
        var parts = directoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var currentParentId = parentFolderId;

        foreach (var part in parts)
        {
            var existingFolderId = await FindFolderIdAsync(part, currentParentId, cancellationToken);

            if (existingFolderId != null)
            {
                currentParentId = existingFolderId;
            }
            else
            {
                currentParentId = await CreateFolderAsync(part, currentParentId, cancellationToken);
            }
        }

        return currentParentId;
    }

    private BlobMetadata MapToBlobMetadata(File file, string? directory)
    {
        var fullName = string.IsNullOrEmpty(directory)
            ? file.Name
            : $"{directory.TrimEnd('/', '\\')}/{file.Name}";

        Uri? uri = null;
        if (!string.IsNullOrEmpty(file.WebContentLink))
        {
            uri = new Uri(file.WebContentLink);
        }
        else if (!string.IsNullOrEmpty(file.WebViewLink))
        {
            uri = new Uri(file.WebViewLink);
        }

        return new BlobMetadata
        {
            FullName = fullName,
            Name = file.Name,
            Uri = uri,
            Container = StorageOptions.FolderId,
            Length = (ulong)(file.Size ?? 0),
            MimeType = file.MimeType,
            CreatedOn = file.CreatedTimeDateTimeOffset ?? DateTimeOffset.MinValue,
            LastModified = file.ModifiedTimeDateTimeOffset ?? DateTimeOffset.MinValue,
            Metadata = new Dictionary<string, string>
            {
                ["FileId"] = file.Id
            }
        };
    }

    private static string EscapeQueryString(string value)
    {
        return value.Replace("'", "\\'");
    }

    #endregion
}


