using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Preview 2: OpenAPI 3.2 support
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_2;
});
builder.Services.AddValidation();

// Preview 2: Native OTEL tracing - ASP.NET Core now adds semantic convention tags to HTTP activity by default
// No need for OpenTelemetry.Instrumentation.AspNetCore anymore!
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Microsoft.AspNetCore")
            .AddConsoleExporter();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// OpenAPI now properly documents file download endpoints
app.MapGet("/download/pdf", () =>
{
    var content = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 Sample PDF Content");
    return TypedResults.File(content, "application/pdf", "document.pdf");
})
.WithName("DownloadPdf")
.WithDescription("Downloads a sample PDF file")
.Produces<FileContentResult>(contentType: "application/pdf");

app.MapGet("/download/image", () =>
{
    var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
    return TypedResults.File(pngBytes, "image/png", "image.png");
})
.WithName("DownloadImage")
.Produces<FileContentResult>(contentType: "image/png");

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

// Preview 2: Validation source generator with indexer properties
// JsonElement and Dictionary<string, object> have indexers that previously crashed the validator
app.MapPost("/validate/product", (ProductRequest product) =>
{
    return TypedResults.Ok(new { Message = $"Product '{product.Name}' validated successfully", product });
})
.WithName("ValidateProduct");

// Endpoint using JsonElement as parameter - indexer property should be skipped by validation source gen
app.MapPost("/validate/json", (JsonElement data) =>
{
    return TypedResults.Ok(new { Message = "JsonElement parameter validated without crash", Data = data.ToString() });
})
.WithName("ValidateJson");

// Endpoint with Dictionary parameter - indexer should be skipped
app.MapPost("/validate/metadata", (Dictionary<string, object> metadata) =>
{
    return TypedResults.Ok(new { Message = "Dictionary parameter validated without crash", Count = metadata.Count });
})
.WithName("ValidateMetadata");

// Endpoint demonstrating validation on a type that contains a JsonElement property
app.MapPost("/validate/wrapper", (JsonWrapper wrapper) =>
{
    return TypedResults.Ok(new { Message = $"Wrapper '{wrapper.Name}' validated", HasData = wrapper.Data.ValueKind != JsonValueKind.Undefined });
})
.WithName("ValidateWrapper");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

class ProductRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Range(0.01, 10000)]
    public decimal Price { get; set; }
}

class JsonWrapper
{
    [Required]
    public string Name { get; set; } = "";

    public JsonElement Data { get; set; }
}
