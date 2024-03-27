using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Cloud.Storage.V1;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Google;

public class GCPStorage : BaseStorage<StorageClient, GCPStorageOptions>, IGCPStorage
{
    private readonly ILogger<GCPStorage>? _logger;
    private UrlSigner urlSigner;

    public GCPStorage(GCPStorageOptions options, ILogger<GCPStorage>? logger = null) : base(options)
    {
        _logger = logger;
        if (options.GoogleCredential != null)
        {
            urlSigner = UrlSigner.FromCredential(options.GoogleCredential);
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
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    public override IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        CancellationToken cancellationToken = default)

    {
        return StorageClient.ListObjectsAsync(StorageOptions.BucketOptions.Bucket, directory,
                new ListObjectsOptions { Projection = Projection.Full })
            .Select(
                x => new BlobMetadata
                {
                    Name = x.Name,
                    Uri = string.IsNullOrEmpty(x.MediaLink) ? null : new Uri(x.MediaLink),
                    Container = x.Bucket,
                    CreatedOn = x.TimeCreated!.Value,
                    LastModified = x.Updated!.Value,
                    MimeType = x.ContentType,
                    Length = (long)(x.Size ?? 0)
                }
            );
    }

    protected override StorageClient CreateStorageClient()
    {
        Contract.Assert(!string.IsNullOrWhiteSpace(StorageOptions.BucketOptions.Bucket));

        if (StorageOptions.StorageClientBuilder != null)
        {
            return StorageOptions.StorageClientBuilder.Build();
        }

        if (StorageOptions.GoogleCredential != null)
        {
            return StorageClient.Create(StorageOptions.GoogleCredential);
        }

        return StorageClient.Create();
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsContainerCreated)
            {
                return Result.Succeed();
            }

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
        catch (GoogleApiException exception)
            when (exception.HttpStatusCode is HttpStatusCode.Conflict)
        {
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var blobs = StorageClient.ListObjectsAsync(StorageOptions.BucketOptions.Bucket, string.Empty,
                    new ListObjectsOptions { Projection = Projection.Full })
                .Select(x => x);

            await foreach (var blob in blobs.WithCancellation(cancellationToken))
            {
                await StorageClient.DeleteObjectAsync(blob, cancellationToken: cancellationToken);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream,
        UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();

            await StorageClient.UploadObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, options.MimeType, stream, null,
                cancellationToken);

            return await GetBlobMetadataInternalAsync(MetadataOptions.FromBaseOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile,
        DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            await StorageClient.DownloadObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, localFile.FileStream, null,
                cancellationToken);

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
            await EnsureContainerExist();
            await StorageClient.DeleteObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);
            return Result<bool>.Succeed(true);
        }
        catch (GoogleApiException ex)
            when (ex.HttpStatusCode is HttpStatusCode.NotFound)
        {
            return Result<bool>.Succeed(false);
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
            await EnsureContainerExist();
            await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);
            return Result<bool>.Succeed(true);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return Result<bool>.Succeed(false);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var obj = await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);

            return Result<BlobMetadata>.Succeed(new BlobMetadata
            {
                Name = obj.Name,
                Uri = string.IsNullOrEmpty(obj.MediaLink) ? null : new Uri(obj.MediaLink),
                Container = obj.Bucket,
                CreatedOn = obj.TimeCreated!.Value,
                LastModified = obj.Updated!.Value,
                MimeType = obj.ContentType,
                Length = (long)(obj.Size ?? 0)
            });
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold,
        LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();

            var storageObject =
                await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, cancellationToken: cancellationToken);
            storageObject.TemporaryHold = hasLegalHold;

            await StorageClient.UpdateObjectAsync(storageObject, cancellationToken: cancellationToken);

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();

            var storageObject =
                await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, cancellationToken: cancellationToken);

            return Result<bool>.Succeed(storageObject.TemporaryHold ?? false);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        if (urlSigner == null)
        {
            return Result<Stream>.Fail("Google credentials are required to get stream");
        }

        string signedUrl = urlSigner.Sign(StorageOptions.BucketOptions.Bucket, fileName, TimeSpan.FromHours(1), HttpMethod.Get);

        using (HttpClient httpClient = new HttpClient())
        {
            Stream stream = await httpClient.GetStreamAsync(signedUrl, cancellationToken);
            return Result<Stream>.Succeed(stream);
        }
    }
}