using System;
using ManagedCode.Storage.Core;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Configuration;

/// <summary>
/// Options for the ManagedCode-backed Orleans grain storage provider.
/// </summary>
public sealed class ManagedCodeStorageGrainStorageOptions : IStorageProviderSerializerOptions
{
    public const string DefaultStateDirectory = "orleans/grain-state";

    /// <summary>
    /// Gets or sets the root directory/prefix used for persisted grain state files.
    /// </summary>
    public string StateDirectory { get; set; } = DefaultStateDirectory;

    /// <summary>
    /// Gets or sets a value indicating whether state files should be deleted on clear.
    /// </summary>
    public bool DeleteStateOnClear { get; set; } = true;

    /// <summary>
    /// Gets or sets the keyed DI identifier for resolving the backing storage.
    /// </summary>
    public string? StorageKey { get; set; }

    /// <summary>
    /// Gets or sets the DI service type used to resolve the backing storage.
    /// </summary>
    public Type? StorageServiceType { get; set; }

    /// <summary>
    /// Gets or sets a custom storage resolver.
    /// </summary>
    public Func<IServiceProvider, IStorage>? StorageFactory { get; set; }

    /// <summary>
    /// Gets or sets a custom path builder for persisted grain state files.
    /// </summary>
    public Func<ManagedCodeStoragePathContext, string>? PathBuilder { get; set; }

    /// <inheritdoc />
    public IGrainStorageSerializer GrainStorageSerializer { get; set; } = default!;
}

/// <summary>
/// Context passed to <see cref="ManagedCodeStorageGrainStorageOptions.PathBuilder"/>.
/// </summary>
/// <param name="ProviderName">The Orleans storage provider name.</param>
/// <param name="StateName">The Orleans state identifier.</param>
/// <param name="GrainId">The grain id.</param>
public readonly record struct ManagedCodeStoragePathContext(string ProviderName, string StateName, GrainId GrainId);
