using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public sealed class FileSystemReadOnlyContainerTests
{
    [Fact]
    public async Task ReadsAgainstMissingContainerDoNotCreateContainerOrNestedDirectories()
    {
        var root = GetTemporaryRoot();
        var storage = CreateStorage(root, createContainerIfNotExists: false);

        try
        {
            var exists = await storage.ExistsAsync("nested/settings.json");
            var stream = await storage.GetStreamAsync("nested/settings.json");
            var metadata = await storage.GetBlobMetadataAsync("nested/settings.json");

            exists.IsSuccess.ShouldBeTrue();
            exists.Value.ShouldBeFalse();
            stream.IsFailed.ShouldBeTrue();
            metadata.IsFailed.ShouldBeTrue();
            Directory.Exists(root).ShouldBeFalse();
            Directory.Exists(Path.Combine(root, "nested")).ShouldBeFalse();
        }
        finally
        {
            DeleteIfPresent(root);
        }
    }

    [Fact]
    public async Task ReadsAgainstExistingContainerDoNotCreateMissingNestedDirectories()
    {
        var root = GetTemporaryRoot();
        Directory.CreateDirectory(root);
        var storage = CreateStorage(root, createContainerIfNotExists: false);

        try
        {
            var exists = await storage.ExistsAsync("nested/settings.json");
            var stream = await storage.GetStreamAsync("nested/settings.json");
            var metadata = await storage.GetBlobMetadataAsync("nested/settings.json");

            exists.IsSuccess.ShouldBeTrue();
            exists.Value.ShouldBeFalse();
            stream.IsFailed.ShouldBeTrue();
            metadata.IsFailed.ShouldBeTrue();
            Directory.Exists(Path.Combine(root, "nested")).ShouldBeFalse();
        }
        finally
        {
            DeleteIfPresent(root);
        }
    }

    [Fact]
    public async Task WritesDoNotCreateMissingContainerWhenAutomaticCreationIsDisabled()
    {
        var root = GetTemporaryRoot();
        var storage = CreateStorage(root, createContainerIfNotExists: false);

        try
        {
            var upload = await storage.UploadAsync(new byte[] { 1, 2, 3 }, new UploadOptions
            {
                FileName = "nested/settings.json"
            });

            upload.IsFailed.ShouldBeTrue();
            Directory.Exists(root).ShouldBeFalse();
        }
        finally
        {
            DeleteIfPresent(root);
        }
    }

    [Fact]
    public async Task ExplicitContainerCreationRemainsAvailableWhenAutomaticCreationIsDisabled()
    {
        var root = GetTemporaryRoot();
        var storage = CreateStorage(root, createContainerIfNotExists: false);

        try
        {
            var created = await storage.CreateContainerAsync();

            created.IsSuccess.ShouldBeTrue();
            Directory.Exists(root).ShouldBeTrue();
        }
        finally
        {
            DeleteIfPresent(root);
        }
    }

    private static FileSystemStorage CreateStorage(string root, bool createContainerIfNotExists) => new(new FileSystemStorageOptions
    {
        BaseFolder = root,
        CreateContainerIfNotExists = createContainerIfNotExists
    });

    private static string GetTemporaryRoot() => Path.Combine(Path.GetTempPath(), "managedcode-storage-readonly", Guid.NewGuid().ToString("N"));

    private static void DeleteIfPresent(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }
}
