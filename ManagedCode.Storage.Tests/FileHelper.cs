using System;
using System.IO;
using System.Linq;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Tests;

public static class FileHelper
{
    private static readonly Random Random = new();

    public static LocalFile GenerateLocalFile(string fileName, int byteSize)
    {
        var path = Path.Combine(Path.GetTempPath(), fileName);
        var localFile = new LocalFile(path);

        var fs = localFile.FileStream;

        fs.Seek(byteSize, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.Close();

        return localFile;
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

    public static string GenerateRandomFileName(string extension = "txt")
    {
        return $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}.{extension}";
    }

    public static string GenerateRandomFileContent()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        return new string(Enumerable.Repeat(chars, 250_000)
            .Select(s => s[Random.Next(s.Length)])
            .ToArray());
    }
}