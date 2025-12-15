using Google.Apis.Drive.v3;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.GoogleDrive.Clients;

namespace ManagedCode.Storage.GoogleDrive.Options;

public class GoogleDriveStorageOptions : IStorageOptions
{
    public IGoogleDriveClient? Client { get; set; }

    public DriveService? DriveService { get; set; }

    public string RootFolderId { get; set; } = "root";

    public bool CreateContainerIfNotExists { get; set; } = true;
}
