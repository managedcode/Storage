using ManagedCode.Storage.Core;
using ManagedCode.Storage.GoogleDrive.Clients;
using ManagedCode.Storage.GoogleDrive.Options;

namespace ManagedCode.Storage.GoogleDrive;

public interface IGoogleDriveStorage : IStorage<IGoogleDriveClient, GoogleDriveStorageOptions>
{
}
