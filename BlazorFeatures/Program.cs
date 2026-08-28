using System.Globalization;
using BlazorFeatures;
using BlazorFeatures.Components;
using BlazorFeatures.Data;
using BlazorFeatures.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Preview 7: CacheView (#65772, #67776) caches rendered SSR output. It uses a bounded
// in-memory store by default, but automatically upgrades to HybridCache when one is
// registered in DI, giving a two-tier local/distributed cache with no other code change.
// (RazorComponentsServiceOptions.CacheViewHybridCache can point at a specific instance.)
builder.Services.AddHybridCache();

// Register the CircuitHandler used by /circuit-pause to capture a
// Circuit reference for Circuit.RequestCircuitPauseAsync (Preview 4 #66265).
builder.Services.AddScoped<CircuitTrackingHandler>();
builder.Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<CircuitTrackingHandler>());

// Preview 5: validation infrastructure backing /client-validation and /async-validation.
// AddValidation discovers [ValidatableType] models so DataAnnotationsValidator routes
// through the new pipeline.
//
// Preview 7 (PR #68005) streamlined validation localization: the separate
// Microsoft.Extensions.Validation.Localization package and its
// AddValidationLocalization<T>() / IValidationLocalizer API are gone. Localization now
// turns on automatically as soon as an IStringLocalizerFactory is in DI (AddLocalization
// below), and the resource lookup is emitted by the validation source generator.
// By default keys resolve against each model's own resources; LocalizerProvider points
// every model at the shared ValidationMessages.resx files in /Resources instead.
builder.Services.AddLocalization();
builder.Services.AddValidation(options =>
{
    options.LocalizerProvider = (_, factory) => factory.Create(typeof(ValidationMessages));
});
builder.Services.AddSingleton<UserService>();

// Preview 5: [SupplyParameterFromSession] (PR #65184) reads and writes ISession on
// SSR component properties. It requires the standard session services + middleware.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Request localization (en/es) for the validation demos. Culture is selected via
// the /Culture/Set endpoint below and persisted in a cookie.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("es-ES") };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRequestLocalization();
app.UseSession();
// No app.UseAntiforgery() call is needed. .NET 11 automatically rejects unsafe
// cross-origin requests based on the browser's Sec-Fetch-Site/Origin headers
// (dotnet/aspnetcore #66585), which protects the SSR forms in this app even
// without the token-based antiforgery middleware.

// Culture switcher target for the validation demos. Writes the cookie that the
// CookieRequestCultureProvider above reads on subsequent requests.
app.MapGet("/Culture/Set", (HttpContext context, string culture, string redirectUri) =>
{
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

    return Results.LocalRedirect(redirectUri);
});

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // Configure client-side startup behavior from the server in C# instead of
    // hand-writing Blazor.start JavaScript (dotnet/aspnetcore #67337). The server
    // serializes these into the page and the Blazor script applies them in the browser.
    .WithBrowserOptions(options =>
    {
        // Client-side log level — visible in the browser dev console.
        options.LogLevel = LogLevel.Information;

        // Interactive Server reconnection behavior (observe by stopping/restarting
        // the server and watching the reconnection UI).
        options.InteractiveServer.ReconnectionMaxRetries = 10;
        options.InteractiveServer.ReconnectionRetryInterval = TimeSpan.FromSeconds(1.5);

        // Preserve the DOM across enhanced navigations.
        options.StaticServer.PreserveDom = true;

        // Preview 7 (#67098): pause the circuit automatically once the tab has been
        // hidden for HiddenDelay, releasing the SignalR connection and server memory
        // until the user returns. Ships in the Microsoft.AspNetCore.Components.Server.AutoPause
        // package. 10s here so the behavior is easy to observe; the default is 2 minutes.
        options.AddAutoPause(pause =>
        {
            pause.Enabled = true;
            pause.HiddenDelay = TimeSpan.FromSeconds(10);
        });
    });

app.Run();
