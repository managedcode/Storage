using Azure.Storage.Blobs.Specialized;
using ManagedCode.Communication;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.IntegrationTests.TestApp.Controllers.Base;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers;

[Route("azure")]
[ApiController]
public class AzureTestController : BaseTestController<IAzureStorage>
{
    private readonly IAzureStorage _storage;

    public AzureTestController(IAzureStorage storage) : base(storage)
    {
        _storage = storage;
    }    
    
    [HttpPost("upload-chunks-stream/create")]
    public async Task<Result<BlobMetadata>> CreateFile([FromBody] long fileSize, CancellationToken cancellationToken)
    {
        return await Storage.UploadAsync(new MemoryStream(new byte[fileSize]), cancellationToken);
    }
    
    [HttpPost("upload-chunks-stream/upload")]
    public async Task<Result> UploadChunksUsingStream([FromForm] FileUploadPayload file, CancellationToken cancellationToken)
    {
        using (var stream = new BlobStream(_storage.StorageClient.GetPageBlobClient(file.Payload.BlobName)))
        {
            byte[] bytes = new byte[file.Payload.ChunkSize];
            int bytesRead = 0;
            int offset = (file.Payload.ChunkIndex - 1) * file.Payload.ChunkSize;
            
            while ((bytesRead = await file.File.OpenReadStream().ReadAsync(bytes, 0, bytes.Length, cancellationToken)) > 0)
            {
                await stream.WriteAsync(bytes, offset, bytesRead, cancellationToken);
            }
        }

        return Result.Succeed();
    }
    
    [HttpPost("upload-chunks-merge/upload")]
    public async Task<Result<string>> UploadChunksUsingMerge([FromForm] FileUploadPayload file, CancellationToken cancellationToken)
    {
        var uploadResult = await _storage.UploadToStorageAsync(file.File, 
            new UploadOptions($"{file.File.Name}_{file.Payload.ChunkIndex}", $"{file.File.Name}_directory" ), 
            cancellationToken: cancellationToken);

        if (uploadResult.IsSuccess)
        {
            return Result.Succeed(uploadResult.Value.FullName);
        }

        return Result.Fail();
    }
    
    [HttpPost("upload-chunks-merge/complete")]
    public async Task<Result<BlobMetadata>> UploadChunksUsingMergeComplete(uint fileCrc, List<string> blobNames, CancellationToken cancellationToken)
    {
        using (var memoryStream = new MemoryStream())
        {
            foreach (var blobName in blobNames)
            {
                var file = await _storage.DownloadAsync(blobName, cancellationToken: cancellationToken);

                using (Stream stream = file.Value.FileStream)
                {
                    await memoryStream.CopyToAsync(stream, cancellationToken);
                }
            }

            var result = await _storage.UploadAsync(memoryStream, cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return Result.Succeed(result.Value);
            }
        }

        return Result.Fail();
    }

    [HttpPost("upload-chunks-stream/complete")]
    public async Task<bool> UploadChunksUsingStramComplete([FromBody] uint fileCrc, string blobName)
    {
        using (var stream = new BlobStream(_storage.StorageClient.GetPageBlobClient(blobName)))
        {
            uint blobCrc = Crc32Helper.Calculate(stream);
            return blobCrc == fileCrc;
        }
    }
}