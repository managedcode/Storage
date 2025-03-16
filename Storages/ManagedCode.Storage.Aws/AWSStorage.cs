using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ManagedCode.Communication;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Aws;

public class AWSStorage : BaseStorage<IAmazonS3, AWSStorageOptions>, IAWSStorage
{
    private readonly ILogger<AWSStorage>? _logger;

    public AWSStorage(AWSStorageOptions options, ILogger<AWSStorage>? logger = null) : base(options)
    {
        _logger = logger;
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.DeleteBucketAsync(StorageOptions.Bucket, cancellationToken);
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
        var objectsRequest = new ListObjectsRequest
        {
            BucketName = StorageOptions.Bucket,
            Prefix = directory,
            MaxKeys = 1_000_000
        };

        await EnsureContainerExist(cancellationToken);

        do
        {
            var objectsResponse = await StorageClient.ListObjectsAsync(objectsRequest, cancellationToken);

            if(cancellationToken.IsCancellationRequested)
                yield break;
            
            foreach (var entry in objectsResponse.S3Objects)
            {
                if(cancellationToken.IsCancellationRequested)
                    yield break;
                
                var objectMetaRequest = new GetObjectMetadataRequest
                {
                    BucketName = StorageOptions.Bucket,
                    Key = entry.Key
                };

                var objectMetaResponse = await StorageClient.GetObjectMetadataAsync(objectMetaRequest, cancellationToken);

                yield return new BlobMetadata
                {
                    Name = entry.Key,
                    FullName = $"{StorageOptions.Bucket}/{entry.Key}",
                    Container = StorageOptions.Bucket,
                    Uri = new Uri($"https://s3.amazonaws.com/{StorageOptions.Bucket}/{entry.Key}"),
                    LastModified = objectMetaResponse.LastModified,
                    CreatedOn = objectMetaResponse.LastModified,
                    MimeType = objectMetaResponse.Headers.ContentType,
                    Length = (ulong)objectMetaResponse.Headers.ContentLength
                };
            }

            // If response is truncated, set the marker to get the next set of keys.
            if (objectsResponse.IsTruncated)
                objectsRequest.Marker = objectsResponse.NextMarker;
            else
                objectsRequest = null;
        } while (objectsRequest is not null);
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var stream = await StorageClient.GetObjectStreamAsync(StorageOptions.Bucket, fileName, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override IAmazonS3 CreateStorageClient()
    {
        // Check if we should use the instance profile credentials.
        if (StorageOptions.UseInstanceProfileCredentials)
        {
            return string.IsNullOrWhiteSpace(StorageOptions.RoleName)
                ? new AmazonS3Client(new InstanceProfileAWSCredentials(), StorageOptions.OriginalOptions)
                : new AmazonS3Client(new InstanceProfileAWSCredentials(StorageOptions.RoleName), StorageOptions.OriginalOptions);
        }

        // If not, use the basic credentials.
        return new AmazonS3Client(new BasicAWSCredentials(StorageOptions.PublicKey, StorageOptions.SecretKey), StorageOptions.OriginalOptions);
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (StorageOptions.CreateContainerIfNotExists)
                await StorageClient.EnsureBucketExistsAsync(StorageOptions.Bucket);

            cancellationToken.ThrowIfCancellationRequested();
            
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
            var items = GetBlobMetadataListAsync(directory, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var item in items)
            {
                await StorageClient.DeleteAsync(StorageOptions.Bucket, item.Name, null, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            
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
        var putRequest = new PutObjectRequest
        {
            BucketName = StorageOptions.Bucket,
            Key = options.FullPath,
            InputStream = stream,
            AutoCloseStream = false,
            ContentType = options.MimeType,
            ServerSideEncryptionMethod = null
        };

        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var response = await StorageClient.PutObjectAsync(putRequest, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var metadataOptions = MetadataOptions.FromBaseOptions(options);
            metadataOptions.ETag = response.ETag;

            return await GetBlobMetadataInternalAsync(metadataOptions, cancellationToken);
        }
        catch (Exception ex)
        {
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
            var response = await StorageClient.GetObjectAsync(StorageOptions.Bucket, options.FullPath, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            localFile.BlobMetadata = new BlobMetadata
            {
                Name = options.FileName,
                Container = StorageOptions.Bucket,
                Uri = new Uri($"https://s3.amazonaws.com/{StorageOptions.Bucket}/{options.FullPath}"),
                LastModified = response.LastModified,
                CreatedOn = response.LastModified,
                MimeType = response.Headers.ContentType,
                Length = (ulong)response.Headers.ContentLength
            };

            await localFile.CopyFromStreamAsync(await StorageClient.GetObjectStreamAsync(StorageOptions.Bucket, options.FullPath, null,
                cancellationToken), cancellationToken);
            
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

            var isExist = await ExistsAsync(ExistOptions.FromBaseOptions(options), cancellationToken);

            if (!isExist.Value)
                return Result<bool>.Succeed(false);

            await StorageClient.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = StorageOptions.Bucket,
                Key = options.FullPath
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            
            return Result<bool>.Succeed(true);
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
            _ = await StorageClient.GetObjectAsync(StorageOptions.Bucket, options.FullPath, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<bool>.Succeed(true);
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return Result<bool>.Succeed(false);

            return Result<bool>.Fail(ex);
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
        var objectMetaRequest = new GetObjectMetadataRequest
        {
            BucketName = StorageOptions.Bucket,
            Key = options.FullPath
        };

        try
        {
            await EnsureContainerExist(cancellationToken);
            var objectMetaResponse = await StorageClient.GetObjectMetadataAsync(objectMetaRequest, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = new BlobMetadata
            {
                Name = options.FileName,
                Container = StorageOptions.Bucket,
                Uri = new Uri($"https://s3.amazonaws.com/{StorageOptions.Bucket}/{options.FullPath}"),
                LastModified = objectMetaResponse.LastModified,
                CreatedOn = objectMetaResponse.LastModified,
                MimeType = objectMetaResponse.Headers.ContentType,
                Length = (ulong)objectMetaResponse.Headers.ContentLength
            };

            return Result<BlobMetadata>.Succeed(metadata);
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
            cancellationToken.ThrowIfCancellationRequested();
            
            var status = hasLegalHold ? ObjectLockLegalHoldStatus.On : ObjectLockLegalHoldStatus.Off;

            PutObjectLegalHoldRequest request = new()
            {
                BucketName = StorageOptions.Bucket,
                Key = options.FullPath,
                LegalHold = new ObjectLockLegalHold
                {
                    Status = status
                }
            };

            await StorageClient.PutObjectLegalHoldAsync(request, cancellationToken);
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
            cancellationToken.ThrowIfCancellationRequested();
            GetObjectLegalHoldRequest request = new()
            {
                BucketName = StorageOptions.Bucket,
                Key = options.FullPath
            };

            var response = await StorageClient.GetObjectLegalHoldAsync(request, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<bool>.Succeed(response.LegalHold.Status == ObjectLockLegalHoldStatus.On);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }
}