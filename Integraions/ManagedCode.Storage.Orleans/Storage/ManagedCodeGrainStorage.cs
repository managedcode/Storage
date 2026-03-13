using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization.Serializers;

namespace Orleans.Storage;

using StorageAbstraction = ManagedCode.Storage.Core.IStorage;

/// <summary>
/// Orleans grain storage backed by a ManagedCode <see cref="StorageAbstraction"/>.
/// </summary>
public sealed class ManagedCodeGrainStorage(
    string name,
    ManagedCodeStorageGrainStorageOptions options,
    StorageAbstraction storage,
    IActivatorProvider activatorProvider,
    ILogger<ManagedCodeGrainStorage> logger) : IGrainStorage
{
    private const string StorageFileExtension = ".state";

    public static ManagedCodeGrainStorage Create(
        string name,
        ManagedCodeStorageGrainStorageOptions options,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var storage = ResolveStorage(serviceProvider, options);
        var activatorProvider = serviceProvider.GetRequiredService<IActivatorProvider>();
        var logger = serviceProvider.GetRequiredService<ILogger<ManagedCodeGrainStorage>>();
        return new ManagedCodeGrainStorage(name, options, storage, activatorProvider, logger);
    }

    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        ArgumentNullException.ThrowIfNull(grainState);

        var storagePath = GetStoragePath(stateName, grainId);

        try
        {
            var record = await TryReadRecordAsync<T>(storagePath, CancellationToken.None).ConfigureAwait(false);
            if (record is null)
            {
                ResetGrainState(grainState);
                return;
            }

            grainState.State = record.RecordExists
                ? record.State ?? CreateInstance<T>()
                : CreateInstance<T>();
            grainState.ETag = record.ETag;
            grainState.RecordExists = record.RecordExists;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read Orleans grain state '{StateName}' for grain '{GrainId}' from '{Path}'.",
                stateName, grainId, storagePath);
            throw;
        }
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        ArgumentNullException.ThrowIfNull(grainState);

        var storagePath = GetStoragePath(stateName, grainId);

        try
        {
            var current = await TryReadRecordAsync<T>(storagePath, CancellationToken.None).ConfigureAwait(false);
            EnsureExpectedEtag(current?.ETag, grainState.ETag, storagePath);

            var nextEtag = CreateETag();
            var record = new ManagedCodeStoredGrainState<T>
            {
                State = grainState.State,
                ETag = nextEtag,
                RecordExists = true
            };

            await WriteRecordAsync(storagePath, record, CancellationToken.None).ConfigureAwait(false);
            grainState.ETag = nextEtag;
            grainState.RecordExists = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write Orleans grain state '{StateName}' for grain '{GrainId}' to '{Path}'.",
                stateName, grainId, storagePath);
            throw;
        }
    }

    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        ArgumentNullException.ThrowIfNull(grainState);

        var storagePath = GetStoragePath(stateName, grainId);

        try
        {
            var current = await TryReadRecordAsync<T>(storagePath, CancellationToken.None).ConfigureAwait(false);
            EnsureExpectedEtag(current?.ETag, grainState.ETag, storagePath);

            if (options.DeleteStateOnClear)
            {
                var deleteResult = await storage.DeleteAsync(storagePath, CancellationToken.None).ConfigureAwait(false);
                ThrowIfFailed(deleteResult, $"Failed to delete Orleans grain state '{storagePath}'.");
                grainState.ETag = null!;
            }
            else
            {
                var nextEtag = CreateETag();
                var record = new ManagedCodeStoredGrainState<T>
                {
                    State = default,
                    ETag = nextEtag,
                    RecordExists = false
                };

                await WriteRecordAsync(storagePath, record, CancellationToken.None).ConfigureAwait(false);
                grainState.ETag = nextEtag;
            }

            grainState.RecordExists = false;
            grainState.State = CreateInstance<T>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clear Orleans grain state '{StateName}' for grain '{GrainId}' at '{Path}'.",
                stateName, grainId, storagePath);
            throw;
        }
    }

    private async Task<ManagedCodeStoredGrainState<T>?> TryReadRecordAsync<T>(string storagePath, CancellationToken cancellationToken)
    {
        var existsResult = await storage.ExistsAsync(storagePath, cancellationToken).ConfigureAwait(false);
        ThrowIfFailed(existsResult, $"Failed to probe Orleans grain state '{storagePath}'.");

        if (!existsResult.Value)
        {
            return null;
        }

        var downloadResult = await storage.DownloadAsync(storagePath, cancellationToken).ConfigureAwait(false);
        ThrowIfFailed(downloadResult, $"Failed to download Orleans grain state '{storagePath}'.");

        using var localFile = downloadResult.Value ?? throw new InvalidOperationException(
            $"Storage '{storagePath}' was downloaded without a file payload.");
        await using var stream = localFile.OpenReadStream(disposeOwner: false);
        var payload = await BinaryData.FromStreamAsync(stream, cancellationToken).ConfigureAwait(false);

        return options.GrainStorageSerializer.Deserialize<ManagedCodeStoredGrainState<T>>(payload);
    }

    private async Task WriteRecordAsync<T>(
        string storagePath,
        ManagedCodeStoredGrainState<T> record,
        CancellationToken cancellationToken)
    {
        var payload = options.GrainStorageSerializer.Serialize(record);
        using var content = payload.ToStream();
        var (directory, fileName) = SplitStoragePath(storagePath);
        var uploadOptions = new UploadOptions
        {
            Directory = directory,
            FileName = fileName,
            MimeType = "application/octet-stream"
        };

        var uploadResult = await storage.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);
        ThrowIfFailed(uploadResult, $"Failed to persist Orleans grain state '{storagePath}'.");
    }

    private static void ThrowIfFailed(Result result, string message)
    {
        if (result.IsSuccess)
        {
            return;
        }

        throw new InvalidOperationException(
            result.Problem?.Detail ?? result.Problem?.Title ?? message);
    }

    private static void ThrowIfFailed<T>(Result<T> result, string message)
    {
        if (result.IsSuccess)
        {
            return;
        }

        throw new InvalidOperationException(
            result.Problem?.Detail ?? result.Problem?.Title ?? message);
    }

    private static StorageAbstraction ResolveStorage(IServiceProvider serviceProvider, ManagedCodeStorageGrainStorageOptions options)
    {
        if (options.StorageFactory is not null)
        {
            return options.StorageFactory(serviceProvider);
        }

        if (options.StorageServiceType is not null && options.StorageKey is not null)
        {
            return (StorageAbstraction)serviceProvider.GetRequiredKeyedService(options.StorageServiceType, options.StorageKey);
        }

        if (options.StorageServiceType is not null)
        {
            return (StorageAbstraction)serviceProvider.GetRequiredService(options.StorageServiceType);
        }

        if (options.StorageKey is not null)
        {
            return serviceProvider.GetRequiredKeyedService<StorageAbstraction>(options.StorageKey);
        }

        return serviceProvider.GetRequiredService<StorageAbstraction>();
    }

    private static void EnsureExpectedEtag(string? storedEtag, string? expectedEtag, string storagePath)
    {
        var normalizedStored = NormalizeEtag(storedEtag);
        var normalizedExpected = NormalizeEtag(expectedEtag);

        if (normalizedExpected is null)
        {
            if (normalizedStored is null)
            {
                return;
            }

            throw new InconsistentStateException(
                $"ManagedCode storage condition not satisfied for '{storagePath}'. A record already exists.",
                normalizedStored,
                string.Empty);
        }

        if (!string.Equals(normalizedStored, normalizedExpected, StringComparison.Ordinal))
        {
            throw new InconsistentStateException(
                $"ManagedCode storage condition not satisfied for '{storagePath}'.",
                normalizedStored ?? string.Empty,
                normalizedExpected);
        }
    }

    private string GetStoragePath(string stateName, GrainId grainId)
    {
        var context = new ManagedCodeStoragePathContext(name, stateName, grainId);
        var customPath = options.PathBuilder?.Invoke(context);
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            return NormalizePath(customPath);
        }

        var providerSegment = EscapeSegment(name);
        var stateSegment = EscapeSegment(stateName);
        var grainSegment = EscapeSegment(grainId.ToString());
        return NormalizePath($"{options.StateDirectory}/{providerSegment}/{stateSegment}/{grainSegment}{StorageFileExtension}");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private static string EscapeSegment(string value)
    {
        return Uri.EscapeDataString(value).Replace("%2F", "%252F", StringComparison.OrdinalIgnoreCase);
    }

    private static (string? Directory, string FileName) SplitStoragePath(string storagePath)
    {
        var normalized = NormalizePath(storagePath);
        var separatorIndex = normalized.LastIndexOf('/');
        if (separatorIndex < 0)
        {
            return (null, normalized);
        }

        var directory = normalized[..separatorIndex];
        var fileName = normalized[(separatorIndex + 1)..];
        return (string.IsNullOrWhiteSpace(directory) ? null : directory, fileName);
    }

    private static string CreateETag() => Guid.NewGuid().ToString("N");

    private static string? NormalizeEtag(string? etag)
    {
        return string.IsNullOrWhiteSpace(etag) ? null : etag;
    }

    private void ResetGrainState<T>(IGrainState<T> grainState)
    {
        grainState.State = CreateInstance<T>();
        grainState.ETag = null!;
        grainState.RecordExists = false;
    }

    private T CreateInstance<T>() => activatorProvider.GetActivator<T>().Create();
}
