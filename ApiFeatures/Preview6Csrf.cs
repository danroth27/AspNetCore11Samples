// .NET 11 Preview 6 — Automatic cross-origin (CSRF) protection.
//
// Apps built with WebApplication.CreateBuilder now automatically reject unsafe
// cross-origin requests based on the browser's Sec-Fetch-Site and Origin headers
// (dotnet/aspnetcore #66585, #67082). This lightweight CSRF protection is on with
// no configuration and applies across Minimal APIs, MVC, Razor Pages, and Blazor.
// Same-origin requests, user-initiated navigations, and non-browser clients are
// allowed; a cross-origin browser request that tries to consume a form is rejected.
//
// Opt an endpoint out with .DisableAntiforgery() (Minimal APIs). Turn it off
// app-wide with the DisableCsrfProtection config key, or register a custom
// ICsrfProtection for full control of the trust decision.

using Microsoft.AspNetCore.Antiforgery;

namespace ApiFeatures;

public static class Preview6Csrf
{
    public static IEndpointRouteBuilder MapCsrfDemo(this IEndpointRouteBuilder app)
    {
        // A tiny same-origin page that POSTs the form below. Browse to /csrf.
        // The form submit is same-origin, so it succeeds. A cross-site page posting
        // the same form carries Sec-Fetch-Site: cross-site and is rejected.
        app.MapGet("/csrf", () => Results.Content(Page, "text/html")).ExcludeFromDescription();

        // PROTECTED by default. The automatic CSRF middleware inspects
        // Sec-Fetch-Site/Origin and, for an unsafe cross-origin request, marks
        // IAntiforgeryValidationFeature as failed (reading the form would then throw).
        // No token and no configuration are involved. Here we surface that decision
        // as a clean 403.
        app.MapPost("/csrf/transfer", async (HttpContext http) =>
        {
            var antiforgery = http.Features.Get<IAntiforgeryValidationFeature>();
            if (antiforgery is { IsValid: false })
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var form = await http.Request.ReadFormAsync();
            return Results.Ok(new { message = $"Transferred {form["amount"]} to {form["toAccount"]}." });
        })
        .WithName("Transfer")
        .WithDescription("Protected by automatic CSRF: a cross-site browser form POST is rejected with 403.");

        // OPTED OUT with .DisableAntiforgery(). The automatic CSRF middleware honors
        // the metadata and leaves IAntiforgeryValidationFeature valid, so cross-origin
        // form POSTs are allowed — use only for endpoints safe to call cross-site.
        app.MapPost("/csrf/transfer-open", async (HttpContext http) =>
        {
            var form = await http.Request.ReadFormAsync();
            return Results.Ok(new { message = $"(unprotected) Transferred {form["amount"]} to {form["toAccount"]}." });
        })
        .DisableAntiforgery()
        .WithName("TransferOpen")
        .WithDescription("Opted out with .DisableAntiforgery() — cross-site form POSTs are allowed.");

        return app;
    }

    private const string Page = """
        <!doctype html>
        <html>
        <head><title>Automatic CSRF protection</title></head>
        <body style="font-family: system-ui; max-width: 40rem; margin: 2rem auto;">
          <h1>Automatic CSRF protection (.NET 11 Preview 6)</h1>
          <p>This form posts to <code>/csrf/transfer</code> from the same origin, so it succeeds.
             A cross-site page posting the same form is rejected automatically.</p>
          <form method="post" action="/csrf/transfer">
            <label>Amount <input name="amount" value="100" /></label>
            <label>To account <input name="toAccount" value="ACME-123" /></label>
            <button type="submit">Transfer (same-origin, allowed)</button>
          </form>
        </body>
        </html>
        """;
}
