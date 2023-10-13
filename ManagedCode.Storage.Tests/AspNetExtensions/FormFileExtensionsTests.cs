// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using FluentAssertions;
// using ManagedCode.Storage.AspNetExtensions;
// using Microsoft.AspNetCore.Http;
// using Xunit;
//
// namespace ManagedCode.Storage.Tests.AspNetExtensions;
//
// public class FormFileExtensionsTests
// {
//     [Fact]
//     public async Task ToLocalFileAsync_SmallFile()
//     {
//         // Arrange
//         const int size = 200 * 1024; // 200 KB
//         var fileName = FileHelper.GenerateRandomFileName();
//         var formFile = FileHelper.GenerateFormFile(fileName, size);
//
//         // Act
//         var localFile = await formFile.ToLocalFileAsync();
//
//         // Assert
//         localFile.FileStream.Length.Should().Be(formFile.Length);
//         localFile.Name.Should().Be(formFile.FileName);
//     }
//
//     [Fact]
//     public async Task ToLocalFileAsync_LargeFile()
//     {
//         // Arrange
//         const int size = 300 * 1024 * 1024; // 300 MB
//         var fileName = FileHelper.GenerateRandomFileName();
//         var formFile = FileHelper.GenerateFormFile(fileName, size);
//
//         // Act
//         var localFile = await formFile.ToLocalFileAsync();
//
//         // Assert
//         localFile.FileStream.Length.Should().Be(formFile.Length);
//         localFile.Name.Should().Be(formFile.FileName);
//     }
//
//     [Fact]
//     public async Task ToLocalFilesAsync_SmallFiles()
//     {
//         // Arrange
//         const int filesCount = 10;
//         Random random = new();
//         FormFileCollection collection = new();
//
//         for (var i = 0; i < filesCount; i++)
//         {
//             var size = random.Next(10, 1000) * 1024;
//             var fileName = FileHelper.GenerateRandomFileName();
//             collection.Add(FileHelper.GenerateFormFile(fileName, size));
//         }
//
//         // Act
//         var localFiles = await collection.ToLocalFilesAsync().ToListAsync();
//
//         // Assert
//         localFiles.Count.Should().Be(filesCount);
//
//         for (var i = 0; i < filesCount; i++)
//         {
//             localFiles[i].FileStream.Length.Should().Be(collection[i].Length);
//             localFiles[i].Name.Should().Be(collection[i].FileName);
//         }
//     }
// }