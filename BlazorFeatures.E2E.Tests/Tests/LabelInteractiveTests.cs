using BlazorFeatures.Components;
using BlazorFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class LabelInteractiveTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;
    private IPage _page = null!;

    public LabelInteractiveTests(ServerFixture<E2ETestAssembly> fixture)
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
    public async Task LabelInteractiveDemo_GeneratesMatchingIdAndForAttributes()
    {
        await _page.GotoAsync($"{_server.TestUrl}/label-interactive-demo");

        // The page uses @rendermode InteractiveServer. WaitForInteractiveAsync
        // blocks until Blazor has attached the SignalR circuit and the form is
        // truly interactive — replacing the usual poll-and-pray retry loops.
        await _page.WaitForInteractiveAsync("input.form-control");

        // Every <Label For="..."> must produce a `for` attribute that matches
        // the generated `id` on its bound <input>. This is the Preview 2 fix
        // covered by the demo page.
        var inputs = await _page.Locator("input.form-control").AllAsync();
        Assert.NotEmpty(inputs);
        foreach (var input in inputs)
        {
            var id = await input.GetAttributeAsync("id");
            Assert.False(string.IsNullOrEmpty(id), "Each interactive input should have an id.");

            var matchingLabel = _page.Locator($"label[for='{id}']");
            await Expect(matchingLabel).ToHaveCountAsync(1);
        }
    }
}
