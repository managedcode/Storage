using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public class FileSystemUnicodeSanitizerTests
{
    [Fact]
    public async Task ShouldResolveExistingUnicodeFileByPathOnly()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "managedcode-vfs-existing", Guid.NewGuid().ToString("N"));
        var directory = Path.Combine(root, "international", "Українська-папка");
        Directory.CreateDirectory(directory);

        var expectedFilePath = Path.Combine(directory, "лист-привіт.txt");
        await File.WriteAllTextAsync(expectedFilePath, "Привіт");

        var storage = new FileSystemStorage(new FileSystemStorageOptions
        {
            BaseFolder = root,
            CreateContainerIfNotExists = true
        });

        try
        {
            var exists = await storage.ExistsAsync("international/Українська-папка/лист-привіт.txt");
            exists.IsSuccess.ShouldBeTrue();
            exists.Value.ShouldBeTrue();

            var metadata = await storage.GetBlobMetadataAsync("international/Українська-папка/лист-привіт.txt");
            metadata.IsSuccess.ShouldBeTrue(metadata.Problem?.ToString());
            metadata.Value!.FullName.ShouldBe("international/Українська-папка/лист-привіт.txt");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
