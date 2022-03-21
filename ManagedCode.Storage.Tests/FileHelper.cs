using System;
using System.IO;
using ManagedCode.Storage.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace ManagedCode.Storage.Tests;

public static class FileHelper
{
    public static LocalFile GenerateLocalFile(string fileName, int byteSize)
    {
        var path = Path.GetTempPath();
        var localFile = new LocalFile($"{path}/{fileName}");

        localFile.FileStream.Seek(byteSize, SeekOrigin.Begin);
        localFile.FileStream.WriteByte(0);
        localFile.FileStream.Close();

        return localFile;
    }

    public static IFormFile GenerateFormFile(string fileName, int byteSize)
    {
        var localFile = GenerateLocalFile(fileName, byteSize);

        var ms = new MemoryStream();
        localFile.FileStream.CopyTo(ms);

        var formFile = new FormFile(ms, 0, ms.Length, fileName, fileName);

        return formFile;
    }

    public static string GenerateRandomFileName(string extension)
    {
        return $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}.{extension}";
    }
}