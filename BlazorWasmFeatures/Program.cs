using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;
using BlazorWasmFeatures;
using BlazorWasmFeatures.Services;

// Populate some environment variables to test the config integration
Environment.SetEnvironmentVariable("API_ENDPOINT", "https://api.example.com/");
Environment.SetEnvironmentVariable("ENABLE_FEATURE_X", "true");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register background IHostedService service that runs when the app starts
builder.Services.AddSingleton<DataRefreshService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DataRefreshService>());

await builder.Build().RunAsync();
