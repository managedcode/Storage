using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;

namespace ManagedCode.Storage.Core;

public interface IStreamer
{
    /// <summary>
    ///     Gets file stream.
    /// </summary>
    Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default);
}