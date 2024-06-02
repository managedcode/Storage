using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Azure;

public interface IAzureStorage : IStorage<BlobContainerClient, IStorageOptions>
{
    Task<Result<Stream>> OpenReadStreamAsync(string fileName, CancellationToken cancellationToken = default);
    Task<Result<Stream>> OpenWriteStreamAsync(string fileName, CancellationToken cancellationToken = default);

    Stream GetBlobStream(string fileName, bool userBuffer = true, int bufferSize = BlobStream.DefaultBufferSize);
}