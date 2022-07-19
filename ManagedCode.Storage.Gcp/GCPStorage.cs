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

namespace ManagedCode.Storage.Gcp;

public class GCPStorage : BaseStorage<GCPStorageOptions>, IGCPStorage
{
    public StorageClient StorageClient { get; }

    public GCPStorage(GCPStorageOptions options) : base(options)
    {
        System.Diagnostics.Contracts.Contract.Assert(!string.IsNullOrWhiteSpace(StorageOptions.BucketOptions?.Bucket));

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

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        await StorageClient.DeleteBucketAsync(StorageOptions.BucketOptions.Bucket, null, cancellationToken);
        return Result.Succeed();
    }

    protected override async Task<Result<string>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.UploadObjectAsync(StorageOptions.BucketOptions.Bucket, options.FileName, options.MimeType, stream, null,
                cancellationToken);
            return Result<string>.Succeed(string.Empty);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.DownloadObjectAsync(StorageOptions.BucketOptions.Bucket, blob, localFile.FileStream, null, cancellationToken);
            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            return Result<LocalFile>.Fail(ex);
        }
    }

    public override async Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.DeleteObjectAsync(StorageOptions.BucketOptions.Bucket, blob, null, cancellationToken);
            return Result<bool>.Succeed(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    public override async Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, blob, null, cancellationToken);
            return Result<bool>.Succeed(true);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return Result<bool>.Succeed(false);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    public override async Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            var obj = await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, blob, null, cancellationToken);

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
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    public override IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(CancellationToken cancellationToken = default)
    {
        return StorageClient.ListObjectsAsync(StorageOptions.BucketOptions.Bucket, string.Empty,
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

    public override async Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        var storageObject = await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, blob, cancellationToken: cancellationToken);
        storageObject.TemporaryHold = hasLegalHold;

        await StorageClient.UpdateObjectAsync(storageObject, cancellationToken: cancellationToken);

        return Result.Succeed();
    }

    public override async Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default)
    {
        var storageObject = await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, blob, cancellationToken: cancellationToken);

        return Result<bool>.Succeed(storageObject.TemporaryHold ?? false);
    }
}