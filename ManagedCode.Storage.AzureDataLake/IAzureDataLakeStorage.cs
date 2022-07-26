using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Files.DataLake;
using ManagedCode.Communication;
using ManagedCode.Storage.AzureDataLake.Options;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.AzureDataLake;

public interface IAzureDataLakeStorage : IStorage<DataLakeFileSystemClient>
{
    /// <summary>
    /// Create directory
    /// </summary>
    Task<Result> CreateDirectoryAsync(string directory, CancellationToken cancellationToken = default);
    /// <summary>
    /// Rename directory
    /// </summary>
    Task<Result> RenameDirectory(string directory, string newDirectory, CancellationToken cancellationToken = default);

    Task<Result<Stream>> OpenWriteStreamAsync(OpenWriteStreamOptions options,
        CancellationToken cancellationToken = default);

    Task<Result<Stream>> OpenReadStreamAsync(OpenReadStreamOptions options,
        CancellationToken cancellationToken = default);
}