using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public class AzureBlobTests : BlobTests<AzuriteContainer>
{
    [Fact]
    public async Task GetBlobMetadataAsync_WhenBlobIsMissing_ShouldNotLogException()
    {
        var logger = new CapturingLogger<AzureStorage>();
        var storage = new AzureStorage(
            new AzureStorageOptions
            {
                ConnectionString = Container.GetConnectionString(),
                Container = $"metadata-{Guid.NewGuid():N}",
                CreateContainerIfNotExists = true
            },
            logger);

        var result = await storage.GetBlobMetadataAsync($"missing-{Guid.NewGuid():N}.txt");

        result.IsSuccess.ShouldBeFalse();
        logger.Exceptions.ShouldBeEmpty();
    }

    protected override AzuriteContainer Build()
    {
        return new AzuriteBuilder(ContainerImages.Azurite)
            .WithCommand("--skipApiVersionCheck")
            .Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return AzureConfigurator.ConfigureServices(Container.GetConnectionString());
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<Exception> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception is not null)
            {
                Exceptions.Enqueue(exception);
            }
        }
    }
}
