using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using ManagedCode.Storage.Sftp;
using ManagedCode.Storage.Sftp.Extensions;
using ManagedCode.Storage.Sftp.Options;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.AWS;
using ManagedCode.Storage.Tests.Storages.Sftp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using Shouldly;
using Testcontainers.Azurite;
using Testcontainers.FakeGcsServer;
using Testcontainers.LocalStack;
using Testcontainers.Sftp;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Orleans;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ManagedCodeOrleansClusterCollection : ICollectionFixture<ManagedCodeOrleansClusterFixture>
{
    public const string Name = "ManagedCodeOrleansCluster";
}

public sealed class ManagedCodeOrleansClusterFixture : IAsyncLifetime
{
    private AzuriteContainer? _azuriteContainer;
    private LocalStackContainer? _localStackContainer;
    private FakeGcsServerContainer? _gcpContainer;
    private SftpContainer? _sftpContainer;
    private string? _fileSystemRoot;

    public TestCluster Cluster { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _fileSystemRoot = Path.Combine(
            Path.GetTempPath(),
            "ManagedCode.Storage.Orleans.Cluster",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_fileSystemRoot);

        _azuriteContainer = new AzuriteBuilder(ContainerImages.Azurite)
            .WithCommand("--skipApiVersionCheck")
            .Build();

        _localStackContainer = AwsContainerFactory.Create();
        _gcpContainer = new FakeGcsServerBuilder(ContainerImages.FakeGCSServer)
            .Build();
        _sftpContainer = SftpContainerFactory.Create();

        await Task.WhenAll(
            _azuriteContainer.StartAsync(),
            _localStackContainer.StartAsync(),
            _gcpContainer.StartAsync(),
            _sftpContainer.StartAsync());

        ManagedCodeOrleansClusterSettings.Configure(new ManagedCodeOrleansClusterSettingsSnapshot(
            _fileSystemRoot,
            _azuriteContainer.GetConnectionString(),
            _localStackContainer.GetConnectionString(),
            _gcpContainer.GetConnectionString(),
            _sftpContainer.GetHost(),
            _sftpContainer.GetPort(),
            SftpContainerFactory.Username,
            SftpContainerFactory.Password,
            SftpContainerFactory.RemoteDirectory));

        var builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<ManagedCodeOrleansSiloConfigurator>();
        Cluster = builder.Build();
        await Cluster.DeployAsync();

        await EnsureContainersCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        ManagedCodeOrleansClusterSettings.Reset();

        if (Cluster is not null)
        {
            await Cluster.DisposeAsync();
        }

        if (_sftpContainer is not null)
        {
            await _sftpContainer.DisposeAsync().AsTask();
        }

        if (_gcpContainer is not null)
        {
            await _gcpContainer.DisposeAsync().AsTask();
        }

        if (_localStackContainer is not null)
        {
            await _localStackContainer.DisposeAsync().AsTask();
        }

        if (_azuriteContainer is not null)
        {
            await _azuriteContainer.DisposeAsync().AsTask();
        }

        if (!string.IsNullOrWhiteSpace(_fileSystemRoot) && Directory.Exists(_fileSystemRoot))
        {
            Directory.Delete(_fileSystemRoot, recursive: true);
        }
    }

    public IServiceProvider GetSiloServiceProvider()
    {
        return Cluster.GetSiloServiceProvider(Cluster.Primary.SiloAddress);
    }

    public IManagedCodeStorageBackedGrain GetGrain(string providerName, string grainKey)
    {
        return providerName switch
        {
            ManagedCodeOrleansProviderNames.FileSystem => Cluster.GrainFactory.GetGrain<IFileSystemStateGrain>(grainKey),
            ManagedCodeOrleansProviderNames.Azure => Cluster.GrainFactory.GetGrain<IAzureStateGrain>(grainKey),
            ManagedCodeOrleansProviderNames.Aws => Cluster.GrainFactory.GetGrain<IAwsStateGrain>(grainKey),
            ManagedCodeOrleansProviderNames.Gcp => Cluster.GrainFactory.GetGrain<IGcpStateGrain>(grainKey),
            ManagedCodeOrleansProviderNames.Sftp => Cluster.GrainFactory.GetGrain<ISftpStateGrain>(grainKey),
            _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, "Unknown provider name.")
        };
    }

    public IStorage ResolveStorage(string providerName)
    {
        var services = GetSiloServiceProvider();
        return providerName switch
        {
            ManagedCodeOrleansProviderNames.FileSystem => services.GetRequiredService<IFileSystemStorage>(),
            ManagedCodeOrleansProviderNames.Azure => services.GetRequiredService<IAzureStorage>(),
            ManagedCodeOrleansProviderNames.Aws => services.GetRequiredService<IAWSStorage>(),
            ManagedCodeOrleansProviderNames.Gcp => services.GetRequiredService<IGCPStorage>(),
            ManagedCodeOrleansProviderNames.Sftp => services.GetRequiredService<ISftpStorage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, "Unknown provider name.")
        };
    }

    private async Task EnsureContainersCreatedAsync()
    {
        foreach (var providerName in ManagedCodeOrleansClusterTestMatrix.AllProviders)
        {
            var storage = ResolveStorage(providerName);
            var result = await storage.CreateContainerAsync(CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
        }
    }
}

