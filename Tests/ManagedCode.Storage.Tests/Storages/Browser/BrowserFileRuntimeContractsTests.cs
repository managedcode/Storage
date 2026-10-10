using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ManagedCode.Storage.Tests.Storages.Browser;

[Collection(BrowserIntegrationCollection.Server)]
public sealed class BrowserFileRuntimeContractsTests(BrowserServerHostFixture fixture)
{
    [Theory]
    [InlineData(4107)]
    [InlineData(524309)]
    public async Task ActualBrowserFile_StreamsThroughBothStorageAndControllerUploadPaths(int size)
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/browser-file-contracts");
        await Expect(page.Locator("#browser-file")).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#browser-file')._blazorInputFileNextFileId !== undefined");
        var bytes = RandomNumberGenerator.GetBytes(size);
        await page.Locator("#browser-file").SetInputFilesAsync(new FilePayload { Name = "source.storagecontract-" + Guid.NewGuid().ToString("N"), MimeType = "application/octet-stream", Buffer = bytes });
        await Expect(page.Locator("#browser-file-status")).ToHaveTextAsync(Convert.ToHexString(SHA256.HashData(bytes)), new() { Timeout = 60000 });
    }
}
