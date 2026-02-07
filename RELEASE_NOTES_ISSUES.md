# Release Notes Issues and Ambiguities

Issues discovered while validating .NET 11 Preview 1 features against the proposed release notes.

## Issues Found

### 1. InputFile Cancel Event - INACCURATE
**Severity:** High  
**Release Notes Claim:** 
> The `InputFile` component now supports detecting when file selection is canceled through the new `OnCancel` event callback.

**Actual Behavior:**
- There is NO `OnCancel` parameter on `InputFile`
- The cancel event fires the existing `OnChange` callback with an empty file list (`e.FileCount == 0`)
- Detection is done by checking `e.FileCount == 0` in the `OnChange` handler

**Suggested Fix:**
```markdown
The `InputFile` component now detects when file selection is canceled. When a user opens the file picker 
but dismisses it without selecting any files, the `OnChange` event fires with an empty file list 
(`FileCount == 0`), allowing you to detect and respond to cancellation.
```

---

### 2. QuickGrid OnRowClick - MINOR CLARIFICATION
**Severity:** Low  
**Release Notes Code Sample:**
```csharp
<QuickGrid Items="@people" OnRowClick="@HandleRowClick">
```

**Actual Syntax Required:**
```csharp
<QuickGrid Items="@people.AsQueryable()" OnRowClick="@((Person p) => HandleRowClick(p))">
```

**Notes:**
- Need lambda syntax for the callback
- Items needs to be `IQueryable<T>` not `IEnumerable<T>`

---

### 3. IOutputCachePolicyProvider - CLARIFICATION NEEDED
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

## Features Verified Working

- [x] EnvironmentBoundary component
- [x] Label component  
- [x] DisplayName component
- [x] QuickGrid OnRowClick (with corrected syntax)
- [x] RelativeToCurrentUri navigation
- [x] GetUriWithHash() extension
- [x] MathML namespace support
- [x] InputFile cancel event (with corrected behavior description)
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
