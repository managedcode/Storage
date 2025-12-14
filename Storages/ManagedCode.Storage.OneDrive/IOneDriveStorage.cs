using ManagedCode.Storage.Core;
using ManagedCode.Storage.OneDrive.Clients;
using ManagedCode.Storage.OneDrive.Options;

namespace ManagedCode.Storage.OneDrive;

public interface IOneDriveStorage : IStorage<IOneDriveClient, OneDriveStorageOptions>
{
}
