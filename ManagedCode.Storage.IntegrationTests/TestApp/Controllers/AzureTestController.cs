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
    
    [HttpPost("upload-chunks/create")]
    public async Task<Result<BlobMetadata>> CreateFile([FromBody] long fileSize, CancellationToken cancellationToken)
    {
        return await Storage.UploadAsync(new MemoryStream(new byte[fileSize]), cancellationToken);
    }
    
    [HttpPost("upload-chunks/upload")]
    public async Task<Result> UploadChunks([FromForm] FileUploadPayload file, CancellationToken cancellationToken)
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
            
            //check crc
        }

        return Result.Succeed();
    }

    [HttpPost("upload-chunks/complete")]
    public async Task<bool> UploadComplete([FromBody] uint fileCrc, string blobName)
    {
        using (var stream = new BlobStream(_storage.StorageClient.GetPageBlobClient(blobName)))
        {
            uint blobCrc = Crc32Helper.Calculate(stream);
            return blobCrc == fileCrc;
        }
    }
}