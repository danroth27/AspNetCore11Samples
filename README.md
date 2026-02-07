# ASP.NET Core .NET 11 Preview 1 Samples

Sample projects demonstrating new ASP.NET Core features in .NET 11 Preview 1.

## Projects

### BlazorFeatures
Blazor Web App demonstrating new Blazor components and features:

- **EnvironmentBoundary** - Conditional rendering based on hosting environment
- **Label Component** - Accessible form labels with `[Display]` attribute support
- **DisplayName Component** - Display property names from metadata attributes
- **QuickGrid OnRowClick** - Row click event handling
- **Navigation Features** - `RelativeToCurrentUri` and `GetUriWithHash()`
- **MathML Support** - Proper MathML namespace in interactive rendering
- **BL0010 Analyzer** - JSInterop best practices analyzer

### ApiFeatures  
Web API demonstrating framework features:

- **FileContentResult in OpenAPI** - Proper binary file schema documentation
- **IOutputCachePolicyProvider** - Custom output cache policy selection

## Running the Samples

Requires .NET 11 Preview 1 SDK.

```bash
# Blazor Features
cd BlazorFeatures/BlazorFeatures
dotnet run

# API Features
cd ApiFeatures
dotnet run
```

## Release Notes Issues

See [RELEASE_NOTES_ISSUES.md](RELEASE_NOTES_ISSUES.md) for documentation issues found during testing.
