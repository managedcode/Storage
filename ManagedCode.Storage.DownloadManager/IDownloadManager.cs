using System.IO;
using System.Threading.Tasks;

namespace ManagedCode.Storage.DownloadManager;

public interface IDownloadManager
{
    Task<Stream> Download(string blob);
    
    Task Upload(Stream stream);
}