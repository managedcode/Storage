using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Shouldly;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.Tests.Common;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Abstracts;

public abstract class StorageClientTests<T> : BaseContainer<T> where T : IContainer
{
    private readonly HttpClient _httpClient;

    private readonly StorageClient _storageClient;

    public StorageClientTests()
    {
        _httpClient = new HttpClient(new FakeHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsoluteUri.Contains("loader.com", StringComparison.Ordinal) == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Test content")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        _storageClient = new StorageClient(_httpClient);
    }

    [Fact]
    public async Task DownloadFile_Successful()
    {
        var fileName = "testFile.txt";
        var apiUrl = "https://loader.com";

        var result = await _storageClient.DownloadFile(fileName, apiUrl);

        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task DownloadFile_HttpRequestException()
    {
        var fileName = "testFile.txt";
        var apiUrl = "https://invalid-url.com";

        var result = await _storageClient.DownloadFile(fileName, apiUrl);

        result.IsSuccess
            .ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public async Task DownloadFile_OtherException()
    {
        var fileName = "testFile.txt";
        var apiUrl = "https://loader.com";

        var result = await _storageClient.DownloadFile(fileName, apiUrl + "/invalid-endpoint");

        result.IsSuccess
            .ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseProvider;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseProvider)
        {
            _responseProvider = responseProvider;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseProvider(request));
        }
    }
}
