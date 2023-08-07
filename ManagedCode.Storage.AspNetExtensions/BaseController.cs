using System;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.AspNetExtensions;

public class BaseController : Controller
{
    private readonly IStorage _storage;

    public BaseController(IStorage storage)
    {
        _storage = storage;
    }

    protected async Task<Result<BlobMetadata>>UploadToStorageAsync(IBrowserFile formFile,
        UploadOptions? options = null)
    {
        return await _storage.UploadToStorageAsync(formFile, options);
    }
    
    protected async Task<Result<BlobMetadata>>UploadToStorageAsync(IBrowserFile formFile,
        Action<UploadOptions> options)
    {
        return await _storage.UploadToStorageAsync(formFile, options);
    }
}