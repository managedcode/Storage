namespace ManagedCode.Storage.Core.Models;

public class ExistOptions : BaseOptions
{
    public static ExistOptions FromBaseOptions(BaseOptions options)
    {
        return new ExistOptions { FileName = options.FileName, Directory = options.Directory };
    }
}