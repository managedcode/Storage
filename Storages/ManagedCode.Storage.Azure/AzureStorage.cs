﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ManagedCode.Communication;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Azure;

public class AzureStorage(IAzureStorageOptions options, ILogger<AzureStorage>? logger = default)
    : BaseStorage<BlobContainerClient, IAzureStorageOptions>(options), IAzureStorage
{
    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await StorageClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            IsContainerCreated = false;
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        await foreach (var item in StorageClient.GetBlobsAsync(prefix: directory, cancellationToken: cancellationToken)
                           .AsPages()
                           .WithCancellation(cancellationToken))
        foreach (var blobItem in item.Values)
        {
            var blobMetadata = new BlobMetadata
            {
                FullName = blobItem.Name,
                Name = Path.GetFileName(blobItem.Name),
                Uri = new Uri(StorageClient.Uri, $"{StorageOptions.Container}/{blobItem.Name}"),
                Container = StorageOptions.Container,
                Length = blobItem.Properties.ContentLength!.Value,
                Metadata = blobItem.Metadata.ToDictionary(k => k.Key, v => v.Value),
                LastModified = blobItem.Properties.LastModified!.Value,
                CreatedOn = blobItem.Properties.CreatedOn!.Value,
                MimeType = blobItem.Properties.ContentType
            };

            yield return blobMetadata;
        }
    }

    public async Task<Result<Stream>> OpenReadStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(fileName);
            var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    public async Task<Result<Stream>> OpenWriteStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(fileName);
            var stream = await blobClient.OpenWriteAsync(false, cancellationToken: cancellationToken);
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    public Stream GetBlobStream(string fileName, bool userBuffer = true, int bufferSize = BlobStream.DefaultBufferSize)
    {
        if (userBuffer)
            return new BufferedStream(new BlobStream(StorageClient.GetPageBlobClient(fileName)), bufferSize);

        return new BlobStream(StorageClient.GetPageBlobClient(fileName));
    }

    public async Task<Result> SetStorageOptions(IStorageOptions options, CancellationToken cancellationToken = default)
    {
        StorageOptions = options as IAzureStorageOptions;

        StorageClient = CreateStorageClient();
        return await CreateContainerAsync(cancellationToken);
    }

    public async Task<Result> SetStorageOptions(Action<IStorageOptions> options, CancellationToken cancellationToken = default)
    {
        var type = options.GetType()
            .GetGenericArguments()[0];

        StorageOptions = JsonSerializer.Deserialize(JsonSerializer.Serialize(StorageOptions), type) as IAzureStorageOptions;

        options.Invoke(StorageOptions);

        StorageClient = CreateStorageClient();

        return await CreateContainerAsync(cancellationToken);
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return await OpenReadStreamAsync(fileName, cancellationToken);
    }

    protected override BlobContainerClient CreateStorageClient()
    {
        return StorageOptions switch
        {
            AzureStorageOptions azureStorageOptions => new BlobContainerClient(azureStorageOptions.ConnectionString, azureStorageOptions.Container,
                azureStorageOptions.OriginalOptions),

            AzureStorageCredentialsOptions azureStorageCredentialsOptions => new BlobContainerClient(
                new Uri($"https://{azureStorageCredentialsOptions.AccountName}.blob.core.windows.net/{azureStorageCredentialsOptions.ContainerName}"),
                azureStorageCredentialsOptions.Credentials, azureStorageCredentialsOptions.OriginalOptions)
        };
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (StorageOptions.CreateContainerIfNotExists)
            {
                var blobInfo = await StorageClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                var policy = await StorageClient.GetAccessPolicyAsync(cancellationToken: cancellationToken);
                if (policy.Value.BlobPublicAccess != StorageOptions.PublicAccessType)
                    await StorageClient.SetAccessPolicyAsync(StorageOptions.PublicAccessType, cancellationToken: cancellationToken);
            }
            else
            {
                try
                {
                    IsContainerCreated = await StorageClient.ExistsAsync(cancellationToken);
                    return Result.From(IsContainerCreated);
                }
                catch (RequestFailedException e)
                {
                    //probably we don't have permission to check if container exists
                    logger.LogException(e);
                }
            }
            
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result.Fail(ex);
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
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, cancellationToken);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        var blobClient = StorageClient.GetBlobClient(options.FullPath);

        var uploadOptions = new BlobUploadOptions
        {
            Metadata = options.Metadata,
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = options.MimeType
            }
        };

        try
        {
            await EnsureContainerExist();
            _ = await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);

            return await GetBlobMetadataInternalAsync(MetadataOptions.FromBaseOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
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
                FullName = blobClient.Name,
                Name = Path.GetFileName(blobClient.Name),
                Uri = blobClient.Uri,
                Container = blobClient.BlobContainerName,
                Length = response.Value.ContentLength,
                CreatedOn = response.Value.Details.LastModified,
                LastModified = response.Value.Details.LastModified,
                Metadata = response.Value.Details.Metadata.ToDictionary(k => k.Key, v => v.Value),
                MimeType = response.Value.ContentType
            };

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(options.FullPath);
            var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, cancellationToken);
            return Result<bool>.Succeed(response);
        }
        catch (RequestFailedException ex) when (ex.Status is 404)
        {
            return Result<bool>.Succeed(false);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
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
            logger.LogException(ex);
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

            if (properties is null)
                return Result<BlobMetadata>.Fail("Properties for file not found");

            return Result<BlobMetadata>.Succeed(new BlobMetadata
            {
                FullName = blobClient.Name,
                Name = Path.GetFileName(blobClient.Name),
                Uri = blobClient.Uri,
                Container = blobClient.BlobContainerName,
                Length = properties.Value.ContentLength,
                CreatedOn = properties.Value.CreatedOn,
                LastModified = properties.Value.LastModified,
                Metadata = properties.Value.Metadata.ToDictionary(k => k.Key, v => v.Value),
                MimeType = properties.Value.ContentType
            });
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(options.FullPath);
            await blobClient.SetLegalHoldAsync(hasLegalHold, cancellationToken);

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetPageBlobClient(options.FullPath);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return Result<bool>.Succeed(properties.Value?.HasLegalHold ?? false);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }
}