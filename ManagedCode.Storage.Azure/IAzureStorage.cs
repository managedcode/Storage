using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Files.DataLake;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Azure;

public interface IAzureStorage : IStorage<BlobContainerClient>
{
    Task<Result<Stream>> OpenReadStreamAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<Stream>> OpenWriteStreamAsync(string blob, CancellationToken cancellationToken = default);
}