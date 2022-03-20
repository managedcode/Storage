using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.AspNetExtensions;

public static class FormFileExtensions
{
    public static async Task<LocalFile> ToLocalFileAsync(this IFormFile formFile)
    {
        LocalFile localFile = new();

        await formFile.CopyToAsync(localFile.FileStream);

        return localFile;
    }

    public static async Task<IEnumerable<LocalFile>> ToLocalFilesAsync(this IFormFileCollection formFileCollection)
    {
        List<LocalFile> localFiles = new();

        foreach (var formFile in formFileCollection)
        {
            localFiles.Add(await formFile.ToLocalFileAsync());
        }

        return localFiles;
    }
}