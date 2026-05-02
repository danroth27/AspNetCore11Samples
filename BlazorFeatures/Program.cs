using BlazorFeatures;
using BlazorFeatures.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;

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

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
