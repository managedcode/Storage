using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Aws
{
    public class AWSStorage : IBlobStorage
    {
        private const string DefaultServiceUrl = "https://s3.amazonaws.com";
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucket;
        private readonly string _serverSideEncryptionMethod;
        private readonly string _serviceUrl;

        public AWSStorage(StorageOptions options)
        {
            _serviceUrl = string.IsNullOrEmpty(options.ServiceUrl) ? DefaultServiceUrl : options.ServiceUrl;
            _bucket = options.Bucket;
            _serverSideEncryptionMethod = options.ServerSideEncryptionMethod;

            var S3Config = new AmazonS3Config
            {
                ServiceURL = _serviceUrl,
                Timeout = ClientConfig.MaxTimeout,
            };

            _s3Client = new AmazonS3Client(ReadCreds(options), S3Config);
        }

        private AWSCredentials ReadCreds(StorageOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.ProfileName))
            {
                var credentialProfileStoreChain = new CredentialProfileStoreChain();
                if (credentialProfileStoreChain.TryGetAWSCredentials(options.ProfileName, out AWSCredentials defaultCredentials))
                {
                    return defaultCredentials;
                }

                throw new AmazonClientException("Unable to find a default profile in CredentialProfileStoreChain.");
            }

            if (!string.IsNullOrEmpty(options.PublicKey) && !string.IsNullOrWhiteSpace(options.SecretKey))
            {
                return new BasicAWSCredentials(options.PublicKey, options.SecretKey);
            }

            return FallbackCredentialsFactory.GetCredentials();
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

        public Task<bool> ExistsAsync(string blob, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<bool> ExistsAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Get

        public Task<Blob> GetBlobAsync(string blob, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
                        Name = entry.Key.Remove(0, 1)
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

        public IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
                ServerSideEncryptionMethod = _serverSideEncryptionMethod
            };

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        }

        public async Task UploadStreamAsync(Blob blob, Stream dataStream, CancellationToken cancellationToken = default)
        {
            await UploadStreamAsync(blob.Name, dataStream, cancellationToken);
        }

        #endregion
    }
}
