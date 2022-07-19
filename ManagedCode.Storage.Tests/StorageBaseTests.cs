using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;
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
    /*

    [Fact]
    public async Task CreateContainerTest()
    {
        var container = await Storage.ContainerManager.CreateAsync("test");
        container.IsSucceeded.Should().BeTrue();
    }
    
    [Fact]
    public async Task CreateAndRemoveContainerTest()
    {
        var create = await Storage.CreateAsync("test-for-delete");
        create.IsSucceeded.Should().BeTrue();
        
        var remove = await Storage.ContainerManager.RemoveAsync("test-for-delete");
        remove.IsSucceeded.Should().BeTrue();
    }
    
    [Fact]
    public async Task StreamUploadAsyncTest()
    {
        var file = await GetTestFile();
        var uploadResult = await Storage.UploadAsync(file.OpenRead());
        uploadResult.IsSucceeded.Should().BeTrue();

        await Storage.UploadAsync("sdfdsf");
       
        await Storage.UploadAsync("sdfdsf", options =>
        {
            options.Container = "super";
        });

        await Storage.UploadAsync("sdfsdf", new UploadOptions(container: "stuper"));
    }
    
    [Fact]
    public async Task ArrayUploadAsyncTest()
    {
        var file = await GetTestFile();
        var bytes = await File.ReadAllBytesAsync(file.FullName);
        var uploadResult = await Storage.UploadAsync(bytes);
        uploadResult.IsSucceeded.Should().BeTrue();
    }
    
    [Fact]
    public async Task StringUploadAsyncTest()
    {
        var file = await GetTestFile();
        var text = await File.ReadAllTextAsync(file.FullName);
        var uploadResult = await Storage.UploadAsync(text);
        uploadResult.IsSucceeded.Should().BeTrue();
    }
    
    [Fact]
    public async Task FileInfoUploadAsyncTest()
    {
        var file = await GetTestFile();
        var uploadResult = await Storage.UploadAsync(file);
        uploadResult.IsSucceeded.Should().BeTrue();
        
        var downloadResult = await Storage.DownloadAsync(uploadResult.Value);
        downloadResult.IsSucceeded.Should().BeTrue();
    }
    
    [Fact]
    public async Task UploadAndDownloadTest()
    {
        var file = await GetTestFile();
        var uploadResult = await Storage.UploadAsync(file);
        uploadResult.IsSucceeded.Should().BeTrue();
        
        var downloadResult = await Storage.DownloadAsync(uploadResult.Value);
        downloadResult.IsSucceeded.Should().BeTrue();
        
        File.ReadAllText(file.FullName).Should().Be(File.ReadAllText(downloadResult.Value.Path));
    }
    
    [Fact]
    public async Task UploadAndGetStreamTest()
    {
        var file = await GetTestFile();
        var uploadResult = await Storage.UploadAsync(file);
        uploadResult.IsSucceeded.Should().BeTrue();
        
        var downloadStream = await Storage.OpenReadStreamAsync(uploadResult.Value);
        downloadStream.IsSucceeded.Should().BeTrue();
        var sw = new StreamReader(downloadStream.Value);

        var content = await sw.ReadToEndAsync();
        sw.Dispose();
        
        File.ReadAllText(file.FullName).Should().Be(content);
    } */
    
    
    
    [Fact]
    public async Task GetFileListAsyncTest()
    {
    //    await StreamUploadAsyncTest();
     //   await ArrayUploadAsyncTest();
    //    await StringUploadAsyncTest();
//
        //var files = await Storage.GetBlobsAsync();
        //files.IsSucceeded.Should().BeTrue();
        //files.Value.Length.Should().BeGreaterOrEqualTo(5);
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
/*
    #region Async

    #region Get

    [Fact]
    public async Task GetBlobListAsync()
    {
        // Arrange
        var fileList = await CreateFileList();

        // Act
        var result = Storage.GetBlobListAsync();
        var listResult = await result.ToListAsync();
        var expectedList = fileList.Select(x => new BlobMetadata {Name = x.FileName});

        // Assert
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
        // Arrange
        var fileList = await CreateFileList();
        var blobList = fileList.Select(f => f.FileName).ToList();

        // Act
        var result = await Storage.GetBlobsAsync(blobList).ToListAsync();

        // Assert
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
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        // Act
        var result = await Storage.GetBlobAsync(fileName);

        // Assert
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
    public async Task UploadStreamAsync_SpecifyingFileName()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        // Act
        await Storage.UploadStreamAsync(fileName, stream);

        // Assert
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadStreamAsync_SpecifyingBlobMetadata()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
        };

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        // Act
        await Storage.UploadStreamAsync(blobMetadata, stream);

        // Assert
        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadAsync_AsText_SpecifyingFileName()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        await Storage.UploadAsync(fileName, uploadContent);

        // Assert
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadAsync_AsText_SpecifyingBlobMetadata()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
        };

        // Act
        await Storage.UploadAsync(blobMetadata, uploadContent);

        // Assert
        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadAsync_FromPath_SpecifyingFileName()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var localFile = await LocalFile.FromStreamAsync(stream);

        // Act
        await Storage.UploadFileAsync(fileName, localFile.FilePath);

        // Assert
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadAsync_FromPath_SpecifyingBlobMetadata()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var blobMetadata = new BlobMetadata
        {
            Name = FileHelper.GenerateRandomFileName()
        };

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var localFile = await LocalFile.FromStreamAsync(stream);

        // Act
        await Storage.UploadFileAsync(blobMetadata, localFile.FilePath);

        // Assert
        var downloadedContent = await DownloadAsync(blobMetadata.Name);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(blobMetadata.Name);
    }

    [Fact]
    public async Task UploadAsync_AsArray_SpecifyingBlobMetadata()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);

        // Act
        await Storage.UploadAsync(new BlobMetadata {Name = fileName}, byteArray);

        // Assert
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadAsync_AsText_WithoutNameSpecified()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();

        // Act
        var fileName = await Storage.UploadAsync(uploadContent);

        // Assert
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task UploadAsync_AsStream_WithoutNameSpecified()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        // Act
        var fileName = await Storage.UploadAsync(stream);

        // Assert
        var downloadedContent = await DownloadAsync(fileName);
        downloadedContent.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    #endregion

    #region Download

    [Fact]
    public async Task DownloadAsync_ByBlobMetadata_AsLocalFile()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        // Act
        var localFile = await Storage.DownloadAsync(new BlobMetadata {Name = fileName});
        using var sr = new StreamReader(localFile!.FileStream, Encoding.UTF8);
        var content = await sr.ReadToEndAsync();

        // Assert
        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsync_ByFileName_AsLocalFile()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        // Act
        var localFile = await Storage.DownloadAsync(fileName);
        using var sr = new StreamReader(localFile!.FileStream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        // Assert
        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsStreamAsyncc_ByBlobMetadata_AsStream()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        // Act
        var stream = await Storage.DownloadAsStreamAsync(new BlobMetadata {Name = fileName});
        using var sr = new StreamReader(stream!, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        // Assert
        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsStreamAsync_ByFileName_AsStream()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        // Act
        var stream = await Storage.DownloadAsStreamAsync(fileName);
        using var sr = new StreamReader(stream!, Encoding.UTF8);
        var content = await sr.ReadToEndAsync();

        // Assert
        content.Should().NotBeNull();
        content.Should().Be(uploadContent);

        await DeleteFileAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsync_ByBlobMetadata_AsStream_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var localFile = await Storage.DownloadAsync(new BlobMetadata {Name = fileName});

        // Assert
        localFile.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsync_ByFileName_AsStream_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var localFile = await Storage.DownloadAsync(fileName);

        // Assert
        localFile.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsStreamAsync_ByBlobMetadata_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var stream = await Storage.DownloadAsStreamAsync(new BlobMetadata {Name = fileName});

        // Assert
        stream.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsStreamAsync_ByFileName_IfFileDontExist()
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
    public async Task DeleteAsync_ByListWithFileNames()
    {
        // Arrange
        var fileList = await CreateFileList();
        var expectedList = fileList.Select(x => x.FileName).ToList();

        // Act
        await Storage.DeleteAsync(expectedList);

        // Assert
        var result = Storage.ExistsAsync(expectedList);
        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteAsync_ByListWithBlobMetadata()
    {
        // Arrange
        var fileList = await CreateFileList();
        var expectedList = fileList.Select(x => new BlobMetadata {Name = x.FileName}).ToList();

        // Act
        await Storage.DeleteAsync(expectedList);

        // Assert
        var result = Storage.ExistsAsync(expectedList);
        var resultList = await result.ToListAsync();

        foreach (var item in resultList)
        {
            item.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteAsync_ByBlobMetadata()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);
        var blobMetadata = await Storage.GetBlobAsync(fileName);

        // Act
        await Storage.DeleteAsync(blobMetadata!);

        // Assert
        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ByFileName()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        // Act
        await Storage.DeleteAsync(fileName);

        // Assert
        var result = await Storage.ExistsAsync(fileName);

        result.Should().BeFalse();
    }

    #endregion

    #region Exist

    [Fact]
    public async Task ExistsAsync_ByFileName()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        // Act
        var result = await Storage.ExistsAsync(fileName);

        // Assert
        result.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistsAsync_ByBlobMetadata()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        await PrepareFileToTest(fileName, uploadContent);

        var file = new BlobMetadata
        {
            Name = fileName
        };

        // Act
        var result = await Storage.ExistsAsync(file);

        // Assert
        result.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task ExistsAsync_ByListWithFileNames()
    {
        // Arrange
        var fileList = await CreateFileList();
        var blobList = fileList.Select(x => x.FileName).ToList();

        // Act
        var result = Storage.ExistsAsync(blobList);
        var resultList = await result.ToListAsync();

        // Assert
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
    public async Task ExistsAsync_ByListWithBlobMetadata()
    {
        // Arrange
        var fileList = await CreateFileList();

        // Act
        var result = Storage.ExistsAsync(fileList.Select(x => new BlobMetadata {Name = x.FileName}));
        var resultList = await result.ToListAsync();

        // Assert
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

    
    */
    #region CreateContainer

    [Fact]
    public async Task CreateContainerAsync()
    {
        await FluentActions.Awaiting(() => Storage.CreateContainerAsync())
            .Should().NotThrowAsync<Exception>();
    }

    #endregion
    

    protected async Task<FileInfo> GetTestFile()
    {
        var fileName = Path.GetTempFileName();
        var fs = File.OpenWrite(fileName);
        var sw = new StreamWriter(fs);

        for (int i = 0; i < 10_000; i++)
        {
            await sw.WriteLineAsync(Guid.NewGuid().ToString());
        }

        await sw.DisposeAsync();
        await fs.DisposeAsync();
        
        return new FileInfo(fileName);
    }
    
    
  
}