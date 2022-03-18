using System.IO;
using System.Text;
using System.Threading.Tasks;
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
                "DefaultEndpointsProtocol=https;AccountName=winktdev;AccountKey=F7F9vhS+SxgY8b0/mrGYZCV6QOoKwv8FqAHsDN/aZC4OPeyPhHS8OKRi3Uc9VIHcel5+oweEmRQs4Be+r0pFMg==;EndpointSuffix=core.windows.net";
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IAzureStorage>();
    }

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        var result = await Storage.ExistsAsync("b.txt");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        var stream = await Storage.DownloadAsStreamAsync("b.txt");
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var content = sr.ReadToEnd();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        var tempFile = await Storage.DownloadAsync("b.txt");
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
        await Storage.DeleteAsync("b.txt");
    }
}