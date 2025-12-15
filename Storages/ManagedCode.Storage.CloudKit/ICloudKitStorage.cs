using ManagedCode.Storage.Core;
using ManagedCode.Storage.CloudKit.Clients;
using ManagedCode.Storage.CloudKit.Options;

namespace ManagedCode.Storage.CloudKit;

public interface ICloudKitStorage : IStorage<ICloudKitClient, CloudKitStorageOptions>
{
}

