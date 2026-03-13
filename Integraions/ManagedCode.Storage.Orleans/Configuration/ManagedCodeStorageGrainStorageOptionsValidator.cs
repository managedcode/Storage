using System;
using ManagedCode.Storage.Core;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Configuration;

/// <summary>
/// Validates <see cref="ManagedCodeStorageGrainStorageOptions"/>.
/// </summary>
public sealed class ManagedCodeStorageGrainStorageOptionsValidator(
    ManagedCodeStorageGrainStorageOptions options,
    string name) : IConfigurationValidator
{
    /// <inheritdoc />
    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrleansConfigurationException("ManagedCode grain storage provider name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.StateDirectory) && options.PathBuilder is null)
        {
            throw new OrleansConfigurationException(
                $"Configuration for {nameof(ManagedCodeGrainStorage)} '{name}' is invalid. " +
                $"{nameof(options.StateDirectory)} cannot be empty when no {nameof(options.PathBuilder)} is supplied.");
        }

        if (options.StorageServiceType is not null && !typeof(IStorage).IsAssignableFrom(options.StorageServiceType))
        {
            throw new OrleansConfigurationException(
                $"Configuration for {nameof(ManagedCodeGrainStorage)} '{name}' is invalid. " +
                $"{nameof(options.StorageServiceType)} must implement {typeof(IStorage).FullName}.");
        }

        if (options.StorageKey is not null && string.IsNullOrWhiteSpace(options.StorageKey))
        {
            throw new OrleansConfigurationException(
                $"Configuration for {nameof(ManagedCodeGrainStorage)} '{name}' is invalid. " +
                $"{nameof(options.StorageKey)} cannot be empty.");
        }
    }
}
