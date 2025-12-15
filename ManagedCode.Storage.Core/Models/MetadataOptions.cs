namespace ManagedCode.Storage.Core.Models;

public class MetadataOptions : BaseOptions
{
    public static MetadataOptions FromBaseOptions(BaseOptions options)
    {
        return new MetadataOptions { FileName = options.FileName, Directory = options.Directory };
    }

    public string ETag { get; set; } = string.Empty;
}