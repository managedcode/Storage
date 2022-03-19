using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Azure;

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
                "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1; AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;";
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IAzureStorage>();
    }

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        await Storage.UploadAsync("b1.txt","");

        var result = await Storage.ExistsAsync("b1.txt");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        await Storage.UploadAsync("b2.txt", "");

        var DownloadAsStream = await Storage.DownloadAsStreamAsync("b2.txt");
        using var sr = new StreamReader(DownloadAsStream, Encoding.UTF8);

        var content = sr.ReadToEnd();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        await Storage.UploadAsync("b3.txt", "");

        var tempFile = await Storage.DownloadAsync("b3.txt");
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

        await Storage.UploadStreamAsync("b4.txt", stream);
    }

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        await Storage.DeleteAsync("b1.txt");
    }
}