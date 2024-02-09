using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Client;

public interface IStorageClient : IUploader, IDownloader
{
    void SetChunkSize(long size);
    
    /// <summary>
    ///     Event triggered when the progress status changes during an upload or download operation.
    /// </summary>
    /// <remarks>
    ///     The event handler receives an argument of type <see cref="ProgressStatus"/> which contains detailed information about the progress of the operation.
    ///     This includes the file name, progress percentage, total bytes, transferred bytes, elapsed time, remaining time, speed, and any error message.
    /// </remarks>
    event EventHandler<ProgressStatus> OnProgressStatusChanged;
}


public record ProgressStatus(
    string File, 
    float Progress,
    long TotalBytes, 
    long TransferredBytes, 
    TimeSpan Elapsed, 
    TimeSpan Remaining, 
    string Speed, 
    string? Error = null);