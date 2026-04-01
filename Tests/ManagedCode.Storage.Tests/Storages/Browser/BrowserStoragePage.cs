using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace ManagedCode.Storage.Tests.Storages.Browser;

internal static class BrowserStoragePage
{
    public static async Task OpenPlaygroundAsync(IPage page)
    {
        await page.GotoAsync("/storage-playground");
        await Expect(page.Locator("#save-text-button")).ToBeVisibleAsync();
        await Expect(page.Locator("#status-output")).ToContainTextAsync("interactive-ready");
    }

    public static async Task FillInputsAsync(IPage page, string directory, string fileName, string content)
    {
        await page.FillAsync("#directory-input", directory);
        await page.FillAsync("#file-name-input", fileName);
        await page.FillAsync("#content-input", content);
    }

    public static Task FillSizeAsync(IPage page, int sizeMiB)
    {
        return page.FillAsync("#size-mib-input", sizeMiB.ToString(CultureInfo.InvariantCulture));
    }

    public static async Task FillVfsInputsAsync(IPage page, string directory, string fileName, string movedFileName, string content)
    {
        await page.FillAsync("#vfs-directory-input", directory);
        await page.FillAsync("#vfs-file-name-input", fileName);
        await page.FillAsync("#vfs-moved-file-name-input", movedFileName);
        await page.FillAsync("#vfs-content-input", content);
    }

    public static Task FillVfsSizeAsync(IPage page, int sizeMiB)
    {
        return page.FillAsync("#vfs-size-mib-input", sizeMiB.ToString(CultureInfo.InvariantCulture));
    }

    public static async Task<string> ReadTextAsync(IPage page, string selector)
    {
        return await page.Locator(selector).TextContentAsync() ?? string.Empty;
    }

    public static Task<int> ReadIndexedDbCountAsync(IPage page, BrowserPlaywrightHostFixtureBase fixture, string directory)
    {
        return page.EvaluateAsync<int>(
            @"async ({ databaseName, containerKey, prefix }) => {
                const blobs = await window.ManagedCodeStorageBrowser.listBlobs(databaseName, containerKey, prefix);
                return blobs.length;
            }",
            new
            {
                databaseName = fixture.DatabaseName,
                containerKey = fixture.ContainerKey,
                prefix = $"{fixture.ContainerKey}::{directory}/"
            });
    }

    public static Task<string?> ReadPayloadStoreAsync(IPage page, BrowserPlaywrightHostFixtureBase fixture, string fullName)
    {
        return page.EvaluateAsync<string?>(
            @"async ({ databaseName, fullName }) => {
                return await window.ManagedCodeStorageBrowser.getPayloadStoreByFullName(databaseName, fullName);
            }",
            new
            {
                databaseName = fixture.DatabaseName,
                fullName
            });
    }

    public static Task<string?> ReadPayloadDigestAsync(IPage page, BrowserPlaywrightHostFixtureBase fixture, string fullName)
    {
        return page.EvaluateAsync<string?>(
            @"async ({ databaseName, fullName }) => {
                const digest = await window.ManagedCodeStorageBrowser.getPayloadDigestByFullName(databaseName, fullName);
                return digest ? `actual:${digest.length}:${digest.crc}` : null;
            }",
            new
            {
                databaseName = fixture.DatabaseName,
                fullName
            });
    }

    public static Task<int> ReadOpfsPayloadFileCountAsync(IPage page, BrowserPlaywrightHostFixtureBase fixture, string blobKeyPrefix)
    {
        return page.EvaluateAsync<int>(
            @"async ({ databaseName, blobKeyPrefix }) => {
                try {
                    const root = await navigator.storage.getDirectory();
                    const databaseDirectory = await root.getDirectoryHandle(encodeURIComponent(databaseName), { create: false });
                    let count = 0;

                    for await (const [entryName] of databaseDirectory.entries()) {
                        const decodedName = decodeURIComponent(entryName);
                        if (decodedName.startsWith(blobKeyPrefix)) {
                            count++;
                        }
                    }

                    return count;
                } catch (error) {
                    if (error?.name === ""NotFoundError"") {
                        return 0;
                    }

                    throw error;
                }
            }",
            new
            {
                databaseName = fixture.DatabaseName,
                blobKeyPrefix
            });
    }
}
