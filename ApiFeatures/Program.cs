using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// =============================================================================
// FEATURE: IOutputCachePolicyProvider
// The new IOutputCachePolicyProvider interface allows customizing how output 
// cache policies are resolved. The default implementation reads from 
// OutputCacheOptions, but you can replace it for dynamic policy selection.
// =============================================================================
builder.Services.AddOutputCache(options =>
{
    // Add named policies
    options.AddPolicy("ShortCache", policy => policy.Expire(TimeSpan.FromSeconds(10)));
    options.AddPolicy("LongCache", policy => policy.Expire(TimeSpan.FromMinutes(5)));
    options.AddPolicy("NoCache", policy => policy.NoCache());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseOutputCache();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// =============================================================================
// FEATURE: FileContentResult in OpenAPI
// OpenAPI now properly documents file download endpoints
// =============================================================================
app.MapGet("/download/pdf", () =>
{
    var content = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 Sample PDF Content");
    return Results.File(content, "application/pdf", "document.pdf");
})
.WithName("DownloadPdf")
.WithDescription("Downloads a sample PDF file")
.Produces<FileContentResult>(StatusCodes.Status200OK, "application/pdf");

app.MapGet("/download/image", () =>
{
    var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
    return Results.File(pngBytes, "image/png", "image.png");
})
.WithName("DownloadImage")
.Produces<FileContentResult>(StatusCodes.Status200OK, "image/png");

// =============================================================================
// Output Cache Examples - demonstrating named policies
// =============================================================================
app.MapGet("/cached/short", () =>
{
    return new { Time = DateTime.Now, Message = "Cached for 10 seconds", Policy = "ShortCache" };
})
.WithName("CachedShort")
.CacheOutput("ShortCache");

app.MapGet("/cached/long", () =>
{
    return new { Time = DateTime.Now, Message = "Cached for 5 minutes", Policy = "LongCache" };
})
.WithName("CachedLong")
.CacheOutput("LongCache");

app.MapGet("/cached/none", () =>
{
    return new { Time = DateTime.Now, Message = "Not cached", Policy = "NoCache" };
})
.WithName("NotCached")
.CacheOutput("NoCache");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// =============================================================================
// IOutputCachePolicyProvider Interface (new in .NET 11)
// =============================================================================
// The new IOutputCachePolicyProvider interface has two methods:
//
// public interface IOutputCachePolicyProvider
// {
//     IReadOnlyList<IOutputCachePolicy> GetBasePolicies();
//     ValueTask<IOutputCachePolicy?> GetPolicyAsync(string policyName);
// }
//
// The latest sample in PR #10237 (TenantOutputCachePolicyProvider) is now CORRECT.
// See TenantOutputCachePolicyProvider.cs for a working implementation that:
// - Returns empty from GetBasePolicies()
// - Loads policies from a custom tenant service
// - Uses a custom IOutputCachePolicy implementation
//
// Note: OutputCacheOptions.BasePolicies and NamedPolicies are still internal,
// but the updated sample doesn't rely on them.
// =============================================================================
