using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.FakeGcsServer;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GCS;

public class GCSDownloadTests : DownloadTests<FakeGcsServerContainer>
{
    protected override FakeGcsServerContainer Build()
    {
        return GcsContainerFactory.Create();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GCSConfigurator.ConfigureServices(Container.GetConnectionString());
    }

    [Fact]
    public async Task DownloadToExistingFile_ReplacesPreviousContents()
    {
        var token = TestContext.Current.CancellationToken;
        const string key = "new.txt";
        (await Storage.UploadAsync("new", new UploadOptions(key), token)).IsSuccess.ShouldBeTrue();
        var path = Path.Combine(Environment.CurrentDirectory, $"gcs-download-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllTextAsync(path, "previous content", token);
            var downloaded = await Storage.DownloadAsync(new DownloadOptions { FileName = key, LocalPath = path }, token);
            downloaded.IsSuccess.ShouldBeTrue();
            await using var file = downloaded.Value;
            (await File.ReadAllTextAsync(path, token)).ShouldBe("new");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