internal static class ManagedCodeOrleansClusterTestMatrix
{
    public static readonly TheoryData<string> AllProviders =
    [
        ManagedCodeOrleansProviderNames.FileSystem,
        ManagedCodeOrleansProviderNames.Azure,
        ManagedCodeOrleansProviderNames.Aws,
        ManagedCodeOrleansProviderNames.Gcp,
        ManagedCodeOrleansProviderNames.Sftp
    ];
}

internal sealed record ManagedCodeOrleansClusterSettingsSnapshot(
    string FileSystemRoot,
    string AzureConnectionString,
    string AwsServiceUrl,
    string GcpBaseUri,
    string SftpHost,
    int SftpPort,
    string SftpUsername,
    string SftpPassword,
    string SftpRemoteDirectory);

internal static class ManagedCodeOrleansClusterSettings
{
    private static ManagedCodeOrleansClusterSettingsSnapshot? _current;

    public static ManagedCodeOrleansClusterSettingsSnapshot Current =>
        _current ?? throw new InvalidOperationException("ManagedCode Orleans cluster settings have not been configured.");

    public static void Configure(ManagedCodeOrleansClusterSettingsSnapshot settings)
    {
        _current = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public static void Reset()
    {
        _current = null;
    }
}

public sealed class ManagedCodeOrleansSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        var settings = ManagedCodeOrleansClusterSettings.Current;

        siloBuilder.ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        siloBuilder.ConfigureServices(services =>
        {
            services.AddStorageFactory();

            services.AddFileSystemStorage(options =>
            {
                options.BaseFolder = settings.FileSystemRoot;
            });

            services.AddAzureStorage(new AzureStorageOptions
            {
                Container = "managed-code-bucket",
                ConnectionString = settings.AzureConnectionString
            });

            var awsOptions = new AmazonS3Config
            {
                ServiceURL = settings.AwsServiceUrl
            };

            services.AddAWSStorage(new AWSStorageOptions
            {
                PublicKey = "localkey",
                SecretKey = "localsecret",
                Bucket = "managed-code-bucket",
                OriginalOptions = awsOptions
            });

            services.AddGCPStorage(new GCPStorageOptions
            {
                BucketOptions = new BucketOptions
                {
                    ProjectId = "api-project-0000000000000",
                    Bucket = "managed-code-bucket"
                },
                StorageClientBuilder = new StorageClientBuilder
                {
                    UnauthenticatedAccess = true,
                    BaseUri = settings.GcpBaseUri
                }
            });

            services.AddSftpStorage(new SftpStorageOptions
            {
                Host = settings.SftpHost,
                Port = settings.SftpPort,
                Username = settings.SftpUsername,
                Password = settings.SftpPassword,
                RemoteDirectory = settings.SftpRemoteDirectory,
                CreateContainerIfNotExists = true,
                CreateDirectoryIfNotExists = true,
                ConnectTimeout = 30000,
                OperationTimeout = 30000
            });
        });

        siloBuilder.AddGrainStorage<IFileSystemStorage>(ManagedCodeOrleansProviderNames.FileSystem, ConfigureGrainStorage);
        siloBuilder.AddGrainStorage<IAzureStorage>(ManagedCodeOrleansProviderNames.Azure, ConfigureGrainStorage);
        siloBuilder.AddGrainStorage<IAWSStorage>(ManagedCodeOrleansProviderNames.Aws, ConfigureGrainStorage);
        siloBuilder.AddGrainStorage<IGCPStorage>(ManagedCodeOrleansProviderNames.Gcp, ConfigureGrainStorage);
        siloBuilder.AddGrainStorage<ISftpStorage>(ManagedCodeOrleansProviderNames.Sftp, ConfigureGrainStorage);
    }

    private static void ConfigureGrainStorage(ManagedCodeStorageGrainStorageOptions options)
    {
        options.StateDirectory = ManagedCodeOrleansTestPathHelper.RootDirectory;
        options.DeleteStateOnClear = true;
        options.PathBuilder = context => ManagedCodeOrleansTestPathHelper.BuildStatePath(
            context.ProviderName,
            context.StateName,
            context.GrainId.Key.ToString());
    }
}
