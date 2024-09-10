using ManagedCode.Communication;
using RestSharp;

namespace TestTask.Infrastructure.Abstractions;

public interface IChunkedFileTransferUtility
{
    Task<Result> UploadFileInChunksAsync(RestClient restClient, string uploadRoute, string fileName, Stream fileStream, CancellationToken cancellationToken);
    Task<Result<Stream>> DownloadFileInChunksAsync(RestClient restClient, string downloadRoute, string fileName, long fileLength, CancellationToken cancellationToken);
}