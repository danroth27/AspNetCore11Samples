namespace BlazorFeatures.Models;

// .NET 11 Preview 6 — C# unions in Blazor.
//
// Story 1: component library API design.
// Every Blazor component library (MudBlazor, FluentUI, Radzen, …) ships
// label/title/header parameters as TWO parameters today — a string and a
// RenderFragment — because there was no way to accept "text OR a template"
// as a single parameter. With a union, there is.
//
// Note: we deliberately do NOT include a MarkupString (raw HTML) case. A union
// makes it trivial to add one, but exposing raw HTML through a public component
// parameter is an XSS footgun — prefer string (HTML-encoded) and RenderFragment
// (safely composed).
public union ToastMessage(string, Microsoft.AspNetCore.Components.RenderFragment);

// Story 2: modeling component state.
// Async data-loading state is usually modeled with several loose fields
// (bool _loading; T? _data; string? _error) plus an if/else ladder — a shape
// that can represent illegal combinations (loading AND failed, or loaded with
// null data). A closed union makes the state EXACTLY one case, and an
// exhaustive switch renders each: add a fourth case and every switch stops
// compiling until it is handled.
public sealed record Loading;
public sealed record Loaded(IReadOnlyList<string> Items);
public sealed record Failed(string Message);
public union LoadState(Loading, Loaded, Failed);
