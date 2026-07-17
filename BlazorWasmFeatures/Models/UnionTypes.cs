namespace BlazorWasmFeatures.Models;

// .NET 11 Preview 6 — C# unions in Blazor WASM (trimming target).
// No MarkupString case on purpose: raw HTML in a public parameter is an XSS
// footgun; string (encoded) + RenderFragment (composed) are safe by default.
public union ToastMessage(string, Microsoft.AspNetCore.Components.RenderFragment);

public sealed record Loading;
public sealed record Loaded(IReadOnlyList<string> Items);
public sealed record Failed(string Message);
public union LoadState(Loading, Loaded, Failed);
