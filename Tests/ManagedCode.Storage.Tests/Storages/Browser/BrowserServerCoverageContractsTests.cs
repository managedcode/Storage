using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ManagedCode.Storage.Tests.Storages.Browser;

[Collection(BrowserIntegrationCollection.Server)]
public sealed class BrowserServerCoverageContractsTests(BrowserServerHostFixture fixture)
{
    [Fact]
    public async Task RegistrationsDownloadsRangesAndLegalHold_UseActualBrowserState()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/coverage-playground");
        await Expect(page.Locator("#run-contracts")).ToBeVisibleAsync();
        await page.ClickAsync("#run-contracts");
        await Expect(page.Locator("#contract-status")).ToHaveTextAsync("all-contracts-passed", new() { Timeout = 60000 });
    }
}
