using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Cloud.Storage.V1;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Gcp.Options;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Gcp;

public class GCPStorage : BaseStorage<GCPStorageOptions>, IGCPStorage
{
    private readonly ILogger<GCPStorage> _logger;
    public StorageClient StorageClient { get; }

    public GCPStorage(GCPStorageOptions options, ILogger<GCPStorage> logger) : base(options)
    {
        _logger = logger;

        System.Diagnostics.Contracts.Contract.Assert(!string.IsNullOrWhiteSpace(StorageOptions.BucketOptions.Bucket));

        if (options.StorageClientBuilder != null)
        {
            StorageClient = options.StorageClientBuilder.Build();
        }
        else if (options.GoogleCredential != null)
        {
            StorageClient = StorageClient.Create(options.GoogleCredential);
        }
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (StorageOptions.OriginalOptions != null)
            {
                await StorageClient.CreateBucketAsync(StorageOptions.BucketOptions.ProjectId, StorageOptions.BucketOptions.Bucket,
                    StorageOptions.OriginalOptions,
                    cancellationToken);
            }
            else
            {
                await StorageClient.CreateBucketAsync(StorageOptions.BucketOptions.ProjectId, StorageOptions.BucketOptions.Bucket,
                    cancellationToken: cancellationToken);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<string>.Fail(ex);
        }
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.DeleteBucketAsync(StorageOptions.BucketOptions.Bucket, null, cancellationToken);
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<string>.Fail(ex);
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobs = StorageClient.ListObjectsAsync(StorageOptions.BucketOptions.Bucket, string.Empty,
                new ListObjectsOptions {Projection = Projection.Full}).Select(x => x);

            await foreach (var blob in blobs.WithCancellation(cancellationToken))
            {
                await StorageClient.DeleteObjectAsync(blob, cancellationToken: cancellationToken);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<string>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.UploadObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, options.MimeType, stream, null,
                cancellationToken);

            return await GetBlobMetadataInternalAsync(MetadataOptions.FromBaseOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }


    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.DownloadObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, localFile.FileStream, null,
                cancellationToken);

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.DeleteObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);
            return Result<bool>.Succeed(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);
            return Result<bool>.Succeed(true);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogError(ex.Message, ex);
            return Result<bool>.Succeed(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var obj = await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);

            return Result<BlobMetadata>.Succeed(new BlobMetadata
            {
                Name = obj.Name,
                Uri = string.IsNullOrEmpty(obj.MediaLink) ? null : new Uri(obj.MediaLink),
                Container = obj.Bucket,
                MimeType = obj.ContentType,
                Length = (long) (obj.Size ?? 0),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    public override IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null, CancellationToken cancellationToken = default)
    {
        return StorageClient.ListObjectsAsync(StorageOptions.BucketOptions.Bucket, directory,
                new ListObjectsOptions {Projection = Projection.Full})
            .Select(
                x => new BlobMetadata
                {
                    Name = x.Name,
                    Uri = string.IsNullOrEmpty(x.MediaLink) ? null : new Uri(x.MediaLink),
                    Container = x.Bucket,
                    MimeType = x.ContentType,
                    Length = (long) (x.Size ?? 0),
                }
            );
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageObject =
                await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, cancellationToken: cancellationToken);
            storageObject.TemporaryHold = hasLegalHold;

            await StorageClient.UpdateObjectAsync(storageObject, cancellationToken: cancellationToken);

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<string>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageObject =
                await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, cancellationToken: cancellationToken);

            return Result<bool>.Succeed(storageObject.TemporaryHold ?? false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Result<bool>.Fail(ex);
        }
    }
}