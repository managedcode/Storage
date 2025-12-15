using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Server;
using ManagedCode.Storage.Server.Extensions.File;
using ManagedCode.Storage.Tests.Common;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ManagedCode.Storage.Tests.ExtensionsTests;

public class FormFileExtensionsTests
{
    [Fact]
    public async Task ToLocalFileAsync_SmallFile()
    {
        // Arrange
        const int size = 200 * 1024; // 200 KB
        var fileName = FileHelper.GenerateRandomFileName();
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        var localFile = await formFile.ToLocalFileAsync();

        // Assert
        localFile.FileStream.Length.ShouldBe(formFile.Length);
        Path.GetExtension(localFile.Name).ShouldBe(Path.GetExtension(formFile.FileName));
    }

    [Fact]
    public async Task ToLocalFileAsync_LargeFile()
    {
        // Arrange
        const int size = 300 * 1024 * 1024; // 300 MB
        var fileName = FileHelper.GenerateRandomFileName();
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        var localFile = await formFile.ToLocalFileAsync();

        // Assert
        localFile.FileStream.Length.ShouldBe(formFile.Length);
        Path.GetExtension(localFile.Name).ShouldBe(Path.GetExtension(formFile.FileName));
    }

    [Fact]
    public async Task ToLocalFilesAsync_SmallFiles()
    {
        // Arrange
        const int filesCount = 10;
        Random random = new();
        FormFileCollection collection = new();

        for (var i = 0; i < filesCount; i++)
        {
            var size = random.Next(10, 1000) * 1024;
            var fileName = FileHelper.GenerateRandomFileName();
            collection.Add(FileHelper.GenerateFormFile(fileName, size));
        }

        // Act
        var localFiles = await collection.ToLocalFilesAsync().ToListAsync();

        // Assert
        localFiles.Count.ShouldBe(filesCount);

        for (var i = 0; i < filesCount; i++)
        {
            localFiles[i].FileStream.Length.ShouldBe(collection[i].Length);
            Path.GetExtension(localFiles[i].Name).ShouldBe(Path.GetExtension(collection[i].FileName));
        }
    }
}

