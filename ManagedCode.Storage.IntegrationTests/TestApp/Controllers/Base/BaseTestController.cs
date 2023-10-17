using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers.Base;

[ApiController]
public abstract class BaseTestController<TStorage> : BaseController
    where TStorage : IStorage
{
    protected BaseTestController(TStorage storage) : base(storage)
    {
        
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