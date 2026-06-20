namespace BlazorFeatures.Models;

// .NET 11 Preview 6 — C# unions in Blazor.
//
// Story 1: component library API design.
// Every Blazor component library (MudBlazor, FluentUI, Radzen, …) ships
// label/title/header parameters as TWO parameters today — a string and a
// RenderFragment — because there was no way to accept "text OR markup OR
// a template" as a single parameter. With a union, there is.
public union ToastMessage(string, Microsoft.AspNetCore.Components.MarkupString, Microsoft.AspNetCore.Components.RenderFragment);

// Story 2: outcome types for callbacks.
// A Save can succeed, can fail validation, or can hit an optimistic-concurrency
// conflict. Today components expose three separate EventCallbacks and callers
// forget to wire OnConflict, so conflicts silently look like successes. A union
// outcome plus an exhaustive switch on the consumer side eliminates that bug.
public sealed record Saved(int Id, DateTimeOffset At);
public sealed record ValidationFailed(IReadOnlyList<string> Errors);
public sealed record Conflict(string CurrentETag, string Hint);
public union SaveOutcome(Saved, ValidationFailed, Conflict);
