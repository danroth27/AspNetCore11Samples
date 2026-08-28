using BlazorFeatures.Components;
using BlazorFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class ValidationLocalizationTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;
    private IPage _page = null!;

    public ValidationLocalizationTests(ServerFixture<E2ETestAssembly> fixture)
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
    public async Task AsyncValidationDemo_UsesConventionalResourceKeys()
    {
        await _page.GotoAsync($"{_server.TestUrl}/async-validation");
        await _page.WaitForInteractiveAsync("form");

        await _page.Locator("button[type=submit]").ClickAsync();

        await Expect(_page.Locator(".validation-message"))
            .ToContainTextAsync(["Username is required.", "Email is required.", "Age is required."]);
    }
}
