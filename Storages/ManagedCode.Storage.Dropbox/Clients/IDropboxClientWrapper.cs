using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api.Files;

namespace ManagedCode.Storage.Dropbox.Clients;

public interface IDropboxClientWrapper
{
    Task EnsureRootAsync(string rootPath, bool createIfNotExists, CancellationToken cancellationToken);

    Task<DropboxItemMetadata> UploadAsync(string rootPath, string path, Stream content, string? contentType, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string rootPath, string path, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string rootPath, string path, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string rootPath, string path, CancellationToken cancellationToken);

    Task<DropboxItemMetadata?> GetMetadataAsync(string rootPath, string path, CancellationToken cancellationToken);

    IAsyncEnumerable<DropboxItemMetadata> ListAsync(string rootPath, string? directory, CancellationToken cancellationToken);
}
