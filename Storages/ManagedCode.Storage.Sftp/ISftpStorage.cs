using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Sftp.Options;

namespace ManagedCode.Storage.Sftp;

/// <summary>
/// Contract implemented by the SFTP storage provider for stream-oriented operations.
/// </summary>
public interface ISftpStorage : IStorage
{
    Task<Result<Stream>> OpenReadStreamAsync(string fileName, CancellationToken cancellationToken = default);
    Task<Result<Stream>> OpenWriteStreamAsync(string fileName, CancellationToken cancellationToken = default);
    Task<Result<bool>> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> GetWorkingDirectoryAsync(CancellationToken cancellationToken = default);
    Task<Result> ChangeWorkingDirectoryAsync(string directory, CancellationToken cancellationToken = default);
}
