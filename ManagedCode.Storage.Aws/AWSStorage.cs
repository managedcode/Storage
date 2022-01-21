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

namespace ManagedCode.Storage.Aws
{
    public class AWSStorage : IBlobStorage
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucket;

        public AWSStorage(StorageOptions options)
        {
            _bucket = options.Bucket;

            var config = new AmazonS3Config
            {
                ServiceURL = Constants.ServiceUrl,
                Timeout = ClientConfig.MaxTimeout,
            };

            _s3Client = new AmazonS3Client(new BasicAWSCredentials(options.PublicKey, options.SecretKey), config);
        }

        public void Dispose() { }

        #region Delete

        public async Task DeleteAsync(string blob, CancellationToken cancellationToken = default)
        {
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucket,
                Key = blob
            }, cancellationToken);
        }

        public async Task DeleteAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            await DeleteAsync(blob.Name, cancellationToken);
        }

        public async Task DeleteAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                await DeleteAsync(blob, cancellationToken);
            }
        }

        public async Task DeleteAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                await DeleteAsync(blob, cancellationToken);
            }
        }

        #endregion

        #region Download

        public async Task<Stream> DownloadAsStreamAsync(string blob, CancellationToken cancellationToken = default)
        {
            return await _s3Client.GetObjectStreamAsync(_bucket, blob, null, cancellationToken);
        }

        public async Task<Stream> DownloadAsStreamAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await DownloadAsStreamAsync(blob.Name, cancellationToken);
        }

        public async Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default)
        {
            var localFile = new LocalFile();

            using (var stream = await DownloadAsStreamAsync(blob, cancellationToken))
            {
                await stream.CopyToAsync(localFile.FileStream, cancellationToken);
            }

            return localFile;
        }

        public async Task<LocalFile> DownloadAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await DownloadAsync(blob.Name, cancellationToken);
        }

        #endregion

        #region Exists

        public async Task<bool> ExistsAsync(string blob, CancellationToken cancellationToken = default)
        {
            try
            {
                await _s3Client.GetObjectAsync(_bucket, blob, cancellationToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExistsAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await ExistsAsync(blob.Name, cancellationToken);
        }

        public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                yield return await ExistsAsync(blob, cancellationToken);
            }
        }

        public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<Blob> blobs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                yield return await ExistsAsync(blob, cancellationToken);
            }
        }

        #endregion

        #region Get

        public async Task<Blob> GetBlobAsync(string blob, CancellationToken cancellationToken = default)
        {
            var objectMetaRequest = new GetObjectMetadataRequest
            {
                BucketName = _bucket,
                Key = blob
            };

            var objectMetaResponse = await _s3Client.GetObjectMetadataAsync(objectMetaRequest);

            var objectAclRequest = new GetACLRequest
            {
                BucketName = _bucket,
                Key = blob
            };

            var objectAclResponse = await _s3Client.GetACLAsync(objectAclRequest);
            var isPublic = objectAclResponse.AccessControlList.Grants.Any(x => x.Grantee.URI == "http://acs.amazonaws.com/groups/global/AllUsers");

            return new Blob
            {
                Name = blob
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

        public async IAsyncEnumerable<Blob> GetBlobListAsync(
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

                foreach (S3Object entry in objectsResponse.S3Objects)
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
                    var isPublic = objectAclResponse.AccessControlList.Grants.Any(x => x.Grantee.URI == "http://acs.amazonaws.com/groups/global/AllUsers");

                    yield return new Blob
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

        public async IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> blobs,
             [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                yield return await GetBlobAsync(blob, cancellationToken);
            }
        }

        #endregion

        #region Upload

        public async Task UploadAsync(string blob, string content, CancellationToken cancellationToken = default)
        {
            await UploadStreamAsync(blob, new MemoryStream(Encoding.UTF8.GetBytes(content)), cancellationToken);
        }

        public async Task UploadAsync(Blob blob, string content, CancellationToken cancellationToken = default)
        {
            await UploadAsync(blob.Name, content, cancellationToken);
        }

        public async Task UploadAsync(Blob blob, byte[] data, CancellationToken cancellationToken = default)
        {
            await UploadStreamAsync(blob.Name, new MemoryStream(data), cancellationToken);
        }

        public async Task UploadFileAsync(string blob, string pathToFile, CancellationToken cancellationToken = default)
        {
            using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
            {
                await UploadStreamAsync(blob, fs, cancellationToken);
            }
        }

        public async Task UploadFileAsync(Blob blob, string pathToFile, CancellationToken cancellationToken = default)
        {
            await UploadFileAsync(blob.Name, pathToFile, cancellationToken);
        }

        public async Task UploadStreamAsync(string blob, Stream dataStream, CancellationToken cancellationToken = default)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = blob,
                InputStream = dataStream,
                AutoCloseStream = true,
                ServerSideEncryptionMethod = null
            };

            await _s3Client.EnsureBucketExistsAsync(_bucket);
            await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        }

        public async Task UploadStreamAsync(Blob blob, Stream dataStream, CancellationToken cancellationToken = default)
        {
            await UploadStreamAsync(blob.Name, dataStream, cancellationToken);
        }

        #endregion
    }
}
