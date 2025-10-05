using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

/// <summary>
/// Security tests for FileSystemStorage - verifying path traversal protection.
/// </summary>
public class FileSystemSecurityTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly FileSystemStorage _storage;

    public FileSystemSecurityTests()
    {
        _testBasePath = Path.Combine(Environment.CurrentDirectory, "FileSystemSecurityTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);

        var options = new FileSystemStorageOptions
        {
            BaseFolder = _testBasePath
        };

        _storage = new FileSystemStorage(options);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\Windows\\System32\\config\\SAM")]
    [InlineData("../../../../secret.txt")]
    [InlineData("..\\..\\sensitive.dat")]
    public async Task UploadAsync_WithPathTraversal_ShouldFail(string maliciousFileName)
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new UploadOptions
        {
            FileName = maliciousFileName
        };

        // Act
        var result = await _storage.UploadAsync(stream, options);

        // Assert - security validation should reject path traversal
        result.IsFailed.ShouldBeTrue();
        result.Problem.Title.ShouldBe("UnauthorizedAccessException");
    }

    [Fact]
    public async Task UploadAsync_WithValidFileName_ShouldSucceed()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new UploadOptions
        {
            FileName = "legitimate-file.txt"
        };

        // Act
        var result = await _storage.UploadAsync(stream, options);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("legitimate-file.txt");
    }

    [Theory]
    [InlineData("../../../malicious")]
    [InlineData("../../outside")]
    public async Task UploadAsync_WithPathTraversalInDirectory_ShouldFail(
        string maliciousDirectory)
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new UploadOptions
        {
            FileName = "file.txt",
            Directory = maliciousDirectory
        };

        // Act
        var result = await _storage.UploadAsync(stream, options);

        // Assert - security validation should reject path traversal
        result.IsFailed.ShouldBeTrue();
        result.Problem.Title.ShouldBe("UnauthorizedAccessException");
    }

    [Fact]
    public async Task UploadAsync_WithValidDirectory_ShouldSucceed()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new UploadOptions
        {
            FileName = "file.txt",
            Directory = "subfolder/nested"
        };

        // Act
        var result = await _storage.UploadAsync(stream, options);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify file is in correct location
        var expectedPath = Path.Combine(_testBasePath, "subfolder", "nested", "file.txt");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testBasePath))
            {
                Directory.Delete(_testBasePath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
