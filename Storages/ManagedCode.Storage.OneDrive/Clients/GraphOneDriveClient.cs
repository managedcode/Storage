using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace ManagedCode.Storage.OneDrive.Clients;

public class GraphOneDriveClient : IOneDriveClient
{
    private readonly GraphServiceClient _graphServiceClient;

    public GraphOneDriveClient(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient ?? throw new ArgumentNullException(nameof(graphServiceClient));
    }

    public Task EnsureRootAsync(string driveId, string rootPath, bool createIfNotExists, CancellationToken cancellationToken)
    {
        // Graph-backed provisioning is not executed in this offline wrapper.
        return Task.CompletedTask;
    }

    public Task<DriveItem> UploadAsync(string driveId, string path, Stream content, string? contentType, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Graph upload requires a configured OneDrive runtime environment.");
    }

    public Task<Stream> DownloadAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Graph download requires a configured OneDrive runtime environment.");
    }

    public Task<bool> DeleteAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Graph deletion requires a configured OneDrive runtime environment.");
    }

    public Task<bool> ExistsAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<DriveItem?> GetMetadataAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        return Task.FromResult<DriveItem?>(null);
    }

    public IAsyncEnumerable<DriveItem> ListAsync(string driveId, string? directory, CancellationToken cancellationToken)
    {
        return AsyncEnumerable.Empty<DriveItem>();
    }
}
