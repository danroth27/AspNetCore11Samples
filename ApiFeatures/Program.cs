using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Preview 2: OpenAPI 3.2 support
builder.Services.AddOpenApi(options =>
{
    // OpenAPI 3.2 also enables the HTTP QUERY method (Preview 3)
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

// Disable throw-on-bad-request so the Preview 4 endpoint-filter sample below
// can observe the framework's 400 status (rather than seeing the dev-exception
// page intercept a thrown BadHttpRequestException).
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o =>
{
    o.ThrowOnBadRequest = false;
});

// Zstd (Zstandard) compression is now a default provider in Preview 3.
// Default priority: zstd > br > gzip
//
// Preview 4 (#55092): Response compression middleware now ALWAYS emits
// Vary: Accept-Encoding when compression is enabled, even when the response
// itself isn't compressed. This prevents shared caches/CDNs from serving
// the wrong encoding to a client that didn't ask for it.
builder.Services.AddResponseCompression();
builder.Services.AddRequestDecompression();

// Preview 5 (#66249): Kestrel applies RequestHeadersTimeout to fragmented HTTP/2 and
// HTTP/3 trailer HEADERS frames. Before P5, only the initial request HEADERS used the
// timeout — a peer that started a trailer block but never finished it could keep a
// stream open until the connection-level timeout fired. The same option now covers both.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
});

// Preview 5 (#66228): Non-body enum parameters keep their C# names in OpenAPI schemas
// even when a JsonStringEnumConverter naming policy is configured here, because the
// framework binds query/route/header/form enums via Enum.TryParse (not JSON).
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRequestDecompression();
app.UseResponseCompression();
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// OpenAPI now properly documents file download endpoints
// Preview 4 (#64562): FileContentHttpResult / FileStreamHttpResult / FileStreamResult
// are described as binary string schemas (`type: string, format: binary`) in
// generated OpenAPI documents when annotated with .Produces<...>().
app.MapGet("/download/pdf", () =>
{
    var content = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 Sample PDF Content");
    return TypedResults.File(content, "application/pdf", "document.pdf");
})
.Produces<FileContentHttpResult>(contentType: "application/pdf")
.WithName("DownloadPdf")
.WithDescription("Returns a FileContentHttpResult — described as { type: string, format: binary } in OpenAPI");

app.MapGet("/download/image", () =>
{
    var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
    return TypedResults.File(pngBytes, "image/png", "image.png");
})
.Produces<FileContentHttpResult>(contentType: "image/png")
.WithName("DownloadImage")
.WithDescription("Returns a FileContentHttpResult");

app.MapGet("/download/stream", () =>
{
    var content = System.Text.Encoding.UTF8.GetBytes("Streamed text content");
    var stream = new MemoryStream(content);
    return TypedResults.Stream(stream, "text/plain", "stream.txt");
})
.Produces<FileStreamHttpResult>(contentType: "text/plain")
.WithName("DownloadStream")
.WithDescription("Returns a FileStreamHttpResult — newly supported in Preview 4 (#64562)");

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

// HTTP QUERY method: like GET but with a request body for complex searches.
// Enabled by OpenAPI 3.2 support added in Preview 2, with QUERY routing in Preview 3.
app.MapMethods("/search", ["QUERY"], (SearchRequest request) =>
{
    // Simulated search results filtered by the request body
    var allProducts = new[]
    {
        new { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999.99 },
        new { Id = 2, Name = "Headphones", Category = "Electronics", Price = 79.99 },
        new { Id = 3, Name = "Coffee Maker", Category = "Kitchen", Price = 49.99 },
        new { Id = 4, Name = "Standing Desk", Category = "Furniture", Price = 599.99 },
        new { Id = 5, Name = "Keyboard", Category = "Electronics", Price = 129.99 },
    };

    var results = allProducts.AsEnumerable();

    if (!string.IsNullOrEmpty(request.Query))
        results = results.Where(p => p.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase));

    if (request.Categories is { Length: > 0 })
        results = results.Where(p => request.Categories.Contains(p.Category, StringComparer.OrdinalIgnoreCase));

    if (request.MaxPrice.HasValue)
        results = results.Where(p => p.Price <= request.MaxPrice.Value);

    return Results.Ok(new { Total = results.Count(), Results = results });
})
.WithName("SearchProducts")
.WithDescription("Search products using an HTTP QUERY request body");

