using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Browser.Models;
using Microsoft.JSInterop;

namespace ManagedCode.Storage.Browser.Interop;

internal sealed class BrowserIndexedDbInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private const string ModulePath = "./_content/ManagedCode.Storage.Browser/browserStorage.js";
    private Task<IJSObjectReference>? _moduleTask;

    public Task<bool> ContainerExistsAsync(string databaseName, string containerName, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(module => module.InvokeAsync<bool>("containerExists", cancellationToken, databaseName, containerName));
    }

    public Task CreateContainerAsync(string databaseName, string containerName, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(async module =>
        {
            await module.InvokeVoidAsync("createContainer", cancellationToken, databaseName, containerName);
            return 0;
        });
    }

    public Task RemoveContainerAsync(string databaseName, string containerName, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(async module =>
        {
            await module.InvokeVoidAsync("removeContainer", cancellationToken, databaseName, containerName);
            return 0;
        });
    }

    public Task<BrowserStoredBlob?> GetBlobAsync(string databaseName, string blobKey, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(module => module.InvokeAsync<BrowserStoredBlob?>("getBlob", cancellationToken, databaseName, blobKey));
    }

    public Task<BrowserStoredBlob[]> ListBlobsAsync(string databaseName, string containerName, string prefix, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(module => module.InvokeAsync<BrowserStoredBlob[]>("listBlobs", cancellationToken, databaseName, containerName, prefix));
    }

    public Task PutBlobAsync(string databaseName, BrowserStoredBlob blob, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(async module =>
        {
            await module.InvokeVoidAsync("putBlob", cancellationToken, databaseName, blob);
            return 0;
        });
    }

    public Task<bool> DeleteBlobAsync(string databaseName, string blobKey, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(module => module.InvokeAsync<bool>("deleteBlob", cancellationToken, databaseName, blobKey));
    }

    public Task<bool> DeletePayloadFileAsync(string databaseName, string payloadKey, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(module => module.InvokeAsync<bool>("deletePayloadFile", cancellationToken, databaseName, payloadKey));
    }

    public Task<int> DeleteByPrefixAsync(string databaseName, string containerName, string prefix, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(module => module.InvokeAsync<int>("deleteByPrefix", cancellationToken, databaseName, containerName, prefix));
    }

    public Task<bool> BeginPayloadWriteAsync(string databaseName, string blobKey, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(module => module.InvokeAsync<bool>("beginPayloadWrite", cancellationToken, databaseName, blobKey));
    }

    public Task AppendPayloadChunksAsync(string databaseName, string blobKey, BrowserChunkWriteRequest[] chunks,
        CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(async module =>
        {
            await module.InvokeVoidAsync("appendPayloadChunks", cancellationToken, databaseName, blobKey, chunks);
            return 0;
        });
    }

    public Task CompletePayloadWriteAsync(string databaseName, string blobKey, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(async module =>
        {
            await module.InvokeVoidAsync("completePayloadWrite", cancellationToken, databaseName, blobKey);
            return 0;
        });
    }

    public Task AbortPayloadWriteAsync(string databaseName, string blobKey, CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(async module =>
        {
            await module.InvokeVoidAsync("abortPayloadWrite", cancellationToken, databaseName, blobKey);
            return 0;
        });
    }

    public Task<byte[]?> ReadPayloadRangeAsync(string databaseName, string blobKey, long offset, int count,
        CancellationToken cancellationToken = default)
    {
        return UseModuleAsync(module => module.InvokeAsync<byte[]?>("readPayloadRange",
            cancellationToken,
            databaseName,
            blobKey,
            offset,
            count));
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask is null)
            return;

        try
        {
            var module = await _moduleTask.ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task<T> UseModuleAsync<T>(Func<IJSObjectReference, ValueTask<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var module = await GetModuleAsync().ConfigureAwait(false);
        return await action(module).ConfigureAwait(false);
    }

    private Task<IJSObjectReference> GetModuleAsync()
    {
        return _moduleTask ??= jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
    }
}
