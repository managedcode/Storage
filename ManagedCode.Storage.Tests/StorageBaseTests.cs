using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;
using Xunit;

namespace ManagedCode.Storage.Tests;

public abstract class StorageBaseTests
{
    protected IStorage Storage;

    #region Get
    #endregion

    #region Upload

    [Fact]
    public async Task WhenUploadAsyncIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenUploadAsyncIsCalled)}";
        const string fileName = $"{nameof(WhenUploadAsyncIsCalled)}.txt";

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        await Storage.UploadStreamAsync(fileName, stream);
    }

    #endregion

    #region Download

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenDownloadAsyncIsCalled)}";
        const string fileName = $"{nameof(WhenDownloadAsyncIsCalled)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var stream = await Storage.DownloadAsStreamAsync(fileName);
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenDownloadAsyncToFileIsCalled)}";
        const string fileName = $"{nameof(WhenDownloadAsyncToFileIsCalled)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var tempFile = await Storage.DownloadAsync(fileName);
        var sr = new StreamReader(tempFile.FileStream, Encoding.UTF8);
        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenDeleteAsyncIsCalled)}";
        const string fileName = $"{nameof(WhenDeleteAsyncIsCalled)}.txt";

        await PrepareFileToTest(uploadContent, fileName);
        await Storage.DeleteAsync(fileName);
    }

    #endregion

    #region Exist

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenSingleBlobExistsIsCalled)}";
        const string fileName = $"{nameof(WhenSingleBlobExistsIsCalled)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeTrue();
    }

    #endregion

    private async Task PrepareFileToTest(string content, string fileName)
    {
        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        await Storage.UploadAsync(fileName, content);
    }
}