// Large payload endpoint useful for testing zstd compression.
app.MapGet("/large-payload", () =>
{
    var items = Enumerable.Range(1, 1000).Select(i => new
    {
        Id = i,
        Name = $"Item {i}",
        Description = $"This is a detailed description for item {i} that adds enough content to demonstrate compression benefits.",
        Tags = new[] { "tag-a", "tag-b", $"tag-{i % 10}" }
    });
    return Results.Ok(items);
})
.WithName("GetLargePayload")
.WithDescription("Returns a large JSON payload to demonstrate zstd/br/gzip response compression");

// Preview 4 (#64539): Endpoint filters can observe parameter-binding failures.
//
// Without a filter, a TryParse failure on a route value short-circuits with 400
// before the delegate runs. With an endpoint filter (or filter factory) attached,
// the filter pipeline now runs even when binding fails, so the filter can read
// the framework's 400 status code and substitute its own response body.
//
// Try: GET /items/abc                                  -> 400 with the filter-supplied JSON body
//      GET /items/12345678-1234-1234-1234-123456789abc -> 200 with the item
// Note: we accept any string segment in the route ("/items/{id}") and let the
// Guid parameter binder do the parsing, so a non-Guid like "abc" reaches the
// param-check failure path (not a 404 from a routing constraint).
app.MapGet("/items/{id}", (Guid id) => Results.Ok(new { id, name = "demo" }))
    .AddEndpointFilterFactory((_, next) => async ctx =>
    {
        // Run the rest of the pipeline first; the framework will set
        // Response.StatusCode = 400 if binding failed (PR #64539).
        var result = await next(ctx);
        if (ctx.HttpContext.Response.StatusCode == StatusCodes.Status400BadRequest)
        {
            return Results.Json(new
            {
                error = "Parameter binding failed (intercepted by endpoint filter)",
                detail = "Preview 4 (#64539): the filter pipeline now runs even when " +
                         "TryParse-style parameter binding fails. Pass a valid GUID for {id}.",
            }, statusCode: StatusCodes.Status400BadRequest);
        }
        return result;
    })
    .WithName("GetItem")
    .WithDescription("Demonstrates Preview 4: endpoint filters observe parameter-binding failures");

// Preview 5 (#66228): OpenAPI keeps the original C# enum member names for non-body
// enum parameters (query/route/header/form), even when ConfigureHttpJsonOptions
// installs a JsonStringEnumConverter naming policy. Body-bound enums still follow
// the JSON policy; non-body binding uses Enum.TryParse, so the schema now matches
// what the framework will actually accept.
//
// Preview 5 (#66583): Array schema component reference IDs now use valid names
// like "stringArray" and "TodoArray" instead of names containing array syntax.
app.MapGet("/orders/{status}", (OrderStatus status) => Results.Ok(new { status }))
    .WithName("GetOrdersByStatus")
    .WithDescription("Route-bound enum — schema uses C# names like PendingReview");

app.MapGet("/orders-by-query", (OrderStatus status) => Results.Ok(new { status }))
    .WithName("GetOrdersByQuery")
    .WithDescription("Query-bound enum — schema uses C# names regardless of JSON naming policy");

app.MapPost("/orders", (Order order) => Results.Ok(order))
    .WithName("CreateOrder")
    .WithDescription("Body-bound enum — schema follows the JSON naming policy");

app.MapGet("/tags", () => TypedResults.Ok<string[]>(new[] { "blazor", "kestrel", "openapi" }))
    .WithName("GetTags")
    .WithDescription("Returns string[] — array schema reference uses stringArray");

app.MapGet("/todos", () => TypedResults.Ok<Todo[]>(new[]
    {
        new Todo(1, "Try Preview 5", false),
        new Todo(2, "Read release notes", true),
    }))
    .WithName("GetTodos")
    .WithDescription("Returns Todo[] — array schema reference uses TodoArray");

app.Run();

record Order(int Id, string Customer, OrderStatus Status);
record Todo(int Id, string Title, bool Done);

enum OrderStatus
{
    Pending,
    PendingReview,
    Shipped,
    Delivered,
}

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

record SearchRequest(string? Query, string[]? Categories, double? MaxPrice);
