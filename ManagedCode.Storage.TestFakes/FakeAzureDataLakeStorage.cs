using Azure.Storage.Files.DataLake;
using ManagedCode.Communication;
using ManagedCode.Storage.AzureDataLake;
using ManagedCode.Storage.AzureDataLake.Options;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.TestFakes;

public class FakeAzureDataLakeStorage : FileSystemStorage, IAzureDataLakeStorage
{
    public FakeAzureDataLakeStorage() : base(new FileSystemStorageOptions())
    {
    }

    public DataLakeFileSystemClient StorageClient { get; }

    public Task<Result> SetStorageOptions(AzureDataLakeStorageOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> SetStorageOptions(Action<AzureDataLakeStorageOptions> options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> CreateDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(directory))
        {
            Task.FromResult(Result.Fail());
        }

        try
        {
            Directory.CreateDirectory(directory);
            return Task.FromResult(Result.Succeed());
        }
        catch (Exception e)
        {
            return Task.FromResult(Result.Fail(e));
        }
    }

    public Task<Result> RenameDirectory(string directory, string newDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directory))
        {
            Task.FromResult(Result.Fail());
        }

        try
        {
            Directory.Move(directory, newDirectory);
            return Task.FromResult(Result.Succeed());
        }
        catch (Exception e)
        {
            return Task.FromResult(Result.Fail(e));
        }
    }

    public Task<Result<Stream>> OpenWriteStreamAsync(OpenWriteStreamOptions options, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(options.FullPath))
        {
            return Task.FromResult(Result<Stream>.Fail());
        }

        try
        {
            return Task.FromResult(Result<Stream>.Succeed(File.OpenWrite(options.FullPath)));
        }
        catch (Exception e)
        {
            return Task.FromResult(Result<Stream>.Fail(e));
        }
    }

    public Task<Result<Stream>> OpenReadStreamAsync(OpenReadStreamOptions options, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(options.FullPath))
        {
            return Task.FromResult(Result<Stream>.Fail());
        }

        try
        {
            return Task.FromResult(Result<Stream>.Succeed(File.OpenRead(options.FullPath)));
        }
        catch (Exception e)
        {
            return Task.FromResult(Result<Stream>.Fail(e));
        }
    }
}