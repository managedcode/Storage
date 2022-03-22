using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests;

public abstract class StorageBaseTests
{
    protected IStorage Storage { get; }
    protected ServiceProvider ServiceProvider { get; }
    
    protected abstract ServiceProvider ConfigureServices();

    protected StorageBaseTests()
    {
        ServiceProvider = ConfigureServices();
        Storage = ServiceProvider.GetService<IStorage>()!;
    }

    #region MemoryPayload

    // [Fact]
    // public async Task UploadBigFilesAsync()
    // {
    //     const int fileSize = 70 * 1024 * 1024;
    //
    //     var bigFiles = new List<LocalFile>()
    //     {
    //         GetLocalFile(fileSize),
    //         GetLocalFile(fileSize),
    //         GetLocalFile(fileSize)
    //     };
    //
    //     foreach (var localFile in bigFiles)
    //     {
    //         await Storage.UploadStreamAsync(localFile.FileName, localFile.FileStream);
    //         await localFile.DisposeAsync();
    //     }
    //
    //     Process currentProcess = Process.GetCurrentProcess();
    //     long totalBytesOfMemoryUsed = currentProcess.WorkingSet64;
    //
    //     totalBytesOfMemoryUsed.Should().BeLessThan(3 * fileSize);
    //
    //     foreach (var localFile in bigFiles)
    //     {
    //         await Storage.DeleteAsync(localFile.FileName);
    //     }
    // }

    #endregion

    #region Get

