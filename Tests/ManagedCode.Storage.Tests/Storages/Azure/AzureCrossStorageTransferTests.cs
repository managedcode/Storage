using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Tests.Common;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public sealed class AzureCrossStorageTransferTests : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder(ContainerImages.Azurite)
        .WithCommand("--skipApiVersionCheck").Build();

    public ValueTask InitializeAsync() => new(_container.StartAsync(TestContext.Current.CancellationToken));

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    [Fact]
    public async Task CopiesAzurePropertiesThenMovesTheFileToLocalStorage()
    {
        var token = TestContext.Current.CancellationToken;
        using var source = CreateStorage();
        using var archive = CreateStorage();
        var folder = Directory.CreateTempSubdirectory("storage-transfer-");
        try
        {
            using var destination = new FileSystemStorage(new FileSystemStorageOptions { BaseFolder = folder.FullName });
            const string original = "incoming/дані.dat";
            const string copy = "copied/content.txt";
            const string moved = "moved/content.bin";
            const string contentType = "application/x-storage-test";
            var payload = "cross-storage payload"u8.ToArray();
            var options = new UploadOptions(original, mimeType: contentType, metadata: new() { ["author"] = "Марія" });
            (await source.UploadAsync(payload, options, token)).IsSuccess.ShouldBeTrue();

            var copied = await source.CopyToAsync(original, archive, copy, token);
            copied.IsSuccess.ShouldBeTrue(copied.Problem?.Detail);
            var persisted = await archive.GetBlobMetadataAsync(copy, token);
            persisted.IsSuccess.ShouldBeTrue(persisted.Problem?.Detail);
            persisted.Value.MimeType.ShouldBe(contentType);
            persisted.Value.Metadata.ShouldNotBeNull()["author"].ShouldBe("Марія");

            var relocated = await archive.MoveToAsync(copy, destination, moved, token);
            relocated.IsSuccess.ShouldBeTrue(relocated.Problem?.Detail);
            (await File.ReadAllBytesAsync(Path.Combine(folder.FullName, moved), token)).ShouldBe(payload);
            (await source.ExistsAsync(original, token)).Value.ShouldBeTrue();
            (await archive.ExistsAsync(copy, token)).Value.ShouldBeFalse();
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private AzureStorage CreateStorage() => new(new AzureStorageOptions
    {
        ConnectionString = _container.GetConnectionString(),
        Container = $"transfer-{Guid.NewGuid():N}",
        CreateContainerIfNotExists = true
    });
}
