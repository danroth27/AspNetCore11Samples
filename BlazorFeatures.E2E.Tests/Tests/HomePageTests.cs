using BlazorFeatures.Components;
using BlazorFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlazorFeatures.E2E.Tests.Tests;

[UITest]
public partial class HomePageTests : BrowserTest
{
    private ServerInstance _server = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        // Acquires (or starts) an instance of BlazorFeatures, hosted behind the
        // shared YARP proxy. The generic argument is the App component type.
        _server = await StartServerAsync<App>(E2ETestAssembly.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task HomePage_HasHelloWorldHeading()
    {
        await _page.GotoAsync(_server.TestUrl);

        await Expect(_page).ToHaveTitleAsync("Home");
        await Expect(_page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
