using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
    private readonly UrlSigner urlSigner;

    public GCPStorage(GCPStorageOptions options, ILogger<GCPStorage>? logger = null) : base(options)
    {
        _logger = logger;
        if (options.GoogleCredential != null)
            urlSigner = UrlSigner.FromCredential(options.GoogleCredential);
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.DeleteBucketAsync(StorageOptions.BucketOptions.Bucket, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist(cancellationToken);

        var pages = StorageClient.ListObjectsAsync(StorageOptions.BucketOptions.Bucket, directory,
            new ListObjectsOptions { Projection = Projection.Full });

        await foreach (var item in pages.WithCancellation(cancellationToken))
        {
            if (item is null)
                continue;

            if (cancellationToken.IsCancellationRequested)
                yield break;

            var blobMetadata = new BlobMetadata();
            try
            {
                blobMetadata = new BlobMetadata
                {
                    Name = Path.GetFileName(item.Name),
                    FullName = item.Name,
                    Uri = string.IsNullOrEmpty(item.MediaLink) ? null : new Uri(item.MediaLink),
                    Container = item.Bucket,
                    CreatedOn = GetFirstSuccessfulValue(DateTimeOffset.UtcNow, () => item.TimeCreatedDateTimeOffset, () => item.TimeCreated),
                    LastModified = GetFirstSuccessfulValue(DateTimeOffset.UtcNow, () => item.UpdatedDateTimeOffset, () => item.Updated),
                    MimeType = item.ContentType,
                    Length = item.Size ?? 0,
                    Metadata = item.Metadata?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, string>()
                };


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            yield return blobMetadata;
        }
    }



    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);

            if (urlSigner == null)
            {
                var tempFile = LocalFile.FromTempFile();

                try
                {
                    await using (var writableStream = tempFile.FileStream)
                    {
                        writableStream.SetLength(0);

                        await StorageClient.DownloadObjectAsync(
                            StorageOptions.BucketOptions.Bucket,
                            fileName,
                            writableStream,
                            cancellationToken: cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var downloadStream = tempFile.OpenReadStream();
                    return Result<Stream>.Succeed(downloadStream);
                }
                catch
                {
                    await tempFile.DisposeAsync();
                    throw;
                }
            }

            var signedUrl = urlSigner.Sign(StorageOptions.BucketOptions.Bucket, fileName, TimeSpan.FromHours(1), HttpMethod.Get);
            cancellationToken.ThrowIfCancellationRequested();

            using var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(signedUrl, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override StorageClient CreateStorageClient()
    {
        Contract.Assert(!string.IsNullOrWhiteSpace(StorageOptions.BucketOptions.Bucket));

        if (StorageOptions.StorageClientBuilder != null)
            return StorageOptions.StorageClientBuilder.Build();

        if (StorageOptions.GoogleCredential != null)
            return StorageClient.Create(StorageOptions.GoogleCredential);

        return StorageClient.Create();
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsContainerCreated)
                return Result.Succeed();

            if (StorageOptions.CreateContainerIfNotExists)
            {
                if (StorageOptions.OriginalOptions != null)
                    await StorageClient.CreateBucketAsync(StorageOptions.BucketOptions.ProjectId, StorageOptions.BucketOptions.Bucket,
                        StorageOptions.OriginalOptions, cancellationToken);
                else
                    await StorageClient.CreateBucketAsync(StorageOptions.BucketOptions.ProjectId, StorageOptions.BucketOptions.Bucket,
                        cancellationToken: cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Result.Succeed();
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode is HttpStatusCode.Conflict)
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
            await EnsureContainerExist(cancellationToken);

            var blobs = StorageClient.ListObjectsAsync(StorageOptions.BucketOptions.Bucket, string.Empty,
                    new ListObjectsOptions { Projection = Projection.Full })
                .Select(x => x);

            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var blob in blobs.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await StorageClient.DeleteObjectAsync(blob, cancellationToken: cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);

            var result = await StorageClient.UploadObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, options.MimeType, stream, null,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var metadataOptions = MetadataOptions.FromBaseOptions(options);
            metadataOptions.ETag = result.ETag;

            return await GetBlobMetadataInternalAsync(metadataOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await StorageClient.DownloadObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, localFile.FileStream, null,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

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
            cancellationToken.ThrowIfCancellationRequested();
            await StorageClient.DeleteObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<bool>.Succeed(true);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode is HttpStatusCode.NotFound)
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
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            //TODO: check logic
            return Result<bool>.Succeed(true);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            //TODO: check logic
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
            await EnsureContainerExist(cancellationToken);
            var obj = await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return Result<BlobMetadata>.Succeed(new BlobMetadata
            {
                FullName = obj.Name,
                Name = Path.GetFileName(obj.Name),
                Uri = string.IsNullOrEmpty(obj.MediaLink) ? null : new Uri(obj.MediaLink),
                Container = obj.Bucket,
                CreatedOn = GetFirstSuccessfulValue(DateTimeOffset.UtcNow, () => obj.TimeCreatedDateTimeOffset, () => obj.TimeCreated),
                LastModified = GetFirstSuccessfulValue(DateTimeOffset.UtcNow, () => obj.UpdatedDateTimeOffset, () => obj.Updated),
                MimeType = obj.ContentType,
                Length = obj.Size ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);

            var storageObject = await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            storageObject.TemporaryHold = hasLegalHold;

            await StorageClient.UpdateObjectAsync(storageObject, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

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
            await EnsureContainerExist(cancellationToken);

            var storageObject = await StorageClient.GetObjectAsync(StorageOptions.BucketOptions.Bucket, options.FullPath, cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return Result<bool>.Succeed(storageObject.TemporaryHold ?? false);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    public static T GetFirstSuccessfulValue<T>(T defaultValue, params Func<T?>[] getters) where T : struct
    {
        foreach (var getter in getters)
        {
            try
            {
                var value = getter();
                if (value.HasValue)
                {
                    return value.Value;
                }
            }
            catch
            {
                continue;
            }
        }

        return defaultValue;
    }
}
