using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.DownloadManager.Helpers;

public static class FileSaver
{
    public static async Task<(FileInfo, Stream)> SaveTemporaryFile(Stream stream)
    {
        var filePath = Path.GetTempFileName();
        FileInfo file = new(filePath);

        using Stream fileStream = file.Create();
        await stream.CopyToAsync(fileStream);
        fileStream.Position = 0;
        stream.Close();

        return (file, fileStream);
    }

    public static async Task<(FileInfo, Stream)> SaveTemporaryFile(IFormFile formFile)
    {
        var filePath = Path.GetTempFileName();
        FileInfo file = new(filePath);

        using Stream fileStream = file.Create();
        await formFile.CopyToAsync(fileStream);
        fileStream.Position = 0;

        return (file, fileStream);
    }
}