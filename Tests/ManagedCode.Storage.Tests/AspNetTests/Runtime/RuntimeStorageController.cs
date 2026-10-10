using ManagedCode.Storage.Core;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Server.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Tests.AspNetTests.Runtime;

[Route("runtime/storage")]
public sealed class RuntimeStorageController(IStorage storage, ChunkUploadService chunks, StorageServerOptions options)
    : StorageControllerBase<IStorage>(storage, chunks, options)
{
    [HttpPost("upload/raw-multipart")]
    [DisableFormBinding]
    public async System.Threading.Tasks.Task<IActionResult> UploadRawMultipartAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await ManagedCode.Storage.Server.Extensions.Controller.ControllerUploadExtensions.UploadFromStreamAsync(
                this, Storage, Request, cancellationToken: cancellationToken);
            return Ok(metadata);
        }
        catch (System.Exception exception) { return BadRequest(exception.Message); }
    }
}
