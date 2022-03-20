using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Xunit;

namespace ManagedCode.Storage.Tests;

public abstract class StorageBaseTests
{
    protected IStorage Storage;

    #region MemoryPayload

    //[Fact]
    //public async Task WhenBigFilesUpload()
    //{
    //    var directory = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket");

    //    var bigFiles = new List<LocalFile>()
    //    {
    //        GetLocalFile($"{directory}/{nameof(WhenBigFilesUpload)}_1.txt", 100 * 1024 * 1024),
    //        GetLocalFile($"{directory}/{nameof(WhenBigFilesUpload)}_2.txt", 100 * 1024 * 1024),
    //        GetLocalFile($"{directory}/{nameof(WhenBigFilesUpload)}_3.txt", 100 * 1024 * 1024)
    //    };

    //    foreach (var localFile in bigFiles)
    //    {
    //        await Storage.UploadStreamAsync(localFile.FileName, localFile.FileStream);  
    //    }

    //    //foreach (var localFile in bigFiles)
    //    //{
    //    //    await Storage.DeleteAsync(localFile.FileName);
    //    //}

    //    //foreach (var localFile in bigFiles)
    //    //{
    //    //    localFile.Dispose();
    //    //}
    //}

    #endregion

    #region Get

    [Fact]
    public async Task GetBlobListAsync()
    {
        //Create list fileName and uploadData
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(GetBlobListAsync)}1.txt", $"test {nameof(GetBlobListAsync)}1"));
        listFile.Add(($"{nameof(GetBlobListAsync)}2.txt", $"test {nameof(GetBlobListAsync)}2"));
        listFile.Add(($"{nameof(GetBlobListAsync)}3.txt", $"test {nameof(GetBlobListAsync)}3"));

        // Upload files to server
        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        }

        //Get uploaded files
        var result = Storage.GetBlobListAsync();
        
        var listResult = await result.ToListAsync();
        var expectedList = listFile.Select(x => new BlobMetadata { Name = x.FileName });

        listResult.Should().BeEquivalentTo(expectedList, x => x.Excluding(f => f.Uri));
        result.Should().NotBeNull();

        // Delete files from server
        foreach (var item in listFile)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public async Task GetBlobsAsync()
    {
        //Create list fileName and uploadData
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(GetBlobsAsync)}1.txt", $"test {nameof(GetBlobsAsync)}1"));
        listFile.Add(($"{nameof(GetBlobsAsync)}2.txt", $"test {nameof(GetBlobsAsync)}2"));
        listFile.Add(($"{nameof(GetBlobsAsync)}3.txt", $"test {nameof(GetBlobsAsync)}3"));

        // Upload files to server
        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        }

        var blobList = new List<string>();
        blobList.Add($"{nameof(GetBlobsAsync)}1.txt");
        blobList.Add($"{nameof(GetBlobsAsync)}2.txt");

        //Get necessary files
        var result = Storage.GetBlobsAsync(blobList);

        var listResult = await result.ToListAsync();
        var expectedList = listFile.Select(x => new BlobMetadata { Name = x.FileName });

        listResult.Should().NotBeEquivalentTo(expectedList);
        result.Should().NotBeNull();

        // Delete files from server
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

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Get file by fileName
        var result = await Storage.GetBlobAsync(fileName);

        result.Should().NotBeNull();
        result.Name.Should().Be(fileName);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Upload

    [Fact]
    public async Task WhenUploadAsyncIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenUploadAsyncIsCalled)}";
        const string fileName = $"{nameof(WhenUploadAsyncIsCalled)}.txt";

        //Forming file to upload
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        //Check is exist file
        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        //Upload file as stream
        await Storage.UploadStreamAsync(fileName, stream);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Download

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenDownloadAsyncIsCalled)}";
        const string fileName = $"{nameof(WhenDownloadAsyncIsCalled)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Download file as stream
        var stream = await Storage.DownloadAsStreamAsync(fileName);
        using var sr = new StreamReader(stream, Encoding.UTF8);

        //Get content from file as string
        string content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenDownloadAsyncToFileIsCalled)}";
        const string fileName = $"{nameof(WhenDownloadAsyncToFileIsCalled)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Download file
        var tempFile = await Storage.DownloadAsync(fileName);
        var sr = new StreamReader(tempFile.FileStream, Encoding.UTF8);

        //Get content from file as string
        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenDeleteAsyncIsCalled)}";
        const string fileName = $"{nameof(WhenDeleteAsyncIsCalled)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Delete file
        await Storage.DeleteAsync(fileName);
    }

    #endregion

    #region Exist

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        const string uploadContent = $"test {nameof(WhenSingleBlobExistsIsCalled)}";
        const string fileName = $"{nameof(WhenSingleBlobExistsIsCalled)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Check is exist file
        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeTrue();

        //Delete file
        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistFileByBlobMetadataAsync()
    {
        const string uploadContent = $"test {nameof(ExistFileByBlobMetadataAsync)}";
        const string fileName = $"{nameof(ExistFileByBlobMetadataAsync)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        var file = new BlobMetadata
        {
            Name = fileName
        };

        //Get file by BlobMetadata
        var result = await Storage.ExistsAsync(file);

        result.Should().BeTrue();

        //Delete file
        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistFileByListStringAsync()
    {
        //Create list fileName and uploadData
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(ExistFileByListStringAsync)}1.txt", $"test {nameof(ExistFileByListStringAsync)}1"));
        listFile.Add(($"{nameof(ExistFileByListStringAsync)}2.txt", $"test {nameof(ExistFileByListStringAsync)}2"));
        listFile.Add(($"{nameof(ExistFileByListStringAsync)}3.txt", $"test {nameof(ExistFileByListStringAsync)}3"));

        // Upload files to server
        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        }

        var blobList = new List<string>();
        blobList.Add($"{nameof(ExistFileByListStringAsync)}1.txt");
        blobList.Add($"{nameof(ExistFileByListStringAsync)}2.txt");

        //Check files is exist
        var result = Storage.ExistsAsync(blobList);

        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeTrue();
        }

        // Delete files from server
        foreach (var item in listFile)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public async Task ExistFileByListBlobMetadataAsync()
    {
        //Create list fileName and uploadData
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(ExistFileByListBlobMetadataAsync)}1.txt", $"test {nameof(ExistFileByListBlobMetadataAsync)}1"));
        listFile.Add(($"{nameof(ExistFileByListBlobMetadataAsync)}2.txt", $"test {nameof(ExistFileByListBlobMetadataAsync)}2"));
        listFile.Add(($"{nameof(ExistFileByListBlobMetadataAsync)}3.txt", $"test {nameof(ExistFileByListBlobMetadataAsync)}3"));

        // Upload files to server
        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        }

        //Check files is exist
        var result = Storage.ExistsAsync(listFile.Select(x => new BlobMetadata { Name = x.FileName }));

        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeTrue();
        }

        // Delete files from server
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

    private LocalFile GetLocalFile(string fileName, int byteSize)
    {
        var localFile = new LocalFile(fileName);
        var fs = localFile.FileStream;

        fs.Seek(byteSize, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.Close();

        return localFile;
    }
}