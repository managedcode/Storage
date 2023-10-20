using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.IntegrationTests.Helpers;

public static class FileHelper
{
    private static readonly Random Random = new();

    public static LocalFile GenerateLocalFile(LocalFile file, int sizeInMegabytes)
    {
        var sizeInBytes = sizeInMegabytes * 1024 * 1024;

        using (var fileStream = file.FileStream)
        {
            Random random = new Random();
            byte[] buffer = new byte[1024]; // Buffer for writing in chunks

            while (sizeInBytes > 0)
            {
                int bytesToWrite = (int) Math.Min(sizeInBytes, buffer.Length);

                for (int i = 0; i < bytesToWrite; i++)
                {
                    buffer[i] = (byte) random.Next(65, 91); // 'A' to 'Z'
                    if (random.Next(2) == 0)
                    {
                        buffer[i] = (byte) random.Next(97, 123); // 'a' to 'z'
                    }
                }

                fileStream.Write(buffer, 0, bytesToWrite);
                sizeInBytes -= bytesToWrite;
            }
        }

        return file;
    }

    /*public static IFormFile GenerateFormFile(string fileName, int byteSize)
    {
        var localFile = GenerateLocalFile(fileName, byteSize);

        var ms = new MemoryStream();
        localFile.FileStream.CopyTo(ms);
        var formFile = new FormFile(ms, 0, ms.Length, fileName, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = MimeHelper.GetMimeType(localFile.FileInfo.Extension)
        };

        localFile.Dispose();

        return formFile;
    }

    public static string GenerateRandomFileName(string extension = "txt")
    {
        return $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}.{extension}";
    }

    public static string GenerateRandomFileContent()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ-0123456789_abcdefghijklmnopqrstuvwxyz";

        return new string(Enumerable.Repeat(chars, 250_000)
            .Select(s => s[Random.Next(s.Length)])
            .ToArray());
    }*/
}