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

// ReSharper disable MethodHasAsyncOverload

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

    #region Async

    #region Get

    [Fact]
    public async Task GetBlobListAsync()
    {
        var fileList = await CreateFileList();

        var result = Storage.GetBlobListAsync();

        var listResult = await result.ToListAsync();
        var expectedList = fileList.Select(x => new BlobMetadata {Name = x.FileName});

        foreach (var item in expectedList)
        {
            var file = listResult.FirstOrDefault(f => f.Name == item.Name);
            file.Should().NotBeNull();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public virtual async Task GetBlobsAsync()
    {
        var fileList = await CreateFileList();

        var blobList = fileList.Select(f => f.FileName).ToList();

        var result = await Storage.GetBlobsAsync(blobList).ToListAsync();

        foreach (var blobMetadata in result)
        {
            blobMetadata.Name.Should().NotBeNull();
            blobMetadata.Uri.Should().NotBeNull();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public virtual async Task GetBlobAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var result = await Storage.GetBlobAsync(fileName);

        result.Should().NotBeNull();
        result!.Name.Should().Be(fileName);
        result.Uri.Should().NotBeNull();

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task GetBlobsAsync_IfSomeFileDontExist()
    {
        // Array
        const int filesCount = 6;
        var fileList = await CreateFileList(filesCount);

        var blobList = fileList.Select(f => f.FileName).ToList();
        blobList.Add(FileHelper.GenerateRandomFileName());
        blobList.Add(FileHelper.GenerateRandomFileName());

        // Act
        var result = Storage.GetBlobsAsync(blobList);
        var listResult = await result.ToListAsync();

        // Assert
        listResult.Count.Should().Be(filesCount);

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public async Task GetBlobAsync_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var blobMetadata = await Storage.GetBlobAsync(fileName);

        // Assert
        blobMetadata.Should().BeNull();
    }

    #endregion

    #region Upload

    [Fact]
    public async Task UploadFileAsStreamSpecifyingFileNameAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

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
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
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
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

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
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
        };

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
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

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
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
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
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);

        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        await Storage.UploadAsync(new BlobMetadata {Name = fileName}, byteArray);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsAsTextWithoutNameSpecifiedAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();

        var fileName = await Storage.UploadAsync(uploadContent);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsAsStreamWithoutNameSpecifiedAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();

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
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var localFile = await Storage.DownloadAsync(new BlobMetadata {Name = fileName});
        using var sr = new StreamReader(localFile!.FileStream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileAsLocalFileAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var localFile = await Storage.DownloadAsync(fileName);
        using var sr = new StreamReader(localFile!.FileStream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileBlobMetadataAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var stream = await Storage.DownloadAsStreamAsync(new BlobMetadata {Name = fileName});
        using var sr = new StreamReader(stream!, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var stream = await Storage.DownloadAsStreamAsync(fileName);
        using var sr = new StreamReader(stream!, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsyncBlobMetadataAsLocalFile_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var localFile = await Storage.DownloadAsync(new BlobMetadata {Name = fileName});

        // Assert
        localFile.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsyncFileNameAsLocalFile_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var localFile = await Storage.DownloadAsync(fileName);

        // Assert
        localFile.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsStreamAsyncBlobMetadata_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var stream = await Storage.DownloadAsStreamAsync(new BlobMetadata {Name = fileName});

        // Assert
        stream.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsyncFileName_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var stream = await Storage.DownloadAsStreamAsync(fileName);

        // Assert
        stream.Should().BeNull();
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteFileListAsync()
    {
        var fileList = await CreateFileList();

        var expectedList = fileList.Select(x => x.FileName).ToList();
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
        var fileList = await CreateFileList();

        var expectedList = fileList.Select(x => new BlobMetadata {Name = x.FileName}).ToList();

        await Storage.DeleteAsync(expectedList);

        var result = Storage.ExistsAsync(expectedList);
        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteFileAsBlobMetadataAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var blobMetadata = await Storage.GetBlobAsync(fileName);

        await Storage.DeleteAsync(blobMetadata!);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsStringAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        await Storage.DeleteAsync(fileName);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();
    }

    #endregion

    #region Exist

    [Fact]
    public async Task SingleBlobExistsAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistFileByBlobMetadataAsync()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

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
        var fileList = await CreateFileList();


        var blobList = fileList.Select(x => x.FileName).ToList();

        var result = Storage.ExistsAsync(blobList);

        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeTrue();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public async Task ExistFileByListBlobMetadataAsync()
    {
        var fileList = await CreateFileList();

        var result = Storage.ExistsAsync(fileList.Select(x => new BlobMetadata {Name = x.FileName}));

        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeTrue();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    #endregion

    #region CreateContainer

    [Fact]
    public async Task CreateContainerAsync()
    {
        await FluentActions.Awaiting(() => Storage.CreateContainerAsync())
            .Should().NotThrowAsync<Exception>();
    }

    #endregion

    #endregion

    #region Sync

    #region Async

    #region Get

    [Fact]
    public async Task GetBlobList()
    {
        var fileList = await CreateFileList();

        var result = Storage.GetBlobList();

        var expectedList = fileList.Select(x => new BlobMetadata {Name = x.FileName});

        foreach (var item in expectedList)
        {
            var file = result.FirstOrDefault(f => f.Name == item.Name);
            file.Should().NotBeNull();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public virtual async Task GetBlobs()
    {
        var fileList = await CreateFileList();

        var blobList = fileList.Select(f => f.FileName).ToList();

        var result = Storage.GetBlobs(blobList);

        foreach (var blobMetadata in result)
        {
            blobMetadata.Name.Should().NotBeNull();
            blobMetadata.Uri.Should().NotBeNull();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public virtual async Task GetBlob()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        // ReSharper disable once MethodHasAsyncOverload
        var result = Storage.GetBlob(fileName);

        result.Should().NotBeNull();
        result!.Name.Should().Be(fileName);
        result.Uri.Should().NotBeNull();

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task GetBlobs_IfSomeFileDontExist()
    {
        // Array
        const int filesCount = 6;
        var fileList = await CreateFileList(filesCount);

        var blobList = fileList.Select(f => f.FileName).ToList();
        blobList.Add(FileHelper.GenerateRandomFileName());
        blobList.Add(FileHelper.GenerateRandomFileName());

        // Act
        var result = Storage.GetBlobs(blobList).ToList();

        // Assert
        result.Count.Should().Be(filesCount);

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public Task GetBlob_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var blobMetadata = Storage.GetBlob(fileName);

        // Assert
        blobMetadata.Should().BeNull();

        return Task.CompletedTask;
    }

    #endregion

    #region Upload

    [Fact]
    public async Task UploadFileAsStream_SpecifyingFileName()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        // ReSharper disable once MethodHasAsyncOverload
        Storage.UploadStream(fileName, stream);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsStream_SpecifyingBlobMetadata()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
        };

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        Storage.UploadStream(blobMetadata, stream);

        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileAsText_SpecifyingFileName()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        Storage.Upload(fileName, uploadContent);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsText_SpecifyingBlobMetadata()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
        };

        Storage.Upload(blobMetadata, uploadContent);

        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileFromPath_SpecifyingFileName()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var localFile = await LocalFile.FromStreamAsync(stream);

        Storage.UploadFile(fileName, localFile.FilePath);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileFromPath_SpecifyingBlobMetadata()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
        };

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var localFile = await LocalFile.FromStreamAsync(stream);

        Storage.UploadFile(blobMetadata, localFile.FilePath);

        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadFileAsArray()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);

        Storage.Upload(new BlobMetadata {Name = fileName}, byteArray);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsAsText_WithoutNameSpecified()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();

        var fileName = Storage.Upload(uploadContent);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadFileAsAsStream_WithoutNameSpecified()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        var fileName = Storage.Upload(stream);

        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Download

    [Fact]
    public async Task DownloadFileBlobMetadata_AsLocalFile()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var localFile = Storage.Download(new BlobMetadata {Name = fileName});
        using var sr = new StreamReader(localFile!.FileStream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFile_AsLocalFile()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var localFile = Storage.Download(fileName);
        using var sr = new StreamReader(localFile!.FileStream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFileBlobMetadata()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var stream = Storage.DownloadAsStream(new BlobMetadata {Name = fileName});
        using var sr = new StreamReader(stream!, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadFile()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var stream = Storage.DownloadAsStream(fileName);
        using var sr = new StreamReader(stream!, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public void DownloadBlobMetadata_AsLocalFile_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var localFile = Storage.Download(new BlobMetadata {Name = fileName});

        // Assert
        localFile.Should().BeNull();
    }

    [Fact]
    public void DownloadFileName_AsLocalFile_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var localFile = Storage.Download(fileName);

        // Assert
        localFile.Should().BeNull();
    }

    [Fact]
    public void Download_AsStreamBlobMetadata_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var stream = Storage.DownloadAsStream(new BlobMetadata {Name = fileName});

        // Assert
        stream.Should().BeNull();
    }

    [Fact]
    public void DownloadFileName_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var stream = Storage.DownloadAsStream(fileName);

        // Assert
        stream.Should().BeNull();
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteFileList()
    {
        var fileList = await CreateFileList();

        var expectedList = fileList.Select(x => x.FileName).ToList();
        await Storage.DeleteAsync(expectedList);

        var result = Storage.Exists(expectedList);

        foreach (var item in result)
        {
            item.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteFile_AsBlobMetadataList()
    {
        var fileList = await CreateFileList();

        var expectedList = fileList.Select(x => new BlobMetadata {Name = x.FileName}).ToList();

        await Storage.DeleteAsync(expectedList);

        var result = Storage.Exists(expectedList);

        foreach (var item in result)
        {
            item.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteFile_AsBlobMetadata()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var blobMetadata = await Storage.GetBlobAsync(fileName);

        Storage.Delete(blobMetadata!);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFile_AsFileName()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        Storage.Delete(fileName);

        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();
    }

    #endregion

    #region Exist

    [Fact]
    public async Task SingleBlobExists()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var result = Storage.Exists(fileName);

        result.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistFile_ByBlobMetadata()
    {
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var file = new BlobMetadata
        {
            Name = fileName
        };

        var result = Storage.Exists(file);

        result.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistFile_ByListString()
    {
        var fileList = await CreateFileList();


        var blobList = fileList.Select(x => x.FileName).ToList();

        var result = Storage.Exists(blobList);

        foreach (var item in result)
        {
            item.Should().BeTrue();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public async Task ExistFile_ByListBlobMetadata()
    {
        var fileList = await CreateFileList();

        var result = Storage.Exists(fileList.Select(x => new BlobMetadata {Name = x.FileName}));

        foreach (var item in result)
        {
            item.Should().BeTrue();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    #endregion

    #region CreateContainer

    [Fact]
    public void CreateContainer()
    {
        FluentActions.Invoking(() => Storage.CreateContainer())
            .Should().NotThrow<Exception>();
    }

    #endregion

    #endregion

    #endregion

    protected async Task PrepareFileToTest(string fileName, string content)
    {
        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }

        await Storage.UploadAsync(fileName, content);
    }

    protected async Task DeleteFileAsync(string fileName)
    {
        if (await Storage.ExistsAsync(fileName))
        {
            await Storage.DeleteAsync(fileName);
        }
    }

    protected async Task<string> DownloadAsync(string fileName)
    {
        var stream = await Storage.DownloadAsStreamAsync(fileName);
        var sr = new StreamReader(stream!, Encoding.UTF8);

        return await sr.ReadToEndAsync();
    }

    protected async Task<List<(string FileName, string UploadedContent)>> CreateFileList(int count = 5)
    {
        var listFile = new List<(string FileName, string UploadedContent)>();

        for (var i = 0; i < count; i++)
        {
            var file = (FileHelper.GenerateRandomFileName(), FileHelper.GenerateRandomFileName());
            await PrepareFileToTest(file.Item1, file.Item2);
            listFile.Add(file);
        }

        return listFile;
    }
}