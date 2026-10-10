using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.VirtualFileSystem;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public sealed class FileSystemLegalHoldContractsTests
{
    [Fact]
    public async Task HoldTransitionsAndMissingFiles_ReturnObservableResults()
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var storage = session.Storage;
        var token = TestContext.Current.CancellationToken;
        (await storage.UploadAsync("held content", new UploadOptions { FileName = "held.txt" }, token)).IsSuccess.ShouldBeTrue();
        (await storage.HasLegalHoldAsync("held.txt", token)).Value.ShouldBeFalse();
        (await storage.SetLegalHoldAsync(true, "held.txt", token)).IsSuccess.ShouldBeTrue();
        (await storage.HasLegalHoldAsync(options => options.FileName = "held.txt", token)).Value.ShouldBeTrue();
        (await storage.SetLegalHoldAsync(true, "held.txt", token)).IsSuccess.ShouldBeTrue();
        (await storage.SetLegalHoldAsync(false, options => options.FileName = "held.txt", token)).IsSuccess.ShouldBeTrue();
        (await storage.HasLegalHoldAsync(new LegalHoldOptions { FileName = "held.txt" }, token)).Value.ShouldBeFalse();
        (await storage.SetLegalHoldAsync(true, "missing.txt", token)).IsFailed.ShouldBeTrue();
        (await storage.DeleteAsync("held.txt", token)).IsSuccess.ShouldBeTrue();
    }
}
