using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Server.Extensions.File;

public static class FormFileExtensions
{
    public static async Task<LocalFile> ToLocalFileAsync(this IFormFile formFile, CancellationToken cancellationToken = default)
    {
        var localFile = LocalFile.FromRandomNameWithExtension(formFile.FileName);
        await using (var stream = formFile.OpenReadStream())
        {
            await localFile.CopyFromStreamAsync(stream, cancellationToken);
        }
        return localFile;
    }

    public static async IAsyncEnumerable<LocalFile> ToLocalFilesAsync(this IFormFileCollection formFileCollection,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var localFileTask in formFileCollection.Select(formFile => formFile.ToLocalFileAsync(cancellationToken)))
        {
            yield return await localFileTask;
        }
    }
}
