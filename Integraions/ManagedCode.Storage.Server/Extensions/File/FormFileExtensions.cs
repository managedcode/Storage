using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Server;

public static class FormFileExtensions
{
    public static async Task<LocalFile> ToLocalFileAsync(this IFormFile formFile, CancellationToken cancellationToken = default)
    {
        var localFile = LocalFile.FromRandomNameWithExtension(formFile.FileName);
        await localFile.CopyFromStreamAsync(formFile.OpenReadStream(), cancellationToken);
        return localFile;
    }

    public static async IAsyncEnumerable<LocalFile> ToLocalFilesAsync(this IFormFileCollection formFileCollection,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var formFile in formFileCollection)
            yield return await formFile.ToLocalFileAsync(cancellationToken);
    }
}
