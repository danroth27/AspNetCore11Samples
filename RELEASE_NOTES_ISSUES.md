# Release Notes Issues and Ambiguities

Issues discovered while validating .NET 11 Preview 1 features against the proposed release notes.

## Issues Found

### 1. QuickGrid OnRowClick - MINOR CLARIFICATION
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

### 2. IOutputCachePolicyProvider - CLARIFICATION NEEDED
**Severity:** Medium  
**Release Notes Claim:**
Shows a custom implementation taking `HttpContext` in the signature.

**Actual Interface:**
```csharp
public interface IOutputCachePolicyProvider
{
    IReadOnlyList<IOutputCachePolicy> GetBasePolicies();
    ValueTask<IOutputCachePolicy?> GetPolicyAsync(string policyName);
}
```

**Notes:**
- The interface does NOT take `HttpContext` - just the policy name
- `OutputCacheOptions.BasePolicies` and `NamedPolicies` are internal
- Custom implementations need to manage their own policy storage

---

## Removed from Release Notes

### InputFile Cancel Event (PR #64772)
Originally included but determined to be a **bug fix**, not a new feature. The PR fixes handling of the browser's native `cancel` event - it now correctly triggers `OnChange` with an empty file list.

---

## Features Verified Working

- [x] EnvironmentBoundary component
- [x] Label component  
- [x] DisplayName component
- [x] QuickGrid OnRowClick (with corrected syntax)
- [x] RelativeToCurrentUri navigation
- [x] GetUriWithHash() extension
- [x] MathML namespace support
- [x] FileContentResult in OpenAPI
- [x] IOutputCachePolicyProvider (interface verified)

## Features Still To Test

- [ ] BasePath component
- [ ] BL0010 analyzer
- [ ] RenderFragment contravariance
- [ ] IComponentPropertyActivator
- [ ] SignalR ConfigureConnection
- [ ] Improved reconnection experience
- [ ] IHostedService in WebAssembly
- [ ] Environment variables in WebAssembly
- [ ] Metrics/tracing in WebAssembly
- [ ] Docker template
- [ ] TimeProvider in Identity
- [ ] Auto-trust dev cert in WSL
