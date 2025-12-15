using ManagedCode.Storage.Core;
using ManagedCode.Storage.Dropbox.Clients;
using ManagedCode.Storage.Dropbox.Options;

namespace ManagedCode.Storage.Dropbox;

public interface IDropboxStorage : IStorage<IDropboxClientWrapper, DropboxStorageOptions>
{
}
