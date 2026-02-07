# Release Notes Issues and Ambiguities

Issues discovered while validating .NET 11 Preview 1 features against the proposed release notes (PR #10237).

## Resolved Issues ✅

### 1. IOutputCachePolicyProvider - ✅ NOW CORRECT
**Severity:** Resolved  
**PR Reference:** PR #10237

The latest sample (`TenantOutputCachePolicyProvider`) now correctly shows how to implement `IOutputCachePolicyProvider` without relying on internal APIs. It:
- Returns an empty list from `GetBasePolicies()` 
- Implements custom policy loading from a tenant service
- Uses a custom `IOutputCachePolicy` implementation

The sample compiles and runs correctly ✅

---

### 2. SignalR ConfigureConnection - ✅ NOW CORRECT
**Severity:** Resolved  
**PR Reference:** PR #10237

The SignalR `ConfigureConnection` documentation has been updated and now shows the correct API:
```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(options =>
    {
        options.ConfigureConnection = dispatcherOptions =>
        {
            dispatcherOptions.CloseOnAuthenticationExpiration = true;
            dispatcherOptions.AllowStatefulReconnects = true;
            dispatcherOptions.ApplicationMaxBufferSize = 1024 * 1024;
        };
    });
```

This correctly uses `AddInteractiveServerRenderMode` with `ServerComponentsEndpointOptions` and the `HttpConnectionDispatcherOptions` callback.

---

### 3. Docker Template - Visual Studio Only
**Severity:** Clarification needed  
**PR Reference:** PR #10237

**Release Notes Claim:** "The Blazor WebAssembly project template now includes Docker support out of the box."

**Clarification:** The Docker option is available in Visual Studio's project creation wizard, not via `dotnet new` CLI. This is expected behavior but could be clarified in the release notes.

---

## Minor Issues

### 4. QuickGrid OnRowClick - Syntax Clarification Needed
**Severity:** Low  

**Release Notes Code Sample:**
```csharp
<QuickGrid Items="@people" OnRowClick="@HandleRowClick">
```

**Working Alternatives:**
```csharp
// Option 1: Explicit TGridItem (allows method group syntax)
<QuickGrid TGridItem="Person" Items="@people.AsQueryable()" OnRowClick="@HandleRowClick">

// Option 2: Lambda syntax (no explicit TGridItem needed)
<QuickGrid Items="@people.AsQueryable()" OnRowClick="@((Person p) => HandleRowClick(p))">
```

**Notes:**
- Items needs to be `IQueryable<T>` (use `.AsQueryable()` on lists)
- Either specify `TGridItem` explicitly OR use lambda syntax for type inference

---

## Removed from Release Notes

### InputFile Cancel Event (PR #64772)
Bug fix, not a new feature.

### RenderFragment contravariance
Bug fix, not a new feature.

---

## Features Verified Working ✅

- [x] EnvironmentBoundary component
- [x] Label component  
- [x] DisplayName component
- [x] QuickGrid OnRowClick (with corrected syntax)
- [x] RelativeToCurrentUri navigation
- [x] GetUriWithHash() extension
- [x] MathML namespace support
- [x] BasePath component (already used in BlazorFeatures App.razor)
- [x] BL0010 analyzer
- [x] FileContentResult in OpenAPI
- [x] IHostedService in Blazor WebAssembly
- [x] Environment variables in Blazor WebAssembly configuration

## Features Not Fully Tested

- [ ] IComponentPropertyActivator (advanced scenario, interface exists)
- [ ] Improved reconnection experience (UX improvement, no API to test)
- [ ] Metrics/tracing in WebAssembly (requires OpenTelemetry setup)
- [ ] TimeProvider in Identity (requires Identity setup)
- [ ] Auto-trust dev cert in WSL (requires WSL environment)
