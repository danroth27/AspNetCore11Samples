var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Backend weather API consumed by the standalone Blazor WebAssembly app.
//
// NOTE: There is intentionally NO CORS configuration here. In development the
// Blazor Gateway hosts the WASM app and proxies "/api/*" requests to this
// service via YARP, so the browser only ever makes same-origin requests to the
// gateway. The cross-origin hop (gateway -> backend) happens server-side and is
// not subject to the browser's same-origin policy.

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/weather", () =>
{
    var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast(
        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        Random.Shared.Next(-20, 55),
        summaries[Random.Shared.Next(summaries.Length)]))
        .ToArray();

    return forecasts;
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
