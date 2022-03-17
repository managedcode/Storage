using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Azure;

public class AzureStorageTests
{
    private readonly IDocumentStorage _documentStorage;
    private readonly IPhotoStorage _photoStorage;

    public AzureStorageTests()
    {
        var services = new ServiceCollection();

        services.AddManagedCodeStorage()
            .AddAzureBlobStorage(opt =>
            {
                //https://github.com/marketplace/actions/azuright
                opt.ConnectionString =
                    "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;";
            })
            .Add<IDocumentStorage>(opt => { opt.Container = "documents"; });

        var provider = services.BuildServiceProvider();

        _photoStorage = provider.GetService<IPhotoStorage>();
        _documentStorage = provider.GetService<IDocumentStorage>();
    }

    [Fact]
    public void WhenDIInitialized()
    {
        _photoStorage.Should().NotBeNull();
        _documentStorage.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        var result = await _photoStorage.ExistsAsync("34.png");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        var stream = await _documentStorage.DownloadAsStreamAsync("a.txt");
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var content = sr.ReadToEnd();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        var tempFile = await _documentStorage.DownloadAsync("a.txt");
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

        await _documentStorage.UploadStreamAsync("b.txt", stream);
    }

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        await _documentStorage.DeleteAsync("a.txt");
    }
}