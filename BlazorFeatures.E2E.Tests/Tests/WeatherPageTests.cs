using BlazorFeatures.Components;
using BlazorFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlazorFeatures.E2E.Tests.Tests;

[UITest]
public partial class WeatherPageTests : BrowserTest
{
    private ServerInstance _server = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(E2ETestAssembly.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task WeatherPage_StreamRendersForecastTable()
    {
        await _page.GotoAsync($"{_server.TestUrl}/weather");

        // The /weather page is decorated with [StreamRendering] and starts with
        // "Loading..." before streaming the forecast table. Playwright auto-waits
        // for the locator to appear, so this implicitly verifies the streamed
        // update lands without any custom polling.
        var table = _page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(5);
    }
}
