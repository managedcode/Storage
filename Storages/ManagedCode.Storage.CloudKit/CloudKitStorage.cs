using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.CloudKit.Clients;
using ManagedCode.Storage.CloudKit.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.CloudKit;

public class CloudKitStorage : BaseStorage<ICloudKitClient, CloudKitStorageOptions>, ICloudKitStorage
{
    private readonly ILogger<CloudKitStorage>? _logger;

    public CloudKitStorage(CloudKitStorageOptions storageOptions, ILogger<CloudKitStorage>? logger = null) : base(storageOptions)
    {
        _logger = logger;
    }

    protected override ICloudKitClient CreateStorageClient()
    {
        if (StorageOptions.Client != null)
        {
            return StorageOptions.Client;
        }

        return new CloudKitClient(StorageOptions, StorageOptions.HttpClient);
    }

    protected override Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        IsContainerCreated = true;
        return Task.FromResult(Result.Succeed());
    }

    public override Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Fail(new NotSupportedException("Deleting a CloudKit container is not supported.")));
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var prefix = BuildDirectoryPrefix(directory);

            await foreach (var record in StorageClient.QueryByPathPrefixAsync(prefix, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _ = await StorageClient.DeleteAsync(record.RecordName, cancellationToken);
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
            cancellationToken.ThrowIfCancellationRequested();

            var fullName = NormalizeRelativePath(options.FullPath);
            var internalPath = BuildInternalPath(fullName);
            var recordName = CreateRecordName(internalPath);

            var record = await StorageClient.UploadAsync(recordName, internalPath, stream, options.MimeType ?? MimeHelper.GetMimeType(options.FileName), cancellationToken);
            return Result<BlobMetadata>.Succeed(ToBlobMetadata(record, StripRoot(record.Path)));
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
            var recordName = CreateRecordName(BuildInternalPath(NormalizeRelativePath(options.FullPath)));

            var remoteStream = await StorageClient.DownloadAsync(recordName, cancellationToken);
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
            var recordName = CreateRecordName(BuildInternalPath(NormalizeRelativePath(options.FullPath)));
            var deleted = await StorageClient.DeleteAsync(recordName, cancellationToken);
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
            var recordName = CreateRecordName(BuildInternalPath(NormalizeRelativePath(options.FullPath)));
            var exists = await StorageClient.ExistsAsync(recordName, cancellationToken);
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
            var fullName = NormalizeRelativePath(options.FullPath);
            var recordName = CreateRecordName(BuildInternalPath(fullName));
            var record = await StorageClient.GetRecordAsync(recordName, cancellationToken);
            if (record == null)
            {
                return Result<BlobMetadata>.Fail(new FileNotFoundException($"CloudKit record '{fullName}' not found."));
            }

            return Result<BlobMetadata>.Succeed(ToBlobMetadata(record, StripRoot(record.Path)));
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
        var prefix = BuildDirectoryPrefix(directory);

        await foreach (var record in StorageClient.QueryByPathPrefixAsync(prefix, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return ToBlobMetadata(record, StripRoot(record.Path));
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var recordName = CreateRecordName(BuildInternalPath(NormalizeRelativePath(fileName)));
            var stream = await StorageClient.DownloadAsync(recordName, cancellationToken);
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

    private string BuildDirectoryPrefix(string? directory)
    {
        var root = NormalizeRelativePath(StorageOptions.RootPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return string.IsNullOrWhiteSpace(root) ? string.Empty : root.TrimEnd('/') + "/";
        }

        var dir = NormalizeRelativePath(directory);
        var combined = string.IsNullOrWhiteSpace(root) ? dir : $"{root}/{dir}";
        return combined.TrimEnd('/') + "/";
    }

    private string BuildInternalPath(string relativeFullName)
    {
        var root = NormalizeRelativePath(StorageOptions.RootPath);
        var normalized = NormalizeRelativePath(relativeFullName);
        return string.IsNullOrWhiteSpace(root)
            ? normalized
            : string.IsNullOrWhiteSpace(normalized) ? root : $"{root}/{normalized}";
    }

    private string StripRoot(string internalPath)
    {
        var root = NormalizeRelativePath(StorageOptions.RootPath);
        var normalized = NormalizeRelativePath(internalPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            return normalized;
        }

        if (normalized.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var prefix = root.TrimEnd('/') + "/";
        return normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? normalized[prefix.Length..]
            : normalized;
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace("\\", "/").Trim('/');
    }

    private static string CreateRecordName(string internalPath)
    {
        var bytes = Encoding.UTF8.GetBytes(internalPath);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private BlobMetadata ToBlobMetadata(CloudKitRecord record, string fullName)
    {
        return new BlobMetadata
        {
            Name = Path.GetFileName(fullName),
            FullName = fullName,
            Container = StorageOptions.ContainerId,
            Uri = record.DownloadUrl,
            CreatedOn = record.CreatedOn,
            LastModified = record.LastModified,
            Length = record.Size,
            MimeType = string.IsNullOrWhiteSpace(record.ContentType) ? MimeHelper.GetMimeType(fullName) : record.ContentType
        };
    }
}

