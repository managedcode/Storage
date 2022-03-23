using ManagedCode.Storage.AspNetExtensions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
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

    #region Upload files using the extension for storage from Managed Code.Storage.AspNet Extensions;

    [HttpPost("file")]
    public async Task<ActionResult<BlobMetadata>> UploadFile(IFormFile formFile)
    {
        var metadata = await _storage.UploadToStorageAsync(formFile);

        return metadata;
    }

    [HttpPost("files")]
    public async Task<ActionResult<IEnumerable<BlobMetadata>>> UploadFiles(IFormFileCollection formFileCollection)
    {
        var metadata = await _storage.UploadToStorageAsync(formFileCollection).ToListAsync();

        return metadata;
    }

    [HttpPost("localFile")]
    public async Task<ActionResult> UploadFileUsingLocalFile(IFormFile formFile)
    {
        await using var localFile = await formFile.ToLocalFileAsync();

        // Here you can do manipulations with file

        await _storage.UploadStreamAsync(localFile.FileName, localFile.FileStream);

        return Ok();
    }

    #endregion

    #region Download files using the extension for storage from Managed Code.Storage.AspNet Extensions;

    [HttpGet("file")]
    public Task<FileResult> DownloadFile(string fileName)
    {
        return _storage.DownloadAsFileResult(fileName);
    }

    #endregion
}