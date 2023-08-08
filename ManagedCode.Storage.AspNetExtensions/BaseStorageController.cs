using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.AspNetExtensions;

public class BaseStorageController : Controller
{
    private readonly IStorage _storage;

    public BaseStorageController(IStorage storage)
    {
        _storage = storage;
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IBrowserFile browserFile,
        UploadOptions? options = null)
    {
        return await _storage.UploadToStorageAsync(browserFile, options);
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IBrowserFile browserFile,
        Action<UploadOptions> options)
    {
        return await _storage.UploadToStorageAsync(browserFile, options);
    }

    protected async Task<Result<FileResult>> DownloadAsFileResultAsync(string blobName)
    {
        return await _storage.DownloadAsFileResultAsync(blobName);
    }

    protected async Task<Result<FileResult>> DownloadAsFileResultAsync(BlobMetadata blobMetadata)
    {
        return await _storage.DownloadAsFileResultAsync(blobMetadata);
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IFormFile formFile, UploadOptions? options = null)
    {
        return await _storage.UploadToStorageAsync(formFile, options);
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IFormFile formFile, Action<UploadOptions> options)
    {
        return await _storage.UploadToStorageAsync(formFile, options);
    }

    protected async IAsyncEnumerable<Result<BlobMetadata>> UploadToStorageAsync(IFormFileCollection formFiles, UploadOptions? options = null)
    {
        foreach (var formFile in formFiles)
        {
            yield return await _storage.UploadToStorageAsync(formFile, options);
        }
    }

    protected async IAsyncEnumerable<Result<BlobMetadata>> UploadToStorageAsync(IFormFileCollection formFiles, Action<UploadOptions> options)
    {
        foreach (var formFile in formFiles)
        {
            yield return await _storage.UploadToStorageAsync(formFile, options);
        }
    }
}