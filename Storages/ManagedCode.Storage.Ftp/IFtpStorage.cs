using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Ftp.Options;

namespace ManagedCode.Storage.Ftp;

/// <summary>
/// Represents an FTP storage interface with specific FTP operations.
/// </summary>
public interface IFtpStorage : IStorage<FtpClient, IFtpStorageOptions>
{
    /// <summary>
    /// Opens a read stream for the specified file.
    /// </summary>
    /// <param name="fileName">The name of the file to read.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the read stream.</returns>
    Task<Result<Stream>> OpenReadStreamAsync(string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Opens a write stream for the specified file.
    /// </summary>
    /// <param name="fileName">The name of the file to write.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the write stream.</returns>
    Task<Result<Stream>> OpenWriteStreamAsync(string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to the FTP server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the connection was successful.</returns>
    Task<Result<bool>> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the working directory on the FTP server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the working directory path.</returns>
    Task<Result<string>> GetWorkingDirectoryAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Changes the working directory on the FTP server.
    /// </summary>
    /// <param name="directory">The directory to change to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<Result> ChangeWorkingDirectoryAsync(string directory, CancellationToken cancellationToken = default);
}