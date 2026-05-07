using BlazorFeatures.Components;
using BlazorFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class WeatherPageTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;
    private IPage _page = null!;

    public WeatherPageTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _server = await _fixture.StartServerAsync<App>();
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [Fact]
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
