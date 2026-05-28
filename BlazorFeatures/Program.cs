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

// Required for [SupplyParameterFromTempData] in Blazor SSR (Preview 4 #65306).
// TempData uses cookie-based storage by default; the controller services
// register the ITempDataProvider that the new attribute reads from.
builder.Services.AddControllers();

// Register the CircuitHandler used by /circuit-pause to capture a
// Circuit reference for Circuit.RequestCircuitPauseAsync (Preview 4 #66265).
builder.Services.AddScoped<CircuitTrackingHandler>();
builder.Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<CircuitTrackingHandler>());

// Preview 5: validation infrastructure backing /client-validation and /async-validation.
// AddValidation discovers [ValidatableType] models so DataAnnotationsValidator routes
// through the new pipeline. AddValidationLocalization<T> (PR #66646) hooks an
// IValidationLocalizer that resolves display names and error messages against the
// IStringLocalizer<ValidationMessages> resx files in /Resources.
builder.Services.AddLocalization();
builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>();
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
app.UseAntiforgery();

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
    .AddInteractiveServerRenderMode();

app.Run();
