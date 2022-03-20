using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.AspNetExtensions;

public static class StorageExtensions
{
    public static async Task UploadToStorageAsync(this IStorage storage, IFormFile formFile, CancellationToken cancellationToken = default)
    {
        var kilobytes = formFile.Length / 1024;

        BlobMetadata blobMetadata = new()
        {
            Name = formFile.FileName,
            ContentType = formFile.ContentType
        };

        if (kilobytes > 512)
        {
            var localFile = await formFile.ToLocalFileAsync();

            await storage.UploadStreamAsync(blobMetadata, localFile.FileStream, cancellationToken);
        }
        else
        {
            using (var stream = formFile.OpenReadStream())
            {
                await storage.UploadStreamAsync(blobMetadata, stream, cancellationToken);
            }
        }
    }
}