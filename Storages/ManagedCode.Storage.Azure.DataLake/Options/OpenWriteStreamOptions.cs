using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Azure.DataLake.Options;

public class OpenWriteStreamOptions : BaseOptions
{
    public bool Overwrite { get; set; }
}