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
        return EnsureRootInternalAsync(driveId, rootPath, createIfNotExists, cancellationToken);
    }

    public Task<DriveItem> UploadAsync(string driveId, string path, Stream content, string? contentType, CancellationToken cancellationToken)
    {
        return UploadInternalAsync(driveId, path, content, contentType, cancellationToken);
    }

    public Task<Stream> DownloadAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        return DownloadInternalAsync(driveId, path, cancellationToken);
    }

    public Task<bool> DeleteAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        return DeleteInternalAsync(driveId, path, cancellationToken);
    }

    public Task<bool> ExistsAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        return ExistsInternalAsync(driveId, path, cancellationToken);
    }

    public Task<DriveItem?> GetMetadataAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        return GetMetadataInternalAsync(driveId, path, cancellationToken);
    }

    public IAsyncEnumerable<DriveItem> ListAsync(string driveId, string? directory, CancellationToken cancellationToken)
    {
        return ListInternalAsync(driveId, directory, cancellationToken);
    }

    private async Task EnsureRootInternalAsync(string driveId, string rootPath, bool createIfNotExists, CancellationToken cancellationToken)
    {
        var normalizedRoot = NormalizePath(rootPath);
        if (string.IsNullOrWhiteSpace(normalizedRoot) || normalizedRoot == "/")
        {
            return;
        }

        var root = await GetRootDriveItemAsync(driveId, cancellationToken).ConfigureAwait(false);
        var parentId = root.Id ?? throw new InvalidOperationException("Drive root is unavailable for the configured account.");
        var segments = normalizedRoot.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = await FindChildAsync(driveId, parentId, segment, cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                parentId = existing.Id!;
                continue;
            }

            if (!createIfNotExists)
            {
                throw new DirectoryNotFoundException($"Folder '{normalizedRoot}' is missing in the configured drive.");
            }

            var childrenBuilder = await GetChildrenBuilderAsync(driveId, parentId, cancellationToken).ConfigureAwait(false);
            var created = await childrenBuilder.PostAsync(new DriveItem
            {
                Name = segment,
                Folder = new Folder()
            }, cancellationToken: cancellationToken).ConfigureAwait(false);

            parentId = created?.Id ?? throw new InvalidOperationException($"Failed to create OneDrive folder '{segment}'.");
        }
    }

    private async Task<DriveItem> UploadInternalAsync(string driveId, string path, Stream content, string? contentType, CancellationToken cancellationToken)
    {
        var rootBuilder = await GetRootItemBuilderAsync(driveId, cancellationToken).ConfigureAwait(false);
        var request = rootBuilder.ItemWithPath(NormalizePath(path)).Content;
        var response = await request.PutAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);

        return response ?? throw new InvalidOperationException("Graph upload returned no item.");
    }

    private async Task<Stream> DownloadInternalAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        var rootBuilder = await GetRootItemBuilderAsync(driveId, cancellationToken).ConfigureAwait(false);
        var request = rootBuilder.ItemWithPath(NormalizePath(path)).Content;
        var stream = await request.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return stream ?? throw new FileNotFoundException($"File '{path}' not found in OneDrive.");
    }

    private async Task<bool> DeleteInternalAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        try
        {
            var rootBuilder = await GetRootItemBuilderAsync(driveId, cancellationToken).ConfigureAwait(false);
            await rootBuilder.ItemWithPath(NormalizePath(path)).DeleteAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            return false;
        }
    }

    private async Task<bool> ExistsInternalAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        var item = await GetMetadataInternalAsync(driveId, path, cancellationToken).ConfigureAwait(false);
        return item != null;
    }

    private async Task<DriveItem?> GetMetadataInternalAsync(string driveId, string path, CancellationToken cancellationToken)
    {
        try
        {
            var rootBuilder = await GetRootItemBuilderAsync(driveId, cancellationToken).ConfigureAwait(false);
            return await rootBuilder.ItemWithPath(NormalizePath(path)).GetAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    private async IAsyncEnumerable<DriveItem> ListInternalAsync(string driveId, string? directory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var normalized = string.IsNullOrWhiteSpace(directory) ? null : NormalizePath(directory!);
        var resolvedDriveId = await ResolveDriveIdAsync(driveId, cancellationToken).ConfigureAwait(false);
        var parent = normalized == null
            ? await _graphServiceClient.Drives[resolvedDriveId].Root.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false)
            : await _graphServiceClient.Drives[resolvedDriveId].Root.ItemWithPath(normalized).GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (parent?.Id == null)
        {
            yield break;
        }

        var builder = _graphServiceClient.Drives[resolvedDriveId].Items[parent.Id].Children;
        var page = await builder.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (page?.Value == null)
        {
            yield break;
        }

        foreach (var item in page.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item != null)
            {
                yield return item;
            }
        }
    }

    private async Task<Microsoft.Graph.Drives.Item.Root.RootRequestBuilder> GetRootItemBuilderAsync(string driveId, CancellationToken cancellationToken)
    {
        var resolvedDriveId = await ResolveDriveIdAsync(driveId, cancellationToken).ConfigureAwait(false);
        return _graphServiceClient.Drives[resolvedDriveId].Root;
    }

    private async Task<Microsoft.Graph.Drives.Item.Items.Item.Children.ChildrenRequestBuilder> GetChildrenBuilderAsync(string driveId, string parentId, CancellationToken cancellationToken)
    {
        var resolvedDriveId = await ResolveDriveIdAsync(driveId, cancellationToken).ConfigureAwait(false);
        return _graphServiceClient.Drives[resolvedDriveId].Items[parentId].Children;
    }

    private async Task<DriveItem> GetRootDriveItemAsync(string driveId, CancellationToken cancellationToken)
    {
        var resolvedDriveId = await ResolveDriveIdAsync(driveId, cancellationToken).ConfigureAwait(false);
        var root = await _graphServiceClient.Drives[resolvedDriveId].Root.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return root ?? throw new InvalidOperationException("Drive root is unavailable for the configured account.");
    }

    private async Task<string> ResolveDriveIdAsync(string driveId, CancellationToken cancellationToken)
    {
        if (!driveId.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            return driveId;
        }

        var drive = await _graphServiceClient.Me.Drive.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return drive?.Id ?? throw new InvalidOperationException("Unable to resolve the current user's drive id.");
    }

    private async Task<DriveItem?> FindChildAsync(string driveId, string parentId, string name, CancellationToken cancellationToken)
    {
        var childrenBuilder = await GetChildrenBuilderAsync(driveId, parentId, cancellationToken).ConfigureAwait(false);
        var children = await childrenBuilder.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return children?.Value?.FirstOrDefault(c => string.Equals(c?.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace("\\", "/").Trim('/');
    }
}