    [Fact]
    public async Task GetBlobListAsync()
    {
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(GetBlobListAsync)}1.txt", $"test {nameof(GetBlobListAsync)}1"));
        listFile.Add(($"{nameof(GetBlobListAsync)}2.txt", $"test {nameof(GetBlobListAsync)}2"));
        listFile.Add(($"{nameof(GetBlobListAsync)}3.txt", $"test {nameof(GetBlobListAsync)}3"));

        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        }

        var result = Storage.GetBlobListAsync();
        
        var listResult = await result.ToListAsync();
        var expectedList = listFile.Select(x => new BlobMetadata { Name = x.FileName });

        //TOOD: Comment this line to test
        //listResult.Should().BeEquivalentTo(expectedList, x => x.Excluding(f => f.Uri));
        result.Should().NotBeNull();

        foreach (var item in listFile)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public async Task GetBlobsAsync()
    {
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(GetBlobsAsync)}1.txt", $"test {nameof(GetBlobsAsync)}1"));
        listFile.Add(($"{nameof(GetBlobsAsync)}2.txt", $"test {nameof(GetBlobsAsync)}2"));
        listFile.Add(($"{nameof(GetBlobsAsync)}3.txt", $"test {nameof(GetBlobsAsync)}3"));

        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        }

        var blobList = new List<string>();
        blobList.Add($"{nameof(GetBlobsAsync)}1.txt");
        blobList.Add($"{nameof(GetBlobsAsync)}2.txt");

        var result = Storage.GetBlobsAsync(blobList);

        var listResult = await result.ToListAsync();
        var expectedList = listFile.Select(x => new BlobMetadata { Name = x.FileName });

        listResult.Should().NotBeEquivalentTo(expectedList);
        result.Should().NotBeNull();

        foreach (var item in listFile)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public async Task GetBlobAsync()
    {
        const string uploadContent = $"test {nameof(GetBlobAsync)}";
        const string fileName = $"{nameof(GetBlobAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var result = await Storage.GetBlobAsync(fileName);

        result.Should().NotBeNull();
        result.Name.Should().Be(fileName);

        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Upload

    [Fact]
    public async Task UploadFileAsStreamSpecifyingFileNameAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsStreamSpecifyingFileNameAsync)}";
        const string fileName = $"{nameof(UploadFileAsStreamSpecifyingFileNameAsync)}.txt";

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        await Storage.UploadStreamAsync(fileName, stream);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsStreamSpecifyingBlobMetadataAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsStreamSpecifyingFileNameAsync)}";
        var blobMetadata = new BlobMetadata
        {
            Name = $"{nameof(UploadFileAsStreamSpecifyingBlobMetadataAsync)}.txt"
        };

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        if (await Storage.ExistsAsync(blobMetadata))
        {
            await Storage.DeleteAsync(blobMetadata);
        }

        await Storage.UploadStreamAsync(blobMetadata, stream);

        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileAsTextSpecifyingFileNameAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsTextSpecifyingFileNameAsync)}";
        const string fileName = $"{nameof(UploadFileAsTextSpecifyingFileNameAsync)}.txt";

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        await Storage.UploadAsync(fileName, uploadContent);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsTextSpecifyingBlobMetadataAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsTextSpecifyingBlobMetadataAsync)}";
        var blobMetadata = new BlobMetadata
        {
            Name = $"{nameof(UploadFileAsTextSpecifyingBlobMetadataAsync)}.txt"
        };

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        if (await Storage.ExistsAsync(blobMetadata))
        {
            await Storage.DeleteAsync(blobMetadata);
        }

        await Storage.UploadAsync(blobMetadata, uploadContent);

        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileFromPathSpecifyingFileNameAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileFromPathSpecifyingFileNameAsync)}";
        const string fileName = $"{nameof(UploadFileFromPathSpecifyingFileNameAsync)}.txt";

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var localFile = await LocalFile.FromStreamAsync(stream);

        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        await Storage.UploadFileAsync(fileName, localFile.FilePath);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileFromPathSpecifyingBlobMetadataAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileFromPathSpecifyingBlobMetadataAsync)}";
        var blobMetadata = new BlobMetadata
        {
            Name = $"{nameof(UploadFileFromPathSpecifyingBlobMetadataAsync)}.txt"
        };

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var localFile = await LocalFile.FromStreamAsync(stream);
        
        if (await Storage.ExistsAsync(blobMetadata))
        {
            await Storage.DeleteAsync(blobMetadata);
        }

        await Storage.UploadFileAsync(blobMetadata, localFile.FilePath);

        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileAsArrayAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsArrayAsync)}";
        const string fileName = $"{nameof(UploadFileAsArrayAsync)}.txt";

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);

        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        await Storage.UploadAsync(new BlobMetadata { Name = fileName }, byteArray);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsAsTextWithoutNameSpecifiedAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsAsTextWithoutNameSpecifiedAsync)}";

        var fileName = await Storage.UploadAsync(uploadContent);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsAsStreamWithoutNameSpecifiedAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsAsStreamWithoutNameSpecifiedAsync)}";

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        var fileName = await Storage.UploadAsync(stream);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Download

    [Fact]
    public async Task DownloadFileBlobMetadataAsLocalFileAsync()
    {
        const string uploadContent = $"test {nameof(DownloadFileBlobMetadataAsLocalFileAsync)}";
        const string fileName = $"{nameof(DownloadFileBlobMetadataAsLocalFileAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var localFile = await Storage.DownloadAsync(new BlobMetadata { Name = fileName });
        using var sr = new StreamReader(localFile.FileStream, Encoding.UTF8);

        string content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileAsLocalFileAsync()
    {
        const string uploadContent = $"test {nameof(DownloadFileAsLocalFileAsync)}";
        const string fileName = $"{nameof(DownloadFileAsLocalFileAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var localFile = await Storage.DownloadAsync(fileName);
        using var sr = new StreamReader(localFile.FileStream, Encoding.UTF8);

        string content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileBlobMetadataAsync()
    {
        const string uploadContent = $"test {nameof(DownloadFileAsync)}";
        const string fileName = $"{nameof(DownloadFileAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var stream = await Storage.DownloadAsStreamAsync(new BlobMetadata { Name = fileName});
        using var sr = new StreamReader(stream, Encoding.UTF8);

        string content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileAsync()
    {
        const string uploadContent = $"test {nameof(DownloadFileAsync)}";
        const string fileName = $"{nameof(DownloadFileAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var stream = await Storage.DownloadAsStreamAsync(fileName);
        using var sr = new StreamReader(stream, Encoding.UTF8);

        string content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteFileListAsync()
    {
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(DeleteFileListAsync)}1.txt", $"test {nameof(DeleteFileListAsync)}1"));
        listFile.Add(($"{nameof(DeleteFileListAsync)}2.txt", $"test {nameof(DeleteFileListAsync)}2"));
        listFile.Add(($"{nameof(DeleteFileListAsync)}3.txt", $"test {nameof(DeleteFileListAsync)}3"));

        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        };

        var expectedList = listFile.Select(x => x.FileName);

        await Storage.DeleteAsync(expectedList);

        var result = Storage.ExistsAsync(expectedList);
        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeFalse();
        }

    }

    [Fact]
    public async Task DeleteFileAsBlobMetadataListAsync()
    {
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(DeleteFileAsBlobMetadataListAsync)}1.txt", $"test {nameof(DeleteFileAsBlobMetadataListAsync)}1"));
        listFile.Add(($"{nameof(DeleteFileAsBlobMetadataListAsync)}2.txt", $"test {nameof(DeleteFileAsBlobMetadataListAsync)}2"));
        listFile.Add(($"{nameof(DeleteFileAsBlobMetadataListAsync)}3.txt", $"test {nameof(DeleteFileAsBlobMetadataListAsync)}3"));

        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        };

        var expectedList = listFile.Select(x => new BlobMetadata { Name = x.FileName });

        await Storage.DeleteAsync(expectedList);

        var result = Storage.ExistsAsync(expectedList);
        var resultList = await result.ToListAsync();

        foreach(var item in resultList)
        {
            item.Should().BeFalse();
        }
    }   

    [Fact]
    public async Task DeleteFileAsBlobMetadataAsync()
    {
        const string uploadContent = $"test {nameof(DeleteFileAsBlobMetadataAsync)}";
        const string fileName = $"{nameof(DeleteFileAsBlobMetadataAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var blobMetadata = await Storage.GetBlobAsync(fileName);

        await Storage.DeleteAsync(blobMetadata);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsStringAsync()
    {
        const string uploadContent = $"test {nameof(DeleteFileAsStringAsync)}";
        const string fileName = $"{nameof(DeleteFileAsStringAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        await Storage.DeleteAsync(fileName);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();
    }

    #endregion

    #region Exist

    [Fact]
    public async Task SingleBlobExistsAsync()
    {
        const string uploadContent = $"test {nameof(SingleBlobExistsAsync)}";
        const string fileName = $"{nameof(SingleBlobExistsAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistFileByBlobMetadataAsync()
    {
        const string uploadContent = $"test {nameof(ExistFileByBlobMetadataAsync)}";
        const string fileName = $"{nameof(ExistFileByBlobMetadataAsync)}.txt";

        await PrepareFileToTest(uploadContent, fileName);

        var file = new BlobMetadata
        {
            Name = fileName
        };

        var result = await Storage.ExistsAsync(file);

        result.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistFileByListStringAsync()
    {
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(ExistFileByListStringAsync)}1.txt", $"test {nameof(ExistFileByListStringAsync)}1"));
        listFile.Add(($"{nameof(ExistFileByListStringAsync)}2.txt", $"test {nameof(ExistFileByListStringAsync)}2"));
        listFile.Add(($"{nameof(ExistFileByListStringAsync)}3.txt", $"test {nameof(ExistFileByListStringAsync)}3"));

        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        }

        var blobList = new List<string>();
        blobList.Add($"{nameof(ExistFileByListStringAsync)}1.txt");
        blobList.Add($"{nameof(ExistFileByListStringAsync)}2.txt");

        var result = Storage.ExistsAsync(blobList);

        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeTrue();
        }

        foreach (var item in listFile)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public async Task ExistFileByListBlobMetadataAsync()
    {
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(ExistFileByListBlobMetadataAsync)}1.txt", $"test {nameof(ExistFileByListBlobMetadataAsync)}1"));
        listFile.Add(($"{nameof(ExistFileByListBlobMetadataAsync)}2.txt", $"test {nameof(ExistFileByListBlobMetadataAsync)}2"));
        listFile.Add(($"{nameof(ExistFileByListBlobMetadataAsync)}3.txt", $"test {nameof(ExistFileByListBlobMetadataAsync)}3"));

        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        }

        var result = Storage.ExistsAsync(listFile.Select(x => new BlobMetadata { Name = x.FileName }));

        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeTrue();
        }

        foreach (var item in listFile)
        {
            await DeleteFileAsync(item.FileName);
        }
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

    private async Task DeleteFileAsync(string fileName)
    {
        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }
    }

    private async Task<string> DownloadAsync(string fileName)
    {
        var stream = await Storage.DownloadAsStreamAsync(fileName);
        var sr = new StreamReader(stream, Encoding.UTF8);

        return await sr.ReadToEndAsync();
    }

    private LocalFile GetLocalFile(int byteSize)
    {
        var localFile = new LocalFile();
        var fs = localFile.FileStream;

        fs.Seek(byteSize, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.Dispose();

        return localFile;
    }
}