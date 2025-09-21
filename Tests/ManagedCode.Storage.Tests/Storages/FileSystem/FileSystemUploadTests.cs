using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public class FileSystemUploadTests : UploadTests<EmptyContainer>
{
    protected override EmptyContainer Build()
    {
        return new EmptyContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return FileSystemConfigurator.ConfigureServices("managed-code-blob");
    }

    [Fact]
    public async Task UploadAsync_AsStream_CorrectlyOverwritesFiles()
    {
        // Arrange

        var uploadStream1 = new MemoryStream(90*1024);
        var buffer = new byte[90 * 1024];
        var random = new Random();
        random.NextBytes(buffer);
        uploadStream1.Write(buffer, 0, buffer.Length);

        var uploadStream2 = new MemoryStream(512);
        var zeroByteBuffer = new byte[512];
        uploadStream2.Write(zeroByteBuffer);
        var filenameToUse = "UploadAsync_AsStream_CorrectlyOverwritesFiles.bin";

        var temporaryDirectory = Path.GetTempPath();

        // Act
        var firstResult = await Storage.UploadAsync(uploadStream1, options =>
        {
            options.FileName = filenameToUse;
            options.Directory = temporaryDirectory;
        });

        firstResult.IsSuccess.ShouldBeTrue();

        // let's download it
        var downloadedResult = await Storage.DownloadAsync(options =>
        {
            options.FileName = filenameToUse;
            options.Directory = temporaryDirectory;
        });
        downloadedResult.IsSuccess.ShouldBeTrue();
        // size
        downloadedResult.Value!.FileInfo.Length.ShouldBe(90*1024);


        var secondResult = await Storage.UploadAsync(uploadStream2, options =>
        {
            options.FileName = filenameToUse;
            options.Directory = temporaryDirectory;
        });

        secondResult.IsSuccess.ShouldBeTrue();

        // let's download it
        downloadedResult = await Storage.DownloadAsync(options =>
        {
            options.FileName = filenameToUse;
            options.Directory = temporaryDirectory;
        });
        downloadedResult.IsSuccess.ShouldBeTrue();
        // size
        downloadedResult.Value!.FileInfo.Length.ShouldBe(512);

        // content
        using var ms = new MemoryStream();
        await downloadedResult.Value!.FileStream.CopyToAsync(ms);
        ms.ToArray().ShouldBe(zeroByteBuffer);
    }
}
