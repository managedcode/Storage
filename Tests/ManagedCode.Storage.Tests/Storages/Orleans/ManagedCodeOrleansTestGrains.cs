using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Storage.Tests.Storages.Orleans;

public static class ManagedCodeOrleansProviderNames
{
    public const string StateName = "state";
    public const string FileSystem = "filesystem";
    public const string Azure = "azure";
    public const string Aws = "aws";
    public const string Gcp = "gcp";
    public const string Sftp = "sftp";
}

internal static class ManagedCodeOrleansTestPathHelper
{
    public const string RootDirectory = "orleans-integration-tests";

    public static string BuildStatePath(string providerName, string stateName, string grainKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(grainKey);

        return $"{RootDirectory}/{providerName}/{stateName}/{Uri.EscapeDataString(grainKey)}.state";
    }
}

public interface IManagedCodeStorageBackedGrain : IGrainWithStringKey
{
    Task SetValueAsync(string value);
    Task<string> GetValueAsync();
    Task ReloadAsync();
    Task ClearAsync();
    Task<string> GetStoragePathAsync();
}

public interface IFileSystemStateGrain : IManagedCodeStorageBackedGrain;

public interface IAzureStateGrain : IManagedCodeStorageBackedGrain;

public interface IAwsStateGrain : IManagedCodeStorageBackedGrain;

public interface IGcpStateGrain : IManagedCodeStorageBackedGrain;

public interface ISftpStateGrain : IManagedCodeStorageBackedGrain;

[GenerateSerializer]
public sealed class ManagedCodeStorageBackedState
{
    [Id(0)]
    public string Value { get; set; } = string.Empty;

    [Id(1)]
    public int WriteCount { get; set; }
}

public abstract class ManagedCodeStorageBackedGrainBase : Grain
{
    protected abstract string ProviderName { get; }

    protected abstract IPersistentState<ManagedCodeStorageBackedState> PersistentState { get; }

    public async Task SetValueAsync(string value)
    {
        PersistentState.State.Value = value;
        PersistentState.State.WriteCount++;
        await PersistentState.WriteStateAsync();
    }

    public Task<string> GetValueAsync()
    {
        return Task.FromResult(PersistentState.State.Value);
    }

    public async Task ReloadAsync()
    {
        await PersistentState.ReadStateAsync();
    }

    public async Task ClearAsync()
    {
        await PersistentState.ClearStateAsync();
    }

    public Task<string> GetStoragePathAsync()
    {
        return Task.FromResult(ManagedCodeOrleansTestPathHelper.BuildStatePath(
            ProviderName,
            ManagedCodeOrleansProviderNames.StateName,
            this.GetPrimaryKeyString()));
    }
}

public sealed class FileSystemStateGrain : ManagedCodeStorageBackedGrainBase, IFileSystemStateGrain
{
    private readonly IPersistentState<ManagedCodeStorageBackedState> _persistentState;

    public FileSystemStateGrain(
        [PersistentState(ManagedCodeOrleansProviderNames.StateName, ManagedCodeOrleansProviderNames.FileSystem)]
        IPersistentState<ManagedCodeStorageBackedState> persistentState)
    {
        _persistentState = persistentState;
    }

    protected override string ProviderName => ManagedCodeOrleansProviderNames.FileSystem;

    protected override IPersistentState<ManagedCodeStorageBackedState> PersistentState => _persistentState;
}

public sealed class AzureStateGrain : ManagedCodeStorageBackedGrainBase, IAzureStateGrain
{
    private readonly IPersistentState<ManagedCodeStorageBackedState> _persistentState;

    public AzureStateGrain(
        [PersistentState(ManagedCodeOrleansProviderNames.StateName, ManagedCodeOrleansProviderNames.Azure)]
        IPersistentState<ManagedCodeStorageBackedState> persistentState)
    {
        _persistentState = persistentState;
    }

    protected override string ProviderName => ManagedCodeOrleansProviderNames.Azure;

    protected override IPersistentState<ManagedCodeStorageBackedState> PersistentState => _persistentState;
}

public sealed class AwsStateGrain : ManagedCodeStorageBackedGrainBase, IAwsStateGrain
{
    private readonly IPersistentState<ManagedCodeStorageBackedState> _persistentState;

    public AwsStateGrain(
        [PersistentState(ManagedCodeOrleansProviderNames.StateName, ManagedCodeOrleansProviderNames.Aws)]
        IPersistentState<ManagedCodeStorageBackedState> persistentState)
    {
        _persistentState = persistentState;
    }

    protected override string ProviderName => ManagedCodeOrleansProviderNames.Aws;

    protected override IPersistentState<ManagedCodeStorageBackedState> PersistentState => _persistentState;
}

public sealed class GcpStateGrain : ManagedCodeStorageBackedGrainBase, IGcpStateGrain
{
    private readonly IPersistentState<ManagedCodeStorageBackedState> _persistentState;

    public GcpStateGrain(
        [PersistentState(ManagedCodeOrleansProviderNames.StateName, ManagedCodeOrleansProviderNames.Gcp)]
        IPersistentState<ManagedCodeStorageBackedState> persistentState)
    {
        _persistentState = persistentState;
    }

    protected override string ProviderName => ManagedCodeOrleansProviderNames.Gcp;

    protected override IPersistentState<ManagedCodeStorageBackedState> PersistentState => _persistentState;
}

public sealed class SftpStateGrain : ManagedCodeStorageBackedGrainBase, ISftpStateGrain
{
    private readonly IPersistentState<ManagedCodeStorageBackedState> _persistentState;

    public SftpStateGrain(
        [PersistentState(ManagedCodeOrleansProviderNames.StateName, ManagedCodeOrleansProviderNames.Sftp)]
        IPersistentState<ManagedCodeStorageBackedState> persistentState)
    {
        _persistentState = persistentState;
    }

    protected override string ProviderName => ManagedCodeOrleansProviderNames.Sftp;

    protected override IPersistentState<ManagedCodeStorageBackedState> PersistentState => _persistentState;
}
