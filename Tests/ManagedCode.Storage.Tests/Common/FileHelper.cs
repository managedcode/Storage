using System;
using System.IO;
using System.Linq;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Tests.Common;

public static class FileHelper
{
    private static readonly Random Random = new();

    public static LocalFile GenerateLocalFile(LocalFile localFile, int byteSize)
    {
        var fs = localFile.FileStream;

        fs.Seek(byteSize, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.Close();

        return localFile;
    }

    public static LocalFile GenerateLocalFile(string fileName, int byteSize)
    {
        var path = Path.Combine(Environment.CurrentDirectory, fileName);
        var localFile = new LocalFile(path);

        var fs = localFile.FileStream;

        fs.Seek(byteSize, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.Close();

        return localFile;
    }

    public static LocalFile GenerateLocalFileWithData(LocalFile file, int sizeInBytes)
    {
        using (var fileStream = file.FileStream)
        {
            var random = new Random();
            var buffer = new byte[1024]; // Buffer for writing in chunks

            while (sizeInBytes > 0)
            {
                var bytesToWrite = Math.Min(sizeInBytes, buffer.Length);

                for (var i = 0; i < bytesToWrite; i++)
                {
                    buffer[i] = (byte)random.Next(65, 91); // 'A' to 'Z'
                    if (random.Next(2) == 0)
                        buffer[i] = (byte)random.Next(97, 123); // 'a' to 'z'
                }

                fileStream.Write(buffer, 0, bytesToWrite);
                sizeInBytes -= bytesToWrite;
            }
        }

        return file;
    }

    public static IFormFile GenerateFormFile(string fileName, int byteSize)
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

    public static string GenerateRandomFileName()
    {
        string[] extensions = { "txt", "jpg", "png", "pdf", "docx", "xlsx", "pptx", "mp3", "mp4", "zip" };
        var randomExtension = extensions[Random.Next(extensions.Length)];
        return $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}.{randomExtension}";
    }

    public static string GenerateRandomFileContent(int charCount = 250_000)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ-0123456789_abcdefghijklmnopqrstuvwxyz";

        return new string(Enumerable.Repeat(chars, charCount)
            .Select(s => s[Random.Next(s.Length)])
            .ToArray());
    }
}
