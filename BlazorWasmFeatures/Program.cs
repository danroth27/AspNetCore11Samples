using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWasmFeatures;
using BlazorWasmFeatures.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// =============================================================================
// FEATURE: IHostedService support in Blazor WebAssembly
// Register background services that run when the app starts
// =============================================================================
builder.Services.AddHostedService<DataRefreshService>();
builder.Services.AddSingleton<DataRefreshService>(sp => 
    sp.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
      .OfType<DataRefreshService>()
      .First());

// =============================================================================
// FEATURE: Environment variables in Blazor WebAssembly configuration
// Environment variables are now accessible via IConfiguration
// =============================================================================
var apiEndpoint = builder.Configuration["API_ENDPOINT"] ?? "Not configured";
var featureFlag = builder.Configuration["ENABLE_FEATURE_X"] ?? "false";
Console.WriteLine($"[Config] API_ENDPOINT: {apiEndpoint}");
Console.WriteLine($"[Config] ENABLE_FEATURE_X: {featureFlag}");

await builder.Build().RunAsync();
