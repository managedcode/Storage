using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.AspNetExtensions.Helpers;

internal static class FileSaver
{
    internal static async Task<(Stream, FileInfo)> SaveTemporaryFile(Stream stream)
    {
        var filePath = Path.GetTempFileName();
        FileInfo file = new(filePath);

        using var fileStream = file.Create();
        await stream.CopyToAsync(fileStream);
        fileStream.Position = 0;
        stream.Close();

        return (fileStream, file);
    }

    internal static async Task<(Stream, FileInfo)> SaveTemporaryFile(IFormFile formFile)
    {
        var filePath = Path.GetTempFileName();
        FileInfo file = new(filePath);

        using var fileStream = file.Create();
        await formFile.CopyToAsync(fileStream);
        fileStream.Position = 0;

        return (fileStream, file);
    }
}