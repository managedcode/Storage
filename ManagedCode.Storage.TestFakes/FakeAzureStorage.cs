using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ManagedCode.Communication;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.TestFakes;

public class FakeAzureStorage : FileSystemStorage, IAzureStorage
{
    public FakeAzureStorage() : base(new FileSystemStorageOptions())
    {
    }

    public BlobContainerClient StorageClient { get; }

    public Task<Result> SetStorageOptions(IStorageOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> SetStorageOptions(Action<IStorageOptions> options,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> SetStorageOptions(AzureStorageOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> SetStorageOptions(Action<AzureStorageOptions> options,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result<Stream>> OpenReadStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(fileName))
        {
            return Task.FromResult(Result<Stream>.Fail());
        }

        try
        {
            return Task.FromResult(Result<Stream>.Succeed(File.OpenRead(fileName)));
        }
        catch (Exception e)
        {
            return Task.FromResult(Result<Stream>.Fail(e));
        }
    }

    public Task<Result<Stream>> OpenWriteStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(fileName))
        {
            return Task.FromResult(Result<Stream>.Fail());
        }

        try
        {
            return Task.FromResult(Result<Stream>.Succeed(File.OpenWrite(fileName)));
        }
        catch (Exception e)
        {
            return Task.FromResult(Result<Stream>.Fail(e));
        }
    }

    public Stream GetBlobStream(string fileName, bool userBuffer = true, int bufferSize = BlobStream.DefaultBufferSize)
    {
        return File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
    }
}