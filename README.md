# ASP.NET Core .NET 11 Preview 1 Samples

Sample projects demonstrating new ASP.NET Core features in .NET 11 Preview 1 for the ASP.NET Community Standup livestream.

## Projects

### BlazorFeatures
Blazor Web App (Interactive Server/WebAssembly) demonstrating new Blazor components and features:

- **EnvironmentBoundary** - Conditional rendering based on hosting environment
- **Label Component** - Accessible form labels with `[Display]` attribute support
- **DisplayName Component** - Display property names from metadata attributes
- **QuickGrid OnRowClick** - Row click event handling
- **Navigation Features** - `RelativeToCurrentUri` and `GetUriWithHash()`
- **MathML Support** - Proper MathML namespace in interactive rendering
- **BL0010 Analyzer** - JSInterop best practices analyzer
- **BasePath Component** - Dynamic base path for Blazor Web Apps
- **SignalR ConfigureConnection** - Connection-level SignalR configuration

### BlazorWasmFeatures
Standalone Blazor WebAssembly app demonstrating WASM-specific features:

- **IHostedService Support** - Background services running in the browser
- **Environment Variables** - Access environment variables via IConfiguration

### ApiFeatures  
Web API demonstrating framework features:

- **FileContentResult in OpenAPI** - Proper binary file schema documentation
- **Output Caching** - Named cache policies with IOutputCachePolicyProvider

## Running the Samples

Requires .NET 11 Preview 1 SDK (11.0.100-preview.1.26104.118).

```bash
# Blazor Web App Features
cd BlazorFeatures/BlazorFeatures
dotnet run

# Blazor WebAssembly Features
cd BlazorWasmFeatures
dotnet run

# API Features
cd ApiFeatures
dotnet run
```

## Release Notes Issues

See [RELEASE_NOTES_ISSUES.md](RELEASE_NOTES_ISSUES.md) for documentation issues found during testing.

### Critical Issues Found:
1. **IOutputCachePolicyProvider** - All code samples use internal APIs (OutputCachePolicyBuilder, BasePolicies, NamedPolicies)
2. **SignalR ConfigureConnection** - Wrong API shown (AddBlazorHub doesn't exist, wrong callback signature)
3. **Docker Template** - Claimed Docker support doesn't exist in the template
