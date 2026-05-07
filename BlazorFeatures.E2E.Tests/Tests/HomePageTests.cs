using BlazorFeatures.Components;
using BlazorFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class HomePageTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;
    private IPage _page = null!;

    public HomePageTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        // Acquires (or starts) an instance of BlazorFeatures, hosted behind the
        // shared YARP proxy. The generic argument is the App component type.
        _server = await _fixture.StartServerAsync<App>();
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [Fact]
    public async Task HomePage_HasHelloWorldHeading()
    {
        await _page.GotoAsync(_server.TestUrl);

        await Expect(_page).ToHaveTitleAsync("Home");
        await Expect(_page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
