using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.AspNetExtensions;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetExtensions;

public class FormFileExtensionsTests
{
    [Fact]
    public async Task ToLocalFileAsync_SmallFile()
    {
        // Arrange
        const int size = 1024 * 1024; // 1 MB
        var fileName = FileHelper.GenerateRandomFileName("txt");
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        var localFile = await formFile.ToLocalFileAsync();

        // Assert
        localFile.FileStream.Length.Should().Be(formFile.Length);
        localFile.FileName.Should().Be(formFile.FileName);
    }

    [Fact]
    public async Task ToLocalFileAsync_LargeFile()
    {
        // Arrange
        const int size = 500 * 1024 * 1024; // 500 MB
        var fileName = FileHelper.GenerateRandomFileName("txt");
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        var localFile = await formFile.ToLocalFileAsync();

        // Assert
        localFile.FileStream.Length.Should().Be(formFile.Length);
        localFile.FileName.Should().Be(formFile.FileName);
    }

    [Fact]
    public async Task ToLocalFileAsync_TooLargeFile()
    {
        // Arrange
        const int size = 2024 * 1024 * 1024; // 2 GB
        var fileName = FileHelper.GenerateRandomFileName("txt");
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        var localFile = await formFile.ToLocalFileAsync();

        // Assert
        localFile.FileStream.Length.Should().Be(formFile.Length);
        localFile.FileName.Should().Be(formFile.FileName);
    }
}