using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Azure;
/*
public class AzureStorageTests : StorageBaseTests
{
    public AzureStorageTests()
    {
        var services = new ServiceCollection();

        services.AddAzureStorage(opt =>
        {
            opt.Container = "documents";
            //https://github.com/marketplace/actions/azuright
            opt.ConnectionString =
                "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;";
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IAzureStorage>();
    }

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        var result = await Storage.ExistsAsync("34.png");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        var stream = await Storage.DownloadAsStreamAsync("a.txt");
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var content = sr.ReadToEnd();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        var tempFile = await Storage.DownloadAsync("a.txt");
        using var sr = new StreamReader(tempFile.FileStream, Encoding.UTF8);

        var content = sr.ReadToEnd();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenUploadAsyncIsCalled()
    {
        var lineToUpload = "some crazy text";

        var byteArray = Encoding.ASCII.GetBytes(lineToUpload);
        var stream = new MemoryStream(byteArray);

        await Storage.UploadStreamAsync("b.txt", stream);
    }

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        await Storage.DeleteAsync("a.txt");
    }
}*/