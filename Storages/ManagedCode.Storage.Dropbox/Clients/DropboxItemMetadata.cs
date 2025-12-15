using System;

namespace ManagedCode.Storage.Dropbox.Clients;

public class DropboxItemMetadata
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public ulong Size { get; set; }
    public DateTime ClientModified { get; set; }
    public DateTime ServerModified { get; set; }
}
