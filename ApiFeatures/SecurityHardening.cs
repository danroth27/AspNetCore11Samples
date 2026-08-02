using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;

namespace ApiFeatures;

// Preview 7 hardening changes that are easiest to see as raw HTTP behavior.
// Each of these was verified against the shipped Preview 7 build; the expected
// responses are recorded in the comments so the sample doubles as documentation.
public static class SecurityHardening
{
    public static void MapSecurityHardening(this WebApplication app)
    {
        // #66961 / #67928: Rewrite middleware collapses a leading run of '/' and '\'
        // in a redirect or rewrite target down to a single '/'. Without this, a rule
        // whose target starts with '//' or '/\' produces a scheme-relative Location
        // header that resolves off-origin -- an open redirect.
        //
        //   GET /open-redirect-slashes   -> Location: /attacker.example
        //   GET /open-redirect-backslash -> Location: /attacker.example
        //   GET /open-redirect-many      -> Location: /attacker.example
        //   GET /redirect-absolute       -> Location: https://example.com/ok  (untouched)
        //
        // Absolute URLs are not affected, so intentional off-origin redirects still
        // work as long as they use the full https://host/... form.
        app.UseRewriter(new RewriteOptions()
            .AddRedirect("^open-redirect-slashes$", "//attacker.example")
            .AddRedirect("^open-redirect-backslash$", "/\\attacker.example")
            .AddRedirect("^open-redirect-many$", "///attacker.example")
            .AddRedirect("^redirect-absolute$", "https://example.com/ok"));

        // #67093: PathString.StartsWithSegments now treats '\' as a segment boundary,
        // matching the WHATWG URL Standard and how HttpSys/IIS surface backslashes.
        //
        // Previously '/segment-guard%5Cbar' (which decodes to '/segment-guard\bar')
        // did NOT match this Map branch, so a segment-guarded middleware branch could
        // be bypassed. Now it matches, with PathBase='/segment-guard' and Path='\bar'.
        //
        //   GET /segment-guard          -> matched, remaining ''
        //   GET /segment-guard/bar      -> matched, remaining '/bar'
        //   GET /segment-guard%5Cbar    -> matched, remaining '\bar'   <-- Preview 7
        //   GET /segment-guardbar       -> 404 (not a segment boundary)
        //
        // Request.Path.Value is the decoded path; interpolating the PathString itself
        // would re-escape it and print '%5Cbar' instead of the '\bar' that makes the
        // segment-boundary change visible.
        app.Map("/segment-guard", branch => branch.Run(async context =>
        {
            await context.Response.WriteAsync(
                $"matched. PathBase='{context.Request.PathBase.Value}' Path='{context.Request.Path.Value}'");
        }));

        // #67635: RFC 9110 defines Content-Length as 1*DIGIT, but the underlying UTF-8
        // parser accepted a leading '+' or '-'. Kestrel now rejects those before the
        // request body is read.
        //
        //   curl -X POST .../echo-body -H 'Content-Length: 5'  --data-binary hello -> 200
        //   curl -X POST .../echo-body -H 'Content-Length: +5' --data-binary hello -> 400
        //   curl -X POST .../echo-body -H 'Content-Length: -5' --data-binary hello -> 400
        app.MapPost("/echo-body", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            return "read: " + await reader.ReadToEndAsync();
        });

        // #67460 / #67839: the default CSRF middleware now validates only endpoints
        // that carry IAntiforgeryMetadata { RequiresValidation: true }, matching .NET 10.
        //
        // A plain MapPost has no antiforgery metadata, so a cross-origin POST reaches
        // the handler. Handlers that bind form data get the metadata automatically and
        // are still rejected without a valid token.
        //
        //   POST /csrf/plain -H 'Sec-Fetch-Site: cross-site'            -> 200
        //   POST /csrf/form  -d 'name=x'                                -> 400 (no token)
        //   POST /csrf/form-opted-out -d 'name=x'                       -> 200
        // Antiforgery validation itself still runs in UseAntiforgery(), which this app
        // registers in Program.cs. The Blazor Web App and Razor Pages templates call it
        // for you; a minimal API project has to add it explicitly.
        app.MapPost("/csrf/plain", () => "reached the handler");

        app.MapPost("/csrf/form", ([FromForm] string name) => $"bound form value: {name}");

        app.MapPost("/csrf/form-opted-out", ([FromForm] string name) => $"bound form value: {name}")
            .DisableAntiforgery();
    }
}
