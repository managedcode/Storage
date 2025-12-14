using Dropbox.Api;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Dropbox.Clients;

namespace ManagedCode.Storage.Dropbox.Options;

public class DropboxStorageOptions : IStorageOptions
{
    public IDropboxClientWrapper? Client { get; set; }

    public DropboxClient? DropboxClient { get; set; }

    public string RootPath { get; set; } = string.Empty;

    public bool CreateContainerIfNotExists { get; set; } = true;
}
