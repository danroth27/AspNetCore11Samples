namespace BlazorWasmFeatures.Models;

// .NET 11 Preview 6 — C# unions, trimming-test target for Blazor WebAssembly.
public union Slot(string, Microsoft.AspNetCore.Components.MarkupString, Microsoft.AspNetCore.Components.RenderFragment);

public sealed record CommandResult(int ItemId, string Message);
public sealed record CommandError(string Code, string Message);
public union CommandOutcome(CommandResult, CommandError);
