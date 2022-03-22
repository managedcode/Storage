using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Aws;

public class AWSStorage : IAWSStorage
{
    private readonly string _bucket;
    private readonly IAmazonS3 _s3Client;

    public AWSStorage(AWSStorageOptions options)
    {
        _bucket = options.Bucket;
        var config = options.OriginalOptions ?? new AmazonS3Config();
        _s3Client = new AmazonS3Client(new BasicAWSCredentials(options.PublicKey, options.SecretKey), config);
    }

    public void Dispose()
    {
        _s3Client.Dispose();
    }

    #region Delete

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = blobName
        }, cancellationToken);
    }

    public async Task DeleteAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(blobMetadata.Name, cancellationToken);
    }

    public async Task DeleteAsync(IEnumerable<string> blobNames, CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            await DeleteAsync(blob, cancellationToken);
        }
    }

    public async Task DeleteAsync(IEnumerable<BlobMetadata> blobNames, CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            await DeleteAsync(blob, cancellationToken);
        }
    }

    #endregion

    #region Download

    public async Task<Stream> DownloadAsStreamAsync(string blobName, CancellationToken cancellationToken = default)
    {
        return await _s3Client.GetObjectStreamAsync(_bucket, blobName, null, cancellationToken);
    }

    public async Task<Stream> DownloadAsStreamAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsStreamAsync(blobMetadata.Name, cancellationToken);
    }

    public async Task<LocalFile> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var localFile = new LocalFile();

        using (var stream = await DownloadAsStreamAsync(blobName, cancellationToken))
        {
            await stream.CopyToAsync(localFile.FileStream, 81920, cancellationToken);
        }

        return localFile;
    }

    public async Task<LocalFile> DownloadAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsync(blobMetadata.Name, cancellationToken);
    }

    #endregion

    #region Exists

    public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.GetObjectAsync(_bucket, blobName, cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await ExistsAsync(blobMetadata.Name, cancellationToken);
    }

    public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            yield return await ExistsAsync(blob, cancellationToken);
        }
    }

    public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<BlobMetadata> blobs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobs)
        {
            yield return await ExistsAsync(blob, cancellationToken);
        }
    }

    #endregion

    #region Get

    public async Task<BlobMetadata> GetBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var objectMetaRequest = new GetObjectMetadataRequest
        {
            BucketName = _bucket,
            Key = blobName
        };

        var objectMetaResponse = await _s3Client.GetObjectMetadataAsync(objectMetaRequest);

        var objectAclRequest = new GetACLRequest
        {
            BucketName = _bucket,
            Key = blobName
        };

        var objectAclResponse = await _s3Client.GetACLAsync(objectAclRequest);
        var isPublic = objectAclResponse.AccessControlList.Grants.Any(x => x.Grantee.URI == "http://acs.amazonaws.com/groups/global/AllUsers");

        return new BlobMetadata
        {
            Name = blobName
            //Container = containerName,
            //Length = objectMetaResponse.Headers.ContentLength,
            //ETag = objectMetaResponse.ETag,
            //ContentMD5 = objectMetaResponse.ETag,
            //ContentType = objectMetaResponse.Headers.ContentType,
            //ContentDisposition = objectMetaResponse.Headers.ContentDisposition,
            //LastModified = objectMetaResponse.LastModified,
            //Security = isPublic ? BlobSecurity.Public : BlobSecurity.Private,
            //Metadata = objectMetaResponse.Metadata.ToMetadata(),
        };
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var objectsRequest = new ListObjectsRequest
        {
            BucketName = _bucket,
            Prefix = string.Empty,
            MaxKeys = 100000
        };

        do
        {
            var objectsResponse = await _s3Client.ListObjectsAsync(objectsRequest);

            foreach (var entry in objectsResponse.S3Objects)
            {
                var objectMetaRequest = new GetObjectMetadataRequest
                {
                    BucketName = _bucket,
                    Key = entry.Key
                };

                var objectMetaResponse = await _s3Client.GetObjectMetadataAsync(objectMetaRequest);

                var objectAclRequest = new GetACLRequest
                {
                    BucketName = _bucket,
                    Key = entry.Key
                };

                var objectAclResponse = await _s3Client.GetACLAsync(objectAclRequest);
                var isPublic = objectAclResponse.AccessControlList.Grants.Any(x =>
                    x.Grantee.URI == "http://acs.amazonaws.com/groups/global/AllUsers");

                yield return new BlobMetadata
                {
                    Name = entry.Key
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
        } while (objectsRequest != null);
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            yield return await GetBlobAsync(blob, cancellationToken);
        }
    }

    #endregion

    #region Upload

    public async Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobName, new MemoryStream(Encoding.UTF8.GetBytes(content)), null, cancellationToken);
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobMetadata.Name, new MemoryStream(Encoding.UTF8.GetBytes(content)), blobMetadata.ContentType,
            cancellationToken);
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobMetadata.Name, new MemoryStream(data), blobMetadata.ContentType, cancellationToken);
    }

    public async Task UploadFileAsync(string blobName, string pathToFile, CancellationToken cancellationToken = default)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await UploadStreamInternalAsync(blobName, fs, null, cancellationToken);
        }
    }


    public async Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await UploadStreamInternalAsync(blobMetadata.Name, fs, blobMetadata.ContentType, cancellationToken);
        }
    }

    public async Task UploadStreamAsync(string blobName, Stream dataStream, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobName, dataStream, null, cancellationToken);
    }

    public async Task UploadStreamAsync(BlobMetadata blobMetadata, Stream dataStream, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobMetadata.Name, dataStream, blobMetadata.ContentType, cancellationToken);
    }

    public async Task<string> UploadAsync(string content, CancellationToken cancellationToken = default)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        await UploadStreamInternalAsync(fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)), null, cancellationToken);

        return fileName;
    }

    public async Task<string> UploadAsync(Stream dataStream, CancellationToken cancellationToken = default)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        await UploadStreamInternalAsync(fileName, dataStream, null, cancellationToken);

        return fileName;
    }

    private async Task UploadStreamInternalAsync(string blobName, Stream dataStream,
        string? contentType = null, CancellationToken cancellationToken = default)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = blobName,
            InputStream = dataStream,
            AutoCloseStream = true,
            ContentType = contentType ?? Constants.ContentType,
            ServerSideEncryptionMethod = null
        };

        try
        {
            await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        }
        catch
        {
            await CreateContainerAsync();
            await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        }
    }

    #endregion

    #region CreateContainer

    public async Task CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        await _s3Client.EnsureBucketExistsAsync(_bucket);
    }
    
    #endregion
}