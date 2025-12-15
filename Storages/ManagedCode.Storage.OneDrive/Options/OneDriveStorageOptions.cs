using ManagedCode.Storage.Core;
using ManagedCode.Storage.OneDrive.Clients;
using Microsoft.Graph;

namespace ManagedCode.Storage.OneDrive.Options;

public class OneDriveStorageOptions : IStorageOptions
{
    public Clients.IOneDriveClient? Client { get; set; }

    public GraphServiceClient? GraphClient { get; set; }

    public string DriveId { get; set; } = "me";

    public string RootPath { get; set; } = "/";

    public bool CreateContainerIfNotExists { get; set; } = true;
}
