using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.AspNetExtensions;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetExtensions;

public class FormFileExtensionsTests
{
    [Fact]
    public async Task ToLocalFileAsync_ShouldBeSame()
    {
        // Arrange
        const int size = 1024 * 1024;
        var fileName = FileHelper.GenerateRandomFileName("txt");
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        var localFile = await formFile.ToLocalFileAsync();

        // Assert
        localFile.FileStream.Length.Should().Be(formFile.Length);
        localFile.FileName.Should().Be(formFile.FileName);
    }
}