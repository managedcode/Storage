using RestSharp;
using TestTask.Infrastructure.Extensions;
using ManagedCode.Communication;
using Microsoft.Extensions.Logging;
using TestTask.Infrastructure.Abstractions;

namespace TestTask.Infrastructure.Utilities
{
    public class ChunkedFileTransferUtility(ILogger<ChunkedFileTransferUtility> logger): IChunkedFileTransferUtility
    {
        private const long MaxChunkSize = 50 * 1024 * 1024; // 50 MB chunks
        private readonly ILogger _logger = logger;

        public async Task<Result> UploadFileInChunksAsync(RestClient restClient, string uploadRoute, string fileName, Stream fileStream, CancellationToken cancellationToken)
        {
            var fileLength = fileStream.Length;
            var totalChunks = (int)Math.Ceiling((double)fileLength / MaxChunkSize);
            var tasks = new List<Task<Result>>();

            for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                var offset = chunkIndex * MaxChunkSize;
                var chunkSize = Math.Min(MaxChunkSize, fileLength - offset);

                tasks.Add(UploadChunkAsync(restClient, uploadRoute, fileName, fileStream, offset, chunkSize, chunkIndex, totalChunks, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);

            return results.Any(result => !result.IsSuccess) ? Result.Fail("Failed to upload one or more chunks.") : Result.Succeed();
        }

        private async Task<Result> UploadChunkAsync(RestClient restClient, string uploadRoute, string fileName, Stream fileStream, long offset, long chunkSize, int chunkIndex, int totalChunks, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[chunkSize];
                lock (fileStream)
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    fileStream.Read(buffer, 0, (int)chunkSize);
                }

                var restRequest = new RestRequest($"{uploadRoute}/chunk", Method.Post);
                restRequest.AddParameter("FileName", fileName);
                restRequest.AddParameter("ChunkIndex", chunkIndex);
                restRequest.AddParameter("TotalChunks", totalChunks);
                restRequest.AddFile("file", buffer, fileName);

                var response = await restClient.HandleRestClientResponse<bool>(restRequest, _logger, cancellationToken);

                return response.IsSuccess ? Result.Succeed() : Result.Fail(response.GetError().GetValueOrDefault());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading chunk {ChunkIndex}", chunkIndex);
                return Result.Fail($"Error uploading chunk {chunkIndex}");
            }
        }

        public async Task<Result<Stream>> DownloadFileInChunksAsync(RestClient restClient, string downloadRoute, string fileName, long fileLength, CancellationToken cancellationToken)
        {
            var totalChunks = (int)Math.Ceiling((double)fileLength / MaxChunkSize);
            var memoryStream = new MemoryStream();

            for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                var offset = chunkIndex * MaxChunkSize;
                var chunkSize = Math.Min(MaxChunkSize, fileLength - offset);

                var result = await DownloadChunkAsync(restClient, downloadRoute, fileName, offset, chunkSize, cancellationToken);
                if (result.IsSuccess)
                {
                    await memoryStream.WriteAsync(result.Value, 0, result.Value.Length, cancellationToken);
                }
                else
                {
                    return Result<Stream>.Fail(result.GetError());
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            return Result<Stream>.Succeed(memoryStream);
        }

        private async Task<Result<byte[]>> DownloadChunkAsync(RestClient restClient, string downloadRoute, string fileName, long offset, long chunkSize, CancellationToken cancellationToken)
        {
            try
            {
                var restRequest = new RestRequest($"{downloadRoute}/chunk/{fileName}", Method.Get);
                restRequest.AddParameter("Offset", offset);
                restRequest.AddParameter("ChunkSize", chunkSize);

                var response = await restClient.ExecuteAsync(restRequest, cancellationToken);

                if (response.IsSuccessful && response.RawBytes != null)
                {
                    return Result<byte[]>.Succeed(response.RawBytes);
                }

                _logger.LogError("Failed to download chunk at offset {Offset}", offset);
                return Result<byte[]>.Fail($"Failed to download chunk at offset {offset}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading chunk at offset {Offset}", offset);
                return Result<byte[]>.Fail($"Error downloading chunk at offset {offset}");
            }
        }
    }
}
