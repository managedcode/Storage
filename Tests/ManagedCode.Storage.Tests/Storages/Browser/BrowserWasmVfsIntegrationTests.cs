using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ManagedCode.Storage.Tests.Storages.Browser;

[Collection(nameof(BrowserWasmHostCollection))]
public sealed class BrowserWasmVfsIntegrationTests(BrowserWasmHostFixture fixture)
{
    [Fact]
    public async Task BrowserStorage_WasmHost_VfsTextFlow_ShouldPersistMoveListAndDelete()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();
        var directory = $"wasm-browser-vfs-{Guid.NewGuid():N}";
        var fileName = $"text-{Guid.NewGuid():N}.txt";
        var movedFileName = $"moved-{Guid.NewGuid():N}.txt";
        var content = $"content-{Guid.NewGuid():N}";

        await BrowserStoragePage.OpenPlaygroundAsync(page);
        await BrowserStoragePage.FillVfsInputsAsync(page, directory, fileName, movedFileName, content);
        await page.ClickAsync("#vfs-save-text-button");
        await Expect(page.Locator("#vfs-status-output")).ToContainTextAsync($"vfs-saved:/{directory}/{fileName}");

        await page.ClickAsync("#vfs-list-button");
        await Expect(page.Locator("#vfs-entry-list")).ToContainTextAsync(fileName);

        await page.ClickAsync("#vfs-move-button");
        await Expect(page.Locator("#vfs-status-output")).ToContainTextAsync($"vfs-moved:/{directory}/{fileName}->/{directory}/{movedFileName}");

        await page.ClickAsync("#vfs-load-text-button");
        await Expect(page.Locator("#vfs-loaded-output")).ToHaveTextAsync(content);

        await page.ClickAsync("#vfs-list-button");
        await Expect(page.Locator("#vfs-status-output")).ToContainTextAsync("vfs-listed:");
        var listedEntries = await BrowserStoragePage.ReadTextAsync(page, "#vfs-entry-list");
        listedEntries.ShouldContain(movedFileName);
        listedEntries.ShouldNotContain(fileName);

        await page.ClickAsync("#vfs-delete-button");
        await Expect(page.Locator("#vfs-status-output")).ToContainTextAsync("vfs-deleted:True");

        await page.ClickAsync("#vfs-list-button");
        await Expect(page.Locator("#vfs-status-output")).ToContainTextAsync("vfs-listed:");
        listedEntries = await BrowserStoragePage.ReadTextAsync(page, "#vfs-entry-list");
        listedEntries.ShouldNotContain(movedFileName);

        var indexedDbCount = await BrowserStoragePage.ReadIndexedDbCountAsync(page, fixture, directory);
        indexedDbCount.ShouldBe(0);
    }

    [Fact]
    public async Task BrowserStorage_WasmHost_VfsLargeFlow_ShouldPersistAcrossPages()
    {
        const int payloadSizeMiB = 16;

        await using var context = await fixture.CreateContextAsync();
        var firstPage = await context.NewPageAsync();
        var directory = $"wasm-browser-vfs-large-{Guid.NewGuid():N}";
        var fileName = $"large-{Guid.NewGuid():N}.bin";
        var movedFileName = $"unused-{Guid.NewGuid():N}.bin";

        await BrowserStoragePage.OpenPlaygroundAsync(firstPage);
        await BrowserStoragePage.FillVfsInputsAsync(firstPage, directory, fileName, movedFileName, "ignored");
        await BrowserStoragePage.FillVfsSizeAsync(firstPage, payloadSizeMiB);
        await firstPage.ClickAsync("#vfs-save-large-button");
        await Expect(firstPage.Locator("#vfs-status-output")).ToContainTextAsync("vfs-large-saved:", new() { Timeout = 90000 });

        var expected = await BrowserStoragePage.ReadTextAsync(firstPage, "#vfs-large-output");

        var secondPage = await context.NewPageAsync();
        await BrowserStoragePage.OpenPlaygroundAsync(secondPage);
        await BrowserStoragePage.FillVfsInputsAsync(secondPage, directory, fileName, movedFileName, "ignored");
        await BrowserStoragePage.FillVfsSizeAsync(secondPage, payloadSizeMiB);
        await secondPage.ClickAsync("#vfs-load-large-button");
        await Expect(secondPage.Locator("#vfs-status-output")).ToContainTextAsync("vfs-large-loaded:", new() { Timeout = 90000 });

        var actual = await BrowserStoragePage.ReadTextAsync(secondPage, "#vfs-large-output");
        actual.ShouldBe(expected.Replace("expected:", "actual:", StringComparison.Ordinal));

        var indexedDbCount = await BrowserStoragePage.ReadIndexedDbCountAsync(secondPage, fixture, directory);
        indexedDbCount.ShouldBe(1);
    }

    [Fact]
    public async Task BrowserStorage_WasmHost_VfsConcurrentTabs_ShouldPersistAllFiles()
    {
        await using var context = await fixture.CreateContextAsync();
        var firstPage = await context.NewPageAsync();
        var secondPage = await context.NewPageAsync();
        var verificationPage = await context.NewPageAsync();
        var directory = $"wasm-browser-vfs-concurrent-{Guid.NewGuid():N}";
        var firstFileName = $"first-{Guid.NewGuid():N}.txt";
        var secondFileName = $"second-{Guid.NewGuid():N}.txt";
        var firstMovedFileName = $"first-moved-{Guid.NewGuid():N}.txt";
        var secondMovedFileName = $"second-moved-{Guid.NewGuid():N}.txt";
        var firstContent = $"content-{Guid.NewGuid():N}";
        var secondContent = $"content-{Guid.NewGuid():N}";

        await BrowserStoragePage.OpenPlaygroundAsync(firstPage);
        await BrowserStoragePage.OpenPlaygroundAsync(secondPage);
        await BrowserStoragePage.FillVfsInputsAsync(firstPage, directory, firstFileName, firstMovedFileName, firstContent);
        await BrowserStoragePage.FillVfsInputsAsync(secondPage, directory, secondFileName, secondMovedFileName, secondContent);

        await Task.WhenAll(
            firstPage.ClickAsync("#vfs-save-text-button"),
            secondPage.ClickAsync("#vfs-save-text-button"));

        await Expect(firstPage.Locator("#vfs-status-output")).ToContainTextAsync($"vfs-saved:/{directory}/{firstFileName}");
        await Expect(secondPage.Locator("#vfs-status-output")).ToContainTextAsync($"vfs-saved:/{directory}/{secondFileName}");

        await BrowserStoragePage.OpenPlaygroundAsync(verificationPage);
        await verificationPage.FillAsync("#vfs-directory-input", directory);
        await verificationPage.ClickAsync("#vfs-list-button");
        await Expect(verificationPage.Locator("#vfs-entry-list")).ToContainTextAsync(firstFileName);
        await Expect(verificationPage.Locator("#vfs-entry-list")).ToContainTextAsync(secondFileName);

        var indexedDbCount = await BrowserStoragePage.ReadIndexedDbCountAsync(verificationPage, fixture, directory);
        indexedDbCount.ShouldBe(2);
    }
}
