using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Extensions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Server.Controllers;

public class FileController : ControllerBase
{
    private readonly IStorage _storage;

    public FileController(IStorage storage)
    {
        _storage = storage;
    }

    [HttpPost("upload")]
    public async Task<Result<BlobMetadata>> UploadToStorageAsync(IBrowserFile formFile,
        UploadOptions? options = null)
    {
        return await _storage.UploadToStorageAsync(formFile, options);
    }
}