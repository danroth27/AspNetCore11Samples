using BlazorFeatures.Components;
using BlazorFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlazorFeatures.E2E.Tests.Tests;

[UITest]
public partial class LabelInteractiveTests : BrowserTest
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
        Assert.IsNotEmpty(inputs);
        foreach (var input in inputs)
        {
            var id = await input.GetAttributeAsync("id");
            Assert.IsFalse(string.IsNullOrEmpty(id), "Each interactive input should have an id.");

            var matchingLabel = _page.Locator($"label[for='{id}']");
            await Expect(matchingLabel).ToHaveCountAsync(1);
        }
    }
}
