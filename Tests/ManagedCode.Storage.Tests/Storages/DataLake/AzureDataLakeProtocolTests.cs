using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure;
using ManagedCode.Storage.Azure.DataLake;
using ManagedCode.Storage.Azure.DataLake.Extensions;
using ManagedCode.Storage.Azure.DataLake.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.DataLake;

public sealed class AzureDataLakeProtocolTests(DataLakeProtocolHost host) : IClassFixture<DataLakeProtocolHost>
{
    private AzureDataLakeStorageOptions Options() => new()
    {
        ConnectionString = host.ConnectionString,
        FileSystem = "contract-" + Guid.NewGuid().ToString("N")
    };

    [Theory]
    [InlineData(null)]
    [InlineData("nested")]
    public async Task FilesAndStreams_RoundTripThroughRealAzureSdk(string? directory)
    {
        var options = Options();
        var storage = new AzureDataLakeStorage(options);
        var token = TestContext.Current.CancellationToken;
        (await storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        if (directory is not null)
            (await storage.CreateDirectoryAsync(directory, token)).IsSuccess.ShouldBeTrue();
        var uploadOptions = new UploadOptions { FileName = "content.bin", Directory = directory };
        var content = Encoding.UTF8.GetBytes("0123456789-Привіт");
        var uploaded = await storage.UploadAsync(content, uploadOptions, token);
        uploaded.IsSuccess.ShouldBeTrue(uploaded.Problem?.ToString());
        uploaded.Value!.Length.ShouldBe((ulong)content.Length);
        var key = directory is null ? "content.bin" : directory + "/content.bin";
        (await storage.ExistsAsync(key, token)).Value.ShouldBeTrue();
        var downloaded = await storage.DownloadAsync(key, token);
        downloaded.IsSuccess.ShouldBeTrue(downloaded.Problem?.ToString());
        await using (var local = downloaded.Value!)
            (await local.ReadAllBytesAsync(token)).ShouldBe(content);
        var opened = await storage.OpenReadStreamAsync(new OpenReadStreamOptions
        {
            FileName = "content.bin",
            Directory = directory,
            Position = 3,
            BufferSize = 4096
        }, token);
        opened.IsSuccess.ShouldBeTrue(opened.Problem?.ToString());
        await using (var stream = opened.Value!)
        {
            using var reader = new StreamReader(stream);
            (await reader.ReadToEndAsync(token)).ShouldBe(Encoding.UTF8.GetString(content.AsSpan(3)));
        }
        var write = await storage.OpenWriteStreamAsync(new OpenWriteStreamOptions { FileName = "streamed", Directory = directory, Overwrite = true }, token);
        write.IsSuccess.ShouldBeTrue(write.Problem?.ToString());
        await using (var stream = write.Value!)
            await stream.WriteAsync(content, token);
        var names = new List<string>();
        await foreach (var metadata in storage.GetBlobMetadataListAsync(directory, token))
            names.Add(metadata.Name!);
        names.ShouldContain(key);
        if (directory is not null)
        {
            (await storage.RenameDirectory(directory, "renamed", token)).IsSuccess.ShouldBeTrue();
            key = "renamed/content.bin";
        }
        (await storage.DeleteAsync(key, token)).IsSuccess.ShouldBeTrue();
        (await storage.ExistsAsync(key, token)).Value.ShouldBeFalse();
        (await storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task PermissionFailures_AreReturnedAcrossProviderOperations()
    {
        var options = Options();
        var storage = new AzureDataLakeStorage(options);
        var token = TestContext.Current.CancellationToken;
        (await storage.UploadAsync("seed", new UploadOptions { FileName = "seed" }, token)).IsSuccess.ShouldBeTrue();
        host.Reject(options.FileSystem);
        (await storage.UploadAsync("new", new UploadOptions { FileName = "seed" }, token)).IsSuccess.ShouldBeFalse();
        (await storage.DownloadAsync("seed", token)).IsSuccess.ShouldBeFalse();
        (await storage.GetStreamAsync("seed", token)).IsSuccess.ShouldBeFalse();
        (await storage.OpenWriteStreamAsync(new OpenWriteStreamOptions { FileName = "seed", Overwrite = true }, token)).IsSuccess.ShouldBeFalse();
        (await storage.GetBlobMetadataAsync("seed", token)).IsSuccess.ShouldBeFalse();
        (await storage.ExistsAsync("seed", token)).IsSuccess.ShouldBeFalse();
        (await storage.DeleteAsync("seed", token)).IsSuccess.ShouldBeFalse();
        (await storage.CreateDirectoryAsync("directory", token)).IsSuccess.ShouldBeFalse();
        (await storage.RenameDirectory("directory", "renamed", token)).IsSuccess.ShouldBeFalse();
        (await storage.DeleteDirectoryAsync("directory", token)).IsSuccess.ShouldBeFalse();
        (await storage.CreateContainerAsync(token)).IsSuccess.ShouldBeFalse();
        (await storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeFalse();
        await Should.ThrowAsync<RequestFailedException>(async () =>
        {
            await foreach (var _ in storage.GetBlobMetadataListAsync(cancellationToken: token)) { }
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Registrations_ResolveConfiguredRealProvider(int registration)
    {
        var options = Options();
        var services = new ServiceCollection();
        void Configure(AzureDataLakeStorageOptions target) { target.ConnectionString = options.ConnectionString; target.FileSystem = options.FileSystem; }
        switch (registration)
        {
            case 0: services.AddAzureDataLakeStorage(options); break;
            case 1: services.AddAzureDataLakeStorage(Configure); break;
            case 2: services.AddAzureDataLakeStorageAsDefault(options); break;
            case 3: services.AddAzureDataLakeStorageAsDefault(Configure); break;
            case 4: services.AddAzureDataLakeStorage("lake", Configure); break;
            case 5: services.AddAzureDataLakeStorageAsDefault("lake", Configure); break;
        }
        await using var provider = services.BuildServiceProvider();
        var storage = registration >= 4 ? provider.GetRequiredKeyedService<IAzureDataLakeStorage>("lake") : provider.GetRequiredService<IAzureDataLakeStorage>();
        var token = TestContext.Current.CancellationToken;
        (await storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        (await storage.UploadAsync("registered", new UploadOptions { FileName = "registered" }, token)).IsSuccess.ShouldBeTrue();
        var opened = await storage.GetStreamAsync("registered", token);
        opened.IsSuccess.ShouldBeTrue(opened.Problem?.ToString());
        await using var stream = opened.Value!;
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(token)).ShouldBe("registered");
        if (registration is 2 or 3)
            provider.GetRequiredService<IStorage>().ShouldBeOfType<AzureDataLakeStorage>();
        if (registration == 5)
            provider.GetRequiredKeyedService<IStorage>("lake").ShouldBeSameAs(storage);
    }
}
