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
            var info = await StorageClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer, cancellationToken: cancellationToken);
            await StorageClient.SetAccessPolicyAsync(StorageOptions.PublicAccessType, cancellationToken: cancellationToken);
            IsContainerCreated = true;
            return Result.Succeeded();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message,e);
            return Result.Failed(e);
        }
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await StorageClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            IsContainerCreated = false;
            return Result.Succeeded();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message,e);
            return Result.Failed(e);
        }
    }

    protected override async Task<Result<string>> UploadInternalAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default)
    {
        var blobClient = StorageClient.GetBlobClient(options.FileName);

        var uploadOptions = new BlobUploadOptions
        {
            Metadata = options.Metadata,
        };
        
        try
        {
            await EnsureContainerExist();
            var info = await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);
        }
        catch (RequestFailedException)
        {
            await CreateContainerAsync(cancellationToken);
            await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<string>.Failed(ex);
        }

        return Result<string>.Succeeded($"{blobClient.Uri}/{StorageOptions.Container}/{options.FileName}");
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob, CancellationToken cancellationToken = default)
    {
        var blobClient = StorageClient.GetBlobClient(blob);

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
                //FullName = $"{blobClient.Uri}/{StorageOptions.Container}/{blob}"
            };
            
            return Result<LocalFile>.Succeeded(localFile);
        }
        catch (Exception ex)
        {
            return Result<LocalFile>.Failed(ex);
        }
    }

    public override async Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var response = await blobClient.DeleteAsync(DeleteSnapshotsOption.None, null, cancellationToken);
            return Result<bool>.Succeeded(!response.IsError);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failed(ex);
        }
    }

    public override async Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var response = await blobClient.ExistsAsync(cancellationToken);
            return Result<bool>.Succeeded(response.Value);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failed(ex);
        }
    }

    public override async Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            if (properties != null)
            {
                return Result<BlobMetadata>.Succeeded(new BlobMetadata
                {
                    Name = blobClient.Name,
                    Uri = blobClient.Uri,
                    Container = blobClient.BlobContainerName,
                    Length = properties.Value.ContentLength,
                    Metadata = properties.Value.Metadata.ToDictionary(k => k.Key, v => v.Value),
                    MimeType = properties.Value.ContentType
                });
            }

            return Result<BlobMetadata>.Failed();
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Failed(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        await foreach (var item in StorageClient.GetBlobsAsync().AsPages().WithCancellation(cancellationToken))
        {
            foreach (var blobItem in item.Values)
            {
                var blobMetadata = new BlobMetadata
                {
                    Name = blobItem.Name,
                    //HasLegalHold = blobItem.Properties.HasLegalHold,
                    Container = StorageOptions.Container,
                    Length = blobItem.Properties.ContentLength.Value,
                    Metadata = blobItem.Metadata.ToDictionary(k => k.Key, v => v.Value),
                    MimeType = blobItem.Properties.ContentType
                };

                yield return blobMetadata;
            }
        }
    }

    public override async Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var response = await blobClient.SetLegalHoldAsync(hasLegalHold, cancellationToken);
            return response.Value.HasLegalHold ? Result.Succeeded() : Result.Failed();
        }
        catch (Exception ex)
        {
            return Result.Failed(ex);
        }
    }

    public override async Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default)
    {
        
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return Result<bool>.Succeeded(properties.Value?.HasLegalHold ?? false);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failed(ex);
        }
    }

    public async Task<Result<Stream>> OpenReadStreamAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken );
            return Result<Stream>.Succeeded(stream);
        }
        catch (Exception ex)
        {
            return Result<Stream>.Failed(ex);
        }
    }

    public async Task<Result<Stream>> OpenWriteStreamAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobClient = StorageClient.GetBlobClient(blob);
            var stream = await blobClient.OpenWriteAsync(true, cancellationToken: cancellationToken);
            return Result<Stream>.Succeeded(stream);
        }
        catch (Exception ex)
        {
            return Result<Stream>.Failed(ex);
        }
    }
}