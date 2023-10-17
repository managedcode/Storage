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

namespace ManagedCode.Storage.Server;

public abstract class BaseController : ControllerBase
{
    protected readonly IStorage Storage;

    protected BaseController(IStorage storage)
    {
        Storage = storage;
    }

    protected async Task<Result<BlobMetadata>>UploadToStorageAsync(IBrowserFile formFile,
        UploadOptions? options = null)
    {
        return await Storage.UploadToStorageAsync(formFile, options);
    }
    
    protected async Task<Result<BlobMetadata>>UploadToStorageAsync(IBrowserFile formFile,
        Action<UploadOptions> options)
    {
        return await Storage.UploadToStorageAsync(formFile, options);
    }

    protected async Task<Result<FileResult>> DownloadAsFileResult(string blobName, CancellationToken cancellationToken = default)
    {
        return await Storage.DownloadAsFileResult(blobName, cancellationToken);
    }
    protected async Task<Result<FileResult>> DownloadAsFileResult(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await Storage.DownloadAsFileResult(blobMetadata, cancellationToken);
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IFormFile formFile,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await Storage.UploadToStorageAsync(formFile, options, cancellationToken);
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IFormFile formFile,
        Action<UploadOptions> options,
        CancellationToken cancellationToken = default)
    {
        return await Storage.UploadToStorageAsync(formFile, options, cancellationToken);
    }
    protected async IAsyncEnumerable<Result<BlobMetadata>> UploadToStorageAsync(IFormFileCollection formFiles,
        UploadOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var formFile in formFiles)
        {
            yield return await Storage.UploadToStorageAsync(formFile, options, cancellationToken);
        }
    }
    protected async IAsyncEnumerable<Result<BlobMetadata>> UploadToStorageAsync(IFormFileCollection formFiles,
        Action<UploadOptions> options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var formFile in formFiles)
        {
            yield return await Storage.UploadToStorageAsync(formFile, options, cancellationToken);
        }
    }

}