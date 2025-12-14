using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.GoogleDrive.Clients;
using ManagedCode.Storage.GoogleDrive.Options;
using Microsoft.Extensions.Logging;
using File = Google.Apis.Drive.v3.Data.File;

namespace ManagedCode.Storage.GoogleDrive;

public class GoogleDriveStorage : BaseStorage<IGoogleDriveClient, GoogleDriveStorageOptions>, IGoogleDriveStorage
{
    private readonly ILogger<GoogleDriveStorage>? _logger;

    public GoogleDriveStorage(GoogleDriveStorageOptions storageOptions, ILogger<GoogleDriveStorage>? logger = null) : base(storageOptions)
    {
        _logger = logger;
    }

    protected override IGoogleDriveClient CreateStorageClient()
    {
        if (StorageOptions.Client != null)
        {
            return StorageOptions.Client;
        }

        if (StorageOptions.DriveService != null)
        {
            return new GoogleDriveClient(StorageOptions.DriveService);
        }

        throw new InvalidOperationException("DriveService client is not configured for Google Drive storage.");
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.EnsureRootAsync(StorageOptions.RootFolderId, StorageOptions.CreateContainerIfNotExists, cancellationToken);
            IsContainerCreated = true;
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    public override Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        // Root folder cleanup is not performed automatically; leave underlying Drive content intact.
        return Task.FromResult(Result.Succeed());
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var normalizedDirectory = NormalizeRelativePath(directory);

            await foreach (var item in StorageClient.ListAsync(StorageOptions.RootFolderId, normalizedDirectory, cancellationToken))
            {
                if (item.MimeType == "application/vnd.google-apps.folder")
                {
                    continue;
                }

                var path = string.IsNullOrWhiteSpace(normalizedDirectory) ? item.Name : $"{normalizedDirectory}/{item.Name}";
                await StorageClient.DeleteAsync(StorageOptions.RootFolderId, path!, cancellationToken);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var uploaded = await StorageClient.UploadAsync(StorageOptions.RootFolderId, path, stream, options.MimeType, cancellationToken);
            return Result<BlobMetadata>.Succeed(ToBlobMetadata(uploaded, path));
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var remoteStream = await StorageClient.DownloadAsync(StorageOptions.RootFolderId, path, cancellationToken);

            await using (remoteStream)
            await using (var fileStream = localFile.FileStream)
            {
                await remoteStream.CopyToAsync(fileStream, cancellationToken);
                fileStream.Position = 0;
            }

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var deleted = await StorageClient.DeleteAsync(StorageOptions.RootFolderId, path, cancellationToken);
            return Result<bool>.Succeed(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var exists = await StorageClient.ExistsAsync(StorageOptions.RootFolderId, path, cancellationToken);
            return Result<bool>.Succeed(exists);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var item = await StorageClient.GetMetadataAsync(StorageOptions.RootFolderId, path, cancellationToken);
            return item == null
                ? Result<BlobMetadata>.Fail(new FileNotFoundException($"File '{path}' not found in Google Drive."))
                : Result<BlobMetadata>.Succeed(ToBlobMetadata(item, path));
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist(cancellationToken);
        var normalizedDirectory = string.IsNullOrWhiteSpace(directory) ? null : NormalizeRelativePath(directory!);

        await foreach (var item in StorageClient.ListAsync(StorageOptions.RootFolderId, normalizedDirectory, cancellationToken))
        {
            if (item.MimeType == "application/vnd.google-apps.folder")
            {
                continue;
            }

            var fullPath = normalizedDirectory == null ? item.Name! : $"{normalizedDirectory}/{item.Name}";
            yield return ToBlobMetadata(item, fullPath);
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(fileName);
            var stream = await StorageClient.DownloadAsync(StorageOptions.RootFolderId, path, cancellationToken);
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    protected override Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    protected override Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Succeed(false));
    }

    private string BuildFullPath(string? relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath ?? string.Empty);
        return normalized;
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace("\\", "/").Trim('/');
    }

    private BlobMetadata ToBlobMetadata(File file, string fullName)
    {
        return new BlobMetadata
        {
            Name = file.Name ?? Path.GetFileName(fullName),
            FullName = fullName,
            Container = StorageOptions.RootFolderId,
            Uri = file.WebViewLink != null ? new Uri(file.WebViewLink) : null,
            CreatedOn = file.CreatedTimeDateTimeOffset ?? DateTimeOffset.UtcNow,
            LastModified = file.ModifiedTimeDateTimeOffset ?? DateTimeOffset.UtcNow,
            Length = (ulong)(file.Size ?? 0),
            MimeType = file.MimeType,
            Metadata = new Dictionary<string, string>
            {
                {"Id", file.Id ?? string.Empty},
                {"Md5", file.Md5Checksum ?? string.Empty}
            }
        };
    }
}
