namespace BlazorWasmFeatures.Models;

// .NET 11 Preview 6 — C# unions in Blazor WASM (trimming target).
public union ToastMessage(string, Microsoft.AspNetCore.Components.MarkupString, Microsoft.AspNetCore.Components.RenderFragment);

public sealed record Saved(int Id, DateTimeOffset At);
public sealed record ValidationFailed(IReadOnlyList<string> Errors);
public sealed record Conflict(string CurrentETag, string Hint);
public union SaveOutcome(Saved, ValidationFailed, Conflict);
