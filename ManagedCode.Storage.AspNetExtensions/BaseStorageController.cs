using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.AspNetExtensions;

public class BaseStorageController : Controller
{
    private readonly IStorage _storage;

    private const int MinLengthForLargeFile = 256 * 1024;

    public BaseStorageController(IStorage storage)
    {
        _storage = storage;
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IBrowserFile formFile,
        UploadOptions? options = null)
    {
        return await _storage.UploadToStorageAsync(formFile, options);
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IBrowserFile formFile,
        Action<UploadOptions> options)
    {
        return await _storage.UploadToStorageAsync(formFile, options);
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(
        IBrowserFile formFile,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UploadOptions(formFile.Name, mimeType: formFile.ContentType);

        if (formFile.Size > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            return await _storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        }

        await using (var stream = formFile.OpenReadStream())
        {
            return await _storage.UploadAsync(stream, options, cancellationToken);
        }
    }

    protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IBrowserFile formFile,
        Action<UploadOptions> options,
        CancellationToken cancellationToken = default)
    {
        var newOptions = new UploadOptions(formFile.Name, mimeType: formFile.ContentType);
        options.Invoke(newOptions);

        if (formFile.Size > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            return await _storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        }

        await using (var stream = formFile.OpenReadStream())
        {
            return await _storage.UploadAsync(stream, options, cancellationToken);
        }
    }
}