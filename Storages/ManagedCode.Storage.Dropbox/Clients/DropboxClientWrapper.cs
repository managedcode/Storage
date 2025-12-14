using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace ManagedCode.Storage.Dropbox.Clients;

public class DropboxClientWrapper : IDropboxClientWrapper
{
    private readonly DropboxClient _client;

    public DropboxClientWrapper(DropboxClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task EnsureRootAsync(string rootPath, bool createIfNotExists, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return;
        }

        var normalized = Normalize(rootPath);
        try
        {
            await _client.Files.GetMetadataAsync(normalized);
        }
        catch (ApiException<GetMetadataError> ex) when (ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath.Value.IsNotFound)
        {
            if (!createIfNotExists)
            {
                return;
            }

            await _client.Files.CreateFolderV2Async(normalized, autorename: false);
        }
    }

    public async Task<DropboxItemMetadata> UploadAsync(string rootPath, string path, Stream content, string? contentType, CancellationToken cancellationToken)
    {
        var fullPath = Combine(rootPath, path);
        var uploaded = await _client.Files.UploadAsync(fullPath, WriteMode.Overwrite.Instance, body: content);
        var metadata = (await _client.Files.GetMetadataAsync(uploaded.PathLower)).AsFile;
        return ToItem(metadata);
    }

    public async Task<Stream> DownloadAsync(string rootPath, string path, CancellationToken cancellationToken)
    {
        var fullPath = Combine(rootPath, path);
        var response = await _client.Files.DownloadAsync(fullPath);
        return await response.GetContentAsStreamAsync();
    }

    public async Task<bool> DeleteAsync(string rootPath, string path, CancellationToken cancellationToken)
    {
        var fullPath = Combine(rootPath, path);
        await _client.Files.DeleteV2Async(fullPath);
        return true;
    }

    public async Task<bool> ExistsAsync(string rootPath, string path, CancellationToken cancellationToken)
    {
        var fullPath = Combine(rootPath, path);
        try
        {
            await _client.Files.GetMetadataAsync(fullPath);
            return true;
        }
        catch (ApiException<GetMetadataError> ex) when (ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath.Value.IsNotFound)
        {
            return false;
        }
    }

    public async Task<DropboxItemMetadata?> GetMetadataAsync(string rootPath, string path, CancellationToken cancellationToken)
    {
        var fullPath = Combine(rootPath, path);
        try
        {
            var metadata = await _client.Files.GetMetadataAsync(fullPath);
            return metadata.IsFile ? ToItem(metadata.AsFile) : null;
        }
        catch (ApiException<GetMetadataError> ex) when (ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath.Value.IsNotFound)
        {
            return null;
        }
    }

    public async IAsyncEnumerable<DropboxItemMetadata> ListAsync(string rootPath, string? directory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fullPath = Combine(rootPath, directory ?? string.Empty);
        var list = await _client.Files.ListFolderAsync(fullPath);
        foreach (var item in list.Entries)
        {
            if (item.IsFile)
            {
                yield return ToItem(item.AsFile);
            }
        }

        while (list.HasMore)
        {
            list = await _client.Files.ListFolderContinueAsync(list.Cursor);
            foreach (var item in list.Entries)
            {
                if (item.IsFile)
                {
                    yield return ToItem(item.AsFile);
                }
            }
        }
    }

    private static DropboxItemMetadata ToItem(FileMetadata file)
    {
        return new DropboxItemMetadata
        {
            Name = file.Name,
            Path = file.PathLower ?? file.PathDisplay ?? string.Empty,
            Size = file.Size,
            ClientModified = file.ClientModified,
            ServerModified = file.ServerModified
        };
    }

    private static string Normalize(string path)
    {
        var normalized = path.Replace("\\", "/");
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/') == string.Empty ? "/" : normalized.TrimEnd('/');
    }

    private static string Combine(string root, string path)
    {
        var normalizedRoot = Normalize(root);
        var normalizedPath = path.Replace("\\", "/").Trim('/');
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return normalizedRoot;
        }

        return normalizedRoot.EndsWith("/") ? normalizedRoot + normalizedPath : normalizedRoot + "/" + normalizedPath;
    }
}
