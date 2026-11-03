using System;
using System.Collections.Generic;
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
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new UploadOptions
        {
            FileName = maliciousFileName
        };

        // Act
        var result = await _storage.UploadAsync(stream, options, TestContext.Current.CancellationToken);

        // Assert - security validation should reject path traversal
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem.Title.ShouldBe("UnauthorizedAccessException");
    }

    [Fact]
    public async Task UploadAsync_WithValidFileName_ShouldSucceed()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new UploadOptions
        {
            FileName = "legitimate-file.txt"
        };

        // Act
        var result = await _storage.UploadAsync(stream, options, TestContext.Current.CancellationToken);

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
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new UploadOptions
        {
            FileName = "file.txt",
            Directory = maliciousDirectory
        };

        // Act
        var result = await _storage.UploadAsync(stream, options, TestContext.Current.CancellationToken);

        // Assert - security validation should reject path traversal
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem.Title.ShouldBe("UnauthorizedAccessException");
    }

    [Fact]
    public async Task UploadAsync_WithValidDirectory_ShouldSucceed()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var options = new UploadOptions
        {
            FileName = "file.txt",
            Directory = "subfolder/nested"
        };

        // Act
        var result = await _storage.UploadAsync(stream, options, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify file is in correct location
        var expectedPath = Path.Combine(_testBasePath, "subfolder", "nested", "file.txt");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectoryOperations_RejectPathsOutsideStorageRoot(bool rooted)
    {
        var token = TestContext.Current.CancellationToken;
        var outside = Directory.CreateDirectory(_testBasePath + "-sibling");
        try
        {
            var directory = rooted ? outside.FullName : Path.GetRelativePath(_testBasePath, outside.FullName);
            var marker = Path.Combine(outside.FullName, "keep.txt");
            await File.WriteAllTextAsync(marker, "keep", token);

            var deleted = await _storage.DeleteDirectoryAsync(directory, token);

            deleted.IsFailed.ShouldBeTrue();
            File.Exists(marker).ShouldBeTrue();
            await Should.ThrowAsync<UnauthorizedAccessException>(() => ListNamesAsync(directory));
        }
        finally
        {
            if (Directory.Exists(outside.FullName))
                outside.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task FileOperations_RejectLinksOutsideRootAndListingSkipsThem()
    {
        var token = TestContext.Current.CancellationToken;
        var outside = Directory.CreateDirectory(_testBasePath + "-link-target");
        try
        {
            var target = Path.Combine(outside.FullName, "keep.txt");
            await File.WriteAllTextAsync(target, "keep", token);
            Directory.CreateSymbolicLink(Path.Combine(_testBasePath, "external"), outside.FullName);
            File.CreateSymbolicLink(Path.Combine(_testBasePath, "linked.txt"), target);
            await File.WriteAllTextAsync(Path.Combine(_testBasePath, "internal.txt"), "internal", token);

            (await _storage.GetStreamAsync("linked.txt", token)).IsFailed.ShouldBeTrue();
            (await _storage.UploadAsync("replace", new UploadOptions { FileName = "external/new/keep.txt" }, token))
                .IsFailed.ShouldBeTrue();
            (await _storage.DeleteDirectoryAsync("external", token)).IsFailed.ShouldBeTrue();
            (await ListNamesAsync(null)).ShouldBe(["internal.txt"]);
            Directory.Exists(Path.Combine(outside.FullName, "new")).ShouldBeFalse();
            (await File.ReadAllTextAsync(target, token)).ShouldBe("keep");
        }
        finally
        {
            outside.Delete(recursive: true);
        }
    }

    private async Task<IReadOnlyList<string>> ListNamesAsync(string? directory)
    {
        var names = new List<string>();
        await foreach (var item in _storage.GetBlobMetadataListAsync(directory, TestContext.Current.CancellationToken))
        {
            names.Add(item.FullName);
        }

        return names;
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

        GC.SuppressFinalize(this);
    }
}
