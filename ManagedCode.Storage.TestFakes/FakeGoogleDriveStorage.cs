using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using ManagedCode.Communication;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.GoogleDrive;
using ManagedCode.Storage.GoogleDrive.Options;

namespace ManagedCode.Storage.TestFakes;

/// <summary>
/// Fake implementation of Google Drive storage for unit testing.
/// Uses the file system storage as the underlying implementation.
/// </summary>
public class FakeGoogleDriveStorage : FileSystemStorage, IGoogleDriveStorage
{
    public FakeGoogleDriveStorage() : base(new FileSystemStorageOptions())
    {
    }

    public FakeGoogleDriveStorage(string basePath) : base(new FileSystemStorageOptions { BaseFolder = basePath })
    {
    }

    /// <summary>
    /// Gets the storage client. Returns null in the fake implementation.
    /// </summary>
    public new DriveService StorageClient => null!;

    public Task<Result> SetStorageOptions(GoogleDriveStorageOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> SetStorageOptions(Action<GoogleDriveStorageOptions> options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }
}


