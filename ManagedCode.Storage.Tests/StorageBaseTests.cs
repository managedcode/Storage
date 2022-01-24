using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Tests;

public class StorageBaseTests
{
    protected IStorage Storage;

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