namespace BlazorFeatures.Models;

// .NET 11 Preview 6 — C# unions. The canonical Blazor scenario: a single
// component parameter that uniformly accepts plain text, raw HTML, or a
// templated render fragment.
public union Slot(string, Microsoft.AspNetCore.Components.MarkupString, Microsoft.AspNetCore.Components.RenderFragment);

// A small domain-style union used by the SlotDemo's "card body" examples.
public sealed record CommandResult(int ItemId, string Message);
public sealed record CommandError(string Code, string Message);
public union CommandOutcome(CommandResult, CommandError);
