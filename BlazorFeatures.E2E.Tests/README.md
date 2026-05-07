# BlazorFeatures.E2E.Tests

A sample showing how to use the new **`Microsoft.AspNetCore.Components.Testing`**
library (.NET 11 Preview 4 — [aspnetcore #65958][pr]) to write end-to-end
tests for a Blazor Web App.

The library combines:

- **xUnit v3** collection fixtures (`ServerFixture<T>`)
- A **YARP** reverse proxy that fronts one or more app processes
- **Playwright** with helpers like `WaitForInteractiveAsync` /
  `WaitForBlazorAsync` / `WaitForEnhancedNavigationAsync` so tests don't need
  the usual poll-and-pray retry loops
- A source generator + analyzer that injects test-only service overrides
  without modifying the app under test

[pr]: https://github.com/dotnet/aspnetcore/pull/65958

## Tests in this sample

| Test | What it exercises |
| ---- | ----------------- |
| `HomePageTests.HomePage_HasHelloWorldHeading` | Static SSR — verifies the landing page title and `<h1>` content. |
| `WeatherPageTests.WeatherPage_StreamRendersForecastTable` | `[StreamRendering]` — verifies the streamed forecast table appears with five rows. Playwright's auto-wait removes the need for explicit polling. |
| `LabelInteractiveTests.LabelInteractiveDemo_GeneratesMatchingIdAndForAttributes` | Interactive Server — uses `WaitForInteractiveAsync` to wait for the SignalR circuit before asserting that every `<input>` has an `id` matching its `<label for="…">`. |

## Running

```pwsh
# 1) Restore + build (this generates BlazorFeatures.E2E.Tests.e2e-manifest.json
#    next to the test DLL — the fixture reads it at runtime to know how to
#    launch the BlazorFeatures app).
dotnet build

# 2) Install Playwright browsers (one-time, per machine).
pwsh bin/Debug/net11.0/playwright.ps1 install

# 3) Run the tests.
dotnet run -c Debug --project BlazorFeatures.E2E.Tests
```

By default the fixture launches `BlazorFeatures` via `dotnet run`. Set
`E2EAppMode=publish` (or `all`) at build time to publish the app first for
faster cold-start:

```pwsh
dotnet build -p:E2EAppMode=publish
```

## Preview 4 packaging workarounds

The `BlazorFeatures.E2E.Tests.csproj` includes two notes-worthy workarounds for
the Preview 4 build of `Microsoft.AspNetCore.Components.Testing`:

1. The package only ships `buildTransitive/net10.0/` props/targets, so net11.0
   projects pick them up via NuGet's TFM compatibility rules — no extra
   `Import` is needed once the project targets net11.0.
2. The targets file probes for the MSBuild task assembly at
   `$(MSBuildThisFileDirectory)tasks\netstandard2.0\…`, but the package
   actually places `tasks/` at the package root (two levels up from
   `buildTransitive/net10.0/`). The csproj sets `_E2ETasksAssembly` explicitly
   to point at the real location. Once the package fixes its layout, that
   override can be removed.
