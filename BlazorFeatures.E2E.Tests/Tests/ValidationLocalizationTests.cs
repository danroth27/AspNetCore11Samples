using BlazorFeatures.Components;
using BlazorFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlazorFeatures.E2E.Tests.Tests;

[UITest]
public partial class ValidationLocalizationTests : BrowserTest
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
    public async Task AsyncValidationDemo_UsesConventionalResourceKeys()
    {
        await _page.GotoAsync($"{_server.TestUrl}/async-validation");
        await _page.WaitForInteractiveAsync("form");

        await _page.Locator("button[type=submit]").ClickAsync();

        await Expect(_page.Locator(".validation-message"))
            .ToContainTextAsync(["Username is required.", "Email is required.", "Age is required."]);
    }
}
