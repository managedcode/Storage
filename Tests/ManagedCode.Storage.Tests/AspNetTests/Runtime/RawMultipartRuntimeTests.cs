using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Runtime;

public sealed class RawMultipartRuntimeTests(StorageHttpRuntimeHost host) : IClassFixture<StorageHttpRuntimeHost>
{
    [Fact]
    public async Task RawMultipart_SkipsFieldsAndPersistsTheActualFile()
    {
        var bytes = Encoding.UTF8.GetBytes("raw multipart Привіт");
        var name = Guid.NewGuid().ToString("N") + ".txt";
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("description"), "note");
        content.Add(new ByteArrayContent(bytes), "file", name);
        var token = TestContext.Current.CancellationToken;
        using var response = await host.Client.PostAsync("runtime/storage/upload/raw-multipart", content, token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(token));
        var metadata = await response.Content.ReadFromJsonAsync<BlobMetadata>(cancellationToken: token);
        metadata!.Name.ShouldBe(name);
        (await host.Client.GetByteArrayAsync("runtime/storage/stream/" + name, token)).ShouldBe(bytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task InvalidMultipart_ReturnsARequestError(int invalid)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("field without a file"), "note");
        if (invalid == 0) content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        if (invalid == 1) content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data");
        if (invalid == 2)
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data");
            content.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("boundary", new string('b', 71)));
        }
        using var response = await host.Client.PostAsync("runtime/storage/upload/raw-multipart", content, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
