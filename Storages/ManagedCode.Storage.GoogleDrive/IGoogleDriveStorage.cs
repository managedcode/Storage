using Google.Apis.Drive.v3;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.GoogleDrive.Options;

namespace ManagedCode.Storage.GoogleDrive;

/// <summary>
/// Represents a Google Drive storage interface.
/// </summary>
public interface IGoogleDriveStorage : IStorage<DriveService, GoogleDriveStorageOptions>
{
}


