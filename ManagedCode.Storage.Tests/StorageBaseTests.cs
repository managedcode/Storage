using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;
using Xunit;

namespace ManagedCode.Storage.Tests;

public abstract class StorageBaseTests
{
    protected IStorage Storage;

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
    
    protected async Task SingleBlobExistsIsCalled(string fileName)
    {
        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeTrue();
    }

    protected void DIInitialized()
    {
        Storage.Should().NotBeNull();
    }

    protected async Task DownloadAsyncIsCalled(string fileName)
    {
        var stream = await Storage.DownloadAsStreamAsync(fileName);
        stream.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var content = sr.ReadToEnd();

        content.Should().NotBeNull();
    }

    protected async Task DownloadAsyncToFileIsCalled(string fileName)
    {
        string content = null;
        using (var tempFile = await Storage.DownloadAsync(fileName))
        {
            using (var sr = new StreamReader(tempFile.FileStream, Encoding.UTF8))
            {
                content = sr.ReadToEnd();
            }
        }

        content.Should().NotBeNull();
    }

    protected async Task UploadAsyncIsCalled(string fileName)
    {
        var lineToUpload = "some text";

        var byteArray = Encoding.ASCII.GetBytes(lineToUpload);
        var stream = new MemoryStream(byteArray);

        await Storage.UploadStreamAsync(fileName, stream);
    }

    protected async Task DeleteAsyncIsCalled(string fileName)
    {
        await Storage.DeleteAsync(fileName);
    }

    protected async Task GetBlobListAsyncIsCalled()
    {
        var aslist = Storage.GetBlobListAsync();
        var list = await aslist.ToListAsync(); // just for debug purposes
    }
}