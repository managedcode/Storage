using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.AzureDataLake.Options;

public class OpenWriteStreamOptions : BaseOptions
{
    public bool Overwrite { get; set; }
}