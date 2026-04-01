using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ManagedCode.Storage.Tests.Storages.Browser;

[Collection(BrowserIntegrationCollection.Name)]
public sealed class BrowserWasmStorageIntegrationTests(BrowserWasmHostFixture fixture)
{
    [Fact]
    public async Task BrowserStorage_WasmHost_TextFlow_ShouldPersistListAndDelete()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();
        var directory = "wasm-browser-storage-tests";
        var fileName = $"text-{Guid.NewGuid():N}.txt";
        var content = $"content-{Guid.NewGuid():N}";
        var updatedContent = $"updated-{Guid.NewGuid():N}";
        var blobKeyPrefix = $"{fixture.ContainerKey}::{directory}/{fileName}::payload::";

        await BrowserStoragePage.OpenPlaygroundAsync(page);
        await BrowserStoragePage.FillInputsAsync(page, directory, fileName, content);

        await page.ClickAsync("#save-text-button");
        await Expect(page.Locator("#status-output")).ToContainTextAsync($"saved:{directory}/{fileName}");

        await page.ClickAsync("#exists-button");
        await Expect(page.Locator("#exists-output")).ToContainTextAsync("True");

        await page.ClickAsync("#list-button");
        await Expect(page.Locator("#blob-list")).ToContainTextAsync(fileName);

        await page.FillAsync("#content-input", updatedContent);
        await page.ClickAsync("#save-text-button");
        await Expect(page.Locator("#status-output")).ToContainTextAsync($"saved:{directory}/{fileName}");

        await page.ClickAsync("#load-text-button");
        await Expect(page.Locator("#loaded-output")).ToHaveTextAsync(updatedContent);

        var payloadStore = await BrowserStoragePage.ReadPayloadStoreAsync(page, fixture, $"{directory}/{fileName}");
        payloadStore.ShouldBe("opfs");

        var payloadFileCount = await BrowserStoragePage.ReadOpfsPayloadFileCountAsync(page, fixture, blobKeyPrefix);
        payloadFileCount.ShouldBe(1);

        var indexedDbCount = await BrowserStoragePage.ReadIndexedDbCountAsync(page, fixture, directory);
        indexedDbCount.ShouldBe(1);

        await page.ClickAsync("#delete-button");
        await Expect(page.Locator("#status-output")).ToContainTextAsync("deleted:True");

