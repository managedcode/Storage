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
using ManagedCode.Storage.OneDrive.Clients;
using ManagedCode.Storage.OneDrive.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace ManagedCode.Storage.OneDrive;

public class OneDriveStorage : BaseStorage<IOneDriveClient, OneDriveStorageOptions>, IOneDriveStorage
{
    private readonly ILogger<OneDriveStorage>? _logger;

    public OneDriveStorage(OneDriveStorageOptions storageOptions, ILogger<OneDriveStorage>? logger = null) : base(storageOptions)
    {
        _logger = logger;
    }

    protected override IOneDriveClient CreateStorageClient()
    {
        if (StorageOptions.Client != null)
        {
            return StorageOptions.Client;
        }

        if (StorageOptions.GraphClient != null)
        {
            return new GraphOneDriveClient(StorageOptions.GraphClient);
        }

        throw new InvalidOperationException("Graph client is not configured for OneDrive storage.");
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.EnsureRootAsync(StorageOptions.DriveId, StorageOptions.RootPath, StorageOptions.CreateContainerIfNotExists, cancellationToken);
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
        // OneDrive containers map to drives or root folders that are typically managed by the account owner.
        return Task.FromResult(Result.Succeed());
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var normalizedDirectory = NormalizeRelativePath(directory);

            await foreach (var item in StorageClient.ListAsync(StorageOptions.DriveId, normalizedDirectory, cancellationToken))
            {
                if (item?.Folder != null)
                {
                    continue;
                }

                var path = $"{normalizedDirectory}/{item!.Name}".Trim('/');
                await StorageClient.DeleteAsync(StorageOptions.DriveId, path, cancellationToken);
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
            var uploaded = await StorageClient.UploadAsync(StorageOptions.DriveId, path, stream, options.MimeType, cancellationToken);
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
            var remoteStream = await StorageClient.DownloadAsync(StorageOptions.DriveId, path, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

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
            var deleted = await StorageClient.DeleteAsync(StorageOptions.DriveId, path, cancellationToken);
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
            var exists = await StorageClient.ExistsAsync(StorageOptions.DriveId, path, cancellationToken);
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
            var item = await StorageClient.GetMetadataAsync(StorageOptions.DriveId, path, cancellationToken);
            return item == null
                ? Result<BlobMetadata>.Fail(new FileNotFoundException($"File '{path}' not found in OneDrive."))
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
        await foreach (var item in StorageClient.ListAsync(StorageOptions.DriveId, normalizedDirectory, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item == null || item.Folder != null)
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
            var stream = await StorageClient.DownloadAsync(StorageOptions.DriveId, path, cancellationToken);
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
        // OneDrive does not expose legal hold controls through the Graph SDK used here.
        return Task.FromResult(Result.Succeed());
    }

    protected override Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        // OneDrive does not expose legal hold controls through the Graph SDK used here.
        return Task.FromResult(Result<bool>.Succeed(false));
    }

    private string BuildFullPath(string? relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath ?? string.Empty);
        var root = NormalizeRelativePath(StorageOptions.RootPath);
        return string.IsNullOrWhiteSpace(root) ? normalized : string.IsNullOrWhiteSpace(normalized) ? root : $"{root}/{normalized}";
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace("\\", "/").Trim('/');
    }

    private BlobMetadata ToBlobMetadata(DriveItem item, string fullName)
    {
        return new BlobMetadata
        {
            Name = item.Name ?? Path.GetFileName(fullName),
            FullName = fullName,
            Container = StorageOptions.DriveId,
            Uri = item.WebUrl != null ? new Uri(item.WebUrl) : null,
            CreatedOn = item.CreatedDateTime ?? DateTimeOffset.UtcNow,
            LastModified = item.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
            Length = (ulong)(item.Size ?? 0),
            MimeType = item.File?.MimeType,
            Metadata = item.AdditionalData?.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? string.Empty) ?? new Dictionary<string, string>()
        };
    }
}
