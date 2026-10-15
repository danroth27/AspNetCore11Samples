var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BlazorFeatures>("blazorfeatures", launchProfileName: "https")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = null;
        endpoint.TargetPort = null;
    })
    .WithEndpoint("https", endpoint =>
    {
        endpoint.Port = null;
        endpoint.TargetPort = null;
    })
    .WithHttpHealthCheck("/health")
    .WithEnvironment("OTEL_METRIC_EXPORT_INTERVAL", "1000");

builder.Build().Run();
