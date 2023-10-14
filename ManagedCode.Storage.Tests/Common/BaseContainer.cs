using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Azure;
using Azure.Storage.Blobs;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Testcontainers.GCS;
using Testcontainers.LocalStack;
using Xunit;

namespace ManagedCode.Storage.Tests;

public abstract class BaseContainer<T> : IAsyncLifetime where T : DockerContainer
{
    protected T Container { get; private set; }
    protected abstract T Build();
    protected abstract ServiceProvider ConfigureServices();
    
    protected IStorage Storage { get; private set; }
    protected ServiceProvider ServiceProvider { get; private set; }
    
    
    public async Task InitializeAsync()
    {
        Container = Build();
        await Container.StartAsync();
        ServiceProvider = ConfigureServices();
        Storage = ServiceProvider.GetService<IStorage>()!;
    }

    public Task DisposeAsync()
    {
        return Container.DisposeAsync().AsTask();
    }
    
    protected async Task<FileInfo> UploadTestFileAsync(string? directory = null)
    {
        var file = await GetTestFileAsync();

        UploadOptions options = new() { FileName = file.Name, Directory = directory };
        var result = await Storage.UploadAsync(file.OpenRead(), options);
        result.IsSuccess.Should().BeTrue();

        return file;
    }

    protected async Task<List<FileInfo>> UploadTestFileListAsync(string? directory = null, int? count = 10)
    {
        List<FileInfo> fileList = new();

        for (var i = 0; i < count; i++)
        {
            var file = await UploadTestFileAsync(directory);
            fileList.Add(file);
        }

        return fileList;
    }

    protected async Task<FileInfo> GetTestFileAsync()
    {
        var fileName = Path.GetTempFileName();
        var fs = File.OpenWrite(fileName);
        var sw = new StreamWriter(fs);

        for (var i = 0; i < 1000; i++)
        {
            await sw.WriteLineAsync(Guid.NewGuid().ToString());
        }

        await sw.DisposeAsync();
        await fs.DisposeAsync();

        return new FileInfo(fileName);
    }
}
