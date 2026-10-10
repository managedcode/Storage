using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Registration;

internal static class RegistrationRoundTrip
{
    public static async Task VerifyAsync(IStorage storage)
    {
        var token = TestContext.Current.CancellationToken;
        (await storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        var name = Guid.NewGuid().ToString("N") + ".bin";
        var bytes = Encoding.UTF8.GetBytes("Registered provider: Привіт, storage!");
        (await storage.UploadAsync(bytes, new UploadOptions { FileName = name }, token)).IsSuccess.ShouldBeTrue();
        var opened = await storage.GetStreamAsync(name, token);
        opened.IsSuccess.ShouldBeTrue();
        await using var stream = opened.Value!;
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, token);
        output.ToArray().ShouldBe(bytes);
        (await storage.DeleteAsync(name, token)).IsSuccess.ShouldBeTrue();
    }
}
