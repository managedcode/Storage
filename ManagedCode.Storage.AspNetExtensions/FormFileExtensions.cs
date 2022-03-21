using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.AspNetExtensions;

public static class FormFileExtensions
{
    public static async Task<LocalFile> ToLocalFileAsync(this IFormFile formFile, CancellationToken cancellationToken = default)
    {
        var tempPath = Path.GetTempPath();
        LocalFile localFile = new($"{tempPath}/{formFile.Name}");

        await formFile.CopyToAsync(localFile.FileStream, cancellationToken);

        return localFile;
    }

    public static async Task<IEnumerable<LocalFile>> ToLocalFilesAsync(this IFormFileCollection formFileCollection,
        CancellationToken cancellationToken = default)
    {
        List<LocalFile> localFiles = new();

        foreach (var formFile in formFileCollection)
        {
            localFiles.Add(await formFile.ToLocalFileAsync(cancellationToken));
        }

        return localFiles;
    }
}