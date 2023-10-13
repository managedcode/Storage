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

public class AWSStorageTests : StorageBaseTests
{
    protected override ServiceProvider ConfigureServices()
    {
        
        
        var services = new ServiceCollection();

        // AWS library overwrites property values. you should only create configurations this way. 
        var awsConfig = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.EUWest1,
            ForcePathStyle = true,
            UseHttp = true,
            ServiceURL = "http://localhost:4566" // this is the default port for the aws s3 emulator, must be last in the list
        };

        services.AddAWSStorageAsDefault(opt =>
        {
            opt.PublicKey = "localkey";
            opt.SecretKey = "localsecret";
            opt.Bucket = "managed-code-bucket";
            opt.OriginalOptions = awsConfig;
        });

        services.AddAWSStorage(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret",
            Bucket = "managed-code-bucket",
            OriginalOptions = awsConfig
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