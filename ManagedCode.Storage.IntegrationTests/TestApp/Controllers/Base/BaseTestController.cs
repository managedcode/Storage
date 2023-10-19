using Amazon.Runtime.Internal;
using ManagedCode.Communication;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers.Base;

[ApiController]
public abstract class BaseTestController<TStorage> : ControllerBase
    where TStorage : IStorage
{
    protected readonly IStorage Storage;
    protected readonly ResponseContext ResponseData;
    protected readonly int ChunkSize;

    protected BaseTestController(TStorage storage)
    {
        Storage = storage;
        ResponseData = new ResponseContext();
        ChunkSize = 100000000;
    }

    [HttpPost("upload")]
    public async Task<Result<BlobMetadata>> UploadFileAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (Request.HasFormContentType is false)
        {
            return Result<BlobMetadata>.Fail("invalid body");
        }

        return await Storage.UploadAsync(file.OpenReadStream(), cancellationToken);
    }

    [HttpGet("download/{fileName}")]
    public async Task<FileResult> DownloadFileAsync([FromRoute] string fileName)
    {
        var result = await Storage.DownloadAsFileResult(fileName);
        
        result.ThrowIfFail();

        return result.Value!;
    }
}