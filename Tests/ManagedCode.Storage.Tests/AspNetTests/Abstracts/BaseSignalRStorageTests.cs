using System;
using ManagedCode.Storage.Client.SignalR;
using ManagedCode.Storage.Client.SignalR.Models;
using ManagedCode.Storage.Tests.Common;

namespace ManagedCode.Storage.Tests.AspNetTests.Abstracts;

public abstract class BaseSignalRStorageTests : BaseControllerTests
{
    protected BaseSignalRStorageTests(StorageTestApplication testApplication, string apiEndpoint)
        : base(testApplication, apiEndpoint)
    {
    }

    protected StorageSignalRClient CreateClient(Action<StorageSignalRClientOptions>? configure = null)
    {
        return TestApplication.CreateSignalRClient(configure);
    }

    protected static StorageUploadStreamDescriptor CreateDescriptor(string fileName, string contentType, long? length)
    {
        return new StorageUploadStreamDescriptor
        {
            FileName = fileName,
            ContentType = contentType,
            FileSize = length
        };
    }
}
