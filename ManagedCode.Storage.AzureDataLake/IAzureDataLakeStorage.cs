using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Files.DataLake;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.AzureDataLake;

public interface IAzureDataLakeStorage : IStorage<DataLakeFileSystemClient>
{
    Task<Result> CreateDirectoryAsync(string directory, CancellationToken cancellationToken = default);
    Task<Result> RenameDirectory(string directory, string newDirectory, CancellationToken cancellationToken = default);
    Task<Result> DeleteDirectory(string directory, CancellationToken cancellationToken = default);

    public abstract IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string directory, CancellationToken cancellationToken = default);
}