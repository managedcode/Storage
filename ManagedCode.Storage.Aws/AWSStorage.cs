using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ManagedCode.Communication;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Aws;

public class AWSStorage : BaseStorage<AWSStorageOptions>, IAWSStorage
{
    private readonly ILogger<AWSStorage> _logger;

    public IAmazonS3 StorageClient { get; }

    public AWSStorage(ILogger<AWSStorage> logger, AWSStorageOptions options) : base(options)
    {
        _logger = logger;
        var config = options.OriginalOptions ?? new AmazonS3Config();
        StorageClient = new AmazonS3Client(new BasicAWSCredentials(options.PublicKey, options.SecretKey), config);
    }


    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.EnsureBucketExistsAsync(StorageOptions.Bucket);
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
            await StorageClient.DeleteBucketAsync(StorageOptions.Bucket, cancellationToken);
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
        var putRequest = new PutObjectRequest
        {
            BucketName = StorageOptions.Bucket,
            Key = options.FileName,
            InputStream = stream,
            AutoCloseStream = false,
            ContentType = options.MimeType,
            ServerSideEncryptionMethod = null
        };

        try
        {
            await EnsureContainerExist();
            await StorageClient.PutObjectAsync(putRequest, cancellationToken);
            _ = await StorageClient.PutObjectAsync(putRequest, cancellationToken);
        }
        catch (AmazonS3Exception)
        {
            await CreateContainerAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex);
        }

        return Result<string>.Succeed(
            $"https://{StorageOptions.Bucket}.s3-{StorageOptions.OriginalOptions.RegionEndpoint.SystemName}.amazonaws.com/{HttpUtility.UrlEncode(options.FileName)}");
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            var response = await StorageClient.GetObjectAsync(StorageOptions.Bucket, blob, null, cancellationToken);

            localFile.BlobMetadata = new BlobMetadata
            {
                Name = blob,
                Uri = new Uri($"https://s3.amazonaws.com/{StorageOptions.Bucket}/{blob}"),
                MimeType = response.Headers.ContentType,
                Length = response.Headers.ContentLength,
                Container = StorageOptions.Bucket,
            };

            await localFile.CopyFromStreamAsync(await StorageClient.GetObjectStreamAsync(StorageOptions.Bucket, blob, null, cancellationToken));

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob, DownloadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected override Task<Result<bool>> DeleteInternalAsync(string blob, DeleteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            _ = await StorageClient.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = StorageOptions.Bucket,
                Key = blob
            }, cancellationToken);

            return Result<bool>.Succeed(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(string blob, ExistOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = options?.Directory ?? blob;

            await EnsureContainerExist();
            _ = await StorageClient.GetObjectAsync(StorageOptions.Bucket, blob, null, cancellationToken);
            return Result<bool>.Succeed(true);
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<bool>.Succeed(false);

            return Result<bool>.Fail(ex);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    protected override Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(string blob, MetadataOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default)
    {
        var objectMetaRequest = new GetObjectMetadataRequest
        {
            BucketName = StorageOptions.Bucket,
            Key = blob
        };

        try
        {
            await EnsureContainerExist();
            var objectMetaResponse = await StorageClient.GetObjectMetadataAsync(objectMetaRequest, cancellationToken);

            var metadata = new BlobMetadata
            {
                Name = blob,
                Uri = new Uri($"https://s3.amazonaws.com/{StorageOptions.Bucket}/{blob}"),
                MimeType = objectMetaResponse.Headers.ContentType,
                Length = objectMetaResponse.Headers.ContentLength
            };

            return Result<BlobMetadata>.Succeed(metadata);
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var objectsRequest = new ListObjectsRequest
        {
            BucketName = StorageOptions.Bucket,
            Prefix = string.Empty,
            MaxKeys = 1_000_000
        };

        await EnsureContainerExist();

        do
        {
            var objectsResponse = await StorageClient.ListObjectsAsync(objectsRequest, cancellationToken);

            foreach (var entry in objectsResponse.S3Objects)
            {
                var objectMetaRequest = new GetObjectMetadataRequest
                {
                    BucketName = StorageOptions.Bucket,
                    Key = entry.Key
                };

                var objectMetaResponse = await StorageClient.GetObjectMetadataAsync(objectMetaRequest, cancellationToken);

                yield return new BlobMetadata
                {
                    Name = entry.Key,
                    Uri = new Uri($"https://s3.amazonaws.com/{StorageOptions.Bucket}/{entry.Key}"),
                    MimeType = objectMetaResponse.Headers.ContentType,
                    Length = objectMetaResponse.Headers.ContentLength
                };
            }

            // If response is truncated, set the marker to get the next set of keys.
            if (objectsResponse.IsTruncated)
            {
                objectsRequest.Marker = objectsResponse.NextMarker;
            }
            else
            {
                objectsRequest = null;
            }
        } while (objectsRequest is not null);
    }


    public override async Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();

            var status = hasLegalHold
                ? ObjectLockLegalHoldStatus.On
                : ObjectLockLegalHoldStatus.Off;

            PutObjectLegalHoldRequest request = new()
            {
                BucketName = StorageOptions.Bucket,
                Key = blob,
                LegalHold = new ObjectLockLegalHold
                {
                    Status = status,
                },
            };

            await StorageClient.PutObjectLegalHoldAsync(request, cancellationToken);
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }


    public override async Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist();
            GetObjectLegalHoldRequest request = new()
            {
                BucketName = StorageOptions.Bucket,
                Key = blob
            };

            var response = await StorageClient.GetObjectLegalHoldAsync(request, cancellationToken);

            return Result<bool>.Succeed(response.LegalHold.Status == ObjectLockLegalHoldStatus.On);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }
}