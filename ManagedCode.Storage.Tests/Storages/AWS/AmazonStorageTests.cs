using System;
using Amazon;
using Amazon.S3;
using FluentAssertions;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.LocalStack;
using Xunit;

namespace ManagedCode.Storage.Tests.AWS;


public class AmazonStorageTests : StorageBaseTests<LocalStackContainer>
{
    protected override LocalStackContainer Build()
    {
        return new LocalStackBuilder().Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        var config = new AmazonS3Config();
        config.ServiceURL = Container.GetConnectionString();
        
        services.AddAWSStorageAsDefault(opt =>
        {
            opt.PublicKey = "localkey";
            opt.SecretKey = "localsecret";
            opt.Bucket = "managed-code-bucket";
            opt.OriginalOptions = config;
        });

        services.AddAWSStorage(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret",
            Bucket = "managed-code-bucket",
            OriginalOptions = config
        });
        return services.BuildServiceProvider();
    }
    
    [Fact]
    public void BadConfigurationForStorage_WithoutPublicKey_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddAWSStorage(opt =>
        {
            opt.SecretKey = "localsecret";
            opt.Bucket = "managed-code-bucket";
        });

        action.Should().Throw<BadConfigurationException>();
    }

    [Fact]
    public void BadConfigurationForStorage_WithoutSecretKey_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddAWSStorageAsDefault(opt =>
        {
            opt.PublicKey = "localkey";
            opt.Bucket = "managed-code-bucket";
        });

        action.Should().Throw<BadConfigurationException>();
    }

    [Fact]
    public void BadConfigurationForStorage_WithoutBucket_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddAWSStorageAsDefault(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret"
        });

        action.Should().Throw<BadConfigurationException>();
    }

    [Fact]
    public void StorageAsDefaultTest()
    {
        var storage = ServiceProvider.GetService<IAWSStorage>();
        var defaultStorage = ServiceProvider.GetService<IStorage>();
        storage?.GetType().FullName.Should().Be(defaultStorage?.GetType().FullName);
    }
    
}

