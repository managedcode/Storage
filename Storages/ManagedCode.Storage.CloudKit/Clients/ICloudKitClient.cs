using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.CloudKit.Clients;

public interface ICloudKitClient
{
    Task<CloudKitRecord> UploadAsync(string recordName, string internalPath, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string recordName, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string recordName, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string recordName, CancellationToken cancellationToken);

    Task<CloudKitRecord?> GetRecordAsync(string recordName, CancellationToken cancellationToken);

    IAsyncEnumerable<CloudKitRecord> QueryByPathPrefixAsync(string pathPrefix, CancellationToken cancellationToken);
}

