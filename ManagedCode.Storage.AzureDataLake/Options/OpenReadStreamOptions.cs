using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.AzureDataLake.Options;

public class OpenReadStreamOptions : BaseOptions
{
    public long Position { get; set; }

    public int BufferSize { get; set; }
}