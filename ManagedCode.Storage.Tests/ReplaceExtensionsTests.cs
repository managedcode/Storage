using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.TestFakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetExtensions;

public class ReplaceExtensionsTests
{
    [Fact]
    public async Task ReplaceAzureStorageAsDefault()
    {
        var options = new AzureStorageOptions
        {
            Container = "test",
            ConnectionString = "ConnectionString"
        };

        var services = new ServiceCollection();

        services.AddAzureStorageAsDefault(options);

        services.ReplaceAzureStorageAsDefault();

        var build = services.BuildServiceProvider();
        build.GetService<IStorage>().GetType().Should().Be(typeof(FakeAzureStorage));
    }

    [Fact]
    public async Task ReplaceAzureStorage()
    {
        var options = new AzureStorageOptions
        {
            Container = "test",
            ConnectionString = "ConnectionString"
        };

        var services = new ServiceCollection();

        services.AddAzureStorage(options);

        services.ReplaceAzureStorage();

        var build = services.BuildServiceProvider();
        build.GetService<IAzureStorage>().GetType().Should().Be(typeof(FakeAzureStorage));
    }
}