        await page.ClickAsync("#exists-button");
        await Expect(page.Locator("#exists-output")).ToContainTextAsync("False");
    }

    [Fact]
    [Trait("Category", "LargeFile")]
    [Trait("Category", "BrowserStress")]
    public async Task BrowserStorage_WasmHost_StressFlow_ShouldPersistAcrossPages()
    {
        const long expectedLengthBytes = BrowserLargeFileTestSettings.StressPayloadSizeMiB * BrowserLargeFileTestSettings.BytesPerMiB;

        await using var context = await fixture.CreateContextAsync();
        var firstPage = await context.NewPageAsync();
        var directory = $"wasm-browser-storage-large-{Guid.NewGuid():N}";
        var fileName = $"large-{Guid.NewGuid():N}.bin";

        await BrowserStoragePage.OpenPlaygroundAsync(firstPage);
        await BrowserStoragePage.FillInputsAsync(firstPage, directory, fileName, "ignored");
        await BrowserStoragePage.FillSizeAsync(firstPage, BrowserLargeFileTestSettings.StressPayloadSizeMiB);
        await firstPage.ClickAsync("#save-large-button");
        await Expect(firstPage.Locator("#status-output")).ToContainTextAsync("large-saved:", new() { Timeout = BrowserLargeFileTestSettings.StressTimeoutMs });
        await Expect(firstPage.Locator("#large-output")).ToContainTextAsync($"expected:{expectedLengthBytes}:", new() { Timeout = BrowserLargeFileTestSettings.StressTimeoutMs });

        var expected = await BrowserStoragePage.ReadTextAsync(firstPage, "#large-output");
        expected.StartsWith($"expected:{expectedLengthBytes}:", StringComparison.Ordinal).ShouldBeTrue();
        var payloadStore = await BrowserStoragePage.ReadPayloadStoreAsync(firstPage, fixture, $"{directory}/{fileName}");
        payloadStore.ShouldBe("opfs");

        var secondPage = await context.NewPageAsync();
        await BrowserStoragePage.OpenPlaygroundAsync(secondPage);
        await BrowserStoragePage.FillInputsAsync(secondPage, directory, fileName, "ignored");
        await secondPage.ClickAsync("#exists-button");
        await Expect(secondPage.Locator("#exists-output")).ToContainTextAsync("True");
        var actual = await BrowserStoragePage.ReadPayloadDigestAsync(secondPage, fixture, $"{directory}/{fileName}");
        actual.ShouldNotBeNull();
        actual.StartsWith($"actual:{expectedLengthBytes}:", StringComparison.Ordinal).ShouldBeTrue();
        actual.ShouldBe(expected.Replace("expected:", "actual:", StringComparison.Ordinal));
        var reloadedPayloadStore = await BrowserStoragePage.ReadPayloadStoreAsync(secondPage, fixture, $"{directory}/{fileName}");
        reloadedPayloadStore.ShouldBe("opfs");
        await secondPage.ClickAsync("#delete-button");
        await Expect(secondPage.Locator("#status-output")).ToContainTextAsync("deleted:True");
    }

    [Fact]
    [Trait("Category", "LargeFile")]
    public async Task BrowserStorage_WasmHost_LargeFlow_ShouldPersistAcrossPages()
    {
        const long expectedLengthBytes = BrowserLargeFileTestSettings.DefaultLargePayloadSizeMiB * BrowserLargeFileTestSettings.BytesPerMiB;

        await using var context = await fixture.CreateContextAsync();
        var firstPage = await context.NewPageAsync();
        var directory = $"wasm-browser-storage-large-{Guid.NewGuid():N}";
        var fileName = $"large-{Guid.NewGuid():N}.bin";

        await BrowserStoragePage.OpenPlaygroundAsync(firstPage);
        await BrowserStoragePage.FillInputsAsync(firstPage, directory, fileName, "ignored");
        await BrowserStoragePage.FillSizeAsync(firstPage, BrowserLargeFileTestSettings.DefaultLargePayloadSizeMiB);
        await firstPage.ClickAsync("#save-large-button");
        await Expect(firstPage.Locator("#status-output")).ToContainTextAsync("large-saved:", new() { Timeout = BrowserLargeFileTestSettings.DefaultLargeTimeoutMs });
        await Expect(firstPage.Locator("#large-output")).ToContainTextAsync($"expected:{expectedLengthBytes}:", new() { Timeout = BrowserLargeFileTestSettings.DefaultLargeTimeoutMs });

        var expected = await BrowserStoragePage.ReadTextAsync(firstPage, "#large-output");
        expected.StartsWith($"expected:{expectedLengthBytes}:", StringComparison.Ordinal).ShouldBeTrue();
        var payloadStore = await BrowserStoragePage.ReadPayloadStoreAsync(firstPage, fixture, $"{directory}/{fileName}");
        payloadStore.ShouldBe("opfs");

        var secondPage = await context.NewPageAsync();
        await BrowserStoragePage.OpenPlaygroundAsync(secondPage);
        await BrowserStoragePage.FillInputsAsync(secondPage, directory, fileName, "ignored");
        await BrowserStoragePage.FillSizeAsync(secondPage, BrowserLargeFileTestSettings.DefaultLargePayloadSizeMiB);
        await secondPage.ClickAsync("#load-large-button");
        await Expect(secondPage.Locator("#status-output")).ToContainTextAsync("large-loading", new() { Timeout = 10000 });
        await Expect(secondPage.Locator("#status-output")).ToContainTextAsync("large-loaded:", new() { Timeout = BrowserLargeFileTestSettings.DefaultLargeTimeoutMs });
        await Expect(secondPage.Locator("#large-output")).ToContainTextAsync($"actual:{expectedLengthBytes}:", new() { Timeout = BrowserLargeFileTestSettings.DefaultLargeTimeoutMs });

        var actual = await BrowserStoragePage.ReadTextAsync(secondPage, "#large-output");
        actual.StartsWith($"actual:{expectedLengthBytes}:", StringComparison.Ordinal).ShouldBeTrue();
        actual.ShouldBe(expected.Replace("expected:", "actual:", StringComparison.Ordinal));
        var reloadedPayloadStore = await BrowserStoragePage.ReadPayloadStoreAsync(secondPage, fixture, $"{directory}/{fileName}");
        reloadedPayloadStore.ShouldBe("opfs");
        await secondPage.ClickAsync("#delete-button");
        await Expect(secondPage.Locator("#status-output")).ToContainTextAsync("deleted:True");
    }

    [Fact]
    public async Task BrowserStorage_WasmHost_ConcurrentTabs_ShouldPersistAllFiles()
    {
        await using var context = await fixture.CreateContextAsync();
        var firstPage = await context.NewPageAsync();
        var secondPage = await context.NewPageAsync();
        var verificationPage = await context.NewPageAsync();
        var directory = $"wasm-browser-storage-concurrent-{Guid.NewGuid():N}";
        var firstFileName = $"first-{Guid.NewGuid():N}.txt";
        var secondFileName = $"second-{Guid.NewGuid():N}.txt";
        var firstContent = $"content-{Guid.NewGuid():N}";
        var secondContent = $"content-{Guid.NewGuid():N}";

        await BrowserStoragePage.OpenPlaygroundAsync(firstPage);
        await BrowserStoragePage.OpenPlaygroundAsync(secondPage);
        await BrowserStoragePage.FillInputsAsync(firstPage, directory, firstFileName, firstContent);
        await BrowserStoragePage.FillInputsAsync(secondPage, directory, secondFileName, secondContent);

        await Task.WhenAll(
            firstPage.ClickAsync("#save-text-button"),
            secondPage.ClickAsync("#save-text-button"));

        await Expect(firstPage.Locator("#status-output")).ToContainTextAsync($"saved:{directory}/{firstFileName}");
        await Expect(secondPage.Locator("#status-output")).ToContainTextAsync($"saved:{directory}/{secondFileName}");

        await BrowserStoragePage.OpenPlaygroundAsync(verificationPage);
        await verificationPage.FillAsync("#directory-input", directory);
        await verificationPage.ClickAsync("#list-button");
        await Expect(verificationPage.Locator("#blob-list")).ToContainTextAsync(firstFileName);
        await Expect(verificationPage.Locator("#blob-list")).ToContainTextAsync(secondFileName);

        var indexedDbCount = await BrowserStoragePage.ReadIndexedDbCountAsync(verificationPage, fixture, directory);
        indexedDbCount.ShouldBe(2);
    }
}
