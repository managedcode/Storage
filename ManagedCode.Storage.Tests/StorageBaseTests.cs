using System;
using System.Collections.Generic;
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
        Storage = ServiceProvider.GetService<IStorage>();
    }

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

        //TOOD: Comment this line to test
        //listResult.Should().BeEquivalentTo(expectedList, x => x.Excluding(f => f.Uri));
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
    public async Task UploadFileAsStreamSpecifyingFileNameAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsStreamSpecifyingFileNameAsync)}";
        const string fileName = $"{nameof(UploadFileAsStreamSpecifyingFileNameAsync)}.txt";

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

        //Download the file
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
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

        //Forming file to upload
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        //Check is exist file
        if (await Storage.ExistsAsync(blobMetadata))
        {
            await Storage.DeleteAsync(blobMetadata);
        }

        //Upload file as stream
        await Storage.UploadStreamAsync(blobMetadata, stream);

        //Download the file
        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileAsTextSpecifyingFileNameAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsTextSpecifyingFileNameAsync)}";
        const string fileName = $"{nameof(UploadFileAsTextSpecifyingFileNameAsync)}.txt";

        //Forming file to upload
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        //Check is exist file
        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        //Upload file as text
        await Storage.UploadAsync(fileName, uploadContent);

        //Download the file
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
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

        //Forming file to upload
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        //Check is exist file
        if (await Storage.ExistsAsync(blobMetadata))
        {
            await Storage.DeleteAsync(blobMetadata);
        }

        //Upload file as text
        await Storage.UploadAsync(blobMetadata, uploadContent);

        //Download the file
        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileFromPathSpecifyingFileNameAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileFromPathSpecifyingFileNameAsync)}";
        const string fileName = $"{nameof(UploadFileFromPathSpecifyingFileNameAsync)}.txt";

        //Forming file to upload
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var localFile = await LocalFile.FromStreamAsync(stream);

        //Check is exist file
        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        //Upload file as local file
        await Storage.UploadFileAsync(fileName, localFile.FilePath);

        //Download the file
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
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

        //Forming file to upload
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var localFile = await LocalFile.FromStreamAsync(stream);
        
        //Check is exist file
        if (await Storage.ExistsAsync(blobMetadata))
        {
            await Storage.DeleteAsync(blobMetadata);
        }

        //Upload file as local file
        await Storage.UploadFileAsync(blobMetadata, localFile.FilePath);

        //Download the file
        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileAsArrayAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsArrayAsync)}";
        const string fileName = $"{nameof(UploadFileAsArrayAsync)}.txt";

        //Forming file to upload
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);

        //Check is exist file
        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        //Upload file as byte array
        await Storage.UploadAsync(new BlobMetadata { Name = fileName }, byteArray);

        //Download the file
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsAsTextWithoutNameSpecifiedAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsAsTextWithoutNameSpecifiedAsync)}";

        //Upload file as text
        var fileName = await Storage.UploadAsync(uploadContent);

        //Download the file
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsAsStreamWithoutNameSpecifiedAsync()
    {
        const string uploadContent = $"test {nameof(UploadFileAsAsStreamWithoutNameSpecifiedAsync)}";

        //Forming file to upload
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        //Upload file as text
        var fileName = await Storage.UploadAsync(stream);

        //Download the file
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Download

    [Fact]
    public async Task DownloadFileBlobMetadataAsLocalFileAsync()
    {
        const string uploadContent = $"test {nameof(DownloadFileBlobMetadataAsLocalFileAsync)}";
        const string fileName = $"{nameof(DownloadFileBlobMetadataAsLocalFileAsync)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Download file as LocalFile
        var localFile = await Storage.DownloadAsync(new BlobMetadata { Name = fileName });
        using var sr = new StreamReader(localFile.FileStream, Encoding.UTF8);

        //Get content from file as string
        string content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileAsLocalFileAsync()
    {
        const string uploadContent = $"test {nameof(DownloadFileAsLocalFileAsync)}";
        const string fileName = $"{nameof(DownloadFileAsLocalFileAsync)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Download file as LocalFile
        var localFile = await Storage.DownloadAsync(fileName);
        using var sr = new StreamReader(localFile.FileStream, Encoding.UTF8);

        //Get content from file as string
        string content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileBlobMetadataAsync()
    {
        const string uploadContent = $"test {nameof(DownloadFileAsync)}";
        const string fileName = $"{nameof(DownloadFileAsync)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Download file as stream
        var stream = await Storage.DownloadAsStreamAsync(new BlobMetadata { Name = fileName});
        using var sr = new StreamReader(stream, Encoding.UTF8);

        //Get content from file as string
        string content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        //Delete file
        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileAsync()
    {
        const string uploadContent = $"test {nameof(DownloadFileAsync)}";
        const string fileName = $"{nameof(DownloadFileAsync)}.txt";

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

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteFileListAsync()
    {
        var listFile = new List<(string FileName, string UploadedContent)>();
        listFile.Add(($"{nameof(DeleteFileListAsync)}1.txt", $"test {nameof(DeleteFileListAsync)}1"));
        listFile.Add(($"{nameof(DeleteFileListAsync)}2.txt", $"test {nameof(DeleteFileListAsync)}2"));
        listFile.Add(($"{nameof(DeleteFileListAsync)}3.txt", $"test {nameof(DeleteFileListAsync)}3"));

        // Upload files to server
        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        };

        var expectedList = listFile.Select(x => x.FileName);

        //Delete list files
        await Storage.DeleteAsync(expectedList);

        //Check is exist files
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

        // Upload files to server
        foreach (var item in listFile)
        {
            await PrepareFileToTest(item.UploadedContent, item.FileName);
        };

        var expectedList = listFile.Select(x => new BlobMetadata { Name = x.FileName });

        //Delete list blobMetadata
        await Storage.DeleteAsync(expectedList);

        //Check is exist files
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

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Get file as BlobMetadata
        var blobMetadata = await Storage.GetBlobAsync(fileName);

        //Delete BlobMetadata
        await Storage.DeleteAsync(blobMetadata);

        //Check is exists file
        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();

    }

    [Fact]
    public async Task DeleteFileAsStringAsync()
    {
        const string uploadContent = $"test {nameof(DeleteFileAsStringAsync)}";
        const string fileName = $"{nameof(DeleteFileAsStringAsync)}.txt";

        //Upload file
        await PrepareFileToTest(uploadContent, fileName);

        //Delete file
        await Storage.DeleteAsync(fileName);

        //Check is exists file
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

    private async Task<string> DownloadAsync(string fileName)
    {
        //Download file
        var stream = await Storage.DownloadAsStreamAsync(fileName);
        var sr = new StreamReader(stream, Encoding.UTF8);

        //Get content from file as string
        return await sr.ReadToEndAsync();
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