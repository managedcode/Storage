using System;
using FluentAssertions;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;


public class AzureConfigTests 
{
 
    
    [Fact]
    public void BadConfigurationForStorage_WithoutContainer_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddAzureStorage(opt =>
        {
            opt.ConnectionString = "test";
        });

        action.Should().Throw<BadConfigurationException>();
    }

    [Fact]
    public void BadConfigurationForStorage_WithoutConnectionString_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () =>
            services.AddAzureStorageAsDefault(options =>
            {
                options.Container = "managed-code-bucket";
                options.ConnectionString = null;
            });

        action.Should().Throw<BadConfigurationException>();
    }

    [Fact]
    public void StorageAsDefaultTest()
    {
        var connectionString = "UseDevelopmentStorage=true";
        var storage = AzureConfigurator.ConfigureServices(connectionString).GetService<IAzureStorage>();
        var defaultStorage = AzureConfigurator.ConfigureServices(connectionString).GetService<IStorage>();
        storage?.GetType().FullName.Should().Be(defaultStorage?.GetType().FullName);
    }
}