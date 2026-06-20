# ASP.NET Core .NET 11 Samples

Sample projects demonstrating new ASP.NET Core features across the .NET 11 preview
releases, built for the ASP.NET Community Standup livestream.

## Projects

### BlazorFeatures
Blazor Web App (Interactive Server/WebAssembly) demonstrating new Blazor components
and features. Demos are grouped by the preview that introduced them.

**Preview 1**
- **EnvironmentBoundary** (`/environment-boundary`) — Conditional rendering based on hosting environment
- **Label Component** (`/label-demo`) — Accessible form labels with `[Display]` attribute support
- **DisplayName Component** (`/displayname-demo`) — Display property names from metadata attributes
- **QuickGrid OnRowClick** (`/quickgrid-onrowclick`) — Row click event handling
- **Navigation Features** (`/navigation-features/overview`) — `RelativeToCurrentUri` and `GetUriWithHash()`
- **MathML Support** (`/mathml-demo`) — Proper MathML namespace in interactive rendering
- **InvokeVoidAsync() Analyzer** (`/invoke-void-async-analyzer`) — JSInterop best-practices analyzer

**Preview 2**
- **TempData for Blazor SSR** (`/tempdata-demo`) — `TempData` flash messages and multi-page flows
- **Label ID (Interactive)** (`/label-interactive-demo`) — `Label` generates matching `id`/`for` in interactive mode

**Preview 3**
- **Variable-Height Virtualize** (`/virtualize-demo`) — `Virtualize` with variable-height items

**Preview 4**
- **SupplyParameterFromTempData** (`/tempdata`) — `[SupplyParameterFromTempData]` one-shot flash messages
- **Virtualize AnchorMode** (`/virtualize-anchor`) — `AnchorMode` + `ItemComparer` keep the viewport stable
- **Circuit Pause** (`/circuit-pause`) — Server-initiated circuit pause/resume

**Preview 5**
- **Client-side Validation** (`/client-validation`) — Validation that runs on the client
- **Async Validation** (`/async-validation`) — Async form validation with localized messages
- **QuickGrid SSR** (`/quickgrid-ssr`) — QuickGrid in statically rendered pages
- **Session Parameter** (`/session-parameter`) — `[SupplyParameterFromSession]`

**Preview 6**
- **C# Unions in Blazor** (`/unions-demo`) — `union Slot(string, MarkupString, RenderFragment)`,
  `EventCallback<TUnion>`, `CascadingValue<TUnion>`, and `DynamicComponent` with a boxed-union
  parameter. Requires `<LangVersion>preview</LangVersion>` and `<EnablePreviewFeatures>true</EnablePreviewFeatures>`.
  Note: the Razor literal-attribute shortcut (`Content="hello"`) does **not** compile against a
  union-typed parameter — use the expression form `Content="@("hello")"`. Tracked at
  [dotnet/razor#13188](https://github.com/dotnet/razor/issues/13188).
  See the design note: [aspnet/specs#782](https://github.com/aspnet/specs/pull/782).

### BlazorFeatures.E2E.Tests
End-to-end tests for the BlazorFeatures app using the new
`Microsoft.AspNetCore.Components.Testing` library (Preview 4), which combines xUnit v3
collection fixtures, a YARP reverse proxy, and Playwright with Blazor-aware wait helpers.
See [BlazorFeatures.E2E.Tests/README.md](BlazorFeatures.E2E.Tests/README.md) for details.

### BlazorWasmFeatures
Standalone Blazor WebAssembly app demonstrating WASM-specific features:

- **IHostedService Support** — Background services running in the browser
- **Environment Variables** — Access environment variables via `IConfiguration`
- **Web Worker** (`/web-worker`) — Offload CPU-intensive work to a Web Worker running a
  separate .NET runtime (uses the `WebWorkerDemo` library)
- **C# Unions (Preview 6)** (`/unions-demo`) — Verified to work end-to-end in a published,
  trimmed WASM build (default ILLink trimming): Slot rendering of all three cases,
  `EventCallback<CommandOutcome>`, and `DynamicComponent` with a boxed union parameter.

### WebWorkerDemo
Reusable Razor class library that wires up a `[JSExport]`/`[JSImport]` Web Worker host so
Blazor WebAssembly apps can run .NET work off the UI thread. Consumed by `BlazorWasmFeatures`.

### ApiFeatures
Web API (minimal APIs) demonstrating framework features:

- **OpenAPI 3.2** (Preview 2) — `OpenApiVersion = OpenApi3_2`
- **Validation source generator** (Preview 2) — Handles `JsonElement`/`Dictionary` indexer properties
- **Native OpenTelemetry tracing** (Preview 2) — Framework emits HTTP semantic-convention tags by default
- **Zstandard response compression** (Preview 3) — zstd as a default provider (`zstd > br > gzip`)
- **HTTP QUERY method** (Preview 3) — GET-like requests with a body for complex searches (`QUERY /search`)
- **FileContentResult/FileStreamResult in OpenAPI** (Preview 4) — Documented as `{ type: string, format: binary }`
- **Endpoint filters observe binding failures** (Preview 4) — Filter pipeline runs even when parameter binding fails
- **Enum parameter naming in OpenAPI** (Preview 5) — Non-body enum params keep their C# names; array schema IDs use valid names
- **Kestrel trailer header timeout** (Preview 5) — `RequestHeadersTimeout` applies to HTTP/2 and HTTP/3 trailer frames

## Running the Samples

Requires the .NET 11 Preview 6 SDK (`11.0.100-preview.6.26315.102`) or later.

```pwsh
dotnet build

# Run a project, e.g. the Blazor Web App:
dotnet run --project BlazorFeatures
```

All packages are published on nuget.org, so no extra NuGet feeds are required.
