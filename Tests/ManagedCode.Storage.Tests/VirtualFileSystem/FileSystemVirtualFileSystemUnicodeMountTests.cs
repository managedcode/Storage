using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

[Collection(VirtualFileSystemCollection.Name)]
public sealed class FileSystemVirtualFileSystemUnicodeMountTests
{
    [Theory]
    [MemberData(nameof(UnicodeVfsTestCases.FolderScenarios), MemberType = typeof(UnicodeVfsTestCases))]
    public async Task MountingExistingUnicodeDirectories_ShouldExposeFiles(
        string directoryName,
        string fileName,
        string content)
    {
        var rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "managedcode-vfs-existing", Guid.NewGuid().ToString("N"));
        var internationalFolder = Path.Combine(rootFolder, "international", directoryName);
        Directory.CreateDirectory(internationalFolder);

        var seededFilePath = Path.Combine(internationalFolder, $"{fileName}.txt");
        await File.WriteAllTextAsync(seededFilePath, content);

        var options = new FileSystemStorageOptions
        {
            BaseFolder = rootFolder,
            CreateContainerIfNotExists = true
        };

        var storage = new FileSystemStorage(options);

        async ValueTask Cleanup()
        {
            try
            {
                await storage.RemoveContainerAsync();
            }
            finally
            {
                if (Directory.Exists(rootFolder))
                {
                    Directory.Delete(rootFolder, recursive: true);
                }
            }
        }

        await using var context = await VirtualFileSystemTestContext.CreateAsync(
            storage,
            containerName: string.Empty,
            ownsStorage: true,
            serviceProvider: null,
            cleanup: Cleanup);

        var vfs = context.FileSystem;
        var expectedPath = new VfsPath($"/international/{directoryName}/{fileName}.txt");

        (await vfs.FileExistsAsync(expectedPath)).ShouldBeTrue();

        var file = await vfs.GetFileAsync(expectedPath);
        var actualContent = await file.ReadAllTextAsync();
        actualContent.ShouldBe(content);

        var entries = new List<IVfsNode>();
        await foreach (var entry in vfs.ListAsync(new VfsPath($"/international/{directoryName}"), new ListOptions
        {
            IncludeDirectories = false,
            IncludeFiles = true,
            Recursive = false
        }))
        {
            entries.Add(entry);
        }

        var entryPaths = entries.ConvertAll(e => e.Path.Value);
        entries.ShouldContain(e => e.Path.Value == expectedPath.Value, $"Entries: {string.Join(", ", entryPaths)}");
    }
}
