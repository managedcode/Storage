using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers.Base;

[ApiController]
public abstract class BaseFileController<TStorage> : ControllerBase
    where TStorage : IStorage
{
    protected readonly IStorage Storage;

    protected BaseFileController(IStorage storage)
    {
        Storage = storage;
    }

    [HttpPost("upload")]
    public async Task<Result<BlobMetadata>> UploadFile([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (Request.HasFormContentType is false)
        {
            return Result<BlobMetadata>.Fail("invalid body");
        }

        return await Storage.UploadAsync(file.OpenReadStream(), cancellationToken);
    }

    [HttpGet("download/{fileName}")]
    public async Task<FileResult> DownloadFile([FromRoute] string fileName, CancellationToken cancellationToken)
    {
        var result = await Storage.DownloadAsFileResult(fileName, cancellationToken: cancellationToken);

        result.ThrowIfFail();

        return result.Value!;
    }
}