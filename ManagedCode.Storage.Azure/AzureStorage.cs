using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ManagedCode.Communication;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Azure;

public class AzureStorage : BaseStorage<AzureStorageOptions>, IAzureStorage
{
    private readonly ILogger<AzureStorage> _logger;

    public AzureStorage(ILogger<AzureStorage> logger, AzureStorageOptions options) : base(options)
    {
        _logger = logger;
        StorageClient = new BlobContainerClient(
            options.ConnectionString,
            options.Container,
            options.OriginalOptions
        );
    }

    public BlobContainerClient StorageClient { get; }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await StorageClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer, cancellationToken: cancellationToken);
            await StorageClient.SetAccessPolicyAsync(StorageOptions.PublicAccessType, cancellationToken: cancellationToken);
            IsContainerCreated = true;
            return Result.Succeed();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return Result.Fail(e);
        }
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await StorageClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            IsContainerCreated = false;
            return Result.Succeed();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return Result.Fail(e);
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobs = StorageClient.GetBlobs(prefix: directory, cancellationToken: cancellationToken);

            foreach (var blob in blobs)
            {
                var blobClient = StorageClient.GetBlobClient(blob.Name);
                _ = await blobClient.DeleteAsync(DeleteSnapshotsOption.None, null, cancellationToken);
            }

            return Result.Succeed();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return Result.Fail(e);
        }
    }

    protected override async Task<Result<string>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        var blobClient = StorageClient.GetBlobClient(options.FullPath);

        var uploadOptions = new BlobUploadOptions
        {
            Metadata = options.Metadata,
        };

        try
        {
            await EnsureContainerExist();
            _ = await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);
        }
        catch (RequestFailedException)
        {
            await CreateContainerAsync(cancellationToken);
            await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex);
        }

        return Result<string>.Succeed($"{blobClient.Uri}/{StorageOptions.Container}/{options.FullPath}");
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        var blobClient = StorageClient.GetBlobClient(options.FullPath);

        try
        {
            await EnsureContainerExist();
            var response = await blobClient.DownloadAsync(cancellationToken);
            await localFile.CopyFromStreamAsync(response.Value.Content);
            localFile.BlobMetadata = new BlobMetadata
            {
                Metadata = response.Value.Details?.Metadata?.ToDictionary(k => k.Key, v => v.Value),
                MimeType = response.Value.ContentType,
                Length = response.Value.ContentLength,
                Name = blobClient.Name,
                Uri = blobClient.Uri,
                Container = blobClient.BlobContainerName,
                FullName = $"{blobClient.Uri}/{StorageOptions.Container}/{options.FullPath}"
            };

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(options.FullPath);
            var response = await blobClient.DeleteAsync(DeleteSnapshotsOption.None, null, cancellationToken);
            return Result<bool>.Succeed(!response.IsError);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }


    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(options.FullPath);
            var response = await blobClient.ExistsAsync(cancellationToken);
            return Result<bool>.Succeed(response.Value);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(options.FullPath);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            if (properties != null)
            {
                return Result<BlobMetadata>.Succeed(new BlobMetadata
                {
                    Name = blobClient.Name,
                    Uri = blobClient.Uri,
                    Container = blobClient.BlobContainerName,
                    Length = properties.Value.ContentLength,
                    Metadata = properties.Value.Metadata.ToDictionary(k => k.Key, v => v.Value),
                    MimeType = properties.Value.ContentType
                });
            }

            return Result<BlobMetadata>.Fail();
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        await foreach (var item in StorageClient.GetBlobsAsync(prefix: directory, cancellationToken: cancellationToken).AsPages()
                           .WithCancellation(cancellationToken))
        {
            foreach (var blobItem in item.Values)
            {
                var blobMetadata = new BlobMetadata
                {
                    Name = blobItem.Name,
                    Container = StorageOptions.Container,
                    Length = blobItem.Properties.ContentLength.Value,
                    Metadata = blobItem.Metadata.ToDictionary(k => k.Key, v => v.Value),
                    MimeType = blobItem.Properties.ContentType
                };

                yield return blobMetadata;
            }
        }
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(options.FullPath);
            var response = await blobClient.SetLegalHoldAsync(hasLegalHold, cancellationToken);
            return response.Value.HasLegalHold ? Result.Succeed() : Result.Fail();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(options.FullPath);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return Result<bool>.Succeed(properties.Value?.HasLegalHold ?? false);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    public async Task<Result<Stream>> OpenReadStreamAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            return Result<Stream>.Fail(ex);
        }
    }

    public async Task<Result<Stream>> OpenWriteStreamAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var stream = await blobClient.OpenWriteAsync(true, cancellationToken: cancellationToken);
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            return Result<Stream>.Fail(ex);
        }
    }
}