using ManagedCode.Storage.Server;
using ManagedCode.Storage.Core;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StorageController : Controller
{
    private readonly IStorage _storage;

    public StorageController(IStorage storage)
    {
        _storage = storage;
    }

    #region Download files using the extension for storage from Managed Code.Storage.AspNet Extensions;

    [HttpGet("file")]
    public async Task<FileResult> DownloadFile(string fileName)
    {
        var result = await _storage.DownloadAsFileResult(fileName);

        return result.Value!;
    }

    #endregion

    #region Upload files using the extension for storage from Managed Code.Storage.AspNet Extensions;

    [HttpPost("file")]
    public async Task<ActionResult<string>> UploadFile(IFormFile formFile)
    {
        var result = await _storage.UploadToStorageAsync(formFile);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.GetError()?.Message);
    }

    [HttpPost("files")]
    public async Task<ActionResult<IEnumerable<string>>> UploadFiles(IFormFileCollection formFileCollection)
    {
        var results = await _storage.UploadToStorageAsync(formFileCollection).ToListAsync();

        return Ok(results.Where(r => r.IsSuccess).Select(r => r.Value));
    }

    [HttpPost("localFile")]
    public async Task<ActionResult> UploadFileUsingLocalFile(IFormFile formFile)
    {
        await using var localFile = await formFile.ToLocalFileAsync();

        // Here you can do manipulations with file

        await _storage.UploadAsync(localFile.FileStream, options =>
        {
            options.FileName = formFile.Name;
            options.MimeType = options.MimeType;
        });

        return Ok();
    }

    #endregion